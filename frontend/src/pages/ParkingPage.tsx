import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useOptimistic } from 'react'
import { useBookingStore } from '../store/bookingStore'
import { useAuthStore } from '../store/authStore'
import type { ParkingSpot } from '../types'

const API = import.meta.env.VITE_API_URL ?? ''

async function fetchSpots(date: string): Promise<ParkingSpot[]> {
  const res = await fetch(`${API}/api/parking/spots?date=${date}`)
  if (!res.ok) throw new Error('Errore nel caricamento dei posti')
  return res.json()
}

const STATUS_COLORS: Record<string, string> = {
  available: 'bg-success text-white',
  occupied:  'bg-danger text-white',
  pending:   'bg-pending text-white',
  reserved:  'bg-warning text-white',
}

const STATUS_LABEL: Record<string, string> = {
  available: 'Libero',
  occupied:  'Occupato',
  pending:   'In prenotazione',
  reserved:  'Riservato',
}

export default function ParkingPage() {
  const { selectedDate, setParkingBooking } = useBookingStore()
  const user = useAuthStore(s => s.user)
  const qc = useQueryClient()

  const { data: spots = [], isLoading, error } = useQuery({
    queryKey: ['parking-spots', selectedDate],
    queryFn: () => fetchSpots(selectedDate),
  })

  const [optimisticSpots, addOptimistic] = useOptimistic(
    spots,
    (current: ParkingSpot[], updatedId: string) =>
      current.map(s => s.spotId === updatedId ? { ...s, status: 'pending' as const } : s)
  )

  const bookMutation = useMutation({
    mutationFn: async (spotId: string) => {
      const res = await fetch(`${API}/api/parking/bookings`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ spotId, bookingDate: selectedDate, userId: user?.oid }),
      })
      if (!res.ok) {
        const err = await res.json()
        throw new Error(err.error ?? 'Errore prenotazione')
      }
      return res.json()
    },
    onSuccess: (booking) => {
      setParkingBooking(booking)
      qc.invalidateQueries({ queryKey: ['parking-spots', selectedDate] })
    },
  })

  if (isLoading) return <div className="text-center py-12 text-text-muted">Caricamento posti…</div>
  if (error) return <div className="text-center py-12 text-danger">Errore nel caricamento</div>

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold text-text">🚗 Parcheggio</h1>
        <p className="text-sm text-text-muted mt-1">{selectedDate}</p>
      </div>

      {bookMutation.error && (
        <div className="bg-red-50 border border-danger rounded-lg px-4 py-3 text-sm text-danger">
          {bookMutation.error.message}
        </div>
      )}

      {/* Mappa SVG placeholder */}
      <div className="bg-white border border-border rounded-xl p-4">
        <p className="text-xs text-text-muted mb-3 text-center">— Mappa parcheggio (SVG) —</p>
        <div className="grid grid-cols-5 gap-2">
          {optimisticSpots.map(spot => (
            <button
              key={spot.spotId}
              disabled={spot.status !== 'available' || bookMutation.isPending}
              onClick={() => {
                addOptimistic(spot.spotId)
                bookMutation.mutate(spot.spotId)
              }}
              className={`rounded-md p-2 text-xs font-medium transition-opacity ${
                STATUS_COLORS[spot.status] ?? 'bg-gray-100'
              } ${spot.status !== 'available' ? 'opacity-60 cursor-not-allowed' : 'hover:opacity-90 cursor-pointer'}`}
              title={`${spot.spotNumber} (${spot.type})`}
            >
              <div>{spot.spotNumber}</div>
              <div className="text-xs opacity-80">{STATUS_LABEL[spot.status]}</div>
            </button>
          ))}
        </div>
      </div>

      {/* Legenda */}
      <div className="flex flex-wrap gap-3 text-xs">
        {Object.entries(STATUS_LABEL).map(([status, label]) => (
          <span key={status} className={`px-2 py-1 rounded-full ${STATUS_COLORS[status]}`}>
            {label}
          </span>
        ))}
      </div>
    </div>
  )
}
