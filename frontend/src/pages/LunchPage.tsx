import { useCallback, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useBookingStore } from '../store/bookingStore'
import { useRestaurantSignalR } from '../hooks/useSignalR'
import type { LunchBooking, LunchBox, Restaurant, RestaurantAvailabilityUpdate, RestaurantBookingResponse, RestaurantMessageEvent } from '../types'

const API = import.meta.env.VITE_API_URL ?? ''

async function readJson<T>(response: Response): Promise<T> {
  const body = await response.json()
  if (!response.ok) throw Object.assign(new Error(body.error ?? 'Operazione non riuscita'), { details: body })
  return body as T
}

export default function LunchPage() {
  const { selectedDate, setLunchBooking } = useBookingStore()
  const queryClient = useQueryClient()
  const [mode, setMode] = useState<'restaurants' | 'lunchbox'>('restaurants')
  const [confirmation, setConfirmation] = useState<RestaurantBookingResponse | null>(null)

  const restaurantsQuery = useQuery<Restaurant[]>({
    queryKey: ['restaurants', selectedDate],
    queryFn: () => fetch(`${API}/api/lunch/restaurants?date=${selectedDate}`).then(response => readJson<Restaurant[]>(response)),
    refetchInterval: import.meta.env.DEV ? 3_000 : false,
  })
  const messagesQuery = useQuery<RestaurantMessageEvent[]>({
    queryKey: ['restaurant-messages'],
    queryFn: () => fetch(`${API}/api/lunch/partner-messages?take=12`).then(response => readJson<RestaurantMessageEvent[]>(response)),
    refetchInterval: import.meta.env.DEV ? 3_000 : false,
  })
  const lunchBoxesQuery = useQuery<LunchBox[]>({
    queryKey: ['lunchboxes'],
    queryFn: () => fetch(`${API}/api/lunch/lunchboxes`).then(response => readJson<LunchBox[]>(response)),
    enabled: mode === 'lunchbox',
  })

  const refreshRestaurants = useCallback((update?: RestaurantAvailabilityUpdate) => {
    if (update) queryClient.setQueryData<Restaurant[]>(['restaurants', selectedDate], current =>
      current?.map(restaurant => restaurant.restaurantId === update.restaurantId ? { ...restaurant, ...update } : restaurant))
    else void queryClient.invalidateQueries({ queryKey: ['restaurants', selectedDate] })
  }, [queryClient, selectedDate])
  const refreshMessages = useCallback(() => { void queryClient.invalidateQueries({ queryKey: ['restaurant-messages'] }) }, [queryClient])
  useRestaurantSignalR(selectedDate, refreshRestaurants, refreshMessages)

  const bookingMutation = useMutation({
    mutationFn: async (payload: { restaurantId?: string; isLunchBox: boolean; lunchBoxId?: string }) => {
      const response = await fetch(`${API}/api/lunch/bookings`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ ...payload, bookingDate: selectedDate }),
      })
      return payload.isLunchBox ? readJson<LunchBooking>(response) : readJson<RestaurantBookingResponse>(response)
    },
    onSuccess: result => {
      if ('booking' in result) { setLunchBooking(result.booking); setConfirmation(result) }
      else setLunchBooking(result)
      refreshRestaurants()
    },
  })
  const tickMutation = useMutation({
    mutationFn: () => fetch(`${API}/api/lunch/demo/tick?date=${selectedDate}`, { method: 'POST' }).then(response => readJson(response)),
    onSuccess: () => { refreshRestaurants(); refreshMessages() },
  })

  const restaurants = restaurantsQuery.data ?? []
  const messages = messagesQuery.data ?? []

  return (
    <div className="space-y-5 pb-10">
      <header className="rounded-2xl bg-slate-950 px-5 py-5 text-white shadow-sm">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.2em] text-emerald-300">Servizio pranzo · {selectedDate}</p>
            <h1 className="mt-2 text-2xl font-semibold tracking-tight">Posti comunicati dai locali</h1>
            <p className="mt-1 max-w-md text-sm leading-5 text-slate-300">Ogni contatore riflette l’ultimo messaggio ricevuto. La conferma del locale aggiorna subito i posti.</p>
          </div>
          <span className="mt-1 inline-flex items-center gap-2 rounded-full border border-emerald-400/30 bg-emerald-400/10 px-3 py-1.5 text-xs text-emerald-200">
            <span className="h-2 w-2 animate-pulse rounded-full bg-emerald-400 motion-reduce:animate-none" />Live
          </span>
        </div>
      </header>

      <div className="flex gap-2" role="tablist" aria-label="Tipo di pranzo">
        <button role="tab" aria-selected={mode === 'restaurants'} onClick={() => setMode('restaurants')}
          className={`rounded-full px-4 py-2 text-sm font-semibold transition ${mode === 'restaurants' ? 'bg-primary text-white' : 'border border-border bg-white text-text-muted'}`}>Locali</button>
        <button role="tab" aria-selected={mode === 'lunchbox'} onClick={() => setMode('lunchbox')}
          className={`rounded-full px-4 py-2 text-sm font-semibold transition ${mode === 'lunchbox' ? 'bg-primary text-white' : 'border border-border bg-white text-text-muted'}`}>Lunch box</button>
        {import.meta.env.DEV && <button onClick={() => tickMutation.mutate()} disabled={tickMutation.isPending}
          className="ml-auto rounded-full border border-dashed border-emerald-600 px-4 py-2 text-sm font-semibold text-emerald-700 hover:bg-emerald-50 disabled:opacity-50">Ricevi aggiornamenti demo</button>}
      </div>

      {bookingMutation.error && <div role="alert" className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-medium text-red-800">{bookingMutation.error.message}</div>}
      {confirmation && <div role="status" className="rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-4 text-emerald-950">
        <p className="text-xs font-bold uppercase tracking-wider text-emerald-700">Conferma locale · {confirmation.partnerCode}</p>
        <p className="mt-1 font-semibold">Prenotazione confermata. Restano {confirmation.availableSeats} posti.</p>
        <p className="mt-1 text-xs text-emerald-700">Riferimento {confirmation.partnerReference}</p>
      </div>}

      {mode === 'restaurants' && <div className="space-y-3">
        {restaurantsQuery.isLoading && <p className="text-sm text-text-muted">Caricamento disponibilità…</p>}
        {restaurants.map(restaurant => {
          const isAvailable = restaurant.availableSeats > 0
          return <article key={restaurant.restaurantId} className="overflow-hidden rounded-2xl border border-border bg-white shadow-sm">
            <div className="grid grid-cols-[1fr_auto] items-stretch">
              <div className="p-4">
                <div className="flex items-center gap-2"><h2 className="font-semibold text-text">{restaurant.name}</h2>
                  <span className="rounded-full bg-slate-100 px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide text-slate-600">Telegram demo</span></div>
                <p className="mt-2 text-xs text-text-muted">Aggiornato {restaurant.updatedAtUtc === '0001-01-01T00:00:00' ? 'mai' : new Date(restaurant.updatedAtUtc).toLocaleTimeString('it-IT', { hour: '2-digit', minute: '2-digit', second: '2-digit' })} · seq. {restaurant.sequence}</p>
                <button disabled={!isAvailable || bookingMutation.isPending} onClick={() => bookingMutation.mutate({ restaurantId: restaurant.restaurantId, isLunchBox: false })}
                  className="mt-4 rounded-lg bg-primary px-4 py-2 text-sm font-semibold text-white hover:bg-primary-dark focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2 disabled:cursor-not-allowed disabled:bg-slate-300">
                  {isAvailable ? 'Prenota 1 posto' : 'Locale completo'}
                </button>
              </div>
              <div className={`flex min-w-28 flex-col items-center justify-center border-l px-4 text-center ${isAvailable ? 'border-emerald-100 bg-emerald-50' : 'border-red-100 bg-red-50'}`}>
                <strong className={`text-4xl font-bold tabular-nums ${isAvailable ? 'text-emerald-700' : 'text-red-700'}`}>{restaurant.availableSeats}</strong>
                <span className="mt-1 text-[11px] font-bold uppercase tracking-wider text-text-muted">posti liberi</span>
              </div>
            </div>
          </article>
        })}
      </div>}

      {mode === 'lunchbox' && <div className="grid gap-3">{(lunchBoxesQuery.data ?? []).map(box =>
        <article key={box.boxId} className="flex items-center justify-between gap-4 rounded-xl border border-border bg-white p-4">
          <div><h2 className="font-semibold">{box.name}</h2><p className="mt-1 text-sm text-text-muted">{box.description}</p></div>
          <button onClick={() => bookingMutation.mutate({ isLunchBox: true, lunchBoxId: box.boxId })} className="rounded-lg bg-primary px-4 py-2 text-sm font-semibold text-white">Ordina</button>
        </article>)}</div>}

      <section aria-labelledby="partner-feed" className="rounded-2xl border border-border bg-white p-4">
        <div className="flex items-center justify-between"><h2 id="partner-feed" className="font-semibold">Messaggi ricevuti</h2><span className="text-xs text-text-muted">ultimi eventi</span></div>
        <ol className="mt-3 divide-y divide-border">
          {messages.length === 0 && <li className="py-3 text-sm text-text-muted">Il prossimo aggiornamento comparirà qui.</li>}
          {messages.slice(0, 6).map((message, index) => <li key={`${message.restaurantId}-${message.sequence}-${index}`} className="flex items-center gap-3 py-2.5 text-sm">
            <span className="h-2 w-2 rounded-full bg-emerald-500" /><span className="font-medium">{message.restaurantId}</span>
            <span className="text-text-muted">disponibilità aggiornata · seq. {message.sequence}</span>
            <time className="ml-auto text-xs text-text-muted">{new Date(message.receivedAtUtc).toLocaleTimeString('it-IT', { hour: '2-digit', minute: '2-digit' })}</time>
          </li>)}
        </ol>
      </section>
    </div>
  )
}
