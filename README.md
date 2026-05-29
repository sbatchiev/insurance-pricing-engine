# Pricing Engine

Insurance pricing service built for precise monetary calculation, quote persistence, and asynchronous audit delivery.

The implementation includes one end-to-end product: `travel-basic`.

## Tech Stack

- .NET 10 / ASP.NET Core Minimal API
- PostgreSQL with JSONB product configuration
- EF Core
- RabbitMQ with `RabbitMQ.Client`
- Outbox pattern with a hosted background publisher
- Docker Compose
- xUnit

## Project Structure

- `PricingEngine.Api` - HTTP endpoints, API contracts, Swagger/OpenAPI, startup.
- `PricingEngine.Application` - quote use case, repository interfaces, pricing strategy abstraction.
- `PricingEngine.Domain` - quote, money, product definition, and installment domain types.
- `PricingEngine.Infrastructure` - EF Core, PostgreSQL records, repositories, RabbitMQ publisher, outbox worker.

Main quote flow:

1. `POST /quotes` receives `productCode`, `channel`, `currency`, and generic `inputs`.
2. `QuoteService` loads the active `ProductDefinition`.
3. `ProductPricingStrategyResolver` selects an `IProductPricingStrategy`.
4. The strategy calculates net premium, taxes, and fees.
5. `InstallmentOptionCalculator` creates configured installment options.
6. The quote and outbox message are saved to PostgreSQL.
7. `OutboxPublisherBackgroundService` publishes pending audit messages to RabbitMQ.
8. `GET /quotes/{quoteId}` returns a stored quote for audit lookup.

## Run Locally

Requirements:

- Docker
- .NET 10 SDK if running tests or the API outside Docker

Start the full stack:

```bash
docker compose up --build
```

The first run builds the API image, starts PostgreSQL and RabbitMQ, creates the local schema, and loads `travel-basic` from `ProductDefinitions/travel-basic.json`.

URLs:

- API: `http://localhost:8080`
- Swagger UI: `http://localhost:8080/swagger`
- OpenAPI: `http://localhost:8080/openapi/v1.json`
- RabbitMQ UI: `http://localhost:15672`

RabbitMQ credentials:

```text
pricing_audit / pricing_audit_password
```

PostgreSQL uses a named Docker volume (`pricing-postgres`) and RabbitMQ uses `pricing-rabbitmq`, so local data survives normal container stops. Use `docker compose down -v` only when you intentionally want to delete local data.

Run tests:

```bash
dotnet test PricingEngine.slnx
```

## Example Request

```bash
curl -X POST http://localhost:8080/quotes/ \
  -H "Content-Type: application/json" \
  -d '{
    "productCode": "travel-basic",
    "channel": "web",
    "currency": "EUR",
    "inputs": {
      "insuredSum": 10000,
      "durationDays": 7,
      "package": "standard"
    }
  }'
```

The same request is available in `requests/quotes.http`.

Fetch a stored quote:

```bash
curl http://localhost:8080/quotes/{quoteId}
```

## Configuration

Installment options and outbox polling are configured in `src/PricingEngine.Api/appsettings.json`:

```json
{
  "Quote": {
    "InstallmentCounts": [1, 2, 4]
  },
  "Outbox": {
    "BatchSize": 20,
    "PollingIntervalSeconds": 5
  }
}
```

The implemented product definition is:

```text
src/PricingEngine.Api/ProductDefinitions/travel-basic.json
```

Product definitions are loaded at startup by:

```text
src/PricingEngine.Infrastructure/Database/DatabaseInitializer.cs
```

## Persistence

The schema avoids product-specific tables.

`product_definitions`

- `code`
- `version`
- `input_schema_json`
- `pricing_config_json`
- `updated_at`

`quotes`

- `product_code`
- `product_version`
- `channel`
- `currency`
- `net_premium`
- `taxes`
- `fees`
- `total`
- `request_json`
- `response_json`
- `created_at`

`outbox_messages`

- `type`
- `payload_json`
- `status`
- `retry_count`
- `next_attempt_at`
- `processed_at`
- `last_error`

