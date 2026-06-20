using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Spotly.Domain.Entities;
using Spotly.Domain.Rules;
using Spotly.Infrastructure.Seed;
using System.Text.Json;

namespace Spotly.Infrastructure.Persistence;

public sealed class SpotlyDbContext(DbContextOptions<SpotlyDbContext> options) : DbContext(options)
{
    public DbSet<ParkingSpot> ParkingSpots => Set<ParkingSpot>();
    public DbSet<ParkingBooking> ParkingBookings => Set<ParkingBooking>();
    public DbSet<DeskSpot> DeskSpots => Set<DeskSpot>();
    public DbSet<DeskBooking> DeskBookings => Set<DeskBooking>();
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<RestaurantSlot> RestaurantSlots => Set<RestaurantSlot>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<LunchBoxCatalog> LunchBoxes => Set<LunchBoxCatalog>();
    public DbSet<LunchBooking> LunchBookings => Set<LunchBooking>();
    public DbSet<RestaurantAvailability> RestaurantAvailabilities => Set<RestaurantAvailability>();
    public DbSet<RestaurantPartnerMessage> RestaurantPartnerMessages => Set<RestaurantPartnerMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ParkingSpot>().HasKey(x => x.SpotId);
        modelBuilder.Entity<ParkingBooking>().HasKey(x => x.BookingId);
        modelBuilder.Entity<ParkingBooking>().HasIndex(x => new { x.UserId, x.BookingDate }).IsUnique().HasFilter($"[{nameof(ParkingBooking.Status)}] = 0");
        modelBuilder.Entity<ParkingBooking>().HasIndex(x => new { x.SpotId, x.BookingDate }).IsUnique().HasFilter($"[{nameof(ParkingBooking.Status)}] = 0");
        modelBuilder.Entity<DeskSpot>().HasKey(x => x.DeskId);
        modelBuilder.Entity<DeskBooking>().HasKey(x => x.BookingId);
        modelBuilder.Entity<DeskBooking>().HasIndex(x => new { x.UserId, x.BookingDate }).IsUnique().HasFilter($"[{nameof(DeskBooking.Status)}] = 0");
        modelBuilder.Entity<DeskBooking>().HasIndex(x => new { x.DeskId, x.BookingDate }).IsUnique().HasFilter($"[{nameof(DeskBooking.Status)}] = 0");
        modelBuilder.Entity<Restaurant>().HasKey(x => x.RestaurantId);
        modelBuilder.Entity<RestaurantSlot>().HasKey(x => x.SlotId);
        modelBuilder.Entity<RestaurantSlot>().HasIndex(x => new { x.RestaurantId, x.BookingDate, x.SlotTime }).IsUnique();
        modelBuilder.Entity<MenuItem>().HasKey(x => x.ItemId);
        modelBuilder.Entity<MenuItem>().HasIndex(x => new { x.RestaurantId, x.MenuDate, x.Name }).IsUnique();
        modelBuilder.Entity<LunchBoxCatalog>().HasKey(x => x.BoxId);
        modelBuilder.Entity<LunchBooking>().HasKey(x => x.BookingId);
        modelBuilder.Entity<LunchBooking>().HasIndex(x => new { x.UserId, x.BookingDate }).IsUnique().HasFilter($"[{nameof(LunchBooking.Status)}] = 0");
        modelBuilder.Entity<LunchBooking>().HasIndex(x => x.PartnerCorrelationId).IsUnique();
        modelBuilder.Entity<LunchBooking>().Property(x => x.MenuItemIds)
            .HasConversion(
                value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                value => string.IsNullOrWhiteSpace(value) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(value) ?? new List<string>())
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (left, right) => left != null && right != null && left.SequenceEqual(right),
                value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode(StringComparison.Ordinal))),
                value => value.ToList()));
        modelBuilder.Entity<RestaurantAvailability>().HasKey(x => new { x.RestaurantId, x.BookingDate });
        modelBuilder.Entity<RestaurantAvailability>().HasIndex(x => new { x.RestaurantId, x.BookingDate }).IsUnique();
        modelBuilder.Entity<RestaurantPartnerMessage>().HasKey(x => x.MessageId);
        modelBuilder.Entity<RestaurantPartnerMessage>().HasIndex(x => new { x.LocationId, x.BookingDate, x.ReceivedAtUtc });
    }
}

public static class SpotlyDbSeeder
{
    public static async Task SeedAsync(SpotlyDbContext db, DateOnly today)
    {
        if (!await db.ParkingSpots.AnyAsync()) db.ParkingSpots.AddRange(SeedData.ParkingSpots());
        if (!await db.DeskSpots.AnyAsync()) db.DeskSpots.AddRange(SeedData.DeskSpots());
        if (!await db.Restaurants.AnyAsync()) db.Restaurants.AddRange(SeedData.Restaurants());
        if (!await db.LunchBoxes.AnyAsync()) db.LunchBoxes.AddRange(SeedData.LunchBoxes());
        await EnsureBookingWindowAsync(db, today);
        await db.SaveChangesAsync();
    }

    public static async Task EnsureBookingWindowAsync(SpotlyDbContext db, DateOnly anchorDate)
    {
        for (var offset = 0; offset <= BookingRules.MaxBookingWindowDays; offset++)
        {
            var date = anchorDate.AddDays(offset);
            if (!await db.RestaurantSlots.AnyAsync(x => x.BookingDate == date)) db.RestaurantSlots.AddRange(SeedData.RestaurantSlots(anchorDate, date));
            if (!await db.MenuItems.AnyAsync(x => x.MenuDate == date)) db.MenuItems.AddRange(SeedData.MenuItems(date));
            if (!await db.RestaurantAvailabilities.AnyAsync(x => x.BookingDate == date))
                db.RestaurantAvailabilities.AddRange(SeedData.RestaurantAvailabilities(anchorDate, date));
        }
    }
}
