using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using ModMonolith.Modules.Customers;
using ModMonolith.Modules.Catalog;
using ModMonolith.Modules.Orders;
using ModMonolith.Shared.Abstractions;
using ModMonolith.Shared.Persistence;

var builder = WebApplication.CreateBuilder(args);
var dataProtectionPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys");

Directory.CreateDirectory(dataProtectionPath);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllersWithViews();
builder.Services.AddDataProtection()
    .SetApplicationName("ModMonolithSample")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));

builder.Services.AddModularMonolith(
    builder.Configuration,
    new CustomersModule(),
    new CatalogModule(),
    new OrdersModule());

builder.Services.AddDbContext<ModMonolithDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ModMonolith")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapGet("/api/system", (ModuleRegistry modules) => Results.Ok(new
{
    name = "ModMonolith Sample",
    description = "A modular monolith sample with ASP.NET Core, EF Core, SQL Server, and module-owned endpoints.",
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
        logger.LogWarning(exception, "Database initialization failed. The MVC shell will still load, but data features will remain unavailable until SQL Server is reachable or the schema is brought up to date.");
    }
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapModules();

app.Run();
