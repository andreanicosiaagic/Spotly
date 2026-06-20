using FluentValidation;
using Serilog;
using Spotly.Api.Dtos;
using Spotly.Api.Endpoints;
using Spotly.Api.Hubs;
using Spotly.Domain.Interfaces;
using Spotly.Infrastructure.Integrations;
using Spotly.Infrastructure.Repositories;

// -- Serilog bootstrap ------------------------------------------------------
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

    // -- CORS --------------------------------------------------------------
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? ["http://localhost:5173"];

    builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
        p.WithOrigins(allowedOrigins)
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials()));

    // -- SignalR -----------------------------------------------------------
    var signalRBuilder = builder.Services.AddSignalR();
    // Uncomment for Azure SignalR Service in production:
    // if (!string.IsNullOrEmpty(builder.Configuration["Azure:SignalR:ConnectionString"]))
    //     signalRBuilder.AddAzureSignalR(builder.Configuration["Azure:SignalR:ConnectionString"]!);

    // -- Repositories (InMemory for POC) -----------------------------------
    builder.Services.AddSingleton<IParkingRepository, InMemoryParkingRepository>();
    builder.Services.AddSingleton<IDeskRepository, InMemoryDeskRepository>();
    builder.Services.AddSingleton<ILunchRepository, InMemoryLunchRepository>();

    // -- Mock integrations -------------------------------------------------
    builder.Services.AddSingleton<ICalendarIntegration, MockCalendarIntegration>();
    builder.Services.AddSingleton<IAccessControlSystem, MockAccessControlSystem>();
    builder.Services.AddSingleton<IRestaurantPartner, MockRestaurantPartner>();
    builder.Services.AddSingleton<IWelfareProvider, MockWelfareProvider>();
    builder.Services.AddSingleton<INotificationService, MockNotificationService>();

    // -- FluentValidation --------------------------------------------------
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    // -- OpenAPI -----------------------------------------------------------
    builder.Services.AddOpenApi();

    // -- Health checks -----------------------------------------------------
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseCors();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        // Simulate Easy Auth in local dev via X-Dev-User header
        app.Use(async (ctx, next) =>
        {
            var devUser = ctx.Request.Headers["X-Dev-User"].FirstOrDefault();
            if (devUser is not null && ctx.User.Identity?.IsAuthenticated != true)
            {
                var claims = new[]
                {
                    new System.Security.Claims.Claim("oid", devUser),
                    new System.Security.Claims.Claim("preferred_username", $"{devUser}@spotly.test"),
                    new System.Security.Claims.Claim("roles", "Dipendente"),
                };
                ctx.User = new System.Security.Claims.ClaimsPrincipal(
                    new System.Security.Claims.ClaimsIdentity(claims, "DevAuth"));
            }
            await next();
        });
    }

    // -- Routes ------------------------------------------------------------
    app.MapHub<AvailabilityHub>("/availability");
    app.MapHealthChecks("/health");
    app.MapParkingEndpoints();
    app.MapDeskEndpoints();
    app.MapLunchEndpoints();
    app.MapMeEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program accessible for integration tests
public partial class Program { }
