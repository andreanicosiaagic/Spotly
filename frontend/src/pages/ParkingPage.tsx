import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useOptimistic, startTransition } from 'react'
import { AppIcon } from '../components/AppIcon'
import { DateStrip } from '../components/DateStrip'
import { ParkingMap } from '../components/ParkingMap'
import { deleteJson, getJson, postJson } from '../lib/api'
import { formatDateLabel, isToday } from '../lib/date'
import { useParkingBooking } from '../hooks/use-bookings'
import { useSignalR } from '../hooks/useSignalR'
import { useBookingStore } from '../store/bookingStore'
import type { ParkingBooking, ParkingSpot } from '../types'

const statusClass = { available: 'border-[var(--c-78b891)] bg-[var(--c-d8efe1)] text-[var(--c-266e49)]', occupied: 'border-[var(--c-d9cfc0)] bg-[var(--c-e9e3d9)] text-[var(--c-928879)]', pending: 'border-[var(--c-ec6a4d)] bg-[var(--c-fcede7)] text-[var(--c-c0563c)]', reserved: 'border-[var(--c-d6ad60)] bg-[var(--c-f8e9c9)] text-[var(--c-8b651e)]' }
const labels = { available: 'Libero', occupied: 'Occupato', pending: 'In prenotazione', reserved: 'Riservato' }
const typeLegend = [
  { glyph: 'EV', label: 'Ricarica elettrica' },
  { glyph: 'H', label: 'Posto disabili' },
  { glyph: 'G', label: 'Ospiti' },
  { glyph: 'R', label: 'Area partner' },
]

