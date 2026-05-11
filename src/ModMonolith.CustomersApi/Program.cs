using Microsoft.EntityFrameworkCore;
using ModMonolith.Modules.Customers;
using ModMonolith.Shared.Abstractions;
using ModMonolith.Shared.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddModularMonolith(
    builder.Configuration,
    new CustomersModule());

builder.Services.AddDbContext<ModMonolithDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ModMonolith")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.MapGet("/", () => Results.Redirect("/api/system"));
app.MapGet("/api/system", (ModuleRegistry modules) => Results.Ok(new
{
    name = "ModMonolith Customers API",
    description = "A dedicated API host for the Customers capability, representing the next extraction stage from the modular monolith.",
    modules = modules.Modules.Select(module => module.Name)
})).WithTags("System");

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger("Startup");

    try
    {
        await app.SeedModulesAsync(recreateOnSchemaMismatch: app.Environment.IsDevelopment());
    }
    catch (Exception exception)
    {
        logger.LogWarning(exception, "Database initialization failed. The customers API will remain unavailable until SQL Server is reachable or the schema is brought up to date.");
    }
}

app.MapModules();

app.Run();
