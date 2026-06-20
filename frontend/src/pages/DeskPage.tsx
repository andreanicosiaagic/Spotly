import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useOptimistic } from 'react'
import { useBookingStore } from '../store/bookingStore'
import { useAuthStore } from '../store/authStore'
import type { DeskSpot } from '../types'

const API = import.meta.env.VITE_API_URL ?? ''

async function fetchSpots(date: string): Promise<DeskSpot[]> {
  const res = await fetch(`${API}/api/desk/spots?date=${date}`)
  if (!res.ok) throw new Error('Errore nel caricamento delle postazioni')
  return res.json()
}

const STATUS_COLORS: Record<string, string> = {
  available: 'bg-success text-white',
  occupied:  'bg-danger text-white',
  pending:   'bg-pending text-white',
  reserved:  'bg-warning text-white',
}

export default function DeskPage() {
  const { selectedDate, setDeskBooking } = useBookingStore()
  const user = useAuthStore(s => s.user)
  const qc = useQueryClient()

  const { data: spots = [], isLoading, error } = useQuery({
    queryKey: ['desk-spots', selectedDate],
    queryFn: () => fetchSpots(selectedDate),
  })

  const [optimisticSpots, addOptimistic] = useOptimistic(
    spots,
    (current: DeskSpot[], updatedId: string) =>
      current.map(s => s.deskId === updatedId ? { ...s, status: 'pending' as const } : s)
  )

  const bookMutation = useMutation({
    mutationFn: async (deskId: string) => {
      const res = await fetch(`${API}/api/desk/bookings`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ deskId, bookingDate: selectedDate, userId: user?.oid }),
      })
      if (!res.ok) {
        const err = await res.json()
        throw new Error(err.error ?? 'Errore prenotazione')
      }
      return res.json()
    },
    onSuccess: (booking) => {
      setDeskBooking(booking)
      qc.invalidateQueries({ queryKey: ['desk-spots', selectedDate] })
    },
  })

  const zones = [...new Set(spots.map(s => s.zone))]

  if (isLoading) return <div className="text-center py-12 text-text-muted">Caricamento postazioni…</div>
  if (error) return <div className="text-center py-12 text-danger">Errore nel caricamento</div>

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold text-text">💼 Postazioni</h1>
        <p className="text-sm text-text-muted mt-1">{selectedDate}</p>
      </div>

      {bookMutation.error && (
        <div className="bg-red-50 border border-danger rounded-lg px-4 py-3 text-sm text-danger">
          {bookMutation.error.message}
        </div>
      )}

      {zones.map(zone => (
        <div key={zone} className="bg-white border border-border rounded-xl p-4">
          <h2 className="font-medium text-text mb-3">Zona {zone}</h2>
          <p className="text-xs text-text-muted mb-3 text-center">— Mappa piano (SVG) —</p>
          <div className="grid grid-cols-4 gap-2">
            {optimisticSpots.filter(s => s.zone === zone).map(desk => (
              <button
                key={desk.deskId}
                disabled={desk.status !== 'available' || bookMutation.isPending}
                onClick={() => {
                  addOptimistic(desk.deskId)
                  bookMutation.mutate(desk.deskId)
                }}
                className={`rounded-md p-2 text-xs font-medium text-left transition-opacity ${
                  STATUS_COLORS[desk.status] ?? 'bg-gray-100'
                } ${desk.status !== 'available' ? 'opacity-60 cursor-not-allowed' : 'hover:opacity-90 cursor-pointer'}`}
              >
                <div>{desk.deskId}</div>
                <div className="opacity-75 mt-0.5">
                  {desk.hasMonitor && '🖥 '}
                  {desk.isStanding && '↕ '}
                  {desk.hasWindow && '🪟'}
                </div>
              </button>
            ))}
          </div>
        </div>
      ))}
    </div>
  )
}
