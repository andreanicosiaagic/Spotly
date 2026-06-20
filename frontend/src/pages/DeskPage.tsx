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
        className={`rounded-[13px] border-[1.5px] px-5 py-2.5 text-sm font-bold ${floor === option.value ? 'border-[#2B2622] bg-[#2B2622] text-white' : 'border-border bg-white text-[#726A60]'}`}>{option.label}</button>)}
      <button onClick={() => setMonitorOnly(value => !value)} className={`ml-auto flex items-center gap-2 rounded-[13px] border-[1.5px] px-4 py-2.5 text-[13px] font-bold ${monitorOnly ? 'border-primary bg-[#FCEDE7] text-[#C0563C]' : 'border-border bg-white text-[#726A60]'}`}><AppIcon name="desktop_windows" />Monitor</button>
    </div>
    {(booking.error || cancellation.error || checkIn.error) && <div role="alert" className="spotly-alert mb-4 border border-[#F3C9BC] bg-[#FBE7E1] text-[#A8432C]">
      {booking.error?.message ?? cancellation.error?.message ?? checkIn.error?.message}
    </div>}
    {activeBooking.data && <section className="spotly-card mb-4 p-4">
      <div className="flex items-center justify-between gap-4">
        <div>
          <p className="m-0 text-[11px] font-extrabold uppercase tracking-[.08em] text-[#A89E92]">Prenotazione attiva</p>
          <h2 className="mt-1 mb-1 text-base font-bold">Desk {activeBooking.data.deskId}</h2>
          <p className="m-0 text-sm text-text-muted">{formatDateLabel(activeBooking.data.bookingDate, { day: 'numeric', month: 'long', year: 'numeric' })}{activeBooking.data.checkedInAtUtc ? ' · check-in completato' : ''}</p>
        </div>
        <div className="flex gap-2">
          {isToday(selectedDate) && !activeBooking.data.checkedInAtUtc && <button onClick={() => checkIn.mutate(activeBooking.data!.bookingId)} disabled={checkIn.isPending} className="rounded-[12px] border border-[#B8DCC7] bg-[#E7F3EC] px-3 py-2 text-xs font-bold text-[#266E49]">Check-in</button>}
          <button onClick={() => cancellation.mutate(activeBooking.data!.bookingId)} disabled={cancellation.isPending} className="rounded-[12px] border border-[#F3C9BC] bg-[#FFF7F2] px-3 py-2 text-xs font-bold text-[#C0563C]">Annulla</button>
        </div>
      </div>
    </section>}
    <div className="grid items-start gap-6 lg:grid-cols-[1fr_320px]">
      <section aria-label="Mappa postazioni" className="rounded-[24px] border border-border bg-[#F7F4EE] p-4 sm:p-[22px]">
        <div className="mb-3 flex items-center justify-between">
          <span className="text-[11px] font-extrabold uppercase tracking-[.1em] text-[#A8987E]">{FLOORS[floor].label}</span>
          <span className="rounded-lg bg-[#E7F3EC] px-2.5 py-1 text-[10px] font-extrabold text-[#2F8A5C]">{freeCount} libere</span>
        </div>
        <FloorMap floor={floor} desks={floorSpots} onSelect={selectDesk} busy={booking.isPending || activeBooking.data !== null} monitorOnly={monitorOnly} />
        <div className="mt-3 flex flex-wrap gap-x-4 gap-y-1 text-[11px] font-semibold text-[#8A8170]">
          <span>▣ Monitor</span><span>↕ Standing desk</span><span>◫ Finestra</span>
        </div>
      </section>
      <aside><div className="spotly-card p-[18px]"><h2 className="mt-0 mb-3 text-sm font-extrabold">Zone · {FLOORS[floor].label}</h2>{zones.map((zone, index) => <div key={zone} className="mb-3 flex items-center gap-2.5 text-[13px] font-semibold text-[#5C544A]"><i className={`h-4 w-4 rounded-[5px] ${['bg-[#9BCBAE]', 'bg-[#AFC0E4]', 'bg-[#D9A89B]'][index % 3]}`} />Zona {zone}</div>)}</div>
        <div className="mt-4 flex gap-2.5 rounded-2xl bg-[#E7F3EC] p-4 text-[13px] leading-5 text-[#3A6B50]"><AppIcon name="groups" className="text-[21px] text-[#2F8A5C]" /><span>Le postazioni vicine al team sono evidenziate in base alla sede Teams.</span></div></aside>
    </div>
  </div>
}
