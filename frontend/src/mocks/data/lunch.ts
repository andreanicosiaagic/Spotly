import type { Restaurant, RestaurantSlot, MenuItem, LunchBox } from '../../types'
import { getTodayDateKey } from '../../lib/date'

const today = getTodayDateKey()

export const SEED_RESTAURANTS: Restaurant[] = [
  { locationId: 'HQ', restaurantId: 'R01', name: 'Bistrot Verde', bookingDate: today, capacity: 40, availableSeats: 18, sequence: 1, updatedAtUtc: new Date().toISOString(), partnerChannelConfigured: true, partnerSequence: 1 },
  { locationId: 'HQ', restaurantId: 'R02', name: 'La Tavola', bookingDate: today, capacity: 30, availableSeats: 9, sequence: 1, updatedAtUtc: new Date().toISOString(), partnerChannelConfigured: true, partnerSequence: 1 },
]

export const SEED_RESTAURANT_SLOTS: RestaurantSlot[] = [
  { slotId: 'S01', restaurantId: 'R01', slotTime: '12:00', capacity: 15, available: 8,  bookingDate: today },
  { slotId: 'S02', restaurantId: 'R01', slotTime: '12:30', capacity: 15, available: 0,  bookingDate: today },
  { slotId: 'S03', restaurantId: 'R01', slotTime: '13:00', capacity: 10, available: 5,  bookingDate: today },
  { slotId: 'S04', restaurantId: 'R02', slotTime: '12:00', capacity: 15, available: 12, bookingDate: today },
  { slotId: 'S05', restaurantId: 'R02', slotTime: '13:00', capacity: 15, available: 3,  bookingDate: today },
]

export const SEED_MENU_ITEMS: MenuItem[] = [
  { itemId: 'm01', restaurantId: 'R01', menuDate: today, name: 'Pasta al pomodoro',    category: 'primo',    allergens: 'glutine' },
  { itemId: 'm02', restaurantId: 'R01', menuDate: today, name: 'Pollo arrosto',         category: 'secondo',  allergens: '' },
  { itemId: 'm03', restaurantId: 'R01', menuDate: today, name: 'Insalata mista',        category: 'contorno', allergens: '' },
  { itemId: 'm04', restaurantId: 'R02', menuDate: today, name: 'Risotto ai funghi',     category: 'primo',    allergens: 'latte' },
  { itemId: 'm05', restaurantId: 'R02', menuDate: today, name: 'Salmone al forno',      category: 'secondo',  allergens: 'pesce' },
]

export const SEED_LUNCH_BOXES: LunchBox[] = [
  { boxId: 'LB01', name: 'Box Classico',    description: 'Pasta, secondo, contorno e frutta', allergens: 'glutine',       isAvailable: true },
  { boxId: 'LB02', name: 'Box Vegano',      description: 'Cereali, legumi, verdure di stagione', allergens: '',            isAvailable: true },
  { boxId: 'LB03', name: 'Box Proteico',    description: 'Proteine, verdure, niente glutine',   allergens: '',            isAvailable: true },
]
