using System.Globalization;
using FluentValidation;
using Spotly.Api.Dtos;
using Spotly.Api.Services;
using Spotly.Domain.Interfaces;

namespace Spotly.Api.Endpoints;

public static class ParkingEndpoints
{
    public static void MapParkingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/parking").WithTags("Parking").RequireAuthorization("Employee");

        group.MapGet("/spots", (ParkingBookingService service, string? locationId, string? date) => service.GetSpotsAsync(locationId, date));
        group.MapGet("/bookings/me", (HttpContext context, ParkingBookingService service, string? date) => service.GetMyBookingAsync(context.User, date));
        group.MapPost("/spots/{spotId}/lock", (string spotId, HttpContext context, ParkingBookingService service, string? date) => service.LockAsync(spotId, context.User, date));
        group.MapPost("/bookings", (CreateParkingBookingRequest request, HttpContext context, ParkingBookingService service) => service.CreateBookingAsync(request, context.User));
        group.MapPost("/bookings/{bookingId}/check-in", (string bookingId, HttpContext context, ParkingBookingService service, IAccessControlSystem access) => service.CheckInAsync(bookingId, context.User, access));
        group.MapDelete("/bookings/{bookingId}", (string bookingId, HttpContext context, ParkingBookingService service) => service.CancelAsync(bookingId, context.User));
    }
}

public sealed class CreateParkingBookingValidator : AbstractValidator<CreateParkingBookingRequest>
{
    public CreateParkingBookingValidator()
    {
        RuleFor(x => x.SpotId).NotEmpty().MaximumLength(50);
        RuleFor(x => x.BookingDate)
            .NotEmpty()
            .Must(x => DateOnly.TryParseExact(x, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            .WithMessage("BookingDate must use yyyy-MM-dd.");
    }
}
