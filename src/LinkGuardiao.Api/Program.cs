using LinkGuardiao.Api;
using LinkGuardiao.Infrastructure.PostgreSQL.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 262_144;
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(15);
});

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithProcessId()
        .Enrich.WithThreadId();
});

var startup = new Startup(builder.Configuration, builder.Environment);
startup.ConfigureServices(builder.Services);

var app = builder.Build();

// Opt-in para ambientes sem passo de migracao separado (ex.: demo no Render + Neon).
if (string.Equals(app.Configuration["STORAGE_PROVIDER"], "postgresql", StringComparison.OrdinalIgnoreCase)
    && app.Configuration.GetValue<bool>("POSTGRESQL_AUTO_MIGRATE"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<LinkGuardiaoDbContext>().Database.Migrate();
}

startup.Configure(app, app.Environment);
app.Run();

public partial class Program { }
