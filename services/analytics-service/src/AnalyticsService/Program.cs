using System.Text;
using AnalyticsService.Application.Services;
using AnalyticsService.Infrastructure;
using AnalyticsService.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Prometheus;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, config) => config
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter()));

    // Database
    builder.Services.AddDbContext<AnalyticsDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Services
    builder.Services.AddScoped<IAnalyticsService, AnalyticsService.Application.Services.AnalyticsService>();

    // Background services
    builder.Services.AddHostedService<KafkaAnalyticsConsumer>();

    // JWT
    var jwtSecret = builder.Configuration["Jwt__Secret"] ?? builder.Configuration["Jwt:Secret"] ?? "default-secret-change-in-production-min-32-chars";
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "auth-service";
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "commerce-platform";

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
            };
        });

    builder.Services.AddAuthorization();

    var corsOrigins = builder.Configuration["Cors__Origins"]?.Split(',') ?? ["http://localhost:3000"];
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod());
    });

    // OpenTelemetry
    var otlpEndpoint = builder.Configuration["Otlp__Endpoint"] ?? "http://otel-collector:4317";
    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing => tracing
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("analytics-service"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)))
        .WithMetrics(metrics => metrics
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("analytics-service"))
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)));

    builder.Services.AddHealthChecks()
        .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty, name: "postgresql");

    builder.Services.AddControllers();

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseHttpMetrics();
    app.MapMetrics("/metrics");
    app.MapHealthChecks("/health");
    app.MapControllers();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
        db.Database.Migrate();
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}
