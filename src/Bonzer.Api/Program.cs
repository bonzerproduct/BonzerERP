using Bonzer.Api;
using Bonzer.Api.Middleware;
using Bonzer.Application;
using Bonzer.Infrastructure;
using Bonzer.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Reflection;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Exceptions.EntityFrameworkCore.Destructurers;

// Bootstrap logger catches failures that happen before configuration is loaded.
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

// Exceptions outside any request (background threads, fire-and-forget tasks) are logged instead of lost.
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
    Log.Fatal(e.ExceptionObject as Exception, "Unhandled exception (process terminating: {IsTerminating})", e.IsTerminating);
TaskScheduler.UnobservedTaskException += (_, e) =>
{
    Log.Error(e.Exception, "Unobserved task exception");
    e.SetObserved();
};

var version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog: structured logs to console, rolling JSON files and Seq (configured in appsettings "Serilog").
    builder.Host.UseSerilog((context, services, logger) => logger
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .Enrich.WithProperty("Application", "Bonzer.Api")
        .Enrich.WithProperty("Version", version)
        // Structured exception properties (SqlState, ConstraintName, inner exceptions) instead of only a stack trace.
        // The EF destructurer skips DbUpdateException.Entries, which would otherwise dump tracked entities (PII).
        .Enrich.WithExceptionDetails(new DestructuringOptionsBuilder()
            .WithDefaultDestructurers()
            .WithDestructurers([new DbUpdateExceptionDestructurer()])));

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration, builder.Environment)
        .AddApi(builder.Configuration);

    var app = builder.Build();
    Log.Information("Starting Bonzer API {Version} in {Environment}", version, app.Environment.EnvironmentName);
    var slowRequestMs = app.Configuration.GetValue("RequestLogging:SlowRequestThresholdMs", 2000);

    using (var scope = app.Services.CreateScope())
        await scope.ServiceProvider.GetRequiredService<DbInitializer>().InitialiseAsync();

    // Order matters: correlation id first (every later log carries it), then the request log, then the
    // exception handler inside it, so the request log records the final status (404, 409...) and not a 500.
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging(o =>
    {
        o.EnrichDiagnosticContext = (diagnostics, http) =>
        {
            diagnostics.Set("ClientIp", http.Connection.RemoteIpAddress?.ToString());
            diagnostics.Set("UserAgent", http.Request.Headers.UserAgent.ToString());
            diagnostics.Set("CorrelationId", http.TraceIdentifier);
        };
        o.GetLevel = (http, elapsedMs, exception) =>
            exception is not null || http.Response.StatusCode >= 500 ? LogEventLevel.Error
            : http.Request.Path.StartsWithSegments("/health") ? LogEventLevel.Verbose
            : elapsedMs > slowRequestMs ? LogEventLevel.Warning
            : LogEventLevel.Information;
    });
    app.UseExceptionHandler();
    app.UseStatusCodePages(); // empty 401/403/404/405 responses get a ProblemDetails body

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    else
    {
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseCors();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseMiddleware<LogEnrichmentMiddleware>();
    app.UseMiddleware<PasswordChangeRequiredMiddleware>();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
    app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") }).AllowAnonymous();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Bonzer API terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
