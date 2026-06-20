import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useOptimistic, useState, startTransition } from 'react'
import { AppIcon } from '../components/AppIcon'
import { DateStrip } from '../components/DateStrip'
import { FloorMap } from '../components/FloorMap'
import { deleteJson, getJson, postJson } from '../lib/api'
import { formatDateLabel, isToday } from '../lib/date'
import { useDeskBooking } from '../hooks/use-bookings'
import { useSignalR } from '../hooks/useSignalR'
import { useBookingStore } from '../store/bookingStore'
import type { DeskBooking, DeskSpot } from '../types'

const FLOORS = [{ value: 0, label: 'Piano Terra' }, { value: 1, label: 'Piano Primo' }]

export default function DeskPage() {
  const { selectedDate } = useBookingStore()
  const [monitorOnly, setMonitorOnly] = useState(false)
  const [floor, setFloor] = useState(1)
  const queryClient = useQueryClient()
  const activeBooking = useDeskBooking(selectedDate)
  const query = useQuery<DeskSpot[]>({ queryKey: ['desk-spots', selectedDate], queryFn: () => getJson<DeskSpot[]>(`/api/desk/spots?date=${selectedDate}`) })
  const spots = query.data ?? []
  const [optimisticSpots, addOptimistic] = useOptimistic(spots, (current, id: string) => current.map(spot => spot.deskId === id ? { ...spot, status: 'pending' as const } : spot))
  useSignalR('HQ', selectedDate, (update) => {
    queryClient.setQueryData<DeskSpot[]>(['desk-spots', selectedDate], (current) => current?.map((desk) => (
      desk.deskId === update.resourceId ? { ...desk, status: update.newStatus } : desk
    )) ?? [])
  })
  const booking = useMutation({
    mutationFn: async (deskId: string) => {
      await postJson(`/api/desk/spots/${deskId}/lock?date=${selectedDate}`)
      return postJson<DeskBooking>('/api/desk/bookings', { deskId, bookingDate: selectedDate })
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['desk-booking', selectedDate] })
      void queryClient.invalidateQueries({ queryKey: ['desk-spots', selectedDate] })
    },
  })
  const cancellation = useMutation({
    mutationFn: async (bookingId: string) => deleteJson(`/api/desk/bookings/${bookingId}`),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['desk-booking', selectedDate] })
      void queryClient.invalidateQueries({ queryKey: ['desk-spots', selectedDate] })
    },
  })
  const checkIn = useMutation({
    mutationFn: async (bookingId: string) => postJson(`/api/desk/bookings/${bookingId}/check-in`),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['desk-booking', selectedDate] }),
  })
  const selectDesk = (deskId: string) => {
    startTransition(async () => {
      addOptimistic(deskId)
      try { await booking.mutateAsync(deskId) } catch { /* errore mostrato via booking.error */ }
    })
  }

  const floorSpots = optimisticSpots.filter(spot => spot.floor === floor)
  const zones = [...new Set(floorSpots.map(spot => spot.zone))]
  const freeCount = floorSpots.filter(spot => spot.status === 'available' && (!monitorOnly || spot.hasMonitor)).length

  if (query.isLoading) return <div className="py-14 text-center text-sm text-text-muted">Caricamento postazioni…</div>
  if (query.isError) return <div className="py-14 text-center text-sm text-danger">Postazioni non disponibili</div>
  return <div>
    <div className="page-mobile-heading"><h1>Postazioni</h1><p>Trova la postazione giusta per te</p></div>
    <div className="mb-5 lg:hidden"><DateStrip /></div>
    <div className="mb-4 flex flex-wrap items-center gap-2">
      {FLOORS.map(option => <button key={option.value} onClick={() => setFloor(option.value)}
        className={`rounded-[13px] border-[1.5px] px-5 py-2.5 text-sm font-bold ${floor === option.value ? 'border-[var(--c-2b2622)] bg-[var(--c-2b2622)] text-white' : 'border-border bg-surface text-[var(--c-726a60)]'}`}>{option.label}</button>)}
      <button onClick={() => setMonitorOnly(value => !value)} className={`ml-auto flex items-center gap-2 rounded-[13px] border-[1.5px] px-4 py-2.5 text-[13px] font-bold ${monitorOnly ? 'border-primary bg-[var(--c-fcede7)] text-[var(--c-c0563c)]' : 'border-border bg-surface text-[var(--c-726a60)]'}`}><AppIcon name="desktop_windows" />Monitor</button>
    </div>
    {(booking.error || cancellation.error || checkIn.error) && <div role="alert" className="spotly-alert mb-4 border border-[var(--c-f3c9bc)] bg-[var(--c-fbe7e1)] text-[var(--c-a8432c)]">
      {booking.error?.message ?? cancellation.error?.message ?? checkIn.error?.message}
    </div>}
    {activeBooking.data && <section className="spotly-card mb-4 p-4">
      <div className="flex items-center justify-between gap-4">
        <div>
          <p className="m-0 text-[11px] font-extrabold uppercase tracking-[.08em] text-[var(--c-a89e92)]">Prenotazione attiva</p>
          <h2 className="mt-1 mb-1 text-base font-bold">Desk {activeBooking.data.deskId}</h2>
          <p className="m-0 text-sm text-text-muted">{formatDateLabel(activeBooking.data.bookingDate, { day: 'numeric', month: 'long', year: 'numeric' })}{activeBooking.data.checkedInAtUtc ? ' · check-in completato' : ''}</p>
        </div>
        <div className="flex gap-2">
          {isToday(selectedDate) && !activeBooking.data.checkedInAtUtc && <button onClick={() => checkIn.mutate(activeBooking.data!.bookingId)} disabled={checkIn.isPending} className="rounded-[12px] border border-[var(--c-b8dcc7)] bg-[var(--c-e7f3ec)] px-3 py-2 text-xs font-bold text-[var(--c-266e49)]">Check-in</button>}
          <button onClick={() => cancellation.mutate(activeBooking.data!.bookingId)} disabled={cancellation.isPending} className="rounded-[12px] border border-[var(--c-f3c9bc)] bg-[var(--c-fff7f2)] px-3 py-2 text-xs font-bold text-[var(--c-c0563c)]">Annulla</button>
        </div>
      </div>
    </section>}
    <div className="grid items-start gap-6 lg:grid-cols-[1fr_320px]">
      <section aria-label="Mappa postazioni" className="rounded-[24px] border border-border bg-[var(--c-f7f4ee)] p-4 sm:p-[22px]">
        <div className="mb-3 flex items-center justify-between">
          <span className="text-[11px] font-extrabold uppercase tracking-[.1em] text-[var(--c-a8987e)]">{FLOORS[floor].label}</span>
          <span className="rounded-lg bg-[var(--c-e7f3ec)] px-2.5 py-1 text-[10px] font-extrabold text-[var(--c-2f8a5c)]">{freeCount} libere</span>
        </div>
        <FloorMap floor={floor} desks={floorSpots} onSelect={selectDesk} busy={booking.isPending || activeBooking.data !== null} monitorOnly={monitorOnly} />
        <div className="mt-3 flex flex-wrap gap-x-4 gap-y-1 text-[11px] font-semibold text-[var(--c-8a8170)]">
          <span>▣ Monitor</span><span>↕ Standing desk</span><span>◫ Finestra</span>
        </div>
      </section>
      <aside><div className="spotly-card p-[18px]"><h2 className="mt-0 mb-3 text-sm font-extrabold">Zone · {FLOORS[floor].label}</h2>{zones.map((zone, index) => <div key={zone} className="mb-3 flex items-center gap-2.5 text-[13px] font-semibold text-[var(--c-5c544a)]"><i className={`h-4 w-4 rounded-[5px] ${['bg-[var(--c-9bcbae)]', 'bg-[var(--c-afc0e4)]', 'bg-[var(--c-d9a89b)]'][index % 3]}`} />Zona {zone}</div>)}</div>
        <div className="mt-4 flex gap-2.5 rounded-2xl bg-[var(--c-e7f3ec)] p-4 text-[13px] leading-5 text-[var(--c-3a6b50)]"><AppIcon name="groups" className="text-[21px] text-[var(--c-2f8a5c)]" /><span>Le postazioni vicine al team sono evidenziate in base alla sede Teams.</span></div></aside>
    </div>
  </div>
}
