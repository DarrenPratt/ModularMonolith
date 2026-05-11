using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using ModMonolith.Modules.Catalog;
using ModMonolith.Modules.Orders;
using ModMonolith.Shared.Abstractions;
using ModMonolith.Shared.Persistence;
using ModMonolith.Web.CustomersIntegration;

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
    new RemoteCustomersModule(),
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
    description = "A staged modular monolith sample where Catalog and Orders remain in-process while Customers has been split into a dedicated service host.",
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
        logger.LogWarning(exception, "Module initialization failed. The MVC shell will still load, but data features will remain limited until SQL Server and the Customers API are reachable.");
    }
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapModules();

app.Run();
