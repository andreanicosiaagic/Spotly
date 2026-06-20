// Shared domain types for Spotly

export type ResourceStatus = 'available' | 'occupied' | 'pending' | 'reserved';

export type UserRole = 'Dipendente' | 'Manager' | 'Facility' | 'Admin';

export interface User {
  oid: string;
  name: string;
  email: string;
  roles: UserRole[];
  department?: string;
  parkingEligibility?: string[];
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
  createdAtUtc?: string;
  lockedUntil?: string;
  checkInOpensAtUtc?: string;
  checkInDeadlineUtc?: string;
  checkedInAtUtc?: string;
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
  createdAtUtc?: string;
  lockedUntil?: string;
  checkInOpensAtUtc?: string;
  checkInDeadlineUtc?: string;
  checkedInAtUtc?: string;
}

// M3 — Lunch
export interface Restaurant {
  locationId: string;
  restaurantId: string;
  name: string;
  bookingDate?: string;
  capacity: number;
  availableSeats: number;
  sequence: number;
  updatedAtUtc: string;
  partnerChannelConfigured: boolean;
  partnerSequence: number;
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
  status: 'active' | 'cancelled' | 'noshow';
  deliveryStatus: 'pending' | 'preparing' | 'delivered' | 'cancelled';
  partnerStatus?: 'notRequired' | 'pendingPartner' | 'confirmed' | 'rejected';
  partnerCode?: string;
  partnerReference?: string;
  partnerAvailableSeats?: number;
  menuItemIds: string[];
  createdAtUtc?: string;
  partnerPendingExpiresAtUtc?: string;
  partnerRespondedAtUtc?: string;
}

export interface RestaurantBookingResponse {
  booking: LunchBooking;
  partnerCode: string;
  availableSeats: number;
  partnerReference?: string;
}

export interface RestaurantMessageEvent {
  locationId: string;
  bookingDate: string;
  restaurantId: string;
  kind: string;
  outcome: string;
  sequence: number;
  receivedAtUtc: string;
}

export interface RestaurantAvailabilityUpdate extends Restaurant {
  source: string;
  bookingDate: string;
}

export interface LunchBoxEligibility {
  eligible: boolean;
  reason: string;
  cutoffLocal: string;
  restaurantsFull: boolean;
  outsideOperatingHours: boolean;
  demoDate: string;
}

export type WorkMode = 'office' | 'remote' | 'unknown';
export type CalendarAvailability = 'free' | 'tentative' | 'busy' | 'outOfOffice' | 'unknown';

export interface TeamMemberMatch {
  userId: string;
  displayName: string;
  workMode: WorkMode;
  locationId?: string;
  locationLabel?: string;
  calendarStatus: CalendarAvailability;
  isMatch: boolean;
  reason: string;
}

export interface TeamAvailabilityMatch {
  date: string;
  windowStartUtc: string;
  windowEndUtc: string;
  currentLocationId?: string;
  currentLocationLabel?: string;
  matchingMembers: number;
  members: TeamMemberMatch[];
}

// Availability update (SignalR payload)
export interface AvailabilityUpdate {
  resourceId: string;
  resourceType: 'parking' | 'desk';
  newStatus: ResourceStatus;
}

export type RealtimeStatus = 'connecting' | 'connected' | 'reconnecting' | 'offline';
