import type { DeskSpot, DeskBooking } from '../../types'

export const SEED_DESK_SPOTS: DeskSpot[] = [
  { deskId: 'D01', locationId: 'HQ', floor: 2, zone: 'Alpha', hasMonitor: true,  isStanding: false, hasWindow: true,  status: 'available' },
  { deskId: 'D02', locationId: 'HQ', floor: 2, zone: 'Alpha', hasMonitor: true,  isStanding: false, hasWindow: false, status: 'occupied'  },
  { deskId: 'D03', locationId: 'HQ', floor: 2, zone: 'Alpha', hasMonitor: false, isStanding: true,  hasWindow: true,  status: 'available' },
  { deskId: 'D04', locationId: 'HQ', floor: 2, zone: 'Alpha', hasMonitor: true,  isStanding: false, hasWindow: false, status: 'available' },
  { deskId: 'D05', locationId: 'HQ', floor: 2, zone: 'Beta',  hasMonitor: false, isStanding: false, hasWindow: true,  status: 'available' },
  { deskId: 'D06', locationId: 'HQ', floor: 2, zone: 'Beta',  hasMonitor: true,  isStanding: false, hasWindow: false, status: 'pending'   },
  { deskId: 'D07', locationId: 'HQ', floor: 3, zone: 'Gamma', hasMonitor: true,  isStanding: false, hasWindow: true,  status: 'available' },
  { deskId: 'D08', locationId: 'HQ', floor: 3, zone: 'Gamma', hasMonitor: false, isStanding: true,  hasWindow: false, status: 'available' },
]

export const SEED_DESK_BOOKINGS: DeskBooking[] = [
  {
    bookingId: 'db-001',
    deskId: 'D02',
    userId: 'u3',
    bookingDate: new Date().toISOString().split('T')[0],
    status: 'active',
  },
]
