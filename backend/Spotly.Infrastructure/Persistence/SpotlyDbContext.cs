using Microsoft.EntityFrameworkCore;
using Spotly.Domain.Entities;
using Spotly.Infrastructure.Seed;

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
        modelBuilder.Entity<DeskSpot>().HasKey(x => x.DeskId);
        modelBuilder.Entity<DeskBooking>().HasKey(x => x.BookingId);
        modelBuilder.Entity<Restaurant>().HasKey(x => x.RestaurantId);
        modelBuilder.Entity<RestaurantSlot>().HasKey(x => x.SlotId);
        modelBuilder.Entity<MenuItem>().HasKey(x => x.ItemId);
        modelBuilder.Entity<LunchBoxCatalog>().HasKey(x => x.BoxId);
        modelBuilder.Entity<LunchBooking>().HasKey(x => x.BookingId);
        modelBuilder.Entity<LunchBooking>().HasIndex(x => new { x.UserId, x.BookingDate, x.Status });
        modelBuilder.Entity<LunchBooking>().HasIndex(x => x.PartnerCorrelationId).IsUnique();
        modelBuilder.Entity<RestaurantAvailability>().HasKey(x => new { x.RestaurantId, x.BookingDate });
        modelBuilder.Entity<RestaurantPartnerMessage>().HasKey(x => x.MessageId);
    }
}

public static class SpotlyDbSeeder
{
    public static async Task SeedAsync(SpotlyDbContext db, DateOnly today)
    {
        if (!await db.ParkingSpots.AnyAsync()) db.ParkingSpots.AddRange(SeedData.ParkingSpots());
        if (!await db.DeskSpots.AnyAsync()) db.DeskSpots.AddRange(SeedData.DeskSpots());
        if (!await db.Restaurants.AnyAsync()) db.Restaurants.AddRange(SeedData.Restaurants());
        if (!await db.RestaurantSlots.AnyAsync(x => x.BookingDate == today)) db.RestaurantSlots.AddRange(SeedData.RestaurantSlots(today));
        if (!await db.MenuItems.AnyAsync(x => x.MenuDate == today)) db.MenuItems.AddRange(SeedData.MenuItems(today));
        if (!await db.LunchBoxes.AnyAsync()) db.LunchBoxes.AddRange(SeedData.LunchBoxes());
        if (!await db.RestaurantAvailabilities.AnyAsync(x => x.BookingDate == today))
            db.RestaurantAvailabilities.AddRange(SeedData.RestaurantAvailabilities(today));
        await db.SaveChangesAsync();
    }
}
