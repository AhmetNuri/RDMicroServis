using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Prometheus;
using SearchIndexerService.Application.Services;
using SearchIndexerService.Infrastructure;
using SearchIndexerService.Middleware;
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

    // Services
    builder.Services.AddSingleton<ISearchService, SearchService>();

    // Background services
    builder.Services.AddHostedService<IndexerService>();

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
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("search-indexer-service"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)))
        .WithMetrics(metrics => metrics
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("search-indexer-service"))
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)));

    builder.Services.AddHealthChecks();
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

    // Ensure Elasticsearch index on startup
    var searchService = app.Services.GetRequiredService<ISearchService>();
    await searchService.EnsureIndexExistsAsync();

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
