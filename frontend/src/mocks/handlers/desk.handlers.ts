import { http, HttpResponse } from 'msw'
import { SEED_DESK_SPOTS, SEED_DESK_BOOKINGS } from '../data/desk'
import type { DeskBooking } from '../../types'

const bookings: DeskBooking[] = [...SEED_DESK_BOOKINGS]
const spots = SEED_DESK_SPOTS.map(s => ({ ...s }))

const BASE = import.meta.env.VITE_API_URL ?? ''

export const deskHandlers = [
  http.get(`${BASE}/api/desk/spots`, ({ request }) => {
    const url = new URL(request.url)
    const date = url.searchParams.get('date') ?? new Date().toISOString().split('T')[0]
    const bookedDeskIds = bookings
      .filter(b => b.bookingDate === date && b.status === 'active')
      .map(b => b.deskId)
    const result = spots.map(s => ({
      ...s,
      status: bookedDeskIds.includes(s.deskId) ? 'occupied' : s.status,
    }))
    return HttpResponse.json(result)
  }),

  http.post(`${BASE}/api/desk/bookings`, async ({ request }) => {
    const body = await request.json() as { deskId: string; bookingDate: string; userId: string }
    // R-01: max 1 active desk booking per user per day
    const existing = bookings.find(
      b => b.userId === body.userId && b.bookingDate === body.bookingDate && b.status === 'active'
    )
    if (existing) {
      return HttpResponse.json({ error: 'R-01: hai già una prenotazione postazione per questo giorno' }, { status: 409 })
    }
    const booking: DeskBooking = {
      bookingId: `db-${Date.now()}`,
      deskId: body.deskId,
      userId: body.userId,
      bookingDate: body.bookingDate,
      status: 'active',
    }
    bookings.push(booking)
    const spot = spots.find(s => s.deskId === body.deskId)
    if (spot) spot.status = 'occupied'
    return HttpResponse.json(booking, { status: 201 })
  }),

  http.delete(`${BASE}/api/desk/bookings/:id`, ({ params }) => {
    const idx = bookings.findIndex(b => b.bookingId === params.id)
    if (idx === -1) return HttpResponse.json({ error: 'Not found' }, { status: 404 })
    const booking = bookings[idx]
    bookings[idx] = { ...booking, status: 'cancelled' }
    const spot = spots.find(s => s.deskId === booking.deskId)
    if (spot) spot.status = 'available'
    return new HttpResponse(null, { status: 204 })
  }),
]
