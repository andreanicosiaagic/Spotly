using System.Data;
using Microsoft.EntityFrameworkCore;
using Spotly.Domain.Entities;
using Spotly.Domain.Interfaces;
using Spotly.Domain.Rules;
using Spotly.Infrastructure.Persistence;

namespace Spotly.Infrastructure.Repositories;

public sealed class InMemoryLunchRepository(IDbContextFactory<SpotlyDbContext> factory, TimeProvider clock) : ILunchRepository
{
    private static readonly Lock Gate = new();

    public Task<IEnumerable<Restaurant>> GetRestaurantsAsync(string locationId)
    {
        using var db = factory.CreateDbContext();
        return Task.FromResult<IEnumerable<Restaurant>>(db.Restaurants.AsNoTracking()
            .Where(x => x.LocationId == locationId && x.IsActive && !string.IsNullOrWhiteSpace(x.WhatsAppNumber))
            .ToArray());
    }

    public Task<IReadOnlyList<RestaurantAvailabilityView>> GetRestaurantAvailabilityAsync(string locationId, DateOnly date)
    {
        using var db = factory.CreateDbContext();
        var rows = (from restaurant in db.Restaurants.AsNoTracking()
                    join availability in db.RestaurantAvailabilities.AsNoTracking().Where(x => x.BookingDate == date)
                        on restaurant.RestaurantId equals availability.RestaurantId into availabilityGroup
                    from availability in availabilityGroup.DefaultIfEmpty()
                    where restaurant.LocationId == locationId && restaurant.IsActive && !string.IsNullOrWhiteSpace(restaurant.WhatsAppNumber)
                    orderby restaurant.Name
                    select new { restaurant, availability })
            .ToList();

        var result = rows.Select(row =>
        {
            var availableSeats = row.availability is null ? 0 : Math.Max(0, row.availability.AvailableSeats - row.availability.PendingSeats);
            return new RestaurantAvailabilityView(
                row.restaurant.LocationId,
                row.restaurant.RestaurantId,
                row.restaurant.Name,
                row.restaurant.Capacity,
                availableSeats,
                row.availability?.Sequence ?? 0,
                row.availability?.UpdatedAtUtc ?? DateTime.MinValue,
                true,
                row.availability?.LastPartnerSequence ?? 0);
        }).ToArray();

        return Task.FromResult<IReadOnlyList<RestaurantAvailabilityView>>(result);
    }

    public Task<RestaurantMessageApplyResult> ApplyAvailabilityAsync(PartnerAvailabilityMessage message, string payload, DateTime receivedAtUtc)
    {
        lock (Gate)
        {
            using var db = factory.CreateDbContext();
            using var transaction = db.Database.IsRelational() ? db.Database.BeginTransaction(IsolationLevel.Serializable) : null;

            if (db.RestaurantPartnerMessages.Any(x => x.MessageId == message.MessageId))
                return Task.FromResult(new RestaurantMessageApplyResult(PartnerMessageOutcome.Duplicate));

            var restaurant = db.Restaurants.FirstOrDefault(x => x.RestaurantId == message.RestaurantId && x.IsActive && !string.IsNullOrWhiteSpace(x.WhatsAppNumber));
            if (restaurant is null)
            {
                SavePartnerMessage(db, string.Empty, message, payload, receivedAtUtc, PartnerMessageOutcome.UnknownRestaurant);
                transaction?.Commit();
                return Task.FromResult(new RestaurantMessageApplyResult(PartnerMessageOutcome.UnknownRestaurant));
            }

            var availability = db.RestaurantAvailabilities.FirstOrDefault(x => x.RestaurantId == message.RestaurantId && x.BookingDate == message.BookingDate);
            var outcome = message.AvailableSeats > restaurant.Capacity
                ? PartnerMessageOutcome.Malformed
                : availability is not null && message.Sequence <= availability.LastPartnerSequence
                    ? PartnerMessageOutcome.Stale
                    : PartnerMessageOutcome.Applied;

            if (outcome == PartnerMessageOutcome.Applied)
            {
                availability ??= new RestaurantAvailability
                {
                    RestaurantId = message.RestaurantId,
                    BookingDate = message.BookingDate,
                };

                if (db.Entry(availability).State == EntityState.Detached) db.RestaurantAvailabilities.Add(availability);
                availability.AvailableSeats = message.AvailableSeats;
                availability.LastPartnerSequence = message.Sequence;
                availability.Sequence = NextSequence(availability);
                availability.LastMessageId = message.MessageId;
                availability.UpdatedAtUtc = message.SentAtUtc;
                availability.Source = "telegram-mock";
            }

            SavePartnerMessage(db, restaurant.LocationId, message, payload, receivedAtUtc, outcome);
            transaction?.Commit();

            var view = outcome == PartnerMessageOutcome.Applied && availability is not null
                ? new RestaurantAvailabilityView(
                    restaurant.LocationId,
                    restaurant.RestaurantId,
                    restaurant.Name,
                    restaurant.Capacity,
                    Math.Max(0, availability.AvailableSeats - availability.PendingSeats),
                    availability.Sequence,
                    availability.UpdatedAtUtc,
                    true,
                    availability.LastPartnerSequence)
                : null;

            return Task.FromResult(new RestaurantMessageApplyResult(outcome, view));
        }
    }

