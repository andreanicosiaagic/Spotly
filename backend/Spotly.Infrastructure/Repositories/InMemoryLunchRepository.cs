using System.Data;
using Microsoft.EntityFrameworkCore;
using Spotly.Domain.Entities;
using Spotly.Domain.Interfaces;
using Spotly.Domain.Rules;
using Spotly.Infrastructure.Persistence;

namespace Spotly.Infrastructure.Repositories;

public sealed class InMemoryLunchRepository(IDbContextFactory<SpotlyDbContext> factory) : ILunchRepository
{
    private readonly Lock _gate = new();

    public Task<IEnumerable<Restaurant>> GetRestaurantsAsync(string locationId)
    {
        using var db = factory.CreateDbContext();
        return Task.FromResult<IEnumerable<Restaurant>>(db.Restaurants.AsNoTracking().Where(x => x.LocationId == locationId &&
            x.IsActive && x.WhatsAppNumber != null && x.WhatsAppNumber != "").ToArray());
    }

    public Task<IReadOnlyList<RestaurantAvailabilityView>> GetRestaurantAvailabilityAsync(string locationId, DateOnly date)
    {
        using var db = factory.CreateDbContext();
        var rows = (from restaurant in db.Restaurants.AsNoTracking()
                    join availability in db.RestaurantAvailabilities.AsNoTracking().Where(x => x.BookingDate == date)
                        on restaurant.RestaurantId equals availability.RestaurantId into availabilityGroup
                    from availability in availabilityGroup.DefaultIfEmpty()
                    where restaurant.LocationId == locationId && restaurant.IsActive && restaurant.WhatsAppNumber != null && restaurant.WhatsAppNumber != ""
                    select new { restaurant, availability }).ToList();
        var result = rows.Select(row => new RestaurantAvailabilityView(row.restaurant.RestaurantId, row.restaurant.Name,
            row.restaurant.Capacity, row.availability is null ? 0 : Math.Max(0, row.availability.AvailableSeats - row.availability.PendingSeats),
            row.availability?.Sequence ?? 0, row.availability?.UpdatedAtUtc ?? DateTime.MinValue, true)).ToArray();
        return Task.FromResult<IReadOnlyList<RestaurantAvailabilityView>>(result);
    }

    public Task<RestaurantMessageApplyResult> ApplyAvailabilityAsync(PartnerAvailabilityMessage message, string payload, DateTime receivedAtUtc)
    {
        lock (_gate)
        {
            using var db = factory.CreateDbContext();
            using var transaction = db.Database.IsRelational() ? db.Database.BeginTransaction(IsolationLevel.Serializable) : null;
            if (db.RestaurantPartnerMessages.Any(x => x.MessageId == message.MessageId))
                return Task.FromResult(new RestaurantMessageApplyResult(PartnerMessageOutcome.Duplicate));
            var restaurant = db.Restaurants.FirstOrDefault(x => x.RestaurantId == message.RestaurantId && x.IsActive &&
                x.WhatsAppNumber != null && x.WhatsAppNumber != "");
            if (restaurant is null)
            {
                SavePartnerMessage(db, message, payload, receivedAtUtc, PartnerMessageOutcome.UnknownRestaurant);
                transaction?.Commit();
                return Task.FromResult(new RestaurantMessageApplyResult(PartnerMessageOutcome.UnknownRestaurant));
            }
            var current = db.RestaurantAvailabilities.FirstOrDefault(x => x.RestaurantId == message.RestaurantId && x.BookingDate == message.BookingDate);
            var outcome = message.AvailableSeats > restaurant.Capacity ? PartnerMessageOutcome.Malformed
                : current is not null && message.Sequence <= current.Sequence ? PartnerMessageOutcome.Stale : PartnerMessageOutcome.Applied;
            if (outcome == PartnerMessageOutcome.Applied)
            {
                current ??= new RestaurantAvailability { RestaurantId = message.RestaurantId, BookingDate = message.BookingDate };
                if (db.Entry(current).State == EntityState.Detached) db.RestaurantAvailabilities.Add(current);
                current.AvailableSeats = message.AvailableSeats;
                current.Sequence = message.Sequence;
                current.LastMessageId = message.MessageId;
                current.UpdatedAtUtc = message.SentAtUtc;
                current.Source = "telegram-mock";
            }
            SavePartnerMessage(db, message, payload, receivedAtUtc, outcome);
            transaction?.Commit();
            var view = outcome == PartnerMessageOutcome.Applied
                ? new RestaurantAvailabilityView(restaurant.RestaurantId, restaurant.Name, restaurant.Capacity,
                    Math.Max(0, message.AvailableSeats - (current?.PendingSeats ?? 0)), message.Sequence, message.SentAtUtc, true)
                : null;
            return Task.FromResult(new RestaurantMessageApplyResult(outcome, view));
        }
    }