export default function ParkingPage() {
  const { selectedDate } = useBookingStore()
  const queryClient = useQueryClient()
  const activeBooking = useParkingBooking(selectedDate)
  const query = useQuery<ParkingSpot[]>({ queryKey: ['parking-spots', selectedDate], queryFn: () => getJson<ParkingSpot[]>(`/api/parking/spots?date=${selectedDate}`) })
  const spots = query.data ?? []
  const [optimisticSpots, addOptimistic] = useOptimistic(spots, (current, id: string) => current.map(spot => spot.spotId === id ? { ...spot, status: 'pending' as const } : spot))
  useSignalR('HQ', selectedDate, (update) => {
    queryClient.setQueryData<ParkingSpot[]>(['parking-spots', selectedDate], (current) => current?.map((spot) => (
      spot.spotId === update.resourceId ? { ...spot, status: update.newStatus } : spot
    )) ?? [])
  })
  const booking = useMutation({
    mutationFn: async (spotId: string) => {
      await postJson(`/api/parking/spots/${spotId}/lock?date=${selectedDate}`)
      return postJson<ParkingBooking>('/api/parking/bookings', { spotId, bookingDate: selectedDate })
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['parking-booking', selectedDate] })
      void queryClient.invalidateQueries({ queryKey: ['parking-spots', selectedDate] })
    },
  })
  const cancellation = useMutation({
    mutationFn: async (bookingId: string) => deleteJson(`/api/parking/bookings/${bookingId}`),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['parking-booking', selectedDate] })
      void queryClient.invalidateQueries({ queryKey: ['parking-spots', selectedDate] })
    },
  })
  const checkIn = useMutation({
    mutationFn: async (bookingId: string) => postJson(`/api/parking/bookings/${bookingId}/check-in`),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['parking-booking', selectedDate] }),
  })
  const selectSpot = (spotId: string) => {
    startTransition(async () => {
      addOptimistic(spotId)
      try { await booking.mutateAsync(spotId) } catch { /* errore mostrato via booking.error */ }
    })
  }

  if (query.isLoading) return <Loading label="Caricamento parcheggio…" />
  if (query.isError) return <Loading label="Parcheggio non disponibile" danger />
  return <div>
    <div className="page-mobile-heading"><h1>Parcheggio</h1><p>Scegli un posto disponibile</p></div>
    <div className="mb-5 lg:hidden"><DateStrip /></div>
    {(booking.error || cancellation.error || checkIn.error) && <div role="alert" className="spotly-alert mb-4 border border-[var(--c-f3c9bc)] bg-[var(--c-fbe7e1)] text-[var(--c-a8432c)]">
      {booking.error?.message ?? cancellation.error?.message ?? checkIn.error?.message}
    </div>}
    {activeBooking.data && <section className="spotly-card mb-4 p-4">
      <div className="flex items-center justify-between gap-4">
        <div>
          <p className="m-0 text-[11px] font-extrabold uppercase tracking-[.08em] text-[var(--c-a89e92)]">Prenotazione attiva</p>
          <h2 className="mt-1 mb-1 text-base font-bold">Posto {activeBooking.data.spotId}</h2>
          <p className="m-0 text-sm text-text-muted">{formatDateLabel(activeBooking.data.bookingDate, { day: 'numeric', month: 'long', year: 'numeric' })}{activeBooking.data.checkedInAtUtc ? ' · check-in completato' : ''}</p>
        </div>
        <div className="flex gap-2">
          {isToday(selectedDate) && !activeBooking.data.checkedInAtUtc && <button onClick={() => checkIn.mutate(activeBooking.data!.bookingId)} disabled={checkIn.isPending} className="rounded-[12px] border border-[var(--c-b8dcc7)] bg-[var(--c-e7f3ec)] px-3 py-2 text-xs font-bold text-[var(--c-266e49)]">Check-in</button>}
          <button onClick={() => cancellation.mutate(activeBooking.data!.bookingId)} disabled={cancellation.isPending} className="rounded-[12px] border border-[var(--c-f3c9bc)] bg-[var(--c-fff7f2)] px-3 py-2 text-xs font-bold text-[var(--c-c0563c)]">Annulla</button>
        </div>
      </div>
    </section>}
    <div className="grid items-start gap-6 lg:grid-cols-[1fr_320px]">
      <section aria-label="Mappa parcheggio" className="rounded-[24px] border border-[var(--c-e3e8df)] bg-[var(--c-f1f6f0)] p-4 sm:p-[22px]">
        <div className="mb-3 flex items-center justify-between">
          <span className="text-[11px] font-extrabold uppercase tracking-[.1em] text-[var(--c-8fa77e)]">Parcheggio esterno</span>
          <span className="rounded-lg bg-[var(--c-e3eedd)] px-2.5 py-1 text-[10px] font-extrabold text-[var(--c-6e8a5c)]">Livello 0</span>
        </div>
        <ParkingMap spots={optimisticSpots} onSelect={selectSpot} busy={booking.isPending || activeBooking.data !== null} />
      </section>
      <aside>
        <div className="spotly-card p-[18px]"><h2 className="mt-0 mb-3 text-sm font-extrabold">Legenda</h2><div className="space-y-3">{Object.entries(labels).map(([status, label]) => <div key={status} className="flex items-center gap-2.5 text-[13px] font-semibold text-[var(--c-5c544a)]"><i className={`h-4 w-4 rounded-[5px] border ${statusClass[status as keyof typeof statusClass]}`} />{label}</div>)}</div>
          <div className="mt-4 border-t border-[var(--c-efe8dc)] pt-3"><h3 className="mt-0 mb-2 text-[11px] font-extrabold uppercase tracking-wider text-[var(--c-a89e92)]">Tipo posto</h3><div className="space-y-2">{typeLegend.map(item => <div key={item.glyph} className="flex items-center gap-2.5 text-[12px] font-semibold text-[var(--c-5c544a)]"><i className="grid h-5 w-5 place-items-center rounded-[5px] bg-[var(--c-efe9df)] text-[9px] font-extrabold text-[var(--c-8a7f6e)]">{item.glyph}</i>{item.label}</div>)}</div></div>
        </div>
        <div className="mt-4 flex gap-2.5 rounded-2xl bg-[var(--c-e9f0fb)] p-4 text-[13px] leading-5 text-[var(--c-3f5577)]"><AppIcon name="info" className="text-[21px] text-[var(--c-3e6bb0)]" /><span>I posti speciali richiedono l’idoneità associata al profilo.</span></div>
      </aside>
    </div>
  </div>
}

function Loading({ label, danger = false }: { label: string; danger?: boolean }) { return <div className={`py-14 text-center text-sm ${danger ? 'text-danger' : 'text-text-muted'}`}>{label}</div> }
