# Modular Monolith Explained

## What it is

A modular monolith is a single application that is intentionally divided into clear internal modules.

It is still a monolith because:

- it runs as one process
- it is built as one deployable unit
- it is usually released as one artifact
- it often uses one database

It is modular because:

- the code is split into bounded areas of responsibility
- each module owns its own business behavior
- modules interact through explicit contracts instead of arbitrary cross-project access
- the design tries to preserve boundaries that could later become services if needed

The point is not to pretend the system is distributed. The point is to keep the codebase disciplined while avoiding the operational cost of microservices.

## Why teams use it

A modular monolith is usually a good fit when:

- the system is growing beyond a simple layered app
- the team wants stronger boundaries between domains
- independent deployment is not yet worth the operational complexity
- the product still benefits from simple local development, debugging, and release management

It gives you many of the code-organization benefits people want from microservices, without immediately paying for distributed systems.

## How it works

At runtime, everything lives inside one host application.

Requests come into the host, and the host routes work to the appropriate module. Each module contains its own logic, models, and infrastructure registrations. The modules are separate in code, but not separate applications.

In this sample:

- `ModMonolith.Web` is the host and MVC application
- `ModMonolith.Modules.Catalog` owns product behavior
- `ModMonolith.Modules.Orders` owns order behavior
- `ModMonolith.Shared` contains shared abstractions and cross-module contracts

The host composes the modules during startup, but the business behavior remains inside the modules.

## The important boundary

The most important rule in a modular monolith is that modules should not reach into each other freely.

Good interaction:

- one module calls another through an interface or contract
- the host wires dependencies together
- data exposed across module boundaries is intentional

Bad interaction:

- one module directly manipulating another module's database entities
- shared "utility" code becoming a dumping ground for everything
- business rules spread across multiple modules without a clear owner

If those boundaries erode, the system becomes a normal tightly coupled monolith again.

## Build and deployment model

This architecture is still one deployable unit.

That means:

- you build the whole application together
- you deploy the whole application together
- a change in one module usually causes a redeploy of the host application

For this repository, `ModMonolith.Web` is the deployable host. The module projects are part of that host, not separate deployed apps.

## Database model

A modular monolith often uses one relational database, but that does not mean every module should behave as if the whole schema is shared indiscriminately.

A healthier pattern is:

- each module owns its own tables or schema area
- cross-module access goes through contracts or application services
- the database is shared physically, but ownership is still explicit logically

In this sample:

- the `Catalog` module owns product data
- the `Orders` module owns order data
- both are stored in the same SQL Server database

This keeps operations simple while still preserving module ownership.

## Request flow in this sample

The request flow looks like this:

1. A browser request reaches `ModMonolith.Web`.
2. MVC controllers in the host handle page rendering and form submission.
3. The host or controller uses module contracts and the shared `DbContext`.
4. Module logic performs the business operation.
5. Data is stored in SQL Server.
6. The host returns an HTML view or an API response.

The API endpoints under `/api/*` are also part of the same application. They are not separate services.

## How this differs from a traditional layered monolith

A traditional layered monolith often has broad shared layers such as:

- Controllers
- Services
- Repositories
- Data

That can work for smaller systems, but as the system grows it often leads to:

- weak ownership
- business logic spread across the codebase
- difficult reasoning about change impact

A modular monolith instead organizes primarily by business capability.

Instead of asking, "Which service layer file should this go in?", the design asks, "Which module owns this behavior?"

## How this differs from microservices

Microservices split modules into independently running applications.

That gives you:

- independent deployment
- isolated runtime boundaries
- team autonomy at larger scale

But it also adds:

- network communication
- distributed failure modes
- more infrastructure
- more monitoring and deployment complexity

A modular monolith keeps the internal design discipline while staying operationally simple.

## Benefits

- easier local development
- simpler debugging
- simpler deployment and rollback
- lower infrastructure overhead
- clearer business boundaries than a naive monolith
- easier future extraction if a module truly needs to become a service

## Limitations

- modules are not independently deployable
- one host failure affects the whole application
- scaling is usually at the whole-application level
- weak discipline can collapse the boundaries over time

The architecture helps, but it does not enforce good design by itself. The team still has to protect the module boundaries.

## When to choose it

Choose a modular monolith when:

- the domain is large enough to need boundaries
- the team is not ready to operate distributed systems
- release independence is not the main requirement
- simplicity is still more valuable than deployment autonomy

Avoid jumping to microservices just because the codebase is growing. In many cases, a modular monolith is the more technically honest intermediate step.

## Summary

A modular monolith is one application with deliberate internal boundaries.

It gives you:

- monolith deployment simplicity
- modular code organization
- a path to scale the design before scaling the infrastructure

In this repository, that means:

- `ModMonolith.Web` is the single host
- `Catalog` and `Orders` are internal modules
- everything is built and deployed together
- the architecture stays modular by design, not by separate deployment