    public Task<RestaurantBookingStart> BeginRestaurantBookingAsync(LunchBooking booking)
    {
        lock (_gate)
        {
            using var db = factory.CreateDbContext();
            using var transaction = db.Database.IsRelational() ? db.Database.BeginTransaction(IsolationLevel.Serializable) : null;
            var restaurant = db.Restaurants.FirstOrDefault(x => x.RestaurantId == booking.RestaurantId && x.IsActive &&
                x.WhatsAppNumber != null && x.WhatsAppNumber != "");
            if (restaurant is null) return Task.FromResult(new RestaurantBookingStart(new(null, BookingFailure.NotFound), 0));
            if (db.LunchBookings.Any(x => x.UserId == booking.UserId && x.BookingDate == booking.BookingDate && x.Status == BookingStatus.Active))
                return Task.FromResult(new RestaurantBookingStart(new(null, BookingFailure.AlreadyBooked), 0));
            var availability = db.RestaurantAvailabilities.FirstOrDefault(x => x.RestaurantId == booking.RestaurantId && x.BookingDate == booking.BookingDate);
            if (availability is null || availability.AvailableSeats - availability.PendingSeats <= 0)
                return Task.FromResult(new RestaurantBookingStart(new(null, BookingFailure.ResourceUnavailable), 0));
            availability.PendingSeats++;
            booking.PartnerStatus = PartnerBookingStatus.PendingPartner;
            booking.PartnerCorrelationId = Guid.NewGuid().ToString("N");
            db.LunchBookings.Add(booking);
            db.SaveChanges(); transaction?.Commit();
            return Task.FromResult(new RestaurantBookingStart(new(booking, BookingFailure.None), availability.AvailableSeats));
        }
    }

    public Task<BookingAttempt<LunchBooking>> CompleteRestaurantBookingAsync(string correlationId, PartnerBookingResult result, DateTime respondedAtUtc)
    {
        lock (_gate)
        {
            using var db = factory.CreateDbContext();
            using var transaction = db.Database.IsRelational() ? db.Database.BeginTransaction(IsolationLevel.Serializable) : null;
            var booking = db.LunchBookings.FirstOrDefault(x => x.PartnerCorrelationId == correlationId && x.PartnerStatus == PartnerBookingStatus.PendingPartner);
            if (booking is null || booking.RestaurantId != result.RestaurantId || booking.BookingDate != result.BookingDate)
                return Task.FromResult(new BookingAttempt<LunchBooking>(null, BookingFailure.NotFound));
            var availability = db.RestaurantAvailabilities.First(x => x.RestaurantId == booking.RestaurantId && x.BookingDate == booking.BookingDate);
            availability.PendingSeats = Math.Max(0, availability.PendingSeats - 1);
            availability.AvailableSeats = result.RemainingSeats;
            availability.UpdatedAtUtc = respondedAtUtc;
            booking.PartnerCode = result.Code;
            booking.PartnerReference = result.PartnerReference;
            booking.PartnerAvailableSeats = result.RemainingSeats;
            booking.PartnerRespondedAtUtc = respondedAtUtc;
            booking.PartnerStatus = result.Confirmed ? PartnerBookingStatus.Confirmed : PartnerBookingStatus.Rejected;
            if (!result.Confirmed) booking.Status = BookingStatus.Cancelled;
            db.SaveChanges(); transaction?.Commit();
            return Task.FromResult(new BookingAttempt<LunchBooking>(booking,
                result.Confirmed ? BookingFailure.None : BookingFailure.ResourceUnavailable));
        }
    }

    public Task<IReadOnlyList<RestaurantPartnerMessage>> GetRecentPartnerMessagesAsync(int take)
    {
        using var db = factory.CreateDbContext();
        return Task.FromResult<IReadOnlyList<RestaurantPartnerMessage>>(db.RestaurantPartnerMessages.AsNoTracking()
            .OrderByDescending(x => x.ReceivedAtUtc).Take(Math.Clamp(take, 1, 50)).ToArray());
    }

    public Task<IEnumerable<RestaurantSlot>> GetSlotsByDateAsync(string locationId, DateOnly date, string? restaurantId = null)
    {
        using var db = factory.CreateDbContext();
        var ids = db.Restaurants.Where(x => x.LocationId == locationId).Select(x => x.RestaurantId).ToHashSet();
        return Task.FromResult<IEnumerable<RestaurantSlot>>(db.RestaurantSlots.AsNoTracking().Where(x => x.BookingDate == date && ids.Contains(x.RestaurantId) &&
            (restaurantId == null || x.RestaurantId == restaurantId)).ToArray());
    }

    public Task<IEnumerable<MenuItem>> GetMenuAsync(DateOnly date, string? restaurantId = null)
    {
        using var db = factory.CreateDbContext();
        return Task.FromResult<IEnumerable<MenuItem>>(db.MenuItems.AsNoTracking().Where(x => x.MenuDate == date &&
            (restaurantId == null || x.RestaurantId == restaurantId)).ToArray());
    }

