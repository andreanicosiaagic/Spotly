using Spotly.Domain.Entities;

namespace Spotly.Infrastructure.Seed;

public static class SeedData
{
    public static List<ParkingSpot> ParkingSpots() =>
    [
        new() { SpotId = "P01", LocationId = "HQ", Level = 1, SpotNumber = "A01", Type = ParkingSpotType.Standard, Status = ResourceStatus.Available },
        new() { SpotId = "P02", LocationId = "HQ", Level = 1, SpotNumber = "A02", Type = ParkingSpotType.Standard, Status = ResourceStatus.Available },
        new() { SpotId = "P03", LocationId = "HQ", Level = 1, SpotNumber = "A03", Type = ParkingSpotType.Standard, Status = ResourceStatus.Occupied  },
        new() { SpotId = "P04", LocationId = "HQ", Level = 1, SpotNumber = "A04", Type = ParkingSpotType.Standard, Status = ResourceStatus.Available },
        new() { SpotId = "P05", LocationId = "HQ", Level = 1, SpotNumber = "A05", Type = ParkingSpotType.Ev,       Status = ResourceStatus.Available },
        new() { SpotId = "P06", LocationId = "HQ", Level = 1, SpotNumber = "A06", Type = ParkingSpotType.Disabled, Status = ResourceStatus.Reserved  },
        new() { SpotId = "P07", LocationId = "HQ", Level = 2, SpotNumber = "B01", Type = ParkingSpotType.Standard, Status = ResourceStatus.Available },
        new() { SpotId = "P08", LocationId = "HQ", Level = 2, SpotNumber = "B02", Type = ParkingSpotType.Standard, Status = ResourceStatus.Available },
        new() { SpotId = "P09", LocationId = "HQ", Level = 2, SpotNumber = "B03", Type = ParkingSpotType.Standard, Status = ResourceStatus.Available },
        new() { SpotId = "P10", LocationId = "HQ", Level = 2, SpotNumber = "B04", Type = ParkingSpotType.Guest,    Status = ResourceStatus.Available },
    ];

    public static List<DeskSpot> DeskSpots() =>
    [
        new() { DeskId = "D01", LocationId = "HQ", Floor = 2, Zone = "Alpha", HasMonitor = true,  IsStanding = false, HasWindow = true,  Status = ResourceStatus.Available },
        new() { DeskId = "D02", LocationId = "HQ", Floor = 2, Zone = "Alpha", HasMonitor = true,  IsStanding = false, HasWindow = false, Status = ResourceStatus.Occupied  },
        new() { DeskId = "D03", LocationId = "HQ", Floor = 2, Zone = "Alpha", HasMonitor = false, IsStanding = true,  HasWindow = true,  Status = ResourceStatus.Available },
        new() { DeskId = "D04", LocationId = "HQ", Floor = 2, Zone = "Alpha", HasMonitor = true,  IsStanding = false, HasWindow = false, Status = ResourceStatus.Available },
        new() { DeskId = "D05", LocationId = "HQ", Floor = 2, Zone = "Beta",  HasMonitor = false, IsStanding = false, HasWindow = true,  Status = ResourceStatus.Available },
        new() { DeskId = "D06", LocationId = "HQ", Floor = 2, Zone = "Beta",  HasMonitor = true,  IsStanding = false, HasWindow = false, Status = ResourceStatus.Available },
        new() { DeskId = "D07", LocationId = "HQ", Floor = 3, Zone = "Gamma", HasMonitor = true,  IsStanding = false, HasWindow = true,  Status = ResourceStatus.Available },
        new() { DeskId = "D08", LocationId = "HQ", Floor = 3, Zone = "Gamma", HasMonitor = false, IsStanding = true,  HasWindow = false, Status = ResourceStatus.Available },
    ];

    public static List<Restaurant> Restaurants() =>
    [
        new() { RestaurantId = "R01", LocationId = "HQ", Name = "Bistrot Verde", Capacity = 40 },
        new() { RestaurantId = "R02", LocationId = "HQ", Name = "La Tavola",     Capacity = 30 },
    ];

    public static List<RestaurantSlot> RestaurantSlots(DateOnly date) =>
    [
        new() { SlotId = "S01", RestaurantId = "R01", SlotTime = new TimeOnly(12, 0),  Capacity = 15, Available = 8,  BookingDate = date },
        new() { SlotId = "S02", RestaurantId = "R01", SlotTime = new TimeOnly(12, 30), Capacity = 15, Available = 0,  BookingDate = date },
        new() { SlotId = "S03", RestaurantId = "R01", SlotTime = new TimeOnly(13, 0),  Capacity = 10, Available = 5,  BookingDate = date },
        new() { SlotId = "S04", RestaurantId = "R02", SlotTime = new TimeOnly(12, 0),  Capacity = 15, Available = 12, BookingDate = date },
        new() { SlotId = "S05", RestaurantId = "R02", SlotTime = new TimeOnly(13, 0),  Capacity = 15, Available = 3,  BookingDate = date },
    ];

    public static List<MenuItem> MenuItems(DateOnly date) =>
    [
        new() { ItemId = "M01", RestaurantId = "R01", MenuDate = date, Name = "Pasta al pomodoro",  Category = "primo",    Allergens = "glutine" },
        new() { ItemId = "M02", RestaurantId = "R01", MenuDate = date, Name = "Pollo arrosto",       Category = "secondo",  Allergens = ""        },
        new() { ItemId = "M03", RestaurantId = "R01", MenuDate = date, Name = "Insalata mista",      Category = "contorno", Allergens = ""        },
        new() { ItemId = "M04", RestaurantId = "R02", MenuDate = date, Name = "Risotto ai funghi",   Category = "primo",    Allergens = "latte"   },
        new() { ItemId = "M05", RestaurantId = "R02", MenuDate = date, Name = "Salmone al forno",    Category = "secondo",  Allergens = "pesce"   },
    ];

    public static List<LunchBoxCatalog> LunchBoxes() =>
    [
        new() { BoxId = "LB01", Name = "Box Classico",  Description = "Pasta, secondo, contorno e frutta",       Allergens = "glutine", IsAvailable = true },
        new() { BoxId = "LB02", Name = "Box Vegano",    Description = "Cereali, legumi, verdure di stagione",     Allergens = "",        IsAvailable = true },
        new() { BoxId = "LB03", Name = "Box Proteico",  Description = "Proteine, verdure, senza glutine",         Allergens = "",        IsAvailable = true },
    ];
}
