using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using ModMonolith.Shared.Contracts.Customers;
using ModMonolith.Shared.Abstractions;
using ModMonolith.Shared.Contracts.Catalog;
using ModMonolith.Shared.Contracts.Orders;
using ModMonolith.Web.Models.Home;

namespace ModMonolith.Web.Controllers;

public sealed class HomeController(
    ModuleRegistry moduleRegistry,
    IProductCatalog productCatalog,
    ICustomerDirectory customerDirectory,
    IOrderService orderService,
    ILogger<HomeController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await BuildIndexViewModelAsync(cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCustomer(
        [Bind(Prefix = "CustomerForm")] CreateCustomerInputModel input,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildIndexViewModelAsync(
                cancellationToken,
                customerInput: input,
                productInput: new CreateProductInputModel(),
                orderInput: new CreateOrderInputModel());
            return View("Index", invalidModel);
        }

        try
        {
            await customerDirectory.CreateAsync(
                input.Name.Trim(),
                input.Email.Trim(),
                cancellationToken);

            TempData["StatusMessage"] = $"Created customer '{input.Name}'.";
            TempData["StatusType"] = "success";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError("CustomerForm.Email", exception.Message);
            var duplicateModel = await BuildIndexViewModelAsync(
                cancellationToken,
                customerInput: input,
                productInput: new CreateProductInputModel(),
                orderInput: new CreateOrderInputModel());
            return View("Index", duplicateModel);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to create customer.");
            ModelState.AddModelError(string.Empty, "The customer could not be created because the database is unavailable.");
            var failedModel = await BuildIndexViewModelAsync(
                cancellationToken,
                customerInput: input,
                productInput: new CreateProductInputModel(),
                orderInput: new CreateOrderInputModel());
            return View("Index", failedModel);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(
        [Bind(Prefix = "ProductForm")] CreateProductInputModel input,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildIndexViewModelAsync(
                cancellationToken,
                customerInput: new CreateCustomerInputModel(),
                productInput: input,
                orderInput: new CreateOrderInputModel());
            return View("Index", invalidModel);
        }

        try
        {
            await productCatalog.CreateAsync(
                input.Name,
                input.Price,
                input.StockQuantity,
                cancellationToken);
            TempData["StatusMessage"] = $"Created product '{input.Name}'.";
            TempData["StatusType"] = "success";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError("ProductForm.Name", exception.Message);
            var invalidBusinessModel = await BuildIndexViewModelAsync(
                cancellationToken,
                customerInput: new CreateCustomerInputModel(),
                productInput: input,
                orderInput: new CreateOrderInputModel());
            return View("Index", invalidBusinessModel);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to create product.");
            ModelState.AddModelError(string.Empty, "The catalog could not be updated because the database is unavailable.");
            var failedModel = await BuildIndexViewModelAsync(
                cancellationToken,
                customerInput: new CreateCustomerInputModel(),
                productInput: input,
                orderInput: new CreateOrderInputModel());
            return View("Index", failedModel);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOrder(
        [Bind(Prefix = "OrderForm")] CreateOrderInputModel input,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildIndexViewModelAsync(
                cancellationToken,
                customerInput: new CreateCustomerInputModel(),
                productInput: new CreateProductInputModel(),
                orderInput: input);
            return View("Index", invalidModel);
        }

        try
        {
            var order = await orderService.CreateAsync(
                new CreateOrder(
                    input.CustomerId,
                    [new CreateOrderLine(input.ProductId, input.Quantity)]),
                cancellationToken);

            TempData["StatusMessage"] = $"Submitted order '{order.Number}'.";
            TempData["StatusType"] = "success";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError("OrderForm.CustomerId", exception.Message);
            var invalidBusinessModel = await BuildIndexViewModelAsync(
                cancellationToken,
                customerInput: new CreateCustomerInputModel(),
                productInput: new CreateProductInputModel(),
                orderInput: input);
            return View("Index", invalidBusinessModel);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to create order.");
            ModelState.AddModelError(string.Empty, "The order could not be submitted because the database is unavailable.");
            var failedModel = await BuildIndexViewModelAsync(
                cancellationToken,
                customerInput: new CreateCustomerInputModel(),
                productInput: new CreateProductInputModel(),
                orderInput: input);
            return View("Index", failedModel);
        }
    }

    [HttpGet]
    public IActionResult Error()
    {
        return View();
    }

    private async Task<HomeIndexViewModel> BuildIndexViewModelAsync(
        CancellationToken cancellationToken,
        CreateCustomerInputModel? customerInput = null,
        CreateProductInputModel? productInput = null,
        CreateOrderInputModel? orderInput = null)
    {
        var model = new HomeIndexViewModel
        {
            CustomerForm = customerInput ?? new CreateCustomerInputModel(),
            ProductForm = productInput ?? new CreateProductInputModel(),
            OrderForm = orderInput ?? new CreateOrderInputModel(),
            Modules = moduleRegistry.Modules.Select(module => module.Name).ToArray(),
            StatusMessage = TempData["StatusMessage"] as string,
            StatusType = TempData["StatusType"] as string
        };

        try
        {
            model.Customers = await customerDirectory.GetAllAsync(cancellationToken);
            model.Products = await productCatalog.GetAllAsync(cancellationToken);
            model.Orders = (await orderService.GetAllAsync(cancellationToken))
                .Select(order => new OrderSummaryViewModel
                {
                    Id = order.Id,
                    Number = order.Number,
                    CustomerId = order.CustomerId,
                    CustomerName = order.CustomerName,
                    CreatedUtc = order.CreatedUtc,
                    TotalAmount = order.TotalAmount,
                    Lines = order.Lines.Select(line => new OrderLineSummaryViewModel
                    {
                        ProductId = line.ProductId,
                        ProductName = line.ProductName,
                        Quantity = line.Quantity,
                        UnitPrice = line.UnitPrice
                    }).ToList()
                })
                .ToList();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Dashboard data could not be loaded.");
            model.DatabaseUnavailableMessage = "The application started, but SQL Server is not reachable. Start the database and refresh the page.";
            model.Customers = [];
            model.Products = [];
            model.Orders = [];
        }

        if (model.OrderForm.ProductId == Guid.Empty && model.Products.Count > 0)
        {
            model.OrderForm.ProductId = model.Products[0].Id;
        }

        if (model.OrderForm.CustomerId == Guid.Empty && model.Customers.Count > 0)
        {
            model.OrderForm.CustomerId = model.Customers[0].Id;
        }

        return model;
    }
}
