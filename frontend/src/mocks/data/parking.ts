import type { ParkingSpot, ParkingBooking } from '../../types'

export const SEED_PARKING_SPOTS: ParkingSpot[] = [
  { spotId: 'P01', locationId: 'HQ', level: 1, spotNumber: 'A01', type: 'standard', status: 'available' },
  { spotId: 'P02', locationId: 'HQ', level: 1, spotNumber: 'A02', type: 'standard', status: 'available' },
  { spotId: 'P03', locationId: 'HQ', level: 1, spotNumber: 'A03', type: 'standard', status: 'occupied' },
  { spotId: 'P04', locationId: 'HQ', level: 1, spotNumber: 'A04', type: 'standard', status: 'available' },
  { spotId: 'P05', locationId: 'HQ', level: 1, spotNumber: 'A05', type: 'ev',       status: 'available' },
  { spotId: 'P06', locationId: 'HQ', level: 1, spotNumber: 'A06', type: 'disabled', status: 'reserved' },
  { spotId: 'P07', locationId: 'HQ', level: 2, spotNumber: 'B01', type: 'standard', status: 'available' },
  { spotId: 'P08', locationId: 'HQ', level: 2, spotNumber: 'B02', type: 'standard', status: 'pending'   },
  { spotId: 'P09', locationId: 'HQ', level: 2, spotNumber: 'B03', type: 'standard', status: 'available' },
  { spotId: 'P10', locationId: 'HQ', level: 2, spotNumber: 'B04', type: 'guest',    status: 'available' },
]

export const SEED_PARKING_BOOKINGS: ParkingBooking[] = [
  {
    bookingId: 'pb-001',
    spotId: 'P03',
    userId: 'u2',
    bookingDate: new Date().toISOString().split('T')[0],
    status: 'active',
  },
]
