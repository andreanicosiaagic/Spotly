import { http, HttpResponse } from 'msw'
import { SEED_RESTAURANTS, SEED_MENU_ITEMS, SEED_LUNCH_BOXES } from '../data/lunch'
import type { LunchBooking, RestaurantMessageEvent } from '../../types'

const BASE = import.meta.env.VITE_API_URL ?? ''
const today = new Date().toISOString().split('T')[0]
const availability = new Map(SEED_RESTAURANTS.map((restaurant, index) => [restaurant.restaurantId, {
  availableSeats: index === 0 ? 18 : 9,
  sequence: 1,
  updatedAtUtc: new Date().toISOString(),
}]))
const bookings: LunchBooking[] = []
const messages: RestaurantMessageEvent[] = []
const nextOutcomes = new Map<string, string>()
let lastAutomaticUpdate = Date.now()

function applyAutomaticUpdates() {
  if (Date.now() - lastAutomaticUpdate < 7_000) return
  for (const restaurant of SEED_RESTAURANTS) {
    const current = availability.get(restaurant.restaurantId)!
    current.availableSeats = current.availableSeats > 2 ? current.availableSeats - 1 : restaurant.capacity
    current.sequence += 1
    current.updatedAtUtc = new Date().toISOString()
    messages.unshift({ restaurantId: restaurant.restaurantId, kind: 'AVAIL', outcome: 'applied', sequence: current.sequence, receivedAtUtc: current.updatedAtUtc })
  }
  lastAutomaticUpdate = Date.now()
}

export const lunchHandlers = [
  http.get(`${BASE}/api/lunch/restaurants`, () => {
    applyAutomaticUpdates()
    return HttpResponse.json(SEED_RESTAURANTS.map(restaurant => ({
      restaurantId: restaurant.restaurantId,
      name: restaurant.name,
      capacity: restaurant.capacity,
      ...availability.get(restaurant.restaurantId),
      partnerChannelConfigured: true,
    })))
  }),
  http.get(`${BASE}/api/lunch/partner-messages`, () => HttpResponse.json(messages.slice(0, 20))),
  http.get(`${BASE}/api/lunch/menu`, ({ request }) => {
    const url = new URL(request.url)
    const date = url.searchParams.get('date') ?? today
    const restaurantId = url.searchParams.get('restaurantId')
    return HttpResponse.json(SEED_MENU_ITEMS.filter(item => item.menuDate === date && (!restaurantId || item.restaurantId === restaurantId)))
  }),
  http.get(`${BASE}/api/lunch/lunchboxes`, () => HttpResponse.json(SEED_LUNCH_BOXES.filter(box => box.isAvailable))),
  http.post(`${BASE}/api/lunch/bookings`, async ({ request }) => {
    const body = await request.json() as { restaurantId?: string; isLunchBox: boolean; lunchBoxId?: string; bookingDate: string }
    if (bookings.some(booking => booking.userId === 'u1' && booking.bookingDate === body.bookingDate && booking.status === 'active'))
      return HttpResponse.json({ error: 'R-01: hai già una prenotazione pranzo per questo giorno' }, { status: 409 })
    if (body.isLunchBox) {
      const booking: LunchBooking = { bookingId: crypto.randomUUID(), userId: 'u1', bookingDate: body.bookingDate,
        isLunchBox: true, lunchBoxId: body.lunchBoxId, status: 'active', deliveryStatus: 'pending' }
      bookings.push(booking)
      return HttpResponse.json(booking, { status: 201 })
    }
    const restaurantId = body.restaurantId!
    const current = availability.get(restaurantId)
    if (!current) return HttpResponse.json({ error: 'Locale non configurato', partnerCode: 'NOT_CONFIGURED', availableSeats: 0 }, { status: 404 })
    const code = nextOutcomes.get(restaurantId) ?? (current.availableSeats > 0 ? 'OK' : 'FULL')
    nextOutcomes.delete(restaurantId)
    if (code !== 'OK') {
      if (code === 'FULL') current.availableSeats = 0
      return HttpResponse.json({ error: `Prenotazione rifiutata dal locale: ${code}`, partnerCode: code, availableSeats: current.availableSeats }, { status: 409 })
    }
    current.availableSeats -= 1
    current.updatedAtUtc = new Date().toISOString()
    const booking: LunchBooking = { bookingId: crypto.randomUUID(), restaurantId, userId: 'u1', bookingDate: body.bookingDate,
      isLunchBox: false, status: 'active', deliveryStatus: 'pending', partnerStatus: 'confirmed', partnerCode: 'OK',
      partnerReference: `TG-${Date.now()}`, partnerAvailableSeats: current.availableSeats }
    bookings.push(booking)
    return HttpResponse.json({ booking, partnerCode: 'OK', availableSeats: current.availableSeats, partnerReference: booking.partnerReference }, { status: 201 })
  }),
  http.post(`${BASE}/api/lunch/demo/tick`, () => {
    lastAutomaticUpdate = 0
    applyAutomaticUpdates()
    return HttpResponse.json({ applied: SEED_RESTAURANTS.length })
  }),
  http.post(`${BASE}/api/lunch/demo/restaurants/:restaurantId/next-booking-outcome`, async ({ params, request }) => {
    const body = await request.json() as { code: string }
    nextOutcomes.set(String(params.restaurantId), body.code.toUpperCase())
    return new HttpResponse(null, { status: 204 })
  }),
]
