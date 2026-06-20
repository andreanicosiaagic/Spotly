import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useOptimistic } from 'react'
import { AppIcon } from '../components/AppIcon'
import { DateStrip } from '../components/DateStrip'
import { useBookingStore } from '../store/bookingStore'
import type { ParkingSpot } from '../types'

const API = import.meta.env.VITE_API_URL ?? ''
const statusClass = { available: 'border-[#78B891] bg-[#D8EFE1] text-[#266E49]', occupied: 'border-[#D9CFC0] bg-[#E9E3D9] text-[#928879]', pending: 'border-[#EC6A4D] bg-[#FCEDE7] text-[#C0563C]', reserved: 'border-[#D6AD60] bg-[#F8E9C9] text-[#8B651E]' }
const labels = { available: 'Libero', occupied: 'Occupato', pending: 'In prenotazione', reserved: 'Riservato' }

export default function ParkingPage() {
  const { selectedDate, setParkingBooking } = useBookingStore()
  const queryClient = useQueryClient()
  const query = useQuery<ParkingSpot[]>({ queryKey: ['parking-spots', selectedDate], queryFn: async () => {
    const response = await fetch(`${API}/api/parking/spots?date=${selectedDate}`); if (!response.ok) throw new Error(); return response.json()
  } })
  const spots = query.data ?? []
  const [optimisticSpots, addOptimistic] = useOptimistic(spots, (current, id: string) => current.map(spot => spot.spotId === id ? { ...spot, status: 'pending' as const } : spot))
  const booking = useMutation({ mutationFn: async (spotId: string) => {
    const response = await fetch(`${API}/api/parking/bookings`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ spotId, bookingDate: selectedDate }) })
    const body = await response.json(); if (!response.ok) throw new Error(body.error ?? 'Prenotazione non riuscita'); return body
  }, onSuccess: result => { setParkingBooking(result); void queryClient.invalidateQueries({ queryKey: ['parking-spots', selectedDate] }) } })

  if (query.isLoading) return <Loading label="Caricamento parcheggio…" />
  if (query.isError) return <Loading label="Parcheggio non disponibile" danger />
  return <div>
    <div className="page-mobile-heading"><h1>Parcheggio</h1><p>Scegli un posto disponibile</p></div>
    <div className="mb-5 lg:hidden"><DateStrip /></div>
    {booking.error && <div role="alert" className="spotly-alert mb-4 border border-[#F3C9BC] bg-[#FBE7E1] text-[#A8432C]">{booking.error.message}</div>}
    <div className="grid items-start gap-6 lg:grid-cols-[1fr_320px]">
      <section aria-label="Mappa parcheggio" className="rounded-[24px] border border-[#E3E8DF] bg-[#F1F6F0] p-4 sm:p-[22px]">
        <div className="mb-4 flex items-center justify-between"><span className="rounded-b-lg bg-[#2B2622] px-3 py-1 text-[10px] font-bold text-white">Ingresso principale</span><span className="grid h-7 w-7 place-items-center rounded-full border border-[#C9BDAB] text-[10px] font-extrabold text-[#A8987E]">N</span></div>
        <div className="grid grid-cols-5 gap-2 sm:gap-3">{optimisticSpots.map(spot => <button key={spot.spotId}
          disabled={spot.status !== 'available' || booking.isPending} title={`${spot.spotNumber} · ${labels[spot.status]}`}
          onClick={() => { addOptimistic(spot.spotId); booking.mutate(spot.spotId) }}
          className={`aspect-[.72] rounded-md border-[1.5px] p-1 text-[10px] font-extrabold transition hover:-translate-y-0.5 disabled:cursor-not-allowed disabled:opacity-75 ${statusClass[spot.status]}`}>
          <AppIcon name={spot.type === 'ev' ? 'ev_station' : spot.type === 'disabled' ? 'accessible' : 'directions_car'} className="block text-[17px]" />{spot.spotNumber}
        </button>)}</div>
        <div className="mt-5 rounded-xl border border-[#DACDB8] bg-[#EAE1D2] py-4 text-center text-[10px] font-extrabold tracking-[.12em] text-[#A8987E]">EDIFICIO</div>
      </section>
      <aside>
        <div className="spotly-card p-[18px]"><h2 className="mt-0 mb-3 text-sm font-extrabold">Legenda</h2><div className="space-y-3">{Object.entries(labels).map(([status, label]) => <div key={status} className="flex items-center gap-2.5 text-[13px] font-semibold text-[#5C544A]"><i className={`h-4 w-4 rounded-[5px] border ${statusClass[status as keyof typeof statusClass]}`} />{label}</div>)}</div></div>
        <div className="mt-4 flex gap-2.5 rounded-2xl bg-[#E9F0FB] p-4 text-[13px] leading-5 text-[#3F5577]"><AppIcon name="info" className="text-[21px] text-[#3E6BB0]" /><span>I posti speciali richiedono l’idoneità associata al profilo.</span></div>
      </aside>
    </div>
  </div>
}

function Loading({ label, danger = false }: { label: string; danger?: boolean }) { return <div className={`py-14 text-center text-sm ${danger ? 'text-danger' : 'text-text-muted'}`}>{label}</div> }