    public Task<IEnumerable<LunchBoxCatalog>> GetLunchBoxesAsync()
    {
        using var db = factory.CreateDbContext();
        return Task.FromResult<IEnumerable<LunchBoxCatalog>>(db.LunchBoxes.AsNoTracking().Where(x => x.IsAvailable).ToArray());
    }

    public Task<LunchBooking?> GetActiveBookingAsync(string userId, DateOnly date)
    {
        using var db = factory.CreateDbContext();
        return Task.FromResult(db.LunchBookings.AsNoTracking().FirstOrDefault(x => x.UserId == userId && x.BookingDate == date && x.Status == BookingStatus.Active));
    }

    public Task<BookingAttempt<LunchBooking>> TryCreateBookingAsync(LunchBooking booking, bool isOutsideHours)
    {
        lock (_gate)
        {
            using var db = factory.CreateDbContext();
            if (db.LunchBookings.Any(x => x.UserId == booking.UserId && x.BookingDate == booking.BookingDate && x.Status == BookingStatus.Active))
                return Task.FromResult(new BookingAttempt<LunchBooking>(null, BookingFailure.AlreadyBooked));
            if (booking.IsLunchBox)
            {
                if (!db.LunchBoxes.Any(x => x.BoxId == booking.LunchBoxId && x.IsAvailable))
                    return Task.FromResult(new BookingAttempt<LunchBooking>(null, BookingFailure.NotFound));
                var restaurantHasAvailability = db.RestaurantAvailabilities.Any(x => x.BookingDate == booking.BookingDate &&
                    x.AvailableSeats - x.PendingSeats > 0);
                if (BookingRules.ValidateLunchBoxEligibility(restaurantHasAvailability, isOutsideHours) is not null)
                    return Task.FromResult(new BookingAttempt<LunchBooking>(null, BookingFailure.Ineligible));
            }
            else
            {
                var slot = db.RestaurantSlots.FirstOrDefault(x => x.SlotId == booking.SlotId && x.BookingDate == booking.BookingDate &&
                    x.RestaurantId == booking.RestaurantId);
                if (slot is null) return Task.FromResult(new BookingAttempt<LunchBooking>(null, BookingFailure.NotFound));
                if (slot.Available <= 0) return Task.FromResult(new BookingAttempt<LunchBooking>(null, BookingFailure.ResourceUnavailable));
                slot.Available--;
            }
            db.LunchBookings.Add(booking); db.SaveChanges();
            return Task.FromResult(new BookingAttempt<LunchBooking>(booking, BookingFailure.None));
        }
    }

    public Task<CancellationOutcome> CancelBookingAsync(string bookingId, string userId, DateTime utcNow)
    {
        lock (_gate)
        {
            using var db = factory.CreateDbContext();
            var booking = db.LunchBookings.FirstOrDefault(x => x.BookingId == bookingId && x.UserId == userId && x.Status == BookingStatus.Active);
            if (booking is null) return Task.FromResult(new CancellationOutcome(false));
            booking.Status = BookingRules.IsFreeCancellation(booking.BookingDate, utcNow) ? BookingStatus.Cancelled : BookingStatus.NoShow;
            booking.DeliveryStatus = DeliveryStatus.Cancelled;
            if (booking.RestaurantId is not null && booking.PartnerStatus == PartnerBookingStatus.Confirmed)
            {
                var availability = db.RestaurantAvailabilities.FirstOrDefault(x => x.RestaurantId == booking.RestaurantId && x.BookingDate == booking.BookingDate);
                var capacity = db.Restaurants.Where(x => x.RestaurantId == booking.RestaurantId).Select(x => x.Capacity).FirstOrDefault();
                if (availability is not null) availability.AvailableSeats = Math.Min(capacity, availability.AvailableSeats + 1);
            }
            if (!booking.IsLunchBox)
            {
                var slot = db.RestaurantSlots.FirstOrDefault(x => x.SlotId == booking.SlotId && x.BookingDate == booking.BookingDate);
                if (slot is not null && slot.Available < slot.Capacity) slot.Available++;
            }
            db.SaveChanges();
            return Task.FromResult(new CancellationOutcome(true, booking.Status,
                booking.RestaurantId ?? booking.SlotId ?? booking.LunchBoxId, booking.BookingDate));
        }
    }

    private static void SavePartnerMessage(SpotlyDbContext db, PartnerAvailabilityMessage message, string payload,
        DateTime receivedAtUtc, PartnerMessageOutcome outcome)
    {
        db.RestaurantPartnerMessages.Add(new RestaurantPartnerMessage
        {
            MessageId = message.MessageId, RestaurantId = message.RestaurantId, Kind = "AVAIL", Sequence = message.Sequence,
            ReceivedAtUtc = receivedAtUtc, Outcome = outcome, Payload = payload,
        });
        db.SaveChanges();
    }
}