    public Task<RestaurantBookingStart> BeginRestaurantBookingAsync(LunchBooking booking)
    {
        lock (Gate)
        {
            using var db = factory.CreateDbContext();
            using var transaction = db.Database.IsRelational() ? db.Database.BeginTransaction(IsolationLevel.Serializable) : null;

            var restaurant = db.Restaurants.FirstOrDefault(x => x.RestaurantId == booking.RestaurantId && x.IsActive && !string.IsNullOrWhiteSpace(x.WhatsAppNumber));
            if (restaurant is null) return Task.FromResult(new RestaurantBookingStart(new(null, BookingFailure.NotFound), 0));
            if (string.IsNullOrWhiteSpace(booking.SlotId)) return Task.FromResult(new RestaurantBookingStart(new(null, BookingFailure.NotFound), 0));
            if (db.LunchBookings.Any(x => x.UserId == booking.UserId && x.BookingDate == booking.BookingDate && x.Status == BookingStatus.Active))
                return Task.FromResult(new RestaurantBookingStart(new(null, BookingFailure.AlreadyBooked), 0));

            var slot = db.RestaurantSlots.FirstOrDefault(x =>
                x.SlotId == booking.SlotId &&
                x.BookingDate == booking.BookingDate &&
                x.RestaurantId == booking.RestaurantId);
            if (slot is null) return Task.FromResult(new RestaurantBookingStart(new(null, BookingFailure.NotFound), 0));
            if (slot.Available <= 0) return Task.FromResult(new RestaurantBookingStart(new(null, BookingFailure.ResourceUnavailable), 0));
            if (!MenusBelongToRestaurant(db, booking.BookingDate, booking.RestaurantId!, booking.MenuItemIds))
                return Task.FromResult(new RestaurantBookingStart(new(null, BookingFailure.NotFound), 0));

            var availability = db.RestaurantAvailabilities.FirstOrDefault(x => x.RestaurantId == booking.RestaurantId && x.BookingDate == booking.BookingDate);
            if (availability is null || availability.AvailableSeats - availability.PendingSeats <= 0)
                return Task.FromResult(new RestaurantBookingStart(new(null, BookingFailure.ResourceUnavailable), 0));

            availability.PendingSeats++;
            availability.Sequence = NextSequence(availability);
            availability.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
            availability.Source = "booking-pending";
            slot.Available--;

            booking.CreatedAtUtc = booking.CreatedAtUtc == default ? clock.GetUtcNow().UtcDateTime : booking.CreatedAtUtc;
            booking.PartnerStatus = PartnerBookingStatus.PendingPartner;
            booking.PartnerCorrelationId = Guid.NewGuid().ToString("N");
            booking.PartnerPendingExpiresAtUtc = clock.GetUtcNow().UtcDateTime.AddMinutes(3);
            db.LunchBookings.Add(booking);
            db.SaveChanges();
            transaction?.Commit();

            return Task.FromResult(new RestaurantBookingStart(new(booking, BookingFailure.None), availability.AvailableSeats));
        }
    }

