import { useCallback, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { AppIcon } from '../components/AppIcon'
import { DateStrip } from '../components/DateStrip'
import { useBookingStore } from '../store/bookingStore'
import { useRestaurantSignalR } from '../hooks/useSignalR'
import type { LunchBooking, LunchBox, Restaurant, RestaurantAvailabilityUpdate, RestaurantBookingResponse, RestaurantMessageEvent } from '../types'

const API = import.meta.env.VITE_API_URL ?? ''
async function readJson<T>(response: Response): Promise<T> { const body = await response.json(); if (!response.ok) throw new Error(body.error ?? 'Operazione non riuscita'); return body as T }

export default function LunchPage() {
  const { selectedDate, setLunchBooking } = useBookingStore()
  const queryClient = useQueryClient()
  const [mode, setMode] = useState<'restaurants' | 'lunchbox'>('restaurants')
  const [confirmation, setConfirmation] = useState<RestaurantBookingResponse | null>(null)
  const restaurantsQuery = useQuery<Restaurant[]>({ queryKey: ['restaurants', selectedDate], queryFn: () => fetch(`${API}/api/lunch/restaurants?date=${selectedDate}`).then(readJson<Restaurant[]>), refetchInterval: import.meta.env.DEV ? 3_000 : false })
  const messagesQuery = useQuery<RestaurantMessageEvent[]>({ queryKey: ['restaurant-messages'], queryFn: () => fetch(`${API}/api/lunch/partner-messages?take=12`).then(readJson<RestaurantMessageEvent[]>), refetchInterval: import.meta.env.DEV ? 3_000 : false })
  const lunchBoxesQuery = useQuery<LunchBox[]>({ queryKey: ['lunchboxes'], queryFn: () => fetch(`${API}/api/lunch/lunchboxes`).then(readJson<LunchBox[]>), enabled: mode === 'lunchbox' })
  const refreshRestaurants = useCallback((update?: RestaurantAvailabilityUpdate) => {
    if (update) queryClient.setQueryData<Restaurant[]>(['restaurants', selectedDate], current => current?.map(restaurant => restaurant.restaurantId === update.restaurantId ? { ...restaurant, ...update } : restaurant))
    else void queryClient.invalidateQueries({ queryKey: ['restaurants', selectedDate] })
  }, [queryClient, selectedDate])
  const refreshMessages = useCallback(() => { void queryClient.invalidateQueries({ queryKey: ['restaurant-messages'] }) }, [queryClient])
  useRestaurantSignalR(selectedDate, refreshRestaurants, refreshMessages)
  const booking = useMutation({ mutationFn: async (payload: { restaurantId?: string; isLunchBox: boolean; lunchBoxId?: string }) => {
    const response = await fetch(`${API}/api/lunch/bookings`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ ...payload, bookingDate: selectedDate }) })
    return payload.isLunchBox ? readJson<LunchBooking>(response) : readJson<RestaurantBookingResponse>(response)
  }, onSuccess: result => { if ('booking' in result) { setLunchBooking(result.booking); setConfirmation(result) } else setLunchBooking(result); refreshRestaurants() } })
  const tick = useMutation({ mutationFn: () => fetch(`${API}/api/lunch/demo/tick?date=${selectedDate}`, { method: 'POST' }).then(readJson<unknown>), onSuccess: () => { refreshRestaurants(); refreshMessages() } })
  const restaurants = restaurantsQuery.data ?? []
  const messages = messagesQuery.data ?? []

  return <div>
    <div className="page-mobile-heading"><h1>Pranzo</h1><p>Prenota un locale o scegli un lunch box</p></div>
    <div className="mb-5 lg:hidden"><DateStrip /></div>
    <div className="mb-[18px] flex flex-wrap gap-2.5">
      <Tab active={mode === 'restaurants'} onClick={() => setMode('restaurants')} label="Locali convenzionati" />
      <Tab active={mode === 'lunchbox'} onClick={() => setMode('lunchbox')} label="Lunch Box" />
      {import.meta.env.DEV && <button onClick={() => tick.mutate()} disabled={tick.isPending} className="ml-auto flex items-center gap-1.5 rounded-[13px] border-[1.5px] border-dashed border-[#EC6A4D] bg-white px-3 py-2.5 text-xs font-bold text-[#C0563C]"><AppIcon name="sync" />Aggiornamento demo</button>}
    </div>
    {booking.error && <div role="alert" className="spotly-alert mb-4 border border-[#F3C9BC] bg-[#FBE7E1] text-[#A8432C]">{booking.error.message}</div>}
    {confirmation && <div role="status" className="spotly-alert mb-4 flex items-center gap-3 border border-[#B8DCC7] bg-[#E7F3EC] text-[#266E49]"><AppIcon name="check_circle" filled className="text-[23px]" /><div><strong className="block text-sm">Prenotazione confermata</strong><span className="text-xs">Restano {confirmation.availableSeats} posti · Rif. {confirmation.partnerReference}</span></div></div>}

    {mode === 'restaurants' && <>
      {restaurantsQuery.isLoading && <p className="text-sm text-text-muted">Caricamento locali…</p>}
      <div className="grid gap-4 md:grid-cols-2">{restaurants.map(restaurant => {
        const ratio = Math.max(0, Math.min(100, restaurant.availableSeats / restaurant.capacity * 100)); const available = restaurant.availableSeats > 0
        return <button key={restaurant.restaurantId} disabled={!available || booking.isPending} onClick={() => booking.mutate({ restaurantId: restaurant.restaurantId, isLunchBox: false })}
          className="spotly-card cursor-pointer p-[18px] text-left transition hover:-translate-y-0.5 hover:shadow-md disabled:cursor-not-allowed disabled:opacity-60">
          <div className="flex items-start justify-between gap-3"><div><h2 className="m-0 text-base font-bold">{restaurant.name}</h2><p className="mt-1 mb-0 text-[12px] text-text-muted">Convenzionato · Telegram demo</p></div>
            <span className={`rounded-lg px-2.5 py-1 text-[10px] font-extrabold ${available ? 'bg-[#E7F3EC] text-[#2F8A5C]' : 'bg-[#FCEDE7] text-[#C0563C]'}`}>{available ? 'DISPONIBILE' : 'COMPLETO'}</span></div>
          <div className="mt-4 flex items-center gap-2.5"><div className="h-[7px] flex-1 overflow-hidden rounded-full bg-[#EFE9DF]"><div className="h-full rounded-full bg-[#2F8A5C]" style={{ width: `${ratio}%` }} /></div><strong className="whitespace-nowrap text-xs text-[#2F8A5C]">{restaurant.availableSeats} posti</strong></div>
          <div className="mt-3 flex items-center gap-1.5 text-[12px] font-semibold text-[#2F8A5C]"><AppIcon name="redeem" className="text-[18px]" />Buono pasto accettato</div>
        </button>
      })}</div>
    </>}
    {mode === 'lunchbox' && <>
      <div className="mb-4 flex items-center gap-2.5 rounded-[14px] bg-[#FCEDE7] p-3.5 text-[13px] text-[#9A4B36]"><AppIcon name="lunch_dining" className="text-[21px] text-[#EC6A4D]" /><span>Locali al completo? Il <strong>Lunch Box</strong> arriva direttamente in ufficio.</span></div>
      <div className="grid gap-4 md:grid-cols-3">{(lunchBoxesQuery.data ?? []).map(box => <button key={box.boxId} onClick={() => booking.mutate({ isLunchBox: true, lunchBoxId: box.boxId })} className="spotly-card cursor-pointer p-[18px] text-left transition hover:-translate-y-0.5 hover:shadow-md">
        <div className="grid h-[52px] w-[52px] place-items-center rounded-[15px] bg-[#FCEDE7] text-[#EC6A4D]"><AppIcon name="lunch_dining" filled className="text-[28px]" /></div><h2 className="mt-3.5 mb-1 text-[15px] font-bold">{box.name}</h2><p className="m-0 text-[12px] leading-5 text-[#726A60]">{box.description}</p>{box.allergens && <p className="mt-1 mb-0 text-[11px] text-[#A89E92]">Allergeni: {box.allergens}</p>}
      </button>)}</div>
    </>}
    <section className="spotly-card mt-5 p-4" aria-labelledby="message-feed"><div className="flex items-center justify-between"><h2 id="message-feed" className="m-0 text-sm font-extrabold">Aggiornamenti locali</h2><span className="text-[11px] text-text-muted">in tempo reale</span></div>
      <ol className="mt-2 mb-0 list-none p-0">{messages.length === 0 && <li className="py-3 text-xs text-text-muted">Il prossimo aggiornamento comparirà qui.</li>}{messages.slice(0, 4).map((message, index) => <li key={`${message.restaurantId}-${message.sequence}-${index}`} className="flex items-center gap-2.5 border-b border-border py-2.5 text-xs last:border-0"><i className="h-2 w-2 rounded-full bg-success" /><strong>{message.restaurantId}</strong><span className="text-text-muted">posti aggiornati · seq. {message.sequence}</span><time className="ml-auto text-text-muted">{new Date(message.receivedAtUtc).toLocaleTimeString('it-IT', { hour: '2-digit', minute: '2-digit' })}</time></li>)}</ol>
    </section>
  </div>
}

function Tab({ active, onClick, label }: { active: boolean; onClick: () => void; label: string }) { return <button onClick={onClick} aria-pressed={active} className={`rounded-[13px] border-[1.5px] px-5 py-2.5 text-sm font-bold ${active ? 'border-[#2B2622] bg-[#2B2622] text-white' : 'border-border bg-white text-[#726A60]'}`}>{label}</button> }
