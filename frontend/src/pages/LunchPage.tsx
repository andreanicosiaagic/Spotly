import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useBookingStore } from '../store/bookingStore'
import { useAuthStore } from '../store/authStore'
import type { Restaurant, RestaurantSlot, LunchBox } from '../types'

const API = import.meta.env.VITE_API_URL ?? ''

export default function LunchPage() {
  const { selectedDate, setLunchBooking } = useBookingStore()
  const user = useAuthStore(s => s.user)
  const qc = useQueryClient()
  const [selectedRestaurantId, setSelectedRestaurantId] = useState<string | null>(null)
  const [showLunchBox, setShowLunchBox] = useState(false)

  const { data: restaurants = [] } = useQuery<Restaurant[]>({
    queryKey: ['restaurants'],
    queryFn: () => fetch(`${API}/api/lunch/restaurants`).then(r => r.json()),
  })

  const { data: slots = [] } = useQuery<RestaurantSlot[]>({
    queryKey: ['lunch-slots', selectedDate, selectedRestaurantId],
    queryFn: () => fetch(`${API}/api/lunch/slots?date=${selectedDate}&restaurantId=${selectedRestaurantId}`).then(r => r.json()),
    enabled: !!selectedRestaurantId,
  })

  const { data: lunchBoxes = [] } = useQuery<LunchBox[]>({
    queryKey: ['lunchboxes'],
    queryFn: () => fetch(`${API}/api/lunch/lunchboxes`).then(r => r.json()),
    enabled: showLunchBox,
  })

  const bookMutation = useMutation({
    mutationFn: async (payload: {
      slotId?: string; restaurantId?: string
      isLunchBox: boolean; lunchBoxId?: string
    }) => {
      const res = await fetch(`${API}/api/lunch/bookings`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ ...payload, bookingDate: selectedDate, userId: user?.oid }),
      })
      if (!res.ok) {
        const err = await res.json()
        throw new Error(err.error ?? 'Errore prenotazione')
      }
      return res.json()
    },
    onSuccess: (booking) => {
      setLunchBooking(booking)
      qc.invalidateQueries({ queryKey: ['lunch-slots'] })
    },
  })

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold text-text">🍽 Pranzo</h1>
        <p className="text-sm text-text-muted mt-1">{selectedDate}</p>
      </div>

      {bookMutation.error && (
        <div className="bg-red-50 border border-danger rounded-lg px-4 py-3 text-sm text-danger">
          {(bookMutation.error as Error).message}
        </div>
      )}

      <div className="flex gap-2">
        <button
          onClick={() => setShowLunchBox(false)}
          className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${!showLunchBox ? 'bg-primary text-white' : 'bg-white border border-border text-text-muted'}`}
        >
          Locali convenzionati
        </button>
        <button
          onClick={() => setShowLunchBox(true)}
          className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${showLunchBox ? 'bg-primary text-white' : 'bg-white border border-border text-text-muted'}`}
        >
          📦 Lunch Box
        </button>
      </div>

      {!showLunchBox && (
        <div className="space-y-4">
          <div className="grid gap-3">
            {restaurants.map(r => (
              <button
                key={r.restaurantId}
                onClick={() => setSelectedRestaurantId(r.restaurantId)}
                className={`bg-white border rounded-xl p-4 text-left transition-colors ${
                  selectedRestaurantId === r.restaurantId ? 'border-primary' : 'border-border hover:border-primary/50'
                }`}
              >
                <p className="font-medium text-text">{r.name}</p>
                <p className="text-xs text-text-muted">Capienza: {r.capacity} posti</p>
              </button>
            ))}
          </div>

          {selectedRestaurantId && slots.length > 0 && (
            <div className="bg-white border border-border rounded-xl p-4 space-y-2">
              <h3 className="font-medium text-text">Scegli uno slot</h3>
              <div className="grid grid-cols-3 gap-2">
                {slots.map(slot => (
                  <button
                    key={slot.slotId}
                    disabled={slot.available <= 0 || bookMutation.isPending}
                    onClick={() => bookMutation.mutate({
                      slotId: slot.slotId,
                      restaurantId: selectedRestaurantId,
                      isLunchBox: false,
                    })}
                    className={`rounded-lg p-3 text-sm font-medium transition-colors ${
                      slot.available > 0
                        ? 'bg-success text-white hover:opacity-90 cursor-pointer'
                        : 'bg-gray-100 text-text-muted opacity-60 cursor-not-allowed'
                    }`}
                  >
                    <div>{slot.slotTime}</div>
                    <div className="text-xs opacity-80">{slot.available > 0 ? `${slot.available} posti` : 'Esaurito'}</div>
                  </button>
                ))}
              </div>
            </div>
          )}
        </div>
      )}

      {showLunchBox && (
        <div className="grid gap-3">
          {lunchBoxes.map(box => (
            <div key={box.boxId} className="bg-white border border-border rounded-xl p-4 flex items-center justify-between">
              <div>
                <p className="font-medium text-text">{box.name}</p>
                <p className="text-sm text-text-muted">{box.description}</p>
                {box.allergens && <p className="text-xs text-warning mt-1">⚠ {box.allergens}</p>}
              </div>
              <button
                disabled={bookMutation.isPending}
                onClick={() => bookMutation.mutate({ isLunchBox: true, lunchBoxId: box.boxId })}
                className="bg-primary text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-primary-dark transition-colors disabled:opacity-60"
              >
                Ordina
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
