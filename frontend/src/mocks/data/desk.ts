import type { DeskSpot, DeskBooking } from '../../types'

// Postazioni hot-desk sui due piani reali della sede:
//   floor 0 → Piano Terra   (Open Space Nord, Uffici Ovest, Operativo Sud)
//   floor 1 → Piano Primo   (Open Space Centrale, Uffici Ovest)
// La posizione di ogni deskId sulla piantina è definita in components/FloorMap.tsx.
export const SEED_DESK_SPOTS: DeskSpot[] = [
  // ── Piano Terra ──────────────────────────────────────────────
  { deskId: 'T01', locationId: 'HQ', floor: 0, zone: 'Open Space', hasMonitor: true,  isStanding: false, hasWindow: true,  status: 'available' },
  { deskId: 'T02', locationId: 'HQ', floor: 0, zone: 'Open Space', hasMonitor: true,  isStanding: false, hasWindow: false, status: 'occupied'  },
  { deskId: 'T03', locationId: 'HQ', floor: 0, zone: 'Open Space', hasMonitor: false, isStanding: true,  hasWindow: true,  status: 'available' },
  { deskId: 'T04', locationId: 'HQ', floor: 0, zone: 'Open Space', hasMonitor: true,  isStanding: false, hasWindow: false, status: 'available' },
  { deskId: 'T05', locationId: 'HQ', floor: 0, zone: 'Uffici',     hasMonitor: true,  isStanding: false, hasWindow: true,  status: 'available' },
  { deskId: 'T06', locationId: 'HQ', floor: 0, zone: 'Uffici',     hasMonitor: false, isStanding: false, hasWindow: true,  status: 'occupied'  },
  { deskId: 'T07', locationId: 'HQ', floor: 0, zone: 'Operativo',  hasMonitor: true,  isStanding: false, hasWindow: false, status: 'available' },
  { deskId: 'T08', locationId: 'HQ', floor: 0, zone: 'Operativo',  hasMonitor: false, isStanding: true,  hasWindow: false, status: 'available' },
  { deskId: 'T09', locationId: 'HQ', floor: 0, zone: 'Operativo',  hasMonitor: true,  isStanding: false, hasWindow: true,  status: 'pending'   },
  // ── Piano Primo ──────────────────────────────────────────────
  { deskId: 'P01', locationId: 'HQ', floor: 1, zone: 'Open Space',   hasMonitor: true,  isStanding: false, hasWindow: true,  status: 'available' },
  { deskId: 'P02', locationId: 'HQ', floor: 1, zone: 'Open Space',   hasMonitor: true,  isStanding: false, hasWindow: false, status: 'occupied'  },
  { deskId: 'P03', locationId: 'HQ', floor: 1, zone: 'Open Space',   hasMonitor: false, isStanding: false, hasWindow: false, status: 'available' },
  { deskId: 'P04', locationId: 'HQ', floor: 1, zone: 'Open Space',   hasMonitor: false, isStanding: true,  hasWindow: false, status: 'available' },
  { deskId: 'P05', locationId: 'HQ', floor: 1, zone: 'Open Space',   hasMonitor: true,  isStanding: false, hasWindow: true,  status: 'available' },
  { deskId: 'P06', locationId: 'HQ', floor: 1, zone: 'Open Space',   hasMonitor: true,  isStanding: false, hasWindow: false, status: 'pending'   },
  { deskId: 'P07', locationId: 'HQ', floor: 1, zone: 'Open Space',   hasMonitor: false, isStanding: false, hasWindow: false, status: 'available' },
  { deskId: 'P08', locationId: 'HQ', floor: 1, zone: 'Open Space',   hasMonitor: true,  isStanding: true,  hasWindow: true,  status: 'available' },
  { deskId: 'P09', locationId: 'HQ', floor: 1, zone: 'Uffici Ovest', hasMonitor: true,  isStanding: false, hasWindow: true,  status: 'available' },
  { deskId: 'P10', locationId: 'HQ', floor: 1, zone: 'Uffici Ovest', hasMonitor: true,  isStanding: false, hasWindow: true,  status: 'occupied'  },
  { deskId: 'P11', locationId: 'HQ', floor: 1, zone: 'Uffici Ovest', hasMonitor: false, isStanding: false, hasWindow: false, status: 'available' },
  { deskId: 'P12', locationId: 'HQ', floor: 1, zone: 'Uffici Ovest', hasMonitor: true,  isStanding: false, hasWindow: true,  status: 'available' },
]

export const SEED_DESK_BOOKINGS: DeskBooking[] = [
  {
    bookingId: 'db-001',
    deskId: 'T02',
    userId: 'u3',
    bookingDate: new Date().toISOString().split('T')[0],
    status: 'active',
  },
]
