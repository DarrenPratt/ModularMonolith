# Comparison of Snapshot `1` and Snapshot `2`

This document describes the meaningful source-level differences between the two solution snapshots in this repository:

- `1`: earlier modular monolith snapshot
- `2`: later snapshot with stronger module boundaries and customer-aware order flow

Both snapshots are currently buildable and structurally consistent.

## High-Level Summary

Snapshot `2` tightens the modular monolith pattern in three places:

1. `Catalog` moves product creation behind its module contract instead of letting the host or HTTP endpoints write entities directly.
2. `Orders` moves order creation and retrieval behind an `IOrderService`, removing business logic from the web host and endpoint handlers.
3. Orders become customer-aware, so a customer must be selected when placing an order and that customer is stored with the order.

## Source Files That Changed

The meaningful diff between `1` and `2` is limited to these files:

- `src/ModMonolith.Modules.Catalog/Application/CatalogProductService.cs`
- `src/ModMonolith.Modules.Catalog/CatalogModule.cs`
- `src/ModMonolith.Modules.Customers/CustomersModule.cs`
- `src/ModMonolith.Modules.Orders/Application/CreateOrderRequest.cs`
- `src/ModMonolith.Modules.Orders/Infrastructure/OrderConfiguration.cs`
- `src/ModMonolith.Modules.Orders/OrdersModule.cs`
- `src/ModMonolith.Shared/Contracts/Catalog/IProductCatalog.cs`
- `src/ModMonolith.Web/Controllers/HomeController.cs`
- `src/ModMonolith.Web/Models/Home/CreateOrderInputModel.cs`
- `src/ModMonolith.Web/Models/Home/OrderSummaryViewModel.cs`
- `src/ModMonolith.Web/Views/Home/Index.cshtml`
- `src/ModMonolith.Web/wwwroot/styles.css`

## Architectural Changes

### 1. Catalog Now Owns Product Creation

In snapshot `1`, product creation was still partly owned by callers. In snapshot `2`:

- `IProductCatalog` adds `CreateAsync(...)`
- `CatalogProductService` implements validation and persistence for product creation
- `CatalogModule` calls `IProductCatalog` instead of constructing `Product` entities inline
- `HomeController` also calls `IProductCatalog` instead of writing through `ModMonolithDbContext`

This is the main change that makes `Catalog` follow the same pattern already used by `Customers`.

### 2. Orders Now Own Order Creation and Querying

In snapshot `1`, `OrdersModule` and `HomeController` still contained order orchestration logic. In snapshot `2`:

- `OrdersModule` delegates reads and writes to `IOrderService`
- `HomeController` delegates reads and writes to `IOrderService`
- direct construction of `Order` and `OrderLine` in the host/controller path is removed
- stock reservation and order assembly happen inside the module service layer instead of in callers

This is the biggest boundary improvement between the two snapshots.

### 3. Orders Are Now Customer-Aware

Snapshot `2` introduces customer ownership in the order flow:

- `CreateOrderRequest` now requires `CustomerId`
- the MVC input model for orders now requires `CustomerId`
- the UI adds a customer selector before order submission
- order summaries now expose `CustomerId` and `CustomerName`
- the order persistence mapping now requires `CustomerName`
- order cards in the UI display the customer that placed the order

This makes `Customers` a real upstream module dependency for `Orders`, rather than a separate isolated module.

### 4. Customers Exposes Single-Customer Lookup

Snapshot `2` adds:

- `GET /api/customers/customers/{id}`

That endpoint supports the customer-aware order flow and better matches the `ICustomerDirectory` contract shape used internally.

## UI Changes

The MVC dashboard in `2` differs from `1` in these ways:

- the order form includes a customer dropdown
- the submit button disables when either customers or products are unavailable
- the order list displays the customer name
- styles were updated to support the new order metadata

## Why Snapshot `2` Is Closer to a Service-Ready Design

Snapshot `2` is not microservices, but it is closer to later extraction because:

- business writes are concentrated inside modules
- the web host is thinner and less coupled to module internals
- module contracts are stronger
- customer and order interactions now look more like a real upstream/downstream relationship

If `Customers`, `Catalog`, or `Orders` were later extracted into separate deployable services, snapshot `2` provides a better starting point than snapshot `1`.

## Validation

The snapshots were verified as follows:

- `1\\src\\ModMonolith.Web\\ModMonolith.Web.csproj` builds successfully
- `2\\src\\ModMonolith.Web\\ModMonolith.Web.csproj` builds successfully

## Important Note About Snapshot `1`

Snapshot `1` is now internally consistent and buildable, but it should not be treated as a guaranteed pristine historical restore point. It was repaired so it can stand alone cleanly beside `2`.
