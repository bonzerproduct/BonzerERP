using Bonzer.Api;
using Bonzer.Api.Middleware;
using Bonzer.Application;
using Bonzer.Infrastructure;
using Bonzer.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

// Bootstrap logger catches failures that happen before configuration is loaded.
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

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
        .Enrich.WithProperty("Application", "Bonzer.Api"));

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration, builder.Environment)
        .AddApi(builder.Configuration);

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
        await scope.ServiceProvider.GetRequiredService<DbInitializer>().InitialiseAsync();

    app.UseExceptionHandler();
    app.UseSerilogRequestLogging(o => o.EnrichDiagnosticContext = (diagnostics, http) =>
    {
        diagnostics.Set("ClientIp", http.Connection.RemoteIpAddress?.ToString());
        diagnostics.Set("UserAgent", http.Request.Headers.UserAgent.ToString());
    });

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
