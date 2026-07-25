# E-Commerce Order & Payment System

A backend assessment project for **Raco** — a simple e-commerce API built with **ASP.NET Core (.NET 10)**, following **Clean Architecture**, **CQRS (via MediatR)**, and integrating two payment gateways: **Stripe** and **bKash**.

Customers can browse products, manage a cart, check out, and pay via Stripe or bKash. Admins can manage the product catalog and category hierarchy.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 10 (Web API) |
| Database | PostgreSQL (via `Npgsql.EntityFrameworkCore.PostgreSQL`) |
| ORM | Entity Framework Core 10 |
| Auth | ASP.NET Core Identity + JWT Bearer |
| CQRS / Mediator | MediatR |
| Validation | FluentValidation (wired as a MediatR pipeline behavior) |
| Error Handling | ErrorOr (functional result pattern) + Global Exception Middleware |
| Caching | Redis (`Microsoft.Extensions.Caching.StackExchangeRedis`) |
| Payments | Stripe.net (card payments) + bKash Tokenized Checkout (sandbox) |
| API Docs | Swashbuckle (Swagger) with JWT Bearer support |

---

## Architecture

The solution follows **Clean Architecture** with strict inward-facing dependencies:

```
Domain  ←  Application  ←  Infrastructure  ←  Api
```

| Project | Responsibility |
|---|---|
| `ECommerce.Domain` | Entities, enums, constants — no external dependencies |
| `ECommerce.Application` | CQRS Commands/Queries, DTOs, validators, interfaces (`IRepository<T>`, `IPaymentProcessor`, `IUserService`) |
| `ECommerce.Infrastructure` | EF Core `DbContext`, Identity, generic Repository, payment processors, DB seeding |
| `ECommerce.Api` | Controllers, JWT/Swagger setup, middleware, composition root |

**Key patterns used:**
- **CQRS** — every use case is a `Command`/`Query` + `Handler`, dispatched through MediatR
- **Generic Repository** (`IRepository<T>`) — simple CRUD abstraction over EF Core, no Specification pattern (kept intentionally lean for this scope)
- **Strategy Pattern** — `IPaymentProcessor` has two implementations (`StripePaymentProcessor`, `BkashPaymentProcessor`); the checkout handler picks the right one at runtime based on the selected `PaymentProvider`, with zero changes needed to add a third gateway later
- **ErrorOr** — handlers return `ErrorOr<T>` instead of throwing for expected failures (not found, validation, conflict); unexpected exceptions are still caught centrally by `GlobalExceptionMiddleware`

---

## Domain Model

```
UserProfile ──1:1── ApplicationUser (Identity)
UserProfile ──1:N── Cart, Order

Cart ──1:N── CartItem ──N:1── Product
Order ──1:N── OrderItem ──N:1── Product
Order ──1:1── Payment

Category ──self-referencing (ParentCategoryId)──> subcategories
Product ──N:1── Category (primary category)
Product ──N:N── Category (via ProductCategory join table, additional tags)
```

- **Cart** is temporary/mutable (pre-purchase); **Order** is created at checkout and is effectively immutable except for status.
- **OrderItem** snapshots `Price`/`Subtotal` at checkout time, so historical orders stay accurate even if product prices change later.
- **Category** supports a hierarchy (e.g. *Electronics → Smartphones*), traversed with DFS for category trees and "recommended products" lookups.
- **Payment** stores `RawResponse` from the gateway for audit/debugging, plus a normalized `Status`/`TransactionId` for querying.

---

## Features

### Auth
- `POST /api/register` — customer self-registration (always assigned the `Customer` role)
- `POST /api/auth` — login, returns a JWT with `UserId`, `UserProfileId`, and `role` claims
- Admin account is seeded on startup (not publicly registrable, by design)

### Catalog (Admin-managed, publicly readable)
- `POST /api/categories` — create category / subcategory
- `GET` category tree (DFS, cached in Redis)
- `POST /api/products`, `PUT /api/products/{id}`, `DELETE /api/products/{id}` — Admin only
- `GET /api/products`, `GET /api/products/{id}` — public
- `GET /api/products/recommendations` — DFS category traversal + popularity-based recommendations, cached in Redis

### Cart & Checkout (Customer only)
- `GET /api/carts/{userProfileId}` — view cart
- `POST /api/carts/add` — add/update item in cart
- `POST /api/orders/checkout` — converts cart → order + payment, calls the selected payment gateway