    public Task<BookingAttempt<LunchBooking>> CompleteRestaurantBookingAsync(string correlationId, PartnerBookingResult result, DateTime respondedAtUtc)
    {
        lock (Gate)
        {
            using var db = factory.CreateDbContext();
            using var transaction = db.Database.IsRelational() ? db.Database.BeginTransaction(IsolationLevel.Serializable) : null;

            var booking = db.LunchBookings.FirstOrDefault(x => x.PartnerCorrelationId == correlationId && x.PartnerStatus == PartnerBookingStatus.PendingPartner);
            if (booking is null || booking.RestaurantId != result.RestaurantId || booking.BookingDate != result.BookingDate)
                return Task.FromResult(new BookingAttempt<LunchBooking>(null, BookingFailure.NotFound));

            var availability = db.RestaurantAvailabilities.First(x => x.RestaurantId == booking.RestaurantId && x.BookingDate == booking.BookingDate);
            var slot = string.IsNullOrWhiteSpace(booking.SlotId)
                ? null
                : db.RestaurantSlots.FirstOrDefault(x => x.SlotId == booking.SlotId && x.BookingDate == booking.BookingDate);

            availability.PendingSeats = Math.Max(0, availability.PendingSeats - 1);
            availability.AvailableSeats = result.RemainingSeats;
            availability.Sequence = NextSequence(availability);
            availability.UpdatedAtUtc = respondedAtUtc;
            availability.Source = "booking-result";

            booking.PartnerCode = result.Code;
            booking.PartnerReference = result.PartnerReference;
            booking.PartnerAvailableSeats = result.RemainingSeats;
            booking.PartnerRespondedAtUtc = respondedAtUtc;
            booking.PartnerPendingExpiresAtUtc = null;
            booking.PartnerStatus = result.Confirmed ? PartnerBookingStatus.Confirmed : PartnerBookingStatus.Rejected;

            if (!result.Confirmed)
            {
                booking.Status = BookingStatus.Cancelled;
                booking.DeliveryStatus = DeliveryStatus.Cancelled;
                if (slot is not null && slot.Available < slot.Capacity) slot.Available++;
            }

            db.SaveChanges();
            transaction?.Commit();
            return Task.FromResult(new BookingAttempt<LunchBooking>(booking,
                result.Confirmed ? BookingFailure.None : BookingFailure.ResourceUnavailable));
        }
    }

    public Task<IReadOnlyList<RestaurantPartnerMessage>> GetRecentPartnerMessagesAsync(DateOnly? date, int take)
    {
        using var db = factory.CreateDbContext();
        var query = db.RestaurantPartnerMessages.AsNoTracking();
        if (date is not null) query = query.Where(x => x.BookingDate == date.Value);
        return Task.FromResult<IReadOnlyList<RestaurantPartnerMessage>>(query
            .OrderByDescending(x => x.ReceivedAtUtc)
            .Take(Math.Clamp(take, 1, 50))
            .ToArray());
    }

    public Task<IEnumerable<RestaurantSlot>> GetSlotsByDateAsync(string locationId, DateOnly date, string? restaurantId = null)
    {
        using var db = factory.CreateDbContext();
        var ids = db.Restaurants.Where(x => x.LocationId == locationId).Select(x => x.RestaurantId).ToHashSet();
        return Task.FromResult<IEnumerable<RestaurantSlot>>(db.RestaurantSlots.AsNoTracking()
            .Where(x => x.BookingDate == date && ids.Contains(x.RestaurantId) && (restaurantId == null || x.RestaurantId == restaurantId))
            .OrderBy(x => x.RestaurantId)
            .ThenBy(x => x.SlotTime)
            .ToArray());
    }

    public Task<IEnumerable<MenuItem>> GetMenuAsync(DateOnly date, string? restaurantId = null)
    {
        using var db = factory.CreateDbContext();
        return Task.FromResult<IEnumerable<MenuItem>>(db.MenuItems.AsNoTracking()
            .Where(x => x.MenuDate == date && (restaurantId == null || x.RestaurantId == restaurantId))
            .OrderBy(x => x.RestaurantId)
            .ThenBy(x => x.Category)
            .ThenBy(x => x.Name)
            .ToArray());
    }

    public Task<IEnumerable<LunchBoxCatalog>> GetLunchBoxesAsync()
    {
        using var db = factory.CreateDbContext();
        return Task.FromResult<IEnumerable<LunchBoxCatalog>>(db.LunchBoxes.AsNoTracking().Where(x => x.IsAvailable).ToArray());
    }

    public Task<LunchBooking?> GetActiveBookingAsync(string userId, DateOnly date)
    {
        using var db = factory.CreateDbContext();
        return Task.FromResult(db.LunchBookings.AsNoTracking().FirstOrDefault(x =>
            x.UserId == userId && x.BookingDate == date && x.Status == BookingStatus.Active));
    }

