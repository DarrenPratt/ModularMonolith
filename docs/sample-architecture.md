# Sample Architecture

This document explains the structure of this repository specifically, not just the general modular monolith pattern.

## Repository shape

The solution is split into one host application and several supporting projects.

```mermaid
flowchart LR
    Web["ModMonolith.Web<br/>ASP.NET Core MVC Host"]
    Catalog["ModMonolith.Modules.Catalog<br/>Catalog Module"]
    Orders["ModMonolith.Modules.Orders<br/>Orders Module"]
    Shared["ModMonolith.Shared<br/>Contracts + Abstractions + DbContext"]

    Web --> Catalog
    Web --> Orders
    Web --> Shared
    Catalog --> Shared
    Orders --> Shared
```

## Responsibility by project

### `ModMonolith.Web`

This is the application host.

It contains:

- ASP.NET Core startup
- MVC controllers
- Razor views
- static assets
- dependency composition

It is the only project you run directly.

### `ModMonolith.Modules.Catalog`

This module owns product-related behavior.

It contains:

- product domain model
- EF Core entity configuration for catalog tables
- catalog API endpoints
- catalog seed data
- catalog service implementation

### `ModMonolith.Modules.Orders`

This module owns order-related behavior.

It contains:

- order domain model
- EF Core entity configuration for order tables
- order API endpoints
- order seed data
- order orchestration logic

### `ModMonolith.Shared`

This project holds cross-cutting building blocks that are intentionally shared.

It contains:

- module abstractions
- module registration extensions
- cross-module contracts
- shared EF Core `DbContext`

It should stay small and deliberate. If too much business logic migrates here, module boundaries weaken.

## Startup and composition

At startup, the host composes the modules into one application.

```mermaid
sequenceDiagram
    participant Host as ModMonolith.Web
    participant Registry as Module Registry
    participant Catalog as Catalog Module
    participant Orders as Orders Module
    participant Db as SQL Server

    Host->>Registry: Register modules
    Host->>Catalog: Register services
    Host->>Orders: Register services
    Host->>Db: Configure DbContext
    Host->>Db: EnsureCreated / seed attempt
    Host->>Catalog: Seed catalog data
    Host->>Orders: Seed order data
```

The host knows which modules exist, but the module behavior remains inside the module projects.

## MVC request flow

The user-facing page is rendered with MVC.

```mermaid
sequenceDiagram
    participant Browser
    participant Web as HomeController
    participant Catalog as IProductCatalog
    participant Db as ModMonolithDbContext
    participant Sql as SQL Server
    participant View as Razor View

    Browser->>Web: GET /
    Web->>Catalog: Load products
    Web->>Db: Load recent orders
    Catalog->>Sql: Query catalog tables
    Db->>Sql: Query order tables
    Web->>View: Build HomeIndexViewModel
    View-->>Browser: HTML response
```

For form posts:

```mermaid
sequenceDiagram
    participant Browser
    participant Web as HomeController
    participant Catalog as IProductCatalog
    participant Db as ModMonolithDbContext
    participant Sql as SQL Server

    Browser->>Web: POST create product / order
    Web->>Catalog: Use contract if product lookup or stock reservation is needed
    Web->>Db: Save aggregate changes
    Db->>Sql: Persist changes
    Web-->>Browser: Redirect back to dashboard
```

## API flow

The sample still exposes module APIs under `/api/*`.

```mermaid
flowchart TD
    Client["Browser / API Client"]
    Api["ASP.NET Core Host"]
    CatalogApi["Catalog Endpoints"]
    OrdersApi["Orders Endpoints"]
    Db["Shared DbContext"]
    Sql["SQL Server"]

    Client --> Api
    Api --> CatalogApi
    Api --> OrdersApi
    CatalogApi --> Db
    OrdersApi --> Db
    Db --> Sql
```

These APIs are internal parts of the same application, not separate services.

## Module interaction

The modules should interact through explicit contracts.

In this sample, `Orders` depends on catalog information through `IProductCatalog`.

```mermaid
flowchart LR
    Orders["Orders Module"]
    Contract["IProductCatalog<br/>Shared Contract"]
    Catalog["Catalog Module"]

    Orders --> Contract
    Catalog --> Contract
```

This matters because it prevents the `Orders` module from directly taking ownership of `Catalog` internals.

## Database ownership

The database is shared physically, but ownership is still modular logically.

```mermaid
flowchart TD
    Db["ModMonolithSample Database"]
    CatalogSchema["catalog schema<br/>Products"]
    OrdersSchema["orders schema<br/>Orders + OrderLines"]

    Db --> CatalogSchema
    Db --> OrdersSchema
```

This is an important part of the design:

- one database keeps operations simple
- separate schema areas preserve module ownership

## Deployment model

Everything is built and deployed together.

```mermaid
flowchart LR
    Source["Solution Source Code"]
    Build["Build Pipeline"]
    Artifact["Single Web Artifact"]
    App["ModMonolith.Web"]
    Sql["SQL Server"]

    Source --> Build
    Build --> Artifact
    Artifact --> App
    App --> Sql
```

That means:

- there is one primary deployable application
- the modules are not independently deployed
- a change in one module typically results in redeploying the host

## What to notice in this sample

This sample is intentionally small, but the architectural signals are the point:

- the host composes modules, but does not own their business rules
- modules own their own domain and persistence configuration
- MVC is used for the web UI
- module APIs still exist alongside MVC
- the application is modular in code, monolithic in deployment

## Practical mental model

The simplest way to think about this repository is:

- `ModMonolith.Web` is the shell
- `Catalog` and `Orders` are internal business units
- `Shared` contains the contracts and infrastructure glue
- SQL Server is shared, but module ownership is still explicit

That is the essence of this sample.
