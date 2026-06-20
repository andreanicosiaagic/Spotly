import { create } from 'zustand'
import type { ParkingBooking, DeskBooking, LunchBooking } from '../types'

interface BookingState {
  selectedDate: string
  parkingBooking: ParkingBooking | null
  deskBooking: DeskBooking | null
  lunchBooking: LunchBooking | null
  setSelectedDate: (date: string) => void
  setParkingBooking: (b: ParkingBooking | null) => void
  setDeskBooking: (b: DeskBooking | null) => void
  setLunchBooking: (b: LunchBooking | null) => void
}

export const useBookingStore = create<BookingState>((set) => ({
  selectedDate: new Date().toISOString().split('T')[0],
  parkingBooking: null,
  deskBooking: null,
  lunchBooking: null,
  setSelectedDate: (date) => set({ selectedDate: date }),
  setParkingBooking: (b) => set({ parkingBooking: b }),
  setDeskBooking: (b) => set({ deskBooking: b }),
  setLunchBooking: (b) => set({ lunchBooking: b }),
}))