    public Task<LunchBooking?> GetUserBookingAsync(string bookingId, string userId)
    {
        using var db = factory.CreateDbContext();
        return Task.FromResult(db.LunchBookings.AsNoTracking().FirstOrDefault(x => x.BookingId == bookingId && x.UserId == userId));
    }

    public Task<BookingAttempt<LunchBooking>> TryCreateBookingAsync(LunchBooking booking, bool isOutsideHours)
    {
        lock (Gate)
        {
            using var db = factory.CreateDbContext();
            using var transaction = db.Database.IsRelational() ? db.Database.BeginTransaction(IsolationLevel.Serializable) : null;

            if (db.LunchBookings.Any(x => x.UserId == booking.UserId && x.BookingDate == booking.BookingDate && x.Status == BookingStatus.Active))
                return Task.FromResult(new BookingAttempt<LunchBooking>(null, BookingFailure.AlreadyBooked));

            booking.CreatedAtUtc = booking.CreatedAtUtc == default ? clock.GetUtcNow().UtcDateTime : booking.CreatedAtUtc;

            if (booking.IsLunchBox)
            {
                if (!db.LunchBoxes.Any(x => x.BoxId == booking.LunchBoxId && x.IsAvailable))
                    return Task.FromResult(new BookingAttempt<LunchBooking>(null, BookingFailure.NotFound));
                var restaurantHasAvailability = db.RestaurantAvailabilities.Any(x => x.BookingDate == booking.BookingDate && x.AvailableSeats - x.PendingSeats > 0);
                if (BookingRules.ValidateLunchBoxEligibility(restaurantHasAvailability, isOutsideHours) is not null)
                    return Task.FromResult(new BookingAttempt<LunchBooking>(null, BookingFailure.Ineligible));
            }
            else
            {
                var slot = db.RestaurantSlots.FirstOrDefault(x => x.SlotId == booking.SlotId && x.BookingDate == booking.BookingDate && x.RestaurantId == booking.RestaurantId);
                if (slot is null) return Task.FromResult(new BookingAttempt<LunchBooking>(null, BookingFailure.NotFound));
                if (slot.Available <= 0) return Task.FromResult(new BookingAttempt<LunchBooking>(null, BookingFailure.ResourceUnavailable));
                if (!MenusBelongToRestaurant(db, booking.BookingDate, booking.RestaurantId!, booking.MenuItemIds))
                    return Task.FromResult(new BookingAttempt<LunchBooking>(null, BookingFailure.NotFound));
                var availability = db.RestaurantAvailabilities.FirstOrDefault(x => x.RestaurantId == booking.RestaurantId && x.BookingDate == booking.BookingDate);
                if (availability is null || availability.AvailableSeats - availability.PendingSeats <= 0)
                    return Task.FromResult(new BookingAttempt<LunchBooking>(null, BookingFailure.ResourceUnavailable));
                slot.Available--;
                availability.AvailableSeats = Math.Max(0, availability.AvailableSeats - 1);
                availability.Sequence = NextSequence(availability);
                availability.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
                availability.Source = "local-booking";
            }

            db.LunchBookings.Add(booking);
            db.SaveChanges();
            transaction?.Commit();
            return Task.FromResult(new BookingAttempt<LunchBooking>(booking, BookingFailure.None));
        }
    }

    public Task<CancellationOutcome> CancelBookingAsync(string bookingId, string userId, DateTime currentLocalTime)
    {
        lock (Gate)
        {
            using var db = factory.CreateDbContext();
            var booking = db.LunchBookings.FirstOrDefault(x => x.BookingId == bookingId && x.UserId == userId && x.Status == BookingStatus.Active);
            if (booking is null) return Task.FromResult(new CancellationOutcome(false));

            booking.Status = BookingRules.IsFreeCancellation(booking.BookingDate, currentLocalTime) ? BookingStatus.Cancelled : BookingStatus.NoShow;
            booking.DeliveryStatus = DeliveryStatus.Cancelled;
            booking.PartnerPendingExpiresAtUtc = null;

            var nowUtc = clock.GetUtcNow().UtcDateTime;
            var slot = string.IsNullOrWhiteSpace(booking.SlotId)
                ? null
                : db.RestaurantSlots.FirstOrDefault(x => x.SlotId == booking.SlotId && x.BookingDate == booking.BookingDate);

            if (booking.RestaurantId is not null)
            {
                var availability = db.RestaurantAvailabilities.FirstOrDefault(x => x.RestaurantId == booking.RestaurantId && x.BookingDate == booking.BookingDate);
                var capacity = db.Restaurants.Where(x => x.RestaurantId == booking.RestaurantId).Select(x => x.Capacity).FirstOrDefault();
                if (availability is not null)
                {
                    if (booking.PartnerStatus == PartnerBookingStatus.PendingPartner)
                    {
                        availability.PendingSeats = Math.Max(0, availability.PendingSeats - 1);
                    }
                    else if (booking.PartnerStatus == PartnerBookingStatus.Confirmed)
                    {
                        availability.AvailableSeats = Math.Min(capacity, availability.AvailableSeats + 1);
                    }

                    availability.Sequence = NextSequence(availability);
                    availability.UpdatedAtUtc = nowUtc;
                    availability.Source = "cancellation";
                }
            }

            if (!booking.IsLunchBox && slot is not null && slot.Available < slot.Capacity) slot.Available++;
            db.SaveChanges();

            return Task.FromResult(new CancellationOutcome(true, booking.Status,
                booking.RestaurantId ?? booking.SlotId ?? booking.LunchBoxId, booking.BookingDate));
        }
    }

