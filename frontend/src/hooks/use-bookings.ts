import { useQuery } from '@tanstack/react-query'
import { getOptionalJson } from '../lib/api'
import type { DeskBooking, LunchBooking, ParkingBooking } from '../types'

export function useParkingBooking(date: string) {
  return useQuery({
    queryKey: ['parking-booking', date],
    queryFn: () => getOptionalJson<ParkingBooking>(`/api/parking/bookings/me?date=${date}`),
  })
}

export function useDeskBooking(date: string) {
  return useQuery({
    queryKey: ['desk-booking', date],
    queryFn: () => getOptionalJson<DeskBooking>(`/api/desk/bookings/me?date=${date}`),
  })
}

export function useLunchBooking(date: string) {
  return useQuery({
    queryKey: ['lunch-booking', date],
    queryFn: () => getOptionalJson<LunchBooking>(`/api/lunch/bookings/me?date=${date}`),
  })
}
