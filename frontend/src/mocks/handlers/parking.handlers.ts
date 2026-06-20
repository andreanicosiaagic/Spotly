import { http, HttpResponse } from 'msw'
import { SEED_PARKING_SPOTS, SEED_PARKING_BOOKINGS } from '../data/parking'
import { getTodayDateKey } from '../../lib/date'
import type { ParkingBooking } from '../../types'

const bookings: ParkingBooking[] = [...SEED_PARKING_BOOKINGS]
const spots = SEED_PARKING_SPOTS.map(s => ({ ...s }))

const BASE = import.meta.env.DEV && import.meta.env.VITE_USE_DIRECT_API !== 'true'
  ? ''
  : (import.meta.env.VITE_API_URL ?? '')
const today = getTodayDateKey()

export const parkingHandlers = [
  http.get(`${BASE}/api/parking/spots`, ({ request }) => {
    const url = new URL(request.url)
    const date = url.searchParams.get('date') ?? today
    const bookedSpotIds = bookings
      .filter(b => b.bookingDate === date && b.status === 'active')
      .map(b => b.spotId)
    const result = spots.map(s => ({
      ...s,
      status: bookedSpotIds.includes(s.spotId) ? 'occupied' : s.status,
    }))
    return HttpResponse.json(result)
  }),
  http.get(`${BASE}/api/parking/bookings/me`, ({ request }) => {
    const url = new URL(request.url)
    const date = url.searchParams.get('date') ?? today
    const booking = bookings.find((item) => item.userId === 'u1' && item.bookingDate === date && item.status === 'active')
    if (!booking) return HttpResponse.json({ error: 'Not found' }, { status: 404 })
    return HttpResponse.json(booking)
  }),
  http.post(`${BASE}/api/parking/spots/:spotId/lock`, () => new HttpResponse(null, { status: 204 })),

  http.post(`${BASE}/api/parking/bookings`, async ({ request }) => {
    const body = await request.json() as { spotId: string; bookingDate: string }
    // R-01: max 1 active booking per user per day
    const existing = bookings.find(
      b => b.userId === 'u1' && b.bookingDate === body.bookingDate && b.status === 'active'
    )
    if (existing) {
      return HttpResponse.json({ error: 'R-01: hai già una prenotazione parcheggio per questo giorno' }, { status: 409 })
    }
    const booking: ParkingBooking = {
      bookingId: `pb-${Date.now()}`,
      spotId: body.spotId,
      userId: 'u1',
      bookingDate: body.bookingDate,
      status: 'active',
    }
    bookings.push(booking)
    const spot = spots.find(s => s.spotId === body.spotId)
    if (spot) spot.status = 'occupied'
    return HttpResponse.json(booking, { status: 201 })
  }),

  http.delete(`${BASE}/api/parking/bookings/:id`, ({ params }) => {
    const idx = bookings.findIndex(b => b.bookingId === params.id)
    if (idx === -1) return HttpResponse.json({ error: 'Not found' }, { status: 404 })
    const booking = bookings[idx]
    bookings[idx] = { ...booking, status: 'cancelled' }
    const spot = spots.find(s => s.spotId === booking.spotId)
    if (spot) spot.status = 'available'
    return new HttpResponse(null, { status: 204 })
  }),
  http.post(`${BASE}/api/parking/bookings/:id/check-in`, () => new HttpResponse(null, { status: 204 })),
]