### Payments
- `POST /api/payments/webhook` — Stripe webhook (signature-verified), confirms payment and deducts stock
- `POST /api/payments/bkash/create` — creates a bKash payment session
- `GET /api/payments/bkash/execute` — bKash redirect/callback endpoint, finalizes payment and deducts stock

### Customer Dashboard
- `GET /api/customerdashboard/orders` — a customer's full order + payment history

---

## Getting Started

### Prerequisites
- .NET 10 SDK
- Docker (for PostgreSQL and Redis)

### 1. Start dependencies

```bash
# PostgreSQL
docker run -d --name postgres-db \
  -e POSTGRES_DB=ECommerce-Order-Payment \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 \
  postgres

# Redis
docker run -d --name ecommerce-redis -p 6379:6379 -v redis_data:/data redis:alpine
```

### 2. Configure secrets

Set the following in `ECommerce.Api/appsettings.Development.json` or via `dotnet user-secrets` (recommended for anything beyond local sandbox testing):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ECommerce-Order-Payment;Username=postgres;Password=postgres",
    "Redis": "localhost:6379"
  },
  "JwtSettings": { "SecretKey": "<your-secret-key>" },
  "AdminSeed": { "Email": "admin@ecommerce.com", "Password": "Admin@123456" },
  "Stripe": {
    "SecretKey": "<stripe-sandbox-secret-key>",
    "PublishableKey": "<stripe-sandbox-publishable-key>",
    "WebhookSecret": "<stripe-webhook-signing-secret>"
  },
  "Bkash": {
    "BaseUrl": "https://tokenized.sandbox.bka.sh/v1.2.0-beta",
    "AppKey": "<bkash-sandbox-app-key>",
    "AppSecret": "<bkash-sandbox-app-secret>",
    "Username": "<bkash-sandbox-username>",
    "Password": "<bkash-sandbox-password>",
    "CallbackUrl": "http://localhost:5004/api/payments/bkash/execute"
  }
}
```

> Both Stripe and bKash are configured against **sandbox/test environments only**. bKash has no publicly self-serve production credentials; the sandbox is sufficient to demonstrate the full integration end-to-end.

### 3. Run migrations

```bash
dotnet ef migrations add InitialMigration \
  --project ECommerce.Infrastructure --startup-project ECommerce.Api \
  --output-dir DbContext/Migrations

dotnet ef database update --project ECommerce.Api
```

### 4. Run the API

```bash
dotnet run --project ECommerce.Api
```

On first run, roles (`Admin`, `Customer`), an Admin account, and sample Electronics categories/products are seeded automatically. Swagger UI is available at `/swagger` in development.

---

## Testing the Payment Flows

**Stripe** — forward webhooks to your local API using the Stripe CLI:
```bash
npx @stripe/cli listen --forward-to http://localhost:5004/api/payments/webhook
npx @stripe/cli trigger payment_intent.succeeded --add payment_intent:metadata[OrderId]=<order-id>
```

**bKash sandbox test wallet:**
- Wallet number: any valid-format 11-digit BD number (e.g. `01770618575`)
- OTP: `123456`
- PIN: `12345`

---

## Design Notes & Trade-offs

- **Repository pattern over raw `DbContext`** — kept deliberately simple (`IRepository<T>` with basic CRUD + `GetQueryable()`/`GetAsync()` for flexible querying) rather than a full Specification pattern, appropriate for this project's scope.
- **`SaveChangesAsync()` lives in the repository**, not a separate Unit of Work — since all repositories share the same scoped `DbContext`, this still commits atomically; a dedicated `IUnitOfWork` was judged unnecessary overhead here.
- **Product has both a single `CategoryId` (primary category) and a `ProductCategory` many-to-many join** — this lets a product belong to multiple categories (e.g. a phone tagged in both "Smartphones" and "Electronics") while still having one clear "primary" category for simple listing/filtering scenarios.
- **Roles use ASP.NET Identity's default many-to-many user-role schema**, with a "one role per user" rule enforced at the application level (registration only ever assigns `Customer`) rather than restructuring Identity's schema.
- **Admin accounts are never publicly registrable** — seeded on startup — to avoid privilege-escalation via the public registration endpoint.
