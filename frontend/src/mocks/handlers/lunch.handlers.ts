import { http, HttpResponse } from 'msw'
import { SEED_RESTAURANTS, SEED_MENU_ITEMS, SEED_LUNCH_BOXES } from '../data/lunch'
import { getTodayDateKey } from '../../lib/date'
import type { LunchBooking, RestaurantMessageEvent } from '../../types'

const BASE = import.meta.env.DEV && import.meta.env.VITE_USE_DIRECT_API !== 'true'
  ? ''
  : (import.meta.env.VITE_API_URL ?? '')
const today = getTodayDateKey()
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
    messages.unshift({
      locationId: restaurant.locationId,
      bookingDate: today,
      restaurantId: restaurant.restaurantId,
      kind: 'AVAIL',
      outcome: 'applied',
      sequence: current.sequence,
      receivedAtUtc: current.updatedAtUtc,
    })
  }
  lastAutomaticUpdate = Date.now()
}

export const lunchHandlers = [
  http.get(`${BASE}/api/lunch/restaurants`, () => {
    applyAutomaticUpdates()
    return HttpResponse.json(SEED_RESTAURANTS.map(restaurant => ({
      locationId: restaurant.locationId,
      restaurantId: restaurant.restaurantId,
      name: restaurant.name,
      bookingDate: restaurant.bookingDate,
      capacity: restaurant.capacity,
      ...availability.get(restaurant.restaurantId),
      partnerChannelConfigured: true,
      partnerSequence: availability.get(restaurant.restaurantId)?.sequence ?? 1,
    })))
  }),
  http.get(`${BASE}/api/lunch/bookings/me`, ({ request }) => {
    const url = new URL(request.url)
    const date = url.searchParams.get('date') ?? today
    const booking = bookings.find((item) => item.userId === 'u1' && item.bookingDate === date && item.status === 'active') ?? null
    if (!booking) return HttpResponse.json({ error: 'Not found' }, { status: 404 })
    return HttpResponse.json(booking)
  }),
  http.get(`${BASE}/api/lunch/partner-messages`, () => HttpResponse.json(messages.slice(0, 20))),
  http.get(`${BASE}/api/lunch/slots`, ({ request }) => {
    const url = new URL(request.url)
    const date = url.searchParams.get('date') ?? today
    const restaurantId = url.searchParams.get('restaurantId')
    const data = SEED_RESTAURANTS
      .filter((restaurant) => !restaurantId || restaurant.restaurantId === restaurantId)
      .flatMap((restaurant, index) => ([
        { slotId: `S${index + 1}A-${date}`, restaurantId: restaurant.restaurantId, slotTime: '12:00', capacity: 15, available: Math.max(0, restaurant.availableSeats - 3), bookingDate: date },
        { slotId: `S${index + 1}B-${date}`, restaurantId: restaurant.restaurantId, slotTime: '13:00', capacity: 15, available: Math.max(0, restaurant.availableSeats - 6), bookingDate: date },
      ]))
    return HttpResponse.json(data)
  }),
  http.get(`${BASE}/api/lunch/menu`, ({ request }) => {
    const url = new URL(request.url)
    const date = url.searchParams.get('date') ?? today
    const restaurantId = url.searchParams.get('restaurantId')
    return HttpResponse.json(SEED_MENU_ITEMS.filter(item => item.menuDate === date && (!restaurantId || item.restaurantId === restaurantId)))
  }),
  http.get(`${BASE}/api/lunch/lunchboxes`, () => HttpResponse.json(SEED_LUNCH_BOXES.filter(box => box.isAvailable))),
  http.get(`${BASE}/api/lunch/lunchbox-eligibility`, ({ request }) => {
    const url = new URL(request.url)
    const date = url.searchParams.get('date') ?? today
    const restaurantsFull = SEED_RESTAURANTS.every((restaurant) => (availability.get(restaurant.restaurantId)?.availableSeats ?? 0) <= 0)
    const eligible = date !== today && restaurantsFull
    return HttpResponse.json({
      eligible,
      reason: eligible ? 'Locali completi: lunch box prenotabile.' : 'Lunch box disponibile solo entro il giorno prima e quando i locali sono completi.',
      cutoffLocal: '23:59',
      restaurantsFull,
      outsideOperatingHours: false,
      demoDate: date,
    })
  }),
  http.post(`${BASE}/api/lunch/bookings`, async ({ request }) => {
    const body = await request.json() as { restaurantId?: string; slotId?: string; menuItemIds?: string[]; isLunchBox: boolean; lunchBoxId?: string; bookingDate: string }
    if (bookings.some(booking => booking.userId === 'u1' && booking.bookingDate === body.bookingDate && booking.status === 'active'))
      return HttpResponse.json({ error: 'R-01: hai già una prenotazione pranzo per questo giorno' }, { status: 409 })
    if (body.isLunchBox) {
      const booking: LunchBooking = { bookingId: crypto.randomUUID(), userId: 'u1', bookingDate: body.bookingDate,
        isLunchBox: true, lunchBoxId: body.lunchBoxId, status: 'active', deliveryStatus: 'pending', menuItemIds: [] }
      bookings.push(booking)
      return HttpResponse.json(booking, { status: 201 })
    }
    const restaurantId = body.restaurantId!
    const current = availability.get(restaurantId)
    if (!current) return HttpResponse.json({ error: 'Locale non configurato', partnerCode: 'NOT_CONFIGURED', availableSeats: 0 }, { status: 404 })
    if (!body.slotId) return HttpResponse.json({ error: 'Seleziona uno slot' }, { status: 422 })
    if (!body.menuItemIds?.length) return HttpResponse.json({ error: 'Seleziona almeno un piatto' }, { status: 422 })
    const code = nextOutcomes.get(restaurantId) ?? (current.availableSeats > 0 ? 'OK' : 'FULL')
    nextOutcomes.delete(restaurantId)
    if (code !== 'OK') {
      if (code === 'FULL') current.availableSeats = 0
      return HttpResponse.json({ error: `Prenotazione rifiutata dal locale: ${code}`, partnerCode: code, availableSeats: current.availableSeats }, { status: 409 })
    }
    current.availableSeats -= 1
    current.sequence += 1
    current.updatedAtUtc = new Date().toISOString()
    const booking: LunchBooking = { bookingId: crypto.randomUUID(), restaurantId, userId: 'u1', bookingDate: body.bookingDate,
      slotId: body.slotId, isLunchBox: false, status: 'active', deliveryStatus: 'pending', partnerStatus: 'confirmed', partnerCode: 'OK',
      menuItemIds: body.menuItemIds,
      partnerReference: `TG-${Date.now()}`, partnerAvailableSeats: current.availableSeats }
    bookings.push(booking)
    return HttpResponse.json({ booking, partnerCode: 'OK', availableSeats: current.availableSeats, partnerReference: booking.partnerReference }, { status: 201 })
  }),
  http.delete(`${BASE}/api/lunch/bookings/:id`, ({ params }) => {
    const index = bookings.findIndex((booking) => booking.bookingId === params.id)
    if (index === -1) return HttpResponse.json({ error: 'Not found' }, { status: 404 })
    const booking = bookings[index]
    bookings[index] = { ...booking, status: 'cancelled', deliveryStatus: 'cancelled' }
    if (booking.restaurantId) {
      const current = availability.get(booking.restaurantId)
      if (current) {
        current.availableSeats += 1
        current.sequence += 1
        current.updatedAtUtc = new Date().toISOString()
      }
    }
    return new HttpResponse(null, { status: 204 })
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
