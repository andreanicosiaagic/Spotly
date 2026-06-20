using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;
using Spotly.Api.Auth;
using Spotly.Api.Endpoints;
using Spotly.Api.Hubs;
using Spotly.Api.Infrastructure;
using Spotly.Api.Services;
using Spotly.Domain.Interfaces;
using Spotly.Infrastructure.Integrations;
using Spotly.Infrastructure.Persistence;
using Spotly.Infrastructure.Repositories;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration).WriteTo.Console());
    builder.Services.AddProblemDetails();
    builder.Services.ConfigureHttpJsonOptions(options =>
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"];
    builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.WithOrigins(allowedOrigins)
        .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

    builder.Services.AddAuthentication(SpotlyAuthenticationHandler.AuthenticationScheme)
        .AddScheme<AuthenticationSchemeOptions, SpotlyAuthenticationHandler>(SpotlyAuthenticationHandler.AuthenticationScheme, _ => { });
    builder.Services.AddAuthorizationBuilder()
        .AddPolicy("Employee", policy => policy.RequireRole("Dipendente", "Manager", "Facility", "Admin"))
        .AddPolicy("Manager", policy => policy.RequireRole("Manager", "Facility", "Admin"))
        .AddPolicy("Facility", policy => policy.RequireRole("Facility", "Admin"))
        .AddPolicy("Admin", policy => policy.RequireRole("Admin"));

    var signalR = builder.Services.AddSignalR();
    var signalRConnection = builder.Configuration["Azure:SignalR:ConnectionString"];
    if (!string.IsNullOrWhiteSpace(signalRConnection)) signalR.AddAzureSignalR(signalRConnection);

    var databaseProvider = builder.Configuration["Database:Provider"] ?? "InMemory";

    builder.Services.AddDbContextFactory<SpotlyDbContext>(options =>
    {
        if (databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = builder.Configuration.GetConnectionString("Spotly")
                ?? throw new InvalidOperationException("ConnectionStrings:Spotly è richiesta quando Database:Provider=SqlServer.");
            // In dev, bypass the full DefaultAzureCredential chain (Managed Identity etc.) che va in timeout
            SqlAuthenticationProvider.SetProvider(
                SqlAuthenticationMethod.ActiveDirectoryDefault,
                new DevSqlAuthProvider());
            options.UseSqlServer(connectionString);
            Log.Information("Database: Azure SQL Server (Authentication=Active Directory Default)");
        }
        else
        {
            options.UseInMemoryDatabase(builder.Configuration["Database:Name"] ?? "spotly-poc");
            Log.Information("Database: InMemory — per usare Azure SQL imposta Database__Provider=SqlServer");
        }
    });
    builder.Services.AddSingleton<IParkingRepository, InMemoryParkingRepository>();
    builder.Services.AddSingleton<IDeskRepository, InMemoryDeskRepository>();
    builder.Services.AddSingleton<ILunchRepository, InMemoryLunchRepository>();

    builder.Services.AddSingleton<ICalendarIntegration, MockCalendarIntegration>();
    builder.Services.AddSingleton<IAccessControlSystem, MockAccessControlSystem>();
    builder.Services.AddSingleton<IRestaurantPartner, MockRestaurantPartner>();
    builder.Services.AddSingleton<IWelfareProvider, MockWelfareProvider>();
    builder.Services.AddSingleton<INotificationService, MockNotificationService>();
    builder.Services.AddSingleton<ICollaborationAvailabilityProvider, MockTeamsCollaborationProvider>();
    builder.Services.AddSingleton<IRestaurantPartnerProtocol, RestaurantPartnerProtocol>();
    builder.Services.AddSingleton<MockTelegramRestaurantGateway>();
    builder.Services.AddSingleton<IRestaurantMessagingGateway>(services => services.GetRequiredService<MockTelegramRestaurantGateway>());
    builder.Services.AddSingleton<IRestaurantDemoGateway>(services => services.GetRequiredService<MockTelegramRestaurantGateway>());
    builder.Services.AddSingleton<RestaurantLiveService>();
    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services.AddSingleton<OfficeTime>();
    builder.Services.AddScoped<ParkingBookingService>();
    builder.Services.AddScoped<DeskBookingService>();
    builder.Services.AddScoped<LunchBookingService>();
    builder.Services.AddScoped<CollaborationQueryService>();
    builder.Services.AddScoped<UserProfileService>();
    builder.Services.AddHostedService<BookingLifecycleService>();
    builder.Services.AddHostedService<RestaurantAvailabilityPollingService>();
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();
    builder.Services.AddOpenApi();
    builder.Services.AddHealthChecks()
        .AddCheck<SpotlyReadinessHealthCheck>("spotly-readiness", failureStatus: HealthStatus.Unhealthy);

    var app = builder.Build();
    await using (var scope = app.Services.CreateAsyncScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<SpotlyDbContext>();
        var officeTime = scope.ServiceProvider.GetRequiredService<OfficeTime>();
        if (databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            using var migrationCts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            await db.Database.MigrateAsync(migrationCts.Token);
            Log.Information("Database: migrazione completata");
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
            await SpotlyDbSeeder.SeedAsync(db, officeTime.Today);
        }
    }

    app.UseExceptionHandler();
    app.UseSerilogRequestLogging(options => options.EnrichDiagnosticContext = (diagnostics, context) =>
    {
        diagnostics.Set("RequestHost", context.Request.Host.Value);
        diagnostics.Set("RequestScheme", context.Request.Scheme);
    });
    app.UseHttpsRedirection();
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();

    if (app.Environment.IsDevelopment()) app.MapOpenApi().AllowAnonymous();
    app.MapHealthChecks("/health/live").AllowAnonymous();
    app.MapHealthChecks("/health/ready").AllowAnonymous();
    app.MapHealthChecks("/health").AllowAnonymous();
    app.MapHub<AvailabilityHub>("/availability").RequireAuthorization("Employee");
    app.MapParkingEndpoints();
    app.MapDeskEndpoints();
    app.MapLunchEndpoints();
    app.MapMeEndpoints();
    app.MapCollaborationEndpoints();
    app.MapFallbackToFile("index.html");
    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Application terminated unexpectedly");
    Environment.ExitCode = 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program;
