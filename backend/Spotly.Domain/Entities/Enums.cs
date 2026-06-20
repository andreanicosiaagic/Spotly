namespace Spotly.Domain.Entities;

public enum ResourceStatus { Available, Occupied, Pending, Reserved }
public enum ParkingSpotType { Standard, Disabled, Ev, Guest }
public enum BookingStatus { Active, Cancelled, NoShow }
public enum DeliveryStatus { Pending, Preparing, Delivered, Cancelled }
public enum PartnerBookingStatus { NotRequired, PendingPartner, Confirmed, Rejected }
public enum PartnerMessageOutcome { Applied, Duplicate, Stale, Malformed, UnknownRestaurant }
