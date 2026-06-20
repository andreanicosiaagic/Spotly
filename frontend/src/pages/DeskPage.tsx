import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useOptimistic, useState } from 'react'
import { AppIcon } from '../components/AppIcon'
import { DateStrip } from '../components/DateStrip'
import { useBookingStore } from '../store/bookingStore'
import type { DeskSpot } from '../types'

const API = import.meta.env.VITE_API_URL ?? ''
const statusClass = { available: 'border-[#78B891] bg-[#D8EFE1] text-[#266E49]', occupied: 'border-[#D9CFC0] bg-[#E9E3D9] text-[#928879]', pending: 'border-[#EC6A4D] bg-[#FCEDE7] text-[#C0563C]', reserved: 'border-[#D6AD60] bg-[#F8E9C9] text-[#8B651E]' }

export default function DeskPage() {
  const { selectedDate, setDeskBooking } = useBookingStore()
  const [monitorOnly, setMonitorOnly] = useState(false)
  const queryClient = useQueryClient()
  const query = useQuery<DeskSpot[]>({ queryKey: ['desk-spots', selectedDate], queryFn: async () => {
    const response = await fetch(`${API}/api/desk/spots?date=${selectedDate}`); if (!response.ok) throw new Error(); return response.json()
  } })
  const spots = query.data ?? []
  const [optimisticSpots, addOptimistic] = useOptimistic(spots, (current, id: string) => current.map(spot => spot.deskId === id ? { ...spot, status: 'pending' as const } : spot))
  const booking = useMutation({ mutationFn: async (deskId: string) => {
    const response = await fetch(`${API}/api/desk/bookings`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ deskId, bookingDate: selectedDate }) })
    const body = await response.json(); if (!response.ok) throw new Error(body.error ?? 'Prenotazione non riuscita'); return body
  }, onSuccess: result => { setDeskBooking(result); void queryClient.invalidateQueries({ queryKey: ['desk-spots', selectedDate] }) } })
  const visibleSpots = monitorOnly ? optimisticSpots.filter(spot => spot.hasMonitor) : optimisticSpots
  const zones = [...new Set(visibleSpots.map(spot => spot.zone))]

  if (query.isLoading) return <div className="py-14 text-center text-sm text-text-muted">Caricamento postazioni…</div>
  if (query.isError) return <div className="py-14 text-center text-sm text-danger">Postazioni non disponibili</div>
  return <div>
    <div className="page-mobile-heading"><h1>Postazioni</h1><p>Trova la postazione giusta per te</p></div>
    <div className="mb-5 lg:hidden"><DateStrip /></div>
    <div className="mb-4 flex gap-2"><button className="rounded-[13px] border-[1.5px] border-[#2B2622] bg-[#2B2622] px-5 py-2.5 text-sm font-bold text-white">Piano 2</button>
      <button onClick={() => setMonitorOnly(value => !value)} className={`ml-auto flex items-center gap-2 rounded-[13px] border-[1.5px] px-4 py-2.5 text-[13px] font-bold ${monitorOnly ? 'border-primary bg-[#FCEDE7] text-[#C0563C]' : 'border-border bg-white text-[#726A60]'}`}><AppIcon name="desktop_windows" />Monitor</button></div>
    {booking.error && <div role="alert" className="spotly-alert mb-4 border border-[#F3C9BC] bg-[#FBE7E1] text-[#A8432C]">{booking.error.message}</div>}
    <div className="grid items-start gap-6 lg:grid-cols-[1fr_320px]">
      <section aria-label="Mappa postazioni" className="rounded-[24px] border border-border bg-[#F7F4EE] p-4 sm:p-[22px]">
        <div className="mb-4 grid grid-cols-3 gap-2"><div className="rounded-lg border border-[#E1D8C8] bg-[#EFE9DF] py-3 text-center text-[10px] font-bold text-[#A8987E]">Uffici</div><div className="rounded-lg border border-[#E1D8C8] bg-[#EFE9DF] py-3 text-center text-[10px] font-bold text-[#A8987E]">Sala A</div><div className="rounded-lg border border-[#E1D8C8] bg-[#EFE9DF] py-3 text-center text-[10px] font-bold text-[#A8987E]">Sala B</div></div>
        <div className="space-y-4">{zones.map((zone, zoneIndex) => <div key={zone} className={`rounded-2xl p-3 ${['bg-[#E6EAF5]', 'bg-[#E7F3EC]', 'bg-[#F6E6E1]'][zoneIndex % 3]}`}>
          <h2 className="mt-0 mb-3 text-[10px] font-extrabold uppercase tracking-wider text-[#726A60]">Zona {zone}</h2><div className="grid grid-cols-4 gap-2">{visibleSpots.filter(spot => spot.zone === zone).map(desk => <button key={desk.deskId}
            disabled={desk.status !== 'available' || booking.isPending} onClick={() => { addOptimistic(desk.deskId); booking.mutate(desk.deskId) }}
            className={`aspect-[1.25] rounded-md border-[1.5px] text-[9px] font-extrabold transition hover:-translate-y-0.5 disabled:cursor-not-allowed disabled:opacity-70 ${statusClass[desk.status]}`}>
            {desk.deskId}<span className="mt-0.5 block text-[9px] font-normal">{desk.hasMonitor ? '▣' : ''}{desk.isStanding ? ' ↕' : ''}{desk.hasWindow ? ' ◫' : ''}</span>
          </button>)}</div></div>)}</div>
      </section>
      <aside><div className="spotly-card p-[18px]"><h2 className="mt-0 mb-3 text-sm font-extrabold">Zone</h2>{zones.map((zone, index) => <div key={zone} className="mb-3 flex items-center gap-2.5 text-[13px] font-semibold text-[#5C544A]"><i className={`h-4 w-4 rounded-[5px] ${['bg-[#AFC0E4]', 'bg-[#9BCBAE]', 'bg-[#D9A89B]'][index % 3]}`} />Zona {zone}</div>)}</div>
        <div className="mt-4 flex gap-2.5 rounded-2xl bg-[#E7F3EC] p-4 text-[13px] leading-5 text-[#3A6B50]"><AppIcon name="groups" className="text-[21px] text-[#2F8A5C]" /><span>Le postazioni vicine al team sono evidenziate in base alla sede Teams.</span></div></aside>
    </div>
  </div>
}
