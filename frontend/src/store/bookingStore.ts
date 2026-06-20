import { create } from 'zustand'
import { getTodayDateKey } from '../lib/date'

interface BookingState {
  selectedDate: string
  setSelectedDate: (date: string) => void
}

export const useBookingStore = create<BookingState>((set) => ({
  selectedDate: getTodayDateKey(),
  setSelectedDate: (date) => set({ selectedDate: date }),
}))
