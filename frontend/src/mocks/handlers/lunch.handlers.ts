import { http, HttpResponse } from 'msw'
import { SEED_RESTAURANTS, SEED_RESTAURANT_SLOTS, SEED_MENU_ITEMS, SEED_LUNCH_BOXES } from '../data/lunch'
import type { LunchBooking } from '../../types'

const bookings: LunchBooking[] = []
const slots = SEED_RESTAURANT_SLOTS.map(s => ({ ...s }))

const BASE = import.meta.env.VITE_API_URL ?? ''

export const lunchHandlers = [
  http.get(`${BASE}/api/lunch/restaurants`, () => {
    return HttpResponse.json(SEED_RESTAURANTS)
  }),

  http.get(`${BASE}/api/lunch/slots`, ({ request }) => {
    const url = new URL(request.url)
    const date = url.searchParams.get('date') ?? new Date().toISOString().split('T')[0]
    const restaurantId = url.searchParams.get('restaurantId')
    const result = slots.filter(s =>
      s.bookingDate === date && (!restaurantId || s.restaurantId === restaurantId)
    )
    return HttpResponse.json(result)
  }),

  http.get(`${BASE}/api/lunch/menu`, ({ request }) => {
    const url = new URL(request.url)
    const date = url.searchParams.get('date') ?? new Date().toISOString().split('T')[0]
    const restaurantId = url.searchParams.get('restaurantId')
    const result = SEED_MENU_ITEMS.filter(m =>
      m.menuDate === date && (!restaurantId || m.restaurantId === restaurantId)
    )
    return HttpResponse.json(result)
  }),

  http.get(`${BASE}/api/lunch/lunchboxes`, () => {
    return HttpResponse.json(SEED_LUNCH_BOXES.filter(lb => lb.isAvailable))
  }),

  http.post(`${BASE}/api/lunch/bookings`, async ({ request }) => {
    const body = await request.json() as {
      slotId?: string
      restaurantId?: string
      isLunchBox: boolean
      lunchBoxId?: string
      bookingDate: string
      userId: string
    }
    // R-01: max 1 lunch booking per user per day
    const existing = bookings.find(
      b => b.userId === body.userId && b.bookingDate === body.bookingDate && b.status === 'active'
    )
    if (existing) {
      return HttpResponse.json({ error: 'R-01: hai già una prenotazione pranzo per questo giorno' }, { status: 409 })
    }
    // R-06: lunch box only if slot full or out of hours
    if (!body.isLunchBox && body.slotId) {
      const slot = slots.find(s => s.slotId === body.slotId)
      if (slot && slot.available <= 0) {
        return HttpResponse.json({ error: 'R-06: slot esaurito — usa il lunch box' }, { status: 409 })
      }
      if (slot && slot.available > 0) slot.available -= 1
    }
    const booking: LunchBooking = {
      bookingId: `lb-${Date.now()}`,
      restaurantId: body.restaurantId,
      slotId: body.slotId,
      userId: body.userId,
      bookingDate: body.bookingDate,
      isLunchBox: body.isLunchBox,
      lunchBoxId: body.lunchBoxId,
      status: 'active',
      deliveryStatus: body.isLunchBox ? 'pending' : 'pending',
    }
    bookings.push(booking)
    return HttpResponse.json(booking, { status: 201 })
  }),

  http.delete(`${BASE}/api/lunch/bookings/:id`, ({ params }) => {
    const idx = bookings.findIndex(b => b.bookingId === params.id)
    if (idx === -1) return HttpResponse.json({ error: 'Not found' }, { status: 404 })
    bookings[idx] = { ...bookings[idx], status: 'cancelled' }
    return new HttpResponse(null, { status: 204 })
  }),
]
