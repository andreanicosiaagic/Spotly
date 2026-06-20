import { useCallback, useEffect, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { AppIcon } from '../components/AppIcon'
import { DateStrip } from '../components/DateStrip'
import { deleteJson, getJson, postJson } from '../lib/api'
import { formatDateLabel } from '../lib/date'
import { useRestaurantSignalR } from '../hooks/useSignalR'
import { useBookingStore } from '../store/bookingStore'
import { useAuth } from '../hooks/useAuth'
import { useLunchBooking } from '../hooks/use-bookings'
import type {
  LunchBooking,
  LunchBox,
  LunchBoxEligibility,
  Restaurant,
  RestaurantAvailabilityUpdate,
  RestaurantBookingResponse,
  RestaurantMessageEvent,
  RestaurantSlot,
  MenuItem,
} from '../types'

export default function LunchPage() {
  const { selectedDate } = useBookingStore()
  const { isFacility } = useAuth()
  const queryClient = useQueryClient()
  const [mode, setMode] = useState<'restaurants' | 'lunchbox'>('restaurants')
  const [selectedRestaurantId, setSelectedRestaurantId] = useState<string | null>(null)
  const [selectedSlotId, setSelectedSlotId] = useState<string | null>(null)
  const [selectedMenuItemIds, setSelectedMenuItemIds] = useState<string[]>([])
  const [confirmation, setConfirmation] = useState<RestaurantBookingResponse | null>(null)

  const bookingQuery = useLunchBooking(selectedDate)
  const restaurantsQuery = useQuery({
    queryKey: ['restaurants', selectedDate],
    queryFn: () => getJson<Restaurant[]>(`/api/lunch/restaurants?date=${selectedDate}`),
  })
  const messagesQuery = useQuery({
    queryKey: ['restaurant-messages', selectedDate],
    queryFn: () => getJson<RestaurantMessageEvent[]>(`/api/lunch/partner-messages?date=${selectedDate}&take=12`),
  })
  const lunchBoxesQuery = useQuery({
    queryKey: ['lunchboxes'],
    queryFn: () => getJson<LunchBox[]>('/api/lunch/lunchboxes'),
    enabled: mode === 'lunchbox',
  })
  const lunchBoxEligibilityQuery = useQuery({
    queryKey: ['lunchbox-eligibility', selectedDate],
    queryFn: () => getJson<LunchBoxEligibility>(`/api/lunch/lunchbox-eligibility?date=${selectedDate}`),
  })
  const slotsQuery = useQuery({
    queryKey: ['restaurant-slots', selectedDate, selectedRestaurantId],
    queryFn: () => getJson<RestaurantSlot[]>(`/api/lunch/slots?date=${selectedDate}&restaurantId=${selectedRestaurantId}`),
    enabled: mode === 'restaurants' && Boolean(selectedRestaurantId),
  })
  const menuQuery = useQuery({
    queryKey: ['restaurant-menu', selectedDate, selectedRestaurantId],
    queryFn: () => getJson<MenuItem[]>(`/api/lunch/menu?date=${selectedDate}&restaurantId=${selectedRestaurantId}`),
    enabled: mode === 'restaurants' && Boolean(selectedRestaurantId),
  })

  useEffect(() => {
    setSelectedRestaurantId(null)
    setSelectedSlotId(null)
    setSelectedMenuItemIds([])
    setConfirmation(null)
  }, [selectedDate])

  const restaurants = restaurantsQuery.data ?? []
  const selectedRestaurant = restaurants.find((restaurant) => restaurant.restaurantId === selectedRestaurantId) ?? null
  const slotOptions = slotsQuery.data ?? []
  const menuItems = menuQuery.data ?? []
  const realtimeStatus = useRestaurantSignalR(
    selectedDate,
    useCallback((update: RestaurantAvailabilityUpdate) => {
      queryClient.setQueryData<Restaurant[]>(['restaurants', selectedDate], (current) =>
        current?.map((restaurant) => {
          if (restaurant.restaurantId !== update.restaurantId) return restaurant
          return update.sequence >= restaurant.sequence ? { ...restaurant, ...update } : restaurant
        }) ?? [],
      )
    }, [queryClient, selectedDate]),
    useCallback((event: RestaurantMessageEvent) => {
      queryClient.setQueryData<RestaurantMessageEvent[]>(['restaurant-messages', selectedDate], (current) => {
        const next = [event, ...(current ?? []).filter((item) => !(item.restaurantId === event.restaurantId && item.sequence === event.sequence))]
        return next.slice(0, 12)
      })
    }, [queryClient, selectedDate]),
  )

  const booking = useMutation({
    mutationFn: async () => {
      if (!selectedRestaurantId || !selectedSlotId) throw new Error('Seleziona un locale e una fascia oraria')
      if (selectedMenuItemIds.length === 0) throw new Error('Seleziona almeno un piatto')
      return postJson<RestaurantBookingResponse>('/api/lunch/bookings', {
        restaurantId: selectedRestaurantId,
        slotId: selectedSlotId,
        menuItemIds: selectedMenuItemIds,
        isLunchBox: false,
        bookingDate: selectedDate,
      })
    },
    onSuccess: (result) => {
      setConfirmation(result)
      void queryClient.invalidateQueries({ queryKey: ['lunch-booking', selectedDate] })
      void queryClient.invalidateQueries({ queryKey: ['restaurants', selectedDate] })
      void queryClient.invalidateQueries({ queryKey: ['restaurant-slots', selectedDate, selectedRestaurantId] })
      void queryClient.invalidateQueries({ queryKey: ['restaurant-messages', selectedDate] })
    },
  })

  const lunchBoxBooking = useMutation({
    mutationFn: async (lunchBoxId: string) => postJson<LunchBooking>('/api/lunch/bookings', {
      lunchBoxId,
      isLunchBox: true,
      bookingDate: selectedDate,
    }),
    onSuccess: () => {
      setConfirmation(null)
      void queryClient.invalidateQueries({ queryKey: ['lunch-booking', selectedDate] })
      void queryClient.invalidateQueries({ queryKey: ['lunchbox-eligibility', selectedDate] })
    },
  })

  const cancellation = useMutation({
    mutationFn: async (bookingId: string) => deleteJson<{ status: string }>(`/api/lunch/bookings/${bookingId}`),
    onSuccess: () => {
      setConfirmation(null)
      void queryClient.invalidateQueries({ queryKey: ['lunch-booking', selectedDate] })
      void queryClient.invalidateQueries({ queryKey: ['restaurants', selectedDate] })
      void queryClient.invalidateQueries({ queryKey: ['restaurant-slots', selectedDate, selectedRestaurantId] })
      void queryClient.invalidateQueries({ queryKey: ['restaurant-messages', selectedDate] })
    },
  })

  const tick = useMutation({
    mutationFn: () => postJson<{ applied: number }>(`/api/lunch/demo/tick?date=${selectedDate}`),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['restaurants', selectedDate] })
      void queryClient.invalidateQueries({ queryKey: ['restaurant-messages', selectedDate] })
    },
  })

  const menuByCategory = useMemo(() => {
    return menuItems.reduce<Record<string, MenuItem[]>>((groups, item) => {
      groups[item.category] = [...(groups[item.category] ?? []), item]
      return groups
    }, {})
  }, [menuItems])

  return <div>
    <div className="page-mobile-heading">
      <h1>Pranzo</h1>
      <p>{formatDateLabel(selectedDate, { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })} · ristorante o lunch box</p>
    </div>
    <div className="mb-5 lg:hidden"><DateStrip /></div>

    <div className="mb-[18px] flex flex-wrap gap-2.5">
      <Tab active={mode === 'restaurants'} onClick={() => setMode('restaurants')} label="Locali convenzionati" />
      <Tab active={mode === 'lunchbox'} onClick={() => setMode('lunchbox')} label="Lunch Box" />
      {isFacility() && <button onClick={() => tick.mutate()} disabled={tick.isPending} className="ml-auto flex items-center gap-1.5 rounded-[13px] border-[1.5px] border-dashed border-[var(--c-ec6a4d)] bg-surface px-3 py-2.5 text-xs font-bold text-[var(--c-c0563c)]"><AppIcon name="sync" />Aggiornamento demo</button>}
    </div>

    <div className="mb-4 flex items-center justify-between rounded-[16px] border border-border bg-surface px-4 py-3 text-xs">
      <span className="font-semibold text-[var(--c-5c544a)]">Realtime {realtimeStatus === 'connected' ? 'attivo' : realtimeStatus === 'reconnecting' ? 'in riconnessione' : 'offline'}</span>
      <span className="text-text-muted">{selectedDate}</span>
    </div>

    {(booking.error || lunchBoxBooking.error || cancellation.error) && <div role="alert" className="spotly-alert mb-4 border border-[var(--c-f3c9bc)] bg-[var(--c-fbe7e1)] text-[var(--c-a8432c)]">
      {booking.error?.message ?? lunchBoxBooking.error?.message ?? cancellation.error?.message}
    </div>}

    {confirmation && <div role="status" className="spotly-alert mb-4 flex items-center gap-3 border border-[var(--c-b8dcc7)] bg-[var(--c-e7f3ec)] text-[var(--c-266e49)]">
      <AppIcon name="check_circle" className="text-[23px]" />
      <div>
        <strong className="block text-sm">Prenotazione confermata</strong>
        <span className="text-xs">Restano {confirmation.availableSeats} posti · Rif. {confirmation.partnerReference}</span>
      </div>
    </div>}

    {bookingQuery.data && <section className="spotly-card mb-5 p-4">
      <div className="flex items-start justify-between gap-4">
        <div>
          <p className="m-0 text-[11px] font-extrabold uppercase tracking-[.08em] text-[var(--c-a89e92)]">Prenotazione attiva</p>
          <h2 className="mt-1 mb-1 text-base font-bold">{bookingQuery.data.isLunchBox ? 'Lunch Box' : `Ristorante ${bookingQuery.data.restaurantId}`}</h2>
          <p className="m-0 text-sm text-text-muted">{bookingQuery.data.slotId ? `Slot ${bookingQuery.data.slotId}` : 'Consegna in ufficio'} · {formatDateLabel(bookingQuery.data.bookingDate, { day: 'numeric', month: 'long', year: 'numeric' })}</p>
        </div>
        <button onClick={() => cancellation.mutate(bookingQuery.data!.bookingId)} disabled={cancellation.isPending} className="rounded-[12px] border border-[var(--c-f3c9bc)] bg-[var(--c-fff7f2)] px-3 py-2 text-xs font-bold text-[var(--c-c0563c)]">Annulla</button>
      </div>
    </section>}

    {mode === 'restaurants' && <>
      {restaurantsQuery.isLoading && <p className="text-sm text-text-muted">Caricamento locali…</p>}
      <div className="grid gap-4 md:grid-cols-2">
        {restaurants.map((restaurant) => {
          const available = restaurant.availableSeats > 0
          const selected = restaurant.restaurantId === selectedRestaurantId
          const ratio = Math.max(0, Math.min(100, restaurant.availableSeats / restaurant.capacity * 100))

          return <button
            key={restaurant.restaurantId}
            onClick={() => {
              setSelectedRestaurantId(restaurant.restaurantId)
              setSelectedSlotId(null)
              setSelectedMenuItemIds([])
            }}
            className={`spotly-card cursor-pointer p-[18px] text-left transition hover:-translate-y-0.5 hover:shadow-md ${selected ? 'ring-2 ring-[var(--c-2b2622)]' : ''}`}>
            <div className="flex items-start justify-between gap-3">
              <div>
                <h2 className="m-0 text-base font-bold">{restaurant.name}</h2>
                <p className="mt-1 mb-0 text-[12px] text-text-muted">Telegram demo · seq {restaurant.sequence}</p>
              </div>
              <span className={`rounded-lg px-2.5 py-1 text-[10px] font-extrabold ${available ? 'bg-[var(--c-e7f3ec)] text-[var(--c-2f8a5c)]' : 'bg-[var(--c-fcede7)] text-[var(--c-c0563c)]'}`}>{available ? 'DISPONIBILE' : 'COMPLETO'}</span>
            </div>
            <div className="mt-4 flex items-center gap-2.5">
              <div className="h-[7px] flex-1 overflow-hidden rounded-full bg-[var(--c-efe9df)]"><div className="h-full rounded-full bg-[var(--c-2f8a5c)]" style={{ width: `${ratio}%` }} /></div>
              <strong className="whitespace-nowrap text-xs text-[var(--c-2f8a5c)]">{restaurant.availableSeats} posti</strong>
            </div>
            <div className="mt-3 flex items-center gap-1.5 text-[12px] font-semibold text-[var(--c-2f8a5c)]"><AppIcon name="redeem" className="text-[18px]" />Buono pasto accettato</div>
          </button>
        })}
      </div>

      {selectedRestaurant && <section className="spotly-card mt-5 p-5">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="m-0 text-[11px] font-extrabold uppercase tracking-[.08em] text-[var(--c-a89e92)]">Dettaglio prenotazione</p>
            <h2 className="mt-1 mb-1 text-lg font-bold">{selectedRestaurant.name}</h2>
            <p className="m-0 text-sm text-text-muted">Seleziona fascia oraria e almeno un piatto per completare la prenotazione.</p>
          </div>
          <span className="rounded-lg bg-[var(--c-f2ede4)] px-3 py-1 text-[11px] font-extrabold text-[var(--c-6f6659)]">{selectedRestaurant.availableSeats} posti residui</span>
        </div>

        <div className="mt-5 grid gap-5 lg:grid-cols-[280px_1fr]">
          <div>
            <h3 className="mt-0 mb-3 text-sm font-extrabold">Slot disponibili</h3>
            <div className="space-y-2">
              {slotOptions.map((slot) => <button key={slot.slotId} onClick={() => setSelectedSlotId(slot.slotId)} disabled={slot.available <= 0}
                className={`flex w-full items-center justify-between rounded-[14px] border px-3 py-3 text-sm font-semibold ${selectedSlotId === slot.slotId ? 'border-[var(--c-2b2622)] bg-[var(--c-2b2622)] text-white' : 'border-border bg-surface text-[var(--c-5c544a)]'} disabled:cursor-not-allowed disabled:opacity-50`}>
                <span>{slot.slotTime}</span><span>{slot.available}/{slot.capacity}</span>
              </button>)}
            </div>
          </div>

          <div>
            <h3 className="mt-0 mb-3 text-sm font-extrabold">Menu del giorno</h3>
            <div className="grid gap-4 md:grid-cols-2">
              {Object.entries(menuByCategory).map(([category, items]) => <div key={category} className="rounded-[16px] border border-border bg-[var(--c-fcfbf8)] p-4">
                <h4 className="mt-0 mb-3 text-xs font-extrabold uppercase tracking-[.08em] text-[var(--c-a89e92)]">{category}</h4>
                <div className="space-y-2.5">
                  {items.map((item) => <label key={item.itemId} className="flex cursor-pointer items-start gap-3 text-sm text-[var(--c-5c544a)]">
                    <input type="checkbox" checked={selectedMenuItemIds.includes(item.itemId)}
                      onChange={(event) => setSelectedMenuItemIds((current) => event.target.checked ? [...current, item.itemId] : current.filter((value) => value !== item.itemId))}
                      className="mt-0.5 h-4 w-4 rounded border-border accent-[var(--c-2b2622)]" />
                    <span><strong className="block">{item.name}</strong>{item.allergens && <span className="text-xs text-text-muted">Allergeni: {item.allergens}</span>}</span>
                  </label>)}
                </div>
              </div>)}
            </div>

            <div className="mt-5 flex flex-wrap items-center gap-3">
              <button
                onClick={() => booking.mutate()}
                disabled={booking.isPending || bookingQuery.data !== null || selectedRestaurant.availableSeats <= 0 || !selectedSlotId || selectedMenuItemIds.length === 0}
                className="rounded-[14px] border-0 bg-[var(--c-2b2622)] px-5 py-3 text-sm font-bold text-white disabled:cursor-not-allowed disabled:bg-[var(--c-c8c2b7)]">
                {booking.isPending ? 'Invio al locale…' : 'Conferma prenotazione'}
              </button>
              <span className="text-xs text-text-muted">{selectedSlotId ? `Slot selezionato: ${selectedSlotId}` : 'Seleziona uno slot'} · {selectedMenuItemIds.length} piatti scelti</span>
            </div>
          </div>
        </div>
      </section>}
    </>}

    {mode === 'lunchbox' && <>
      {lunchBoxEligibilityQuery.data && <div className={`mb-4 flex items-center gap-2.5 rounded-[14px] p-3.5 text-[13px] ${lunchBoxEligibilityQuery.data.eligible ? 'bg-[var(--c-e7f3ec)] text-[var(--c-266e49)]' : 'bg-[var(--c-fcede7)] text-[var(--c-9a4b36)]'}`}>
        <AppIcon name="lunch_dining" className="text-[21px]" />
        <span>{lunchBoxEligibilityQuery.data.reason}{!lunchBoxEligibilityQuery.data.eligible && ` · scenario demo pronto dal ${lunchBoxEligibilityQuery.data.demoDate}`}</span>
      </div>}
      <div className="grid gap-4 md:grid-cols-3">
        {(lunchBoxesQuery.data ?? []).map((box) => <button key={box.boxId} onClick={() => lunchBoxBooking.mutate(box.boxId)}
          disabled={!lunchBoxEligibilityQuery.data?.eligible || lunchBoxBooking.isPending || bookingQuery.data !== null}
          className="spotly-card cursor-pointer p-[18px] text-left transition hover:-translate-y-0.5 hover:shadow-md disabled:cursor-not-allowed disabled:opacity-55">
          <div className="grid h-[52px] w-[52px] place-items-center rounded-[15px] bg-[var(--c-fcede7)] text-[var(--c-ec6a4d)]"><AppIcon name="lunch_dining" className="text-[28px]" /></div>
          <h2 className="mt-3.5 mb-1 text-[15px] font-bold">{box.name}</h2>
          <p className="m-0 text-[12px] leading-5 text-[var(--c-726a60)]">{box.description}</p>
          {box.allergens && <p className="mt-1 mb-0 text-[11px] text-[var(--c-a89e92)]">Allergeni: {box.allergens}</p>}
        </button>)}
      </div>
    </>}

    <section className="spotly-card mt-5 p-4" aria-labelledby="message-feed">
      <div className="flex items-center justify-between">
        <h2 id="message-feed" className="m-0 text-sm font-extrabold">Aggiornamenti locali</h2>
        <span className="text-[11px] text-text-muted">{realtimeStatus === 'connected' ? 'SignalR attivo' : realtimeStatus === 'reconnecting' ? 'riconnessione' : 'fallback query'}</span>
      </div>
      <ol className="mt-2 mb-0 list-none p-0">
        {(messagesQuery.data ?? []).length === 0 && <li className="py-3 text-xs text-text-muted">Il prossimo aggiornamento comparira qui.</li>}
        {(messagesQuery.data ?? []).slice(0, 6).map((message, index) => <li key={`${message.restaurantId}-${message.sequence}-${index}`} className="flex items-center gap-2.5 border-b border-border py-2.5 text-xs last:border-0">
          <i className="h-2 w-2 rounded-full bg-success" />
          <strong>{message.restaurantId}</strong>
          <span className="text-text-muted">{message.outcome} · seq. {message.sequence}</span>
          <time className="ml-auto text-text-muted">{new Date(message.receivedAtUtc).toLocaleTimeString('it-IT', { hour: '2-digit', minute: '2-digit' })}</time>
        </li>)}
      </ol>
    </section>
  </div>
}

function Tab({ active, onClick, label }: { active: boolean; onClick: () => void; label: string }) {
  return <button onClick={onClick} aria-pressed={active} className={`rounded-[13px] border-[1.5px] px-5 py-2.5 text-sm font-bold ${active ? 'border-[var(--c-2b2622)] bg-[var(--c-2b2622)] text-white' : 'border-border bg-surface text-[var(--c-726a60)]'}`}>{label}</button>
}