    public Task<IReadOnlyList<ReleasedResource>> ReleaseExpiredPendingPartnerBookingsAsync(DateTime utcNow)
    {
        lock (Gate)
        {
            using var db = factory.CreateDbContext();
            var expired = db.LunchBookings
                .Where(x => x.Status == BookingStatus.Active &&
                    x.PartnerStatus == PartnerBookingStatus.PendingPartner &&
                    x.PartnerPendingExpiresAtUtc != null &&
                    x.PartnerPendingExpiresAtUtc < utcNow)
                .ToList();

            foreach (var booking in expired)
            {
                booking.Status = BookingStatus.Cancelled;
                booking.DeliveryStatus = DeliveryStatus.Cancelled;
                booking.PartnerStatus = PartnerBookingStatus.Rejected;
                booking.PartnerCode = "EXPIRED";
                booking.PartnerRespondedAtUtc = utcNow;
                booking.PartnerPendingExpiresAtUtc = null;

                if (booking.RestaurantId is not null)
                {
                    var availability = db.RestaurantAvailabilities.FirstOrDefault(x => x.RestaurantId == booking.RestaurantId && x.BookingDate == booking.BookingDate);
                    if (availability is not null)
                    {
                        availability.PendingSeats = Math.Max(0, availability.PendingSeats - 1);
                        availability.Sequence = NextSequence(availability);
                        availability.UpdatedAtUtc = utcNow;
                        availability.Source = "pending-expired";
                    }
                }

                if (!string.IsNullOrWhiteSpace(booking.SlotId))
                {
                    var slot = db.RestaurantSlots.FirstOrDefault(x => x.SlotId == booking.SlotId && x.BookingDate == booking.BookingDate);
                    if (slot is not null && slot.Available < slot.Capacity) slot.Available++;
                }
            }

            db.SaveChanges();

            return Task.FromResult<IReadOnlyList<ReleasedResource>>(expired
                .Where(x => x.RestaurantId is not null)
                .Select(x => new ReleasedResource(x.RestaurantId!, x.BookingDate))
                .ToArray());
        }
    }

    private static long NextSequence(RestaurantAvailability availability) => Math.Max(availability.Sequence, availability.LastPartnerSequence) + 1;

    private static bool MenusBelongToRestaurant(SpotlyDbContext db, DateOnly bookingDate, string restaurantId, IReadOnlyCollection<string> menuItemIds)
    {
        if (menuItemIds.Count == 0) return false;
        var count = db.MenuItems.Count(x => x.MenuDate == bookingDate && x.RestaurantId == restaurantId && menuItemIds.Contains(x.ItemId));
        return count == menuItemIds.Count;
    }

    private static void SavePartnerMessage(
        SpotlyDbContext db,
        string locationId,
        PartnerAvailabilityMessage message,
        string payload,
        DateTime receivedAtUtc,
        PartnerMessageOutcome outcome)
    {
        db.RestaurantPartnerMessages.Add(new RestaurantPartnerMessage
        {
            MessageId = message.MessageId,
            LocationId = locationId,
            RestaurantId = message.RestaurantId,
            BookingDate = message.BookingDate,
            Kind = "AVAIL",
            Sequence = message.Sequence,
            ReceivedAtUtc = receivedAtUtc,
            Outcome = outcome,
            Payload = payload,
        });
        db.SaveChanges();
    }
}
