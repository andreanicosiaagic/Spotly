using System.Globalization;
using FluentValidation;
using Spotly.Api.Dtos;
using Spotly.Api.Services;
using Spotly.Domain.Interfaces;

namespace Spotly.Api.Endpoints;

public static class DeskEndpoints
{
    public static void MapDeskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/desk").WithTags("Desk").RequireAuthorization("Employee");

        group.MapGet("/spots", (DeskBookingService service, string? locationId, string? date) => service.GetSpotsAsync(locationId, date));
        group.MapGet("/bookings/me", (HttpContext context, DeskBookingService service, string? date) => service.GetMyBookingAsync(context.User, date));
        group.MapPost("/spots/{deskId}/lock", (string deskId, HttpContext context, DeskBookingService service, string? date) => service.LockAsync(deskId, context.User, date));
        group.MapPost("/bookings", (CreateDeskBookingRequest request, HttpContext context, DeskBookingService service) => service.CreateBookingAsync(request, context.User));
        group.MapPost("/bookings/{bookingId}/check-in", (string bookingId, HttpContext context, DeskBookingService service, IAccessControlSystem access) => service.CheckInAsync(bookingId, context.User, access));
        group.MapDelete("/bookings/{bookingId}", (string bookingId, HttpContext context, DeskBookingService service) => service.CancelAsync(bookingId, context.User));
    }
}

public sealed class CreateDeskBookingValidator : AbstractValidator<CreateDeskBookingRequest>
{
    public CreateDeskBookingValidator()
    {
        RuleFor(x => x.DeskId).NotEmpty().MaximumLength(50);
        RuleFor(x => x.BookingDate)
            .NotEmpty()
            .Must(x => DateOnly.TryParseExact(x, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            .WithMessage("BookingDate must use yyyy-MM-dd.");
    }
}