## Resilience and Auditing

Every successful quote is persisted before the API returns.

`QuoteRepository` saves the quote and a `QuoteGenerated` outbox message in the same EF Core `SaveChangesAsync` call. RabbitMQ publishing happens later in `OutboxPublisherBackgroundService`.

If RabbitMQ is unavailable, the quote remains stored and the outbox message stays pending. Failed publish attempts update `retry_count`, `next_attempt_at`, and `last_error`.

## Precision

Money is represented by `PricingEngine.Domain/Money.cs`.

The code uses `decimal`, not floating point types. `Money` rounds to 2 decimals with `MidpointRounding.AwayFromZero`.

Installment schedules preserve the quote total exactly. Any rounding remainder is applied to the last installment.

## Assumptions

- A generated quote is an audit record; bind/purchase flow is out of scope.
- Product definitions are loaded from JSON files for this proof-of-concept.
- The active product definition with the highest version is used.
- Full JSON Schema validation is a future improvement; strategies currently validate the inputs they need.
- Monetary values are rounded to 2 decimal places.
- Installments are monthly, starting from quote creation date.
- PostgreSQL is the source of truth for audit records; RabbitMQ delivery is eventually consistent.
- `EnsureCreated` is used for local setup; production would use migrations.
- Authentication and authorization are out of scope.

## Adding a new product

The engine supports two extension paths.

### 1. Existing pricing model

If the product fits the existing model:

```text
insured sum * tariff + fixed fees + taxes
```

add a JSON file under:

```text
src/PricingEngine.Api/ProductDefinitions
```

Example `home-basic.json`:

```json
{
  "id": "11111111-1111-1111-1111-111111111111",
  "code": "home-basic",
  "name": "Home Basic",
  "version": 1,
  "isActive": true,
  "inputSchema": {
    "type": "object",
    "required": ["buildingValue"],
    "properties": {
      "buildingValue": { "type": "number", "minimum": 1 },
      "constructionType": { "type": "string" }
    }
  },
  "pricingConfig": {
    "pricingModel": "insured_sum_tariff",
    "insuredSumInput": "buildingValue",
    "tariff": 0.0025,
    "fixedFee": 20.00,
    "taxRate": 0.08,
    "policyFee": 5.00
  }
}
```

No API endpoint, request DTO, database migration, or quote engine change is needed. `InsuredSumTariffPricingStrategy` reads the configured input name and coefficients.

### 2. Structurally different pricing model

For a product with tiered tariffs:

1. Add `src/PricingEngine.Infrastructure/Products/TieredTariffPricingStrategy.cs`.
2. Implement `IProductPricingStrategy`.
3. Make `CanPrice` check for `"pricingModel": "tiered_tariff"`.
4. Register it in `ServiceCollectionExtensions.cs`:

```csharp
services.AddScoped<IProductPricingStrategy, TieredTariffPricingStrategy>();
```

5. Add `src/PricingEngine.Api/ProductDefinitions/home-tiered.json`:

```json
{
  "id": "11111111-1111-1111-1111-111111111112",
  "code": "home-tiered",
  "name": "Home Tiered",
  "version": 1,
  "isActive": true,
  "inputSchema": {
    "type": "object",
    "required": ["buildingValue"],
    "properties": {
      "buildingValue": { "type": "number", "minimum": 1 },
      "constructionType": { "type": "string" },
      "hasSecuritySystem": { "type": "boolean" }
    }
  },
  "pricingConfig": {
    "pricingModel": "tiered_tariff",
    "insuredSumInput": "buildingValue",
    "tiers": [
      { "upTo": 50000, "tariff": 0.0040 },
      { "upTo": 200000, "tariff": 0.0030 },
      { "upTo": null, "tariff": 0.0022 }
    ],
    "fixedFee": 25.00,
    "taxRate": 0.08
  }
}
```

The request still uses `POST /quotes`; only `productCode` and `inputs` change. `QuoteService`, persistence, outbox, and the API contract remain unchanged.
