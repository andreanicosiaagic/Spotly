import type { ParkingSpot, ParkingBooking } from '../../types'

// Parcheggio esterno della sede — singolo livello a raso (Livello 0).
// La piantina riproduce il parcheggio reale: ingresso principale a Nord-Ovest,
// strip "Area Partner" riservata sul lato edificio, fila principale lungo il
// fronte e posti speciali (EV / disabili / ospiti) verso il corpo di fabbrica.
// L'ordine/posizione di ogni spotId è mappato in components/ParkingMap.tsx.
export const SEED_PARKING_SPOTS: ParkingSpot[] = [
  // Strip riservata partner (lato edificio, verticale)
  { spotId: 'P01', locationId: 'HQ', level: 0, spotNumber: 'R1', type: 'guest',    status: 'reserved'  },
  { spotId: 'P02', locationId: 'HQ', level: 0, spotNumber: 'R2', type: 'guest',    status: 'reserved'  },
  // Fila principale lungo il fronte (standard)
  { spotId: 'P03', locationId: 'HQ', level: 0, spotNumber: '01', type: 'standard', status: 'available' },
  { spotId: 'P04', locationId: 'HQ', level: 0, spotNumber: '02', type: 'standard', status: 'occupied'  },
  { spotId: 'P05', locationId: 'HQ', level: 0, spotNumber: '03', type: 'standard', status: 'available' },
  { spotId: 'P06', locationId: 'HQ', level: 0, spotNumber: '04', type: 'standard', status: 'pending'   },
  { spotId: 'P07', locationId: 'HQ', level: 0, spotNumber: '05', type: 'standard', status: 'available' },
  // Seconda fascia verso il corpo di fabbrica
  { spotId: 'P08', locationId: 'HQ', level: 0, spotNumber: 'EV', type: 'ev',       status: 'available' },
  { spotId: 'P09', locationId: 'HQ', level: 0, spotNumber: '06', type: 'standard', status: 'occupied'  },
  { spotId: 'P10', locationId: 'HQ', level: 0, spotNumber: '07', type: 'standard', status: 'available' },
  // Posti speciali vicino all'ingresso edificio
  { spotId: 'P11', locationId: 'HQ', level: 0, spotNumber: 'H',  type: 'disabled', status: 'available' },
  { spotId: 'P12', locationId: 'HQ', level: 0, spotNumber: 'G',  type: 'guest',    status: 'available' },
]

export const SEED_PARKING_BOOKINGS: ParkingBooking[] = [
  {
    bookingId: 'pb-001',
    spotId: 'P04',
    userId: 'u2',
    bookingDate: new Date().toISOString().split('T')[0],
    status: 'active',
  },
]
