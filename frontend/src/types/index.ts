// Shared domain types for Spotly

export type ResourceStatus = 'available' | 'occupied' | 'pending' | 'reserved';

export type UserRole = 'Dipendente' | 'Manager' | 'Facility' | 'Admin';

export interface User {
  oid: string;
  name: string;
  email: string;
  roles: UserRole[];
}

// M1 — Parking
export type ParkingSpotType = 'standard' | 'disabled' | 'ev' | 'guest';

export interface ParkingSpot {
  spotId: string;
  locationId: string;
  level: number;
  spotNumber: string;
  type: ParkingSpotType;
  status: ResourceStatus;
}

export interface ParkingBooking {
  bookingId: string;
  spotId: string;
  userId: string;
  bookingDate: string; // ISO date
  status: 'active' | 'cancelled' | 'noshow';
  lockedUntil?: string;
}

// M2 — Desk
export interface DeskSpot {
  deskId: string;
  locationId: string;
  floor: number;
  zone: string;
  hasMonitor: boolean;
  isStanding: boolean;
  hasWindow: boolean;
  status: ResourceStatus;
}

export interface DeskBooking {
  bookingId: string;
  deskId: string;
  userId: string;
  bookingDate: string;
  status: 'active' | 'cancelled' | 'noshow';
  lockedUntil?: string;
}

// M3 — Lunch
export interface Restaurant {
  restaurantId: string;
  locationId: string;
  name: string;
  capacity: number;
}

export interface RestaurantSlot {
  slotId: string;
  restaurantId: string;
  slotTime: string; // e.g. "12:00"
  capacity: number;
  available: number;
  bookingDate: string;
}

export interface MenuItem {
  itemId: string;
  restaurantId: string;
  menuDate: string;
  name: string;
  category: 'primo' | 'secondo' | 'contorno' | 'dessert';
  allergens?: string;
}

export interface LunchBox {
  boxId: string;
  name: string;
  description: string;
  allergens?: string;
  isAvailable: boolean;
}

export interface LunchBooking {
  bookingId: string;
  restaurantId?: string;
  slotId?: string;
  userId: string;
  bookingDate: string;
  isLunchBox: boolean;
  lunchBoxId?: string;
  status: 'active' | 'cancelled';
  deliveryStatus: 'pending' | 'preparing' | 'delivered' | 'cancelled';
}

// Availability update (SignalR payload)
export interface AvailabilityUpdate {
  resourceId: string;
  resourceType: 'parking' | 'desk';
  newStatus: ResourceStatus;
}
