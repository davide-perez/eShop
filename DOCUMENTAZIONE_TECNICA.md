# eShop — Documentazione Tecnico-Applicativa

## Guida Completa all'Architettura a Microservizi con .NET Aspire

---

## Sommario

1. [Introduzione e Panoramica](#1-introduzione-e-panoramica)
2. [Stack Tecnologico](#2-stack-tecnologico)
3. [.NET Aspire — L'Orchestratore Cloud-Native](#3-net-aspire--lorchestratorecloud-native)
4. [Architettura dei Microservizi](#4-architettura-dei-microservizi)
5. [Catalog.API — Servizio Catalogo (REST + AI)](#5-catalogapi--servizio-catalogo-rest--ai)
6. [Basket.API — Servizio Carrello (gRPC)](#6-basketapi--servizio-carrello-grpc)
7. [Ordering.API — Servizio Ordini (CQRS + DDD)](#7-orderingapi--servizio-ordini-cqrs--ddd)
8. [Ordering.Domain — Domain-Driven Design in Pratica](#8-orderingdomain--domain-driven-design-in-pratica)
9. [Ordering.Infrastructure — Il Layer di Persistenza](#9-orderinginfrastructure--il-layer-di-persistenza)
10. [OrderProcessor — Worker di Background](#10-orderprocessor--worker-di-background)
11. [PaymentProcessor — Processore di Pagamenti Event-Driven](#11-paymentprocessor--processore-di-pagamenti-event-driven)
12. [Identity.API — Autenticazione e Autorizzazione (OpenID Connect)](#12-identityapi--autenticazione-e-autorizzazione-openid-connect)
13. [WebApp — Frontend Blazor Server](#13-webapp--frontend-blazor-server)
14. [WebAppComponents — Libreria Componenti Condivisi](#14-webappcomponents--libreria-componenti-condivisi)
15. [Mobile.Bff.Shopping — Backend-for-Frontend con YARP](#15-mobilebffshopping--backend-for-frontend-con-yarp)
16. [Webhooks.API e WebhookClient](#16-webhooksapi-e-webhookclient)
17. [Comunicazione tra Microservizi — Event Bus](#17-comunicazione-tra-microservizi--event-bus)
18. [Integration Events e Outbox Pattern](#18-integration-events-e-outbox-pattern)
19. [Flusso Completo di un Ordine (Caso d'Uso End-to-End)](#19-flusso-completo-di-un-ordine-caso-duso-end-to-end)
20. [Cross-Cutting Concerns](#20-cross-cutting-concerns)
21. [Testing](#21-testing)
22. [Gestione Centralizzata delle Dipendenze](#22-gestione-centralizzata-delle-dipendenze)
23. [Pattern Architetturali Utilizzati — Riepilogo](#23-pattern-architetturali-utilizzati--riepilogo)
24. [Glossario](#24-glossario)

---

## 1. Introduzione e Panoramica

**eShop** è un'applicazione di riferimento (reference application) sviluppata dal team .NET di Microsoft che implementa un e-commerce completo utilizzando un'**architettura a microservizi**. Il progetto dimostra le best practice per la costruzione di applicazioni distribuite cloud-native con .NET 9 e .NET Aspire.

### Che cos'è un'architettura a microservizi?

Un'architettura a microservizi decompone un'applicazione monolitica in **servizi piccoli, indipendenti e autonomi**, ognuno dei quali:

- **Possiede il proprio dominio** (bounded context)
- **Ha il proprio database** (database-per-service)
- **Comunica con gli altri** tramite protocolli ben definiti (HTTP/REST, gRPC, messaggi asincroni)
- **Può essere deployato, scalato e aggiornato indipendentemente**

```
┌─────────────────────────────────────────────────────────────────┐
│                        eShop Architecture                       │
│                                                                 │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌───────────────┐   │
│  │ WebApp   │  │ Mobile   │  │ Webhook  │  │   Identity    │   │
│  │ (Blazor) │  │   BFF    │  │  Client  │  │     API       │   │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └───────────────┘   │
│       │              │             │                             │
│  ─────┼──────────────┼─────────────┼──────────────────────────  │
│       │              │             │           API Gateway       │
│  ─────┼──────────────┼─────────────┼──────────────────────────  │
│       │              │             │                             │
│  ┌────▼─────┐  ┌─────▼────┐  ┌────▼─────┐  ┌───────────────┐   │
│  │ Catalog  │  │  Basket  │  │ Ordering │  │   Webhooks    │   │
│  │   API    │  │   API    │  │   API    │  │     API       │   │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └───────────────┘   │
│       │              │             │                             │
│  ┌────▼─────┐  ┌─────▼────┐  ┌────▼─────┐                      │
│  │PostgreSQL│  │  Redis   │  │PostgreSQL│                      │
│  │(catalogdb)│ │          │  │(orderdb) │                      │
│  └──────────┘  └──────────┘  └──────────┘                      │
│                                                                 │
│            ┌──────────────────────────────┐                      │
│            │     RabbitMQ (Event Bus)     │                      │
│            └──────────────────────────────┘                      │
│                     │              │                             │
│            ┌────────▼───┐  ┌───────▼────────┐                   │
│            │   Order    │  │    Payment     │                   │
│            │ Processor  │  │   Processor    │                   │
│            └────────────┘  └────────────────┘                   │
└─────────────────────────────────────────────────────────────────┘
```

### I microservizi di eShop

| Servizio | Tipo | Protocollo | Database | Responsabilità |
|----------|------|-----------|----------|----------------|
| **Catalog.API** | REST (Minimal API) | HTTP | PostgreSQL + pgvector | Gestione prodotti, catalogo, ricerca AI |
| **Basket.API** | gRPC | HTTP/2 | Redis | Gestione del carrello utente |
| **Ordering.API** | REST (Minimal API) + CQRS | HTTP | PostgreSQL | Gestione degli ordini |
| **OrderProcessor** | Background Worker | — | PostgreSQL | Elaborazione asincrona degli ordini |
| **PaymentProcessor** | Event-Driven Worker | — | — | Simulazione pagamenti |
| **Identity.API** | OpenID Connect Server | HTTP | PostgreSQL | Autenticazione / Autorizzazione |
| **Webhooks.API** | REST (Minimal API) | HTTP | PostgreSQL | Sottoscrizioni webhook |
| **WebApp** | Blazor Server | HTTP | — | Frontend utente |
| **Mobile.Bff.Shopping** | Reverse Proxy (YARP) | HTTP | — | BFF per app mobile |
| **WebhookClient** | Web App | HTTP | — | Client dimostrativo per webhook |

---

## 2. Stack Tecnologico

### Framework e Runtime

| Tecnologia | Versione | Scopo |
|-----------|---------|-------|
| **.NET** | 9.0 | Runtime e SDK |
| **.NET Aspire** | 9.0.0 | Orchestrazione cloud-native |
| **ASP.NET Core** | 9.0 | Framework web |
| **Entity Framework Core** | 9.0 | ORM per l'accesso ai dati |
| **Blazor Server** | 9.0 | UI interattiva server-side |
| **gRPC** | 2.67.0 | Comunicazione ad alte prestazioni |
| **MediatR** | 12.4.1 | Mediator pattern per CQRS |
| **FluentValidation** | 11.3.0 | Validazione dei comandi |
| **Duende IdentityServer** | 7.0.6 | Server OpenID Connect / OAuth 2.0 |
| **YARP** | 2.2.0 | Reverse proxy |
| **Polly** | 8.4.2 | Resilienza e retry |

### Infrastruttura

| Componente | Tecnologia | Scopo |
|------------|-----------|-------|
| **Database relazionale** | PostgreSQL + pgvector | Dati di catalogo, ordini, identità, webhook |
| **Cache distribuita** | Redis | Gestione state del carrello |
| **Message Broker** | RabbitMQ | Event Bus per comunicazione asincrona |
| **Osservabilità** | OpenTelemetry (OTLP) | Tracing distribuito, metriche, logging |
| **API Documentation** | OpenAPI + Scalar | Documentazione interattiva API |

### AI e Machine Learning

| Componente | Tecnologia | Scopo |
|------------|-----------|-------|
| **Embeddings** | OpenAI / Ollama + pgvector | Ricerca semantica nel catalogo |
| **Chat** | GPT-4o-mini | Assistente AI (opzionale) |

---

## 3. .NET Aspire — L'Orchestratore Cloud-Native

.NET Aspire è un framework di orchestrazione che semplifica lo sviluppo di applicazioni distribuite. Il progetto **eShop.AppHost** è il cuore dell'orchestrazione.

### Come funziona

Il file `Program.cs` dell'AppHost definisce **tutte le risorse** (servizi, database, cache, message broker) e le loro **dipendenze**:

```csharp
// 1. Creazione del builder dell'applicazione distribuita
var builder = DistributedApplication.CreateBuilder(args);

// 2. Aggiunta di un hook globale per i forwarded headers
builder.AddForwardedHeaders();

// 3. Definizione delle risorse infrastrutturali
var redis = builder.AddRedis("redis");
var rabbitMq = builder.AddRabbitMQ("eventbus")
    .WithLifetime(ContainerLifetime.Persistent);  // Il container sopravvive ai riavvii
var postgres = builder.AddPostgres("postgres")
    .WithImage("ankane/pgvector")                  // Immagine custom con supporto vettoriale
    .WithImageTag("latest")
    .WithLifetime(ContainerLifetime.Persistent);

// 4. Definizione dei database (4 database separati su un singolo server PostgreSQL)
var catalogDb = postgres.AddDatabase("catalogdb");
var identityDb = postgres.AddDatabase("identitydb");
var orderDb = postgres.AddDatabase("orderingdb");
var webhooksDb = postgres.AddDatabase("webhooksdb");
```

### Registrazione dei Microservizi

Ogni microservizio è registrato con `AddProject<>` e le sue dipendenze sono dichiarate con `WithReference()` e `WaitFor()`:

```csharp
// Il Basket API dipende da Redis (per i dati) e RabbitMQ (per gli eventi)
var basketApi = builder.AddProject<Projects.Basket_API>("basket-api")
    .WithReference(redis)                           // Connessione a Redis
    .WithReference(rabbitMq).WaitFor(rabbitMq)      // Connessione + attesa avvio RabbitMQ
    .WithEnvironment("Identity__Url", identityEndpoint);  // URL dell'Identity Server

// Il Catalog API dipende da PostgreSQL e RabbitMQ
var catalogApi = builder.AddProject<Projects.Catalog_API>("catalog-api")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(catalogDb);                      // Connessione al database catalogo

// L'Ordering API ha una dipendenza dal database E da un health check
var orderingApi = builder.AddProject<Projects.Ordering_API>("ordering-api")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(orderDb).WaitFor(orderDb)
    .WithHttpHealthCheck("/health")                 // Health check HTTP
    .WithEnvironment("Identity__Url", identityEndpoint);
```

### Concetti Chiave di Aspire

| Concetto | Descrizione | Esempio |
|----------|-------------|---------|
| **`WithReference`** | Inietta automaticamente la connection string | `.WithReference(redis)` → `ConnectionStrings__redis` |
| **`WaitFor`** | Attende che la dipendenza sia attiva prima dell'avvio | `.WaitFor(rabbitMq)` |
| **`WithEnvironment`** | Imposta una variabile d'ambiente | `.WithEnvironment("Identity__Url", endpoint)` |
| **`GetEndpoint`** | Recupera l'URL di un endpoint | `identityApi.GetEndpoint("https")` |
| **`WithExternalHttpEndpoints`** | Espone l'endpoint all'esterno | Per WebApp e Identity |
| **`WithLifetime(Persistent)`** | Il container non viene ricreato ad ogni avvio | Per Redis, RabbitMQ, PostgreSQL |

### Extensions.cs — Hook del Lifecycle

L'AppHost include estensioni personalizzate, come l'hook per i forwarded headers:

```csharp
internal static class Extensions
{
    // Aggiunge ASPNETCORE_FORWARDEDHEADERS_ENABLED=true a TUTTI i progetti
    public static IDistributedApplicationBuilder AddForwardedHeaders(
        this IDistributedApplicationBuilder builder)
    {
        builder.Services.TryAddLifecycleHook<AddForwardHeadersHook>();
        return builder;
    }

    private class AddForwardHeadersHook : IDistributedApplicationLifecycleHook
    {
        public Task BeforeStartAsync(DistributedApplicationModel appModel, 
            CancellationToken cancellationToken = default)
        {
            // Itera su tutti i progetti e aggiunge la variabile d'ambiente
            foreach (var p in appModel.GetProjectResources())
            {
                p.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
                {
                    context.EnvironmentVariables["ASPNETCORE_FORWARDEDHEADERS_ENABLED"] = "true";
                }));
            }
            return Task.CompletedTask;
        }
    }
}
```

> **Didattica**: I forwarded headers sono necessari quando l'applicazione è dietro un reverse proxy (come in produzione). Permettono al server di conoscere l'indirizzo IP reale del client e lo schema originale (HTTP/HTTPS).

### Integrazione OpenAI (opzionale)

```csharp
public static IDistributedApplicationBuilder AddOpenAI(
    this IDistributedApplicationBuilder builder,
    IResourceBuilder<ProjectResource> catalogApi,
    IResourceBuilder<ProjectResource> webApp)
{
    const string textEmbeddingModelName = "text-embedding-3-small";
    const string chatModelName = "gpt-4o-mini";

    // Supporta sia OpenAI diretto che Azure OpenAI
    IResourceBuilder<IResourceWithConnectionString> openAI;
    if (builder.Configuration.GetConnectionString("openai") is not null)
    {
        // Usa una connessione esistente (API key o Azure endpoint)
        openAI = builder.AddConnectionString("openai");
    }
    else
    {
        // Provisioning automatico su Azure
        openAI = builder.AddAzureOpenAI("openai")
            .AddDeployment(new AzureOpenAIDeployment(chatModelName, "gpt-4o-mini", "2024-07-18"))
            .AddDeployment(new AzureOpenAIDeployment(textEmbeddingModelName, 
                "text-embedding-3-small", "1", skuCapacity: 20));
    }

    // Inietta negli specifici servizi che necessitano dell'AI
    catalogApi.WithReference(openAI)
              .WithEnvironment("AI__OPENAI__EMBEDDINGMODEL", textEmbeddingModelName);
    webApp.WithReference(openAI)
          .WithEnvironment("AI__OPENAI__CHATMODEL", chatModelName);
}
```

---

## 4. Architettura dei Microservizi

### Principi Fondamentali

#### 4.1 Database-per-Service

Ogni microservizio **possiede il proprio database**. Nessun altro servizio può accedere direttamente ai dati di un altro:

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│ Catalog API │     │ Ordering API│     │ Identity API│
└──────┬──────┘     └──────┬──────┘     └──────┬──────┘
       │                   │                   │
       ▼                   ▼                   ▼
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  catalogdb  │     │  orderingdb │     │  identitydb │
└─────────────┘     └─────────────┘     └─────────────┘
      PostgreSQL           PostgreSQL          PostgreSQL
```

> **Perché?** Questo pattern garantisce l'**autonomia** dei servizi. Se l'Ordering API modifica il suo schema, il Catalog API non ne è influenzato.

#### 4.2 Comunicazione Sincrona vs Asincrona

eShop utilizza **entrambi i modelli**:

| Tipo | Tecnologia | Quando |
|------|-----------|--------|
| **Sincrona** (richiesta-risposta) | REST (HTTP) / gRPC | Quando il client necessita di una risposta immediata |
| **Asincrona** (event-driven) | RabbitMQ | Quando un evento deve propagarsi senza attendere risposta |

```
Sincrona (query/command diretto):
WebApp ──HTTP──▶ Catalog.API ──▶ PostgreSQL
WebApp ──gRPC──▶ Basket.API  ──▶ Redis

Asincrona (event-driven):
Ordering.API ──publish──▶ RabbitMQ ──subscribe──▶ Catalog.API
                                   ──subscribe──▶ Basket.API
                                   ──subscribe──▶ PaymentProcessor
```

#### 4.3 Bounded Context

Ogni microservizio ha il proprio **bounded context** (contesto delimitato), un concetto fondamentale del Domain-Driven Design (DDD):

- **Catalog Context**: Prodotti, brand, tipi, prezzi, immagini
- **Basket Context**: Carrello dell'utente (ProductId + Quantity)
- **Ordering Context**: Ordini, buyer, payment methods, order items
- **Identity Context**: Utenti, ruoli, autenticazione

> **Esempio pratico**: Un `CatalogItem` nel Catalog Context ha ~15 proprietà (Name, Description, Price, Embedding, Stock...). Nel Basket Context, lo stesso prodotto è rappresentato solo come `{ ProductId, Quantity }`. Nel Ordering Context diventa un `OrderItem` con proprietà diverse (UnitPrice al momento dell'ordine, Discount, Units).

---

## 5. Catalog.API — Servizio Catalogo (REST + AI)

Il servizio Catalogo gestisce i prodotti, le categorie e implementa la ricerca — inclusa la **ricerca semantica AI**.

### 5.1 Struttura del Progetto

```
Catalog.API/
├── Program.cs                    # Entry point
├── CatalogOptions.cs             # Configurazione
├── Apis/
│   └── CatalogApi.cs             # Definizione delle Minimal API routes
├── Extensions/
│   └── Extensions.cs             # Registrazione servizi (DI)
├── Infrastructure/
│   ├── CatalogContext.cs         # DbContext EF Core
│   ├── CatalogContextSeed.cs     # Seed dei dati iniziali
│   ├── EntityConfigurations/     # Configurazioni Fluent API
│   ├── Exceptions/               # Eccezioni di dominio
│   └── Migrations/               # Migrazioni EF Core
├── IntegrationEvents/
│   ├── CatalogIntegrationEventService.cs
│   ├── EventHandling/            # Handler degli eventi in ingresso
│   └── Events/                   # Definizione degli eventi
├── Model/
│   ├── CatalogItem.cs            # Entità prodotto
│   ├── CatalogBrand.cs           # Entità marca
│   ├── CatalogType.cs            # Entità tipo/categoria
│   ├── PaginatedItems.cs         # DTO paginazione
│   ├── PaginationRequest.cs      # Request paginazione
│   └── CatalogServices.cs       # Parameter object per DI
├── Services/
│   ├── ICatalogAI.cs             # Interfaccia servizio AI
│   └── CatalogAI.cs              # Implementazione AI
├── Pics/                         # Immagini dei prodotti
└── Setup/                        # Script di setup
```

### 5.2 Program.cs — Bootstrap del Servizio

```csharp
var builder = WebApplication.CreateBuilder(args);

// Aggiunge i service defaults (OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Registra tutti i servizi applicativi (DB, EventBus, AI...)
builder.AddApplicationServices();

// ProblemDetails per una gestione errori standardizzata (RFC 7807)
builder.Services.AddProblemDetails();

// API Versioning
var withApiVersioning = builder.Services.AddApiVersioning();
builder.AddDefaultOpenApi(withApiVersioning);

var app = builder.Build();

// Mappa gli endpoint di default (health checks)
app.MapDefaultEndpoints();

// Mappa le API del catalogo con versioning
app.NewVersionedApi("Catalog").MapCatalogApiV1();

// Abilita OpenAPI/Scalar
app.UseDefaultOpenApi();

app.Run();
```

### 5.3 Registrazione Servizi (Extensions.cs)

```csharp
public static void AddApplicationServices(this IHostApplicationBuilder builder)
{
    // 1. Registrazione PostgreSQL con EF Core + pgvector
    builder.AddNpgsqlDbContext<CatalogContext>("catalogdb", 
        configureDbContextOptions: dbContextOptionsBuilder =>
    {
        dbContextOptionsBuilder.UseNpgsql(npgsqlBuilder =>
        {
            npgsqlBuilder.UseVector();  // Abilita il supporto per i vettori pgvector
        });
    });

    // 2. Migrazione automatica del database con seed dei dati
    builder.Services.AddMigration<CatalogContext, CatalogContextSeed>();

    // 3. Servizio per l'Outbox Pattern degli Integration Events
    builder.Services.AddTransient<IIntegrationEventLogService, 
                                  IntegrationEventLogService<CatalogContext>>();
    builder.Services.AddTransient<ICatalogIntegrationEventService, 
                                  CatalogIntegrationEventService>();

    // 4. Registrazione Event Bus (RabbitMQ) con sottoscrizioni
    builder.AddRabbitMqEventBus("eventbus")
           .AddSubscription<OrderStatusChangedToAwaitingValidationIntegrationEvent, 
                            OrderStatusChangedToAwaitingValidationIntegrationEventHandler>()
           .AddSubscription<OrderStatusChangedToPaidIntegrationEvent, 
                            OrderStatusChangedToPaidIntegrationEventHandler>();

    // 5. Configurazione AI (opzionale - Ollama o OpenAI)
    if (builder.Configuration["AI:Ollama:Endpoint"] is string ollamaEndpoint 
        && !string.IsNullOrWhiteSpace(ollamaEndpoint))
    {
        builder.Services.AddEmbeddingGenerator<string, Embedding<float>>(b => b
            .UseOpenTelemetry()
            .UseLogging()
            .Use(new OllamaEmbeddingGenerator(new Uri(ollamaEndpoint), 
                 builder.Configuration["AI:Ollama:EmbeddingModel"])));
    }
    else if (!string.IsNullOrWhiteSpace(
             builder.Configuration.GetConnectionString("openai")))
    {
        builder.AddOpenAIClientFromConfiguration("openai");
        builder.Services.AddEmbeddingGenerator<string, Embedding<float>>(b => b
            .UseOpenTelemetry()
            .UseLogging()
            .Use(b.Services.GetRequiredService<OpenAIClient>()
                 .AsEmbeddingGenerator(
                     builder.Configuration["AI:OpenAI:EmbeddingModel"]!)));
    }

    builder.Services.AddScoped<ICatalogAI, CatalogAI>();
}
```

### 5.4 Minimal API — Le Routes del Catalogo

Le **Minimal API** di ASP.NET Core 9 sostituiscono i controller MVC tradizionali con un approccio più leggero e funzionale:

```csharp
public static class CatalogApi
{
    public static RouteGroupBuilder MapCatalogApiV1(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/catalog").HasApiVersion(1.0);

        // ── GET: Lettura ──────────────────────────────────────────
        api.MapGet("/items", GetAllItems);
        api.MapGet("/items/by", GetItemsByIds);
        api.MapGet("/items/{id:int}", GetItemById);
        api.MapGet("/items/by/{name:minlength(1)}", GetItemsByName);
        api.MapGet("/items/{id:int}/pic", GetItemPictureById);
        api.MapGet("/items/type/{typeId}/brand/{brandId?}", GetItemsByBrandAndTypeId);
        api.MapGet("/items/type/all/brand/{brandId:int?}", GetItemsByBrandId);
        api.MapGet("/catalogtypes", GetAllCatalogTypes);
        api.MapGet("/catalogbrands", GetAllCatalogBrands);

        // ── GET: Ricerca Semantica AI ─────────────────────────────
        api.MapGet("/items/withsemanticrelevance/{text:minlength(1)}", 
                   GetItemsBySemanticRelevance);

        // ── PUT/POST/DELETE: Scrittura ────────────────────────────
        api.MapPut("/items", UpdateItem);
        api.MapPost("/items", CreateItem);
        api.MapDelete("/items/{id:int}", DeleteItemById);

        return api;
    }
}
```

#### Esempio: Endpoint con paginazione

```csharp
public static async Task<Results<Ok<PaginatedItems<CatalogItem>>, BadRequest<string>>> 
    GetAllItems(
        [AsParameters] PaginationRequest paginationRequest,
        [AsParameters] CatalogServices services)
{
    var pageSize = paginationRequest.PageSize;
    var pageIndex = paginationRequest.PageIndex;
    var totalItems = await services.Context.CatalogItems
        .LongCountAsync();

    var itemsOnPage = await services.Context.CatalogItems
        .OrderBy(c => c.Name)
        .Skip(pageSize * pageIndex)
        .Take(pageSize)
        .ToListAsync();

    return TypedResults.Ok(
        new PaginatedItems<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage));
}
```

#### Esempio: Ricerca Semantica con AI

```csharp
public static async Task<Results<Ok<PaginatedItems<CatalogItem>>, 
                                 RedirectToRouteHttpResult>> 
    GetItemsBySemanticRelevance(
        [AsParameters] PaginationRequest paginationRequest,
        [AsParameters] CatalogServices services,
        string text)
{
    // Se l'AI non è configurata, fallback alla ricerca per nome
    if (!services.CatalogAI.IsEnabled)
    {
        return TypedResults.RedirectToRoute("GetItemsByName", 
            new { name = text });
    }

    // 1. Genera l'embedding vettoriale del testo di ricerca
    var vector = await services.CatalogAI.GetEmbeddingAsync(text);

    // 2. Cerca per distanza coseno nel database (pgvector)
    var itemsOnPage = await services.Context.CatalogItems
        .OrderBy(c => c.Embedding!.CosineDistance(vector))  // ← pgvector!
        .Skip(paginationRequest.PageSize * paginationRequest.PageIndex)
        .Take(paginationRequest.PageSize)
        .ToListAsync();

    // ...
}
```

> **Didattica**: pgvector è un'estensione di PostgreSQL che aggiunge il supporto per vettori (array di float). La **distanza coseno** misura la similarità tra due vettori: più è vicina a 0, più i concetti sono semanticamente simili. Questo permette di cercare "scarpe sportive rosse" e trovare anche "sneakers cremisi per running".

#### Esempio: Update con Integration Event

```csharp
public static async Task<Results<Created, NotFound<string>>> UpdateItem(
    [AsParameters] CatalogServices services,
    CatalogItem productToUpdate)
{
    var catalogItem = await services.Context.CatalogItems
        .SingleOrDefaultAsync(i => i.Id == productToUpdate.Id);

    if (catalogItem == null)
    {
        return TypedResults.NotFound($"Item with id {productToUpdate.Id} not found.");
    }

    // Se il prezzo è cambiato, pubblica un evento di integrazione
    var priceEntry = services.Context.Entry(catalogItem).Property(i => i.Price);
    if (priceEntry.IsModified)
    {
        // Crea l'evento di cambio prezzo
        var priceChangedEvent = new ProductPriceChangedIntegrationEvent(
            catalogItem.Id, productToUpdate.Price, priceEntry.OriginalValue);

        // Salva atomicamente: modifica DB + registrazione evento (Outbox Pattern)
        await services.IntegrationEventService
            .SaveEventAndCatalogContextChangesAsync(priceChangedEvent);

        // Pubblica l'evento sul bus
        await services.IntegrationEventService
            .PublishThroughEventBusAsync(priceChangedEvent);
    }
    else
    {
        await services.Context.SaveChangesAsync();
    }

    return TypedResults.Created($"/api/catalog/items/{productToUpdate.Id}");
}
```

### 5.5 Modello di Dominio del Catalogo

```csharp
public class CatalogItem
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string PictureFileName { get; set; }

    // Relazioni
    public int CatalogTypeId { get; set; }
    public CatalogType CatalogType { get; set; }
    public int CatalogBrandId { get; set; }
    public CatalogBrand CatalogBrand { get; set; }

    // Gestione dello stock
    public int AvailableStock { get; set; }
    public int RestockThreshold { get; set; }
    public int MaxStockThreshold { get; set; }
    public bool OnReorder { get; set; }

    // AI: Embedding vettoriale per ricerca semantica
    public Vector Embedding { get; set; }

    // Logica di dominio con protezione degli invarianti
    public int RemoveStock(int quantityDesired)
    {
        if (AvailableStock == 0)
            throw new CatalogDomainException($"Empty stock for item: {Name}");
        
        int removed = Math.Min(quantityDesired, AvailableStock);
        AvailableStock -= removed;
        return removed;
    }

    public int AddStock(int quantity)
    {
        int original = AvailableStock;
        if ((AvailableStock + quantity) > MaxStockThreshold)
        {
            AvailableStock += (MaxStockThreshold - AvailableStock);
        }
        else
        {
            AvailableStock += quantity;
        }
        OnReorder = false;
        return AvailableStock - original;
    }
}
```

### 5.6 Parameter Object Pattern (`[AsParameters]`)

Invece di iniettare molti parametri in ogni endpoint, si usa un **parameter object**:

```csharp
public class CatalogServices(
    CatalogContext context,
    ICatalogAI catalogAI,
    IOptions<CatalogOptions> options,
    ILogger<CatalogServices> logger,
    ICatalogIntegrationEventService integrationEventService)
{
    public CatalogContext Context { get; } = context;
    public ICatalogAI CatalogAI { get; } = catalogAI;
    public IOptions<CatalogOptions> Options { get; } = options;
    public ILogger<CatalogServices> Logger { get; } = logger;
    public ICatalogIntegrationEventService IntegrationEventService { get; } = integrationEventService;
}

// Utilizzo nell'endpoint:
public static async Task<Ok<PaginatedItems<CatalogItem>>> GetAllItems(
    [AsParameters] PaginationRequest paginationRequest,
    [AsParameters] CatalogServices services)  // ← Tutti i servizi iniettati in un oggetto
```

---

## 6. Basket.API — Servizio Carrello (gRPC)

Il servizio Basket utilizza **gRPC** per la comunicazione ad alte prestazioni e **Redis** per lo storage.

### 6.1 Perché gRPC?

| Caratteristica | REST (JSON) | gRPC (Protocol Buffers) |
|---------------|------------|------------------------|
| **Serializzazione** | Testo (JSON) | Binario (protobuf) |
| **Performance** | Buona | 5-10x più veloce |
| **Payload size** | Maggiore | ~30% più piccolo |
| **Streaming** | Limitato | Nativo (unary, server, client, bidirezionale) |
| **Type-safety** | No (schema opzionale) | Sì (contratto `.proto`) |
| **Protocollo** | HTTP/1.1 o 2 | HTTP/2 obbligatorio |

### 6.2 Definizione del Contratto (Proto)

```protobuf
syntax = "proto3";

option csharp_namespace = "eShop.Basket.API.Grpc";
package BasketApi;

// Definizione del servizio con 3 operazioni
service Basket {
    rpc GetBasket(GetBasketRequest) returns (CustomerBasketResponse) {}
    rpc UpdateBasket(UpdateBasketRequest) returns (CustomerBasketResponse) {}
    rpc DeleteBasket(DeleteBasketRequest) returns (DeleteBasketResponse) {}
}

// Messaggi di request/response
message GetBasketRequest { }    // Vuoto: il buyerId viene dal token JWT

message CustomerBasketResponse {
    repeated BasketItem items = 1;  // Lista di prodotti nel carrello
}

message BasketItem {
    int32 product_id = 2;   // ID del prodotto nel catalogo
    int32 quantity = 6;     // Quantità
}

message UpdateBasketRequest {
    repeated BasketItem items = 2;  // Nuovo contenuto del carrello
}

message DeleteBasketRequest { }
message DeleteBasketResponse { }
```

> **Didattica**: I file `.proto` fungono da **contratto** tra client e server. Il compilatore `protoc` genera automaticamente le classi C# per client e server. I numeri nei campi (`= 1`, `= 2`, `= 6`) sono **tag** usati nella serializzazione binaria e non devono mai cambiare una volta in produzione.

### 6.3 Implementazione del Servizio gRPC

```csharp
public class BasketService(IBasketRepository repository) : Basket.BasketBase
{
    // Consente l'accesso anche senza autenticazione (per la navigazione)
    [AllowAnonymous]
    public override async Task<CustomerBasketResponse> GetBasket(
        GetBasketRequest request, ServerCallContext context)
    {
        // Ottiene il buyerId dal token JWT dell'utente autenticato
        var userId = context.GetUserIdentity();
        if (string.IsNullOrEmpty(userId))
        {
            return new();  // Carrello vuoto per utenti non autenticati
        }

        // Recupera il carrello da Redis
        var data = await repository.GetBasketAsync(userId);
        if (data is not null)
        {
            return MapToCustomerBasketResponse(data);
        }
        return new();
    }

    public override async Task<CustomerBasketResponse> UpdateBasket(
        UpdateBasketRequest request, ServerCallContext context)
    {
        var userId = context.GetUserIdentity();

        // Mappa dai tipi gRPC al modello di dominio
        var customerBasket = MapToCustomerBasket(userId, request);

        // Salva in Redis
        var response = await repository.UpdateBasketAsync(customerBasket);
        if (response is null)
        {
            ThrowNotFound();
        }

        return MapToCustomerBasketResponse(response);
    }

    public override async Task<DeleteBasketResponse> DeleteBasket(
        DeleteBasketRequest request, ServerCallContext context)
    {
        var userId = context.GetUserIdentity();
        await repository.DeleteBasketAsync(userId);
        return new();
    }
}
```

### 6.4 Repository Redis

```csharp
public class RedisBasketRepository(
    ILogger<RedisBasketRepository> logger, 
    IConnectionMultiplexer redis) : IBasketRepository
{
    private readonly IDatabase _database = redis.GetDatabase();

    // Prefisso per le chiavi Redis (/basket/userId)
    private static RedisKey BasketKeyPrefix = "/basket/"u8.ToArray();
    private static RedisKey GetBasketKey(string userId) => 
        BasketKeyPrefix.Append(userId);

    public async Task<CustomerBasket> GetBasketAsync(string customerId)
    {
        // Usa StringGetLeaseAsync per evitare copie di memoria
        using var data = await _database.StringGetLeaseAsync(
            GetBasketKey(customerId));

        if (data is null || data.Length == 0) return null;

        // Deserializza con source-generated JsonSerializer (AOT-friendly)
        return JsonSerializer.Deserialize(data.Span, 
            BasketSerializationContext.Default.CustomerBasket);
    }

    public async Task<CustomerBasket> UpdateBasketAsync(CustomerBasket basket)
    {
        // Serializza in UTF-8 bytes (più efficiente di string)
        var json = JsonSerializer.SerializeToUtf8Bytes(basket, 
            BasketSerializationContext.Default.CustomerBasket);

        var created = await _database.StringSetAsync(
            GetBasketKey(basket.BuyerId), json);

        if (!created)
        {
            logger.LogInformation("Problem occurred persisting the item.");
            return null;
        }
        return await GetBasketAsync(basket.BuyerId);
    }

    public async Task<bool> DeleteBasketAsync(string id)
    {
        return await _database.KeyDeleteAsync(GetBasketKey(id));
    }
}

// Source Generator per la serializzazione JSON (performance ottimali, AOT-compatible)
[JsonSerializable(typeof(CustomerBasket))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
public partial class BasketSerializationContext : JsonSerializerContext { }
```

> **Didattica**: `JsonSerializerContext` con source generation genera il codice di serializzazione/deserializzazione a **compile time** anziché a runtime con reflection. Questo porta a:
> - Startup più veloce
> - Meno allocazioni di memoria
> - Compatibilità con AOT (Ahead-of-Time compilation)

### 6.5 Integration Events del Basket

Quando un ordine viene creato, il carrello del cliente deve essere svuotato:

```csharp
// L'evento è pubblicato dall'Ordering.API quando un nuovo ordine inizia
public record OrderStartedIntegrationEvent(string UserId) : IntegrationEvent;

// L'handler nel Basket.API reagisce svuotando il carrello
public class OrderStartedIntegrationEventHandler(
    IBasketRepository repository,
    ILogger<OrderStartedIntegrationEventHandler> logger) 
    : IIntegrationEventHandler<OrderStartedIntegrationEvent>
{
    public async Task Handle(OrderStartedIntegrationEvent @event)
    {
        logger.LogInformation(
            "Handling integration event: {IntegrationEventId} - ({@IntegrationEvent})", 
            @event.Id, @event);

        // Elimina il carrello dell'utente che ha creato l'ordine
        await repository.DeleteBasketAsync(@event.UserId);
    }
}
```

---

## 7. Ordering.API — Servizio Ordini (CQRS + DDD)

Il servizio Ordering è il più complesso e dimostra l'implementazione del pattern **CQRS** (Command Query Responsibility Segregation) con **MediatR** e **Domain-Driven Design**.

### 7.1 Struttura Applicativa

```
Ordering.API/
├── Program.cs
├── Apis/
│   ├── OrdersApi.cs              # Route definitions (Minimal API)
│   └── OrderServices.cs          # Parameter object per DI
├── Application/
│   ├── Behaviors/                # Pipeline behaviors di MediatR
│   │   ├── LoggingBehavior.cs    # Logging automatico dei comandi
│   │   ├── TransactionBehavior.cs # Gestione transazioni
│   │   └── ValidatorBehavior.cs  # Validazione con FluentValidation
│   ├── Commands/                 # Comandi CQRS (scrittura)
│   │   ├── CreateOrderCommand.cs
│   │   ├── CancelOrderCommand.cs
│   │   ├── ShipOrderCommand.cs
│   │   ├── IdentifiedCommand.cs  # Idempotency wrapper
│   │   └── ...Handler.cs        # Handler per ogni comando
│   ├── DomainEventHandlers/      # Handler per eventi di dominio
│   ├── IntegrationEvents/        # Eventi di integrazione
│   │   ├── EventHandling/        # Handler eventi in ingresso
│   │   └── Events/               # Definizioni eventi
│   ├── Models/                   # DTO condivisi
│   ├── Queries/                  # Query CQRS (lettura)
│   │   ├── IOrderQueries.cs
│   │   ├── OrderQueries.cs       # Query con Dapper (read-optimized)
│   │   └── OrderViewModel.cs     # DTO di output
│   └── Validations/              # Validatori FluentValidation
```

### 7.2 CQRS — Command Query Responsibility Segregation

Il pattern CQRS **separa le operazioni di lettura da quelle di scrittura**:

```
                       ┌───────────────────────────────┐
                       │         API Endpoints          │
                       └───────────┬───────────────────┘
                                   │
                      ┌────────────┴────────────┐
                      │                         │
              ┌───────▼───────┐         ┌───────▼───────┐
              │   COMMANDS    │         │    QUERIES    │
              │  (Scrittura)  │         │   (Lettura)   │
              └───────┬───────┘         └───────┬───────┘
                      │                         │
              ┌───────▼───────┐         ┌───────▼───────┐
              │   MediatR     │         │    Dapper     │
              │  + EF Core    │         │  (SQL diretto)│
              │  + Domain     │         └───────┬───────┘
              │    Events     │                 │
              └───────┬───────┘                 │
                      │                         │
              ┌───────▼─────────────────────────▼───────┐
              │              PostgreSQL                  │
              │             (orderingdb)                 │
              └─────────────────────────────────────────┘
```

> **Perché CQRS?** Le operazioni di lettura e scrittura hanno esigenze diverse:
> - **Scrittura**: Necessita di validazione, domain logic, transazioni, eventi
> - **Lettura**: Necessita di performance, shape dei dati ottimizzata per la UI, nessuna logica di business
> 
> Separandole, si possono ottimizzare indipendentemente.

### 7.3 Esempio di Command

```csharp
// Definizione del comando (immutabile)
public record CreateOrderCommand(
    List<BasketItem> Items,
    string UserId,
    string UserName,
    string City,
    string Street,
    string State,
    string Country,
    string ZipCode,
    string CardNumber,
    string CardHolderName,
    DateTime CardExpiration,
    string CardSecurityNumber,
    int CardTypeId) : IRequest<bool>;  // IRequest<bool> → MediatR lo gestirà

// Handler del comando (logica di business)
public class CreateOrderCommandHandler(
    IOrderRepository orderRepository) 
    : IRequestHandler<CreateOrderCommand, bool>
{
    public async Task<bool> Handle(
        CreateOrderCommand message, CancellationToken cancellationToken)
    {
        // Crea l'indirizzo (Value Object)
        var address = new Address(
            message.Street, message.City, message.State, 
            message.Country, message.ZipCode);

        // Crea l'ordine (Aggregate Root) — genera OrderStartedDomainEvent
        var order = new Order(
            message.UserId, message.UserName, address, 
            message.CardTypeId, message.CardNumber,
            message.CardSecurityNumber, message.CardHolderName, 
            message.CardExpiration);

        // Aggiunge gli item all'ordine
        foreach (var item in message.Items)
        {
            order.AddOrderItem(item.ProductId, item.ProductName, 
                item.UnitPrice, item.Discount, item.PictureUrl, item.Units);
        }

        // Salva (il domainEvent viene dispatchato da SaveEntitiesAsync)
        orderRepository.Add(order);
        return await orderRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
    }
}
```

### 7.4 Idempotency con IdentifiedCommand

Per garantire l'idempotenza (evitare ordini duplicati), ogni command è wrappato in un `IdentifiedCommand`:

```csharp
// Wrapper che aggiunge un RequestId univoco
public class IdentifiedCommand<T, R>(T command, Guid id) : IRequest<R>
    where T : IRequest<R>
{
    public T Command { get; } = command;
    public Guid Id { get; } = id;  // Id univoco della richiesta
}

// Chiamata dall'endpoint API:
public static async Task<Results<Ok, BadRequest<string>>> CreateOrderAsync(
    [FromHeader(Name = "x-requestid")] Guid requestId,  // ID nell'header HTTP
    CreateOrderRequest request,
    [AsParameters] OrderServices services)
{
    var createOrderCommand = new CreateOrderCommand(/* ... */);

    // Wrappa il comando con l'ID di idempotenza
    var requestCreateOrder = new IdentifiedCommand<CreateOrderCommand, bool>(
        createOrderCommand, requestId);

    var result = await services.Mediator.Send(requestCreateOrder);
    return TypedResults.Ok();
}
```

### 7.5 MediatR Pipeline Behaviors

I **Pipeline Behaviors** intercettano ogni comando prima e dopo l'handler, implementando cross-cutting concerns:

```
Richiesta → [LoggingBehavior] → [ValidatorBehavior] → [TransactionBehavior] → Handler
                                                                                 │
Risposta  ← [LoggingBehavior] ← [ValidatorBehavior] ← [TransactionBehavior] ←───┘
```

```csharp
// 1. LoggingBehavior: Logga ogni comando in entrata e uscita
public class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger) 
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling command {CommandName} ({@Command})", 
            typeof(TRequest).Name, request);

        var response = await next();  // ← Passa al behavior successivo

        logger.LogInformation("Command {CommandName} handled - response: {@Response}", 
            typeof(TRequest).Name, response);
        return response;
    }
}

// 2. ValidatorBehavior: Valida il comando con FluentValidation
public class ValidatorBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators) 
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var failures = validators
            .Select(v => v.Validate(request))
            .SelectMany(result => result.Errors)
            .Where(error => error != null)
            .ToList();

        if (failures.Any())
        {
            throw new OrderingDomainException(
                $"Command Validation Errors for type {typeof(TRequest).Name}",
                new ValidationException("Validation exception", failures));
        }

        return await next();
    }
}

// 3. TransactionBehavior: Gestisce la transazione DB
// Garantisce che domain events e integration events siano
// publishati SOLO se la transazione ha successo (atomicità)
```

### 7.6 Query con Dapper (Read Side)

Le query usano **Dapper** (micro-ORM) per performance ottimali:

```csharp
public class OrderQueries(string connectionString) : IOrderQueries
{
    public async Task<Order> GetOrderAsync(int id)
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        var result = await connection.QueryAsync<dynamic>(
            @"SELECT o.""Id"" as ordernumber, o.""OrderDate"" as date, 
                     o.""Description"" as description, o.""Address_City"" as city,
                     o.""Address_Country"" as country, os.""Name"" as status,
                     oi.""ProductName"" as productname, oi.""Units"" as units,
                     oi.""UnitPrice"" as unitprice, oi.""PictureUrl"" as pictureurl
              FROM ordering.orders o
              LEFT JOIN ordering.orderitems oi ON o.""Id"" = oi.""OrderId""
              LEFT JOIN ordering.orderstatus os ON o.""OrderStatusId"" = os.""Id""
              WHERE o.""Id"" = @id",
            new { id });

        // Mappa il risultato in un DTO ottimizzato per la UI
        // ...
    }
}
```

> **Didattica**: Perché Dapper e non EF Core per le query?
> - EF Core ha overhead per il change tracking (inutile in read-only)
> - Dapper è 2-5x più veloce per query semplici
> - Le query possono avere uno shape diverso dal modello di dominio
> - Si può ottimizzare la query SQL manualmente

### 7.7 API Endpoints dell'Ordering

```csharp
public static RouteGroupBuilder MapOrdersApiV1(this IEndpointRouteBuilder app)
{
    var api = app.MapGroup("api/orders").HasApiVersion(1.0);

    api.MapPut("/cancel", CancelOrderAsync);          // Annulla un ordine
    api.MapPut("/ship", ShipOrderAsync);               // Spedisci un ordine
    api.MapGet("{orderId:int}", GetOrderAsync);         // Dettaglio ordine
    api.MapGet("/", GetOrdersByUserAsync);              // Ordini dell'utente
    api.MapGet("/cardtypes", GetCardTypesAsync);        // Tipi di carta
    api.MapPost("/draft", CreateOrderDraftAsync);       // Bozza ordine (preview)
    api.MapPost("/", CreateOrderAsync);                 // Crea ordine

    return api;
}
```

---

## 8. Ordering.Domain — Domain-Driven Design in Pratica

Il progetto `Ordering.Domain` è un esempio eccellente di **Domain-Driven Design (DDD)** applicato.

### 8.1 Concetti DDD Implementati

```
┌─────────────────────────────────────────────────────────────────┐
│                    Domain-Driven Design                         │
│                                                                 │
│  ┌──────────────────────────────────────────┐                   │
│  │           Order Aggregate                │                   │
│  │  ┌──────────────────────────┐            │                   │
│  │  │    Order (Root Entity)   │ ◄─── Aggregate Root            │
│  │  │  • OrderDate             │            │                   │
│  │  │  • OrderStatus           │ ◄─── Smart Enumeration         │
│  │  │  • Address               │ ◄─── Value Object              │
│  │  │  • _orderItems           │ ◄─── Encapsulated Collection   │
│  │  │  • DomainEvents          │ ◄─── Domain Events             │
│  │  └──────────┬───────────────┘            │                   │
│  │             │ contains                   │                   │
│  │  ┌──────────▼───────────────┐            │                   │
│  │  │    OrderItem (Entity)    │            │                   │
│  │  │  • ProductName           │            │                   │
│  │  │  • UnitPrice             │            │                   │
│  │  │  • Units                 │            │                   │
│  │  │  • Discount              │            │                   │
│  │  └──────────────────────────┘            │                   │
│  └──────────────────────────────────────────┘                   │
│                                                                 │
│  ┌──────────────────────────────────────────┐                   │
│  │           Buyer Aggregate                │                   │
│  │  ┌──────────────────────────┐            │                   │
│  │  │   Buyer (Root Entity)    │            │                   │
│  │  │  • IdentityGuid          │            │                   │
│  │  │  • _paymentMethods       │            │                   │
│  │  └──────────┬───────────────┘            │                   │
│  │             │ contains                   │                   │
│  │  ┌──────────▼───────────────┐            │                   │
│  │  │  PaymentMethod (Entity)  │            │                   │
│  │  │  • CardNumber            │            │                   │
│  │  │  • CardType              │ ◄─── Smart Enumeration         │
│  │  │  • Expiration            │            │                   │
│  │  └──────────────────────────┘            │                   │
│  └──────────────────────────────────────────┘                   │
└─────────────────────────────────────────────────────────────────┘
```

### 8.2 SeedWork — Le Basi del DDD

Il namespace `SeedWork` contiene le classi base per implementare DDD:

#### Entity — Classe base per le entità

```csharp
public abstract class Entity
{
    int? _requestedHashCode;
    int _Id;

    public virtual int Id
    {
        get => _Id;
        protected set => _Id = value;
    }

    // Collezione di eventi di dominio
    private List<INotification> _domainEvents;
    public IReadOnlyCollection<INotification> DomainEvents => 
        _domainEvents?.AsReadOnly();

    public void AddDomainEvent(INotification eventItem)
    {
        _domainEvents = _domainEvents ?? new List<INotification>();
        _domainEvents.Add(eventItem);
    }

    public void RemoveDomainEvent(INotification eventItem) =>
        _domainEvents?.Remove(eventItem);

    public void ClearDomainEvents() =>
        _domainEvents?.Clear();

    // Identità basata sull'ID (non sulle proprietà)
    public override bool Equals(object obj)
    {
        if (obj is not Entity entity) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (GetType() != obj.GetType()) return false;
        if (entity.IsTransient() || IsTransient()) return false;
        return entity.Id == Id;
    }

    public bool IsTransient() => Id == default;
}
```

#### Value Object — Oggetto immutabile senza identità

```csharp
public abstract class ValueObject
{
    // L'uguaglianza è determinata dai valori delle proprietà
    protected abstract IEnumerable<object> GetEqualityComponents();

    public override bool Equals(object obj)
    {
        if (obj == null || obj.GetType() != GetType()) return false;
        var other = (ValueObject)obj;
        return GetEqualityComponents()
            .SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x != null ? x.GetHashCode() : 0)
            .Aggregate((x, y) => x ^ y);
    }
}

// Esempio: Address come Value Object
public class Address : ValueObject
{
    public string Street { get; private set; }
    public string City { get; private set; }
    public string State { get; private set; }
    public string Country { get; private set; }
    public string ZipCode { get; private set; }

    public Address(string street, string city, string state, 
                   string country, string zipcode)
    {
        Street = street; City = city; State = state;
        Country = country; ZipCode = zipcode;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return Country;
        yield return ZipCode;
    }
}
```

> **Didattica**: La differenza fondamentale tra Entity e Value Object:
> - **Entity**: Ha un'identità (`Id`). Due entità con le stesse proprietà ma Id diversi sono **diverse** (es. due ordini con lo stesso contenuto).
> - **Value Object**: Non ha identità. Due VO con gli stessi valori sono **uguali** (es. due indirizzi "Via Roma 1, Milano" sono lo stesso indirizzo).

#### Smart Enumeration — Enum come classi

```csharp
public abstract class Enumeration : IComparable
{
    public string Name { get; private set; }
    public int Id { get; private set; }

    protected Enumeration(int id, string name) => (Id, Name) = (id, name);

    public static IEnumerable<T> GetAll<T>() where T : Enumeration =>
        typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                 .Select(f => f.GetValue(null))
                 .Cast<T>();

    public static T FromValue<T>(int value) where T : Enumeration =>
        Parse<T, int>(value, "value", item => item.Id == value);

    public static T FromDisplayName<T>(string displayName) where T : Enumeration =>
        Parse<T, string>(displayName, "display name", item => item.Name == displayName);
}

// Utilizzo concreto:
public class OrderStatus : Enumeration
{
    public static OrderStatus Submitted = new(1, nameof(Submitted));
    public static OrderStatus AwaitingValidation = new(2, nameof(AwaitingValidation));
    public static OrderStatus StockConfirmed = new(3, nameof(StockConfirmed));
    public static OrderStatus Paid = new(4, nameof(Paid));
    public static OrderStatus Shipped = new(5, nameof(Shipped));
    public static OrderStatus Cancelled = new(6, nameof(Cancelled));
}

public class CardType : Enumeration
{
    public static CardType Amex = new(1, nameof(Amex));
    public static CardType Visa = new(2, nameof(Visa));
    public static CardType MasterCard = new(3, nameof(MasterCard));
}
```

> **Didattica**: Le Smart Enumeration offrono vantaggi rispetto agli `enum` standard di C#:
> - Possono contenere logica di business
> - Sono serializzabili come entità nel database
> - Supportano l'ereditarietà
> - Offrono metodi come `GetAll()`, `FromValue()`, `FromDisplayName()`

### 8.3 Order — L'Aggregate Root

```csharp
public class Order : Entity, IAggregateRoot
{
    public DateTime OrderDate { get; private set; }
    public Address Address { get; private set; }
    public int? BuyerId { get; private set; }
    public OrderStatus OrderStatus { get; private set; }
    public string Description { get; private set; }

    // ⚡ Collezione incapsulata: solo l'Aggregate Root può modificarla
    private readonly List<OrderItem> _orderItems;
    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();

    // ── Costruttore ──────────────────────────────────────────────
    public Order(string userId, string userName, Address address,
                 int cardTypeId, string cardNumber, string cardSecurityNumber,
                 string cardHolderName, DateTime cardExpiration,
                 int? buyerId = null, int? paymentMethodId = null)
    {
        _orderItems = new List<OrderItem>();

        OrderStatus = OrderStatus.Submitted;
        OrderDate = DateTime.UtcNow;
        Address = address;
        BuyerId = buyerId;

        // Genera un Domain Event → verrà gestito da un handler
        AddDomainEvent(new OrderStartedDomainEvent(
            this, userId, userName, cardTypeId, cardNumber,
            cardSecurityNumber, cardHolderName, cardExpiration));
    }

    // ── Metodi di business ──────────────────────────────────────
    public void AddOrderItem(int productId, string productName,
        decimal unitPrice, decimal discount, string pictureUrl, int units = 1)
    {
        // Verifica se il prodotto è già nel carrello → incrementa quantità
        var existingOrderForProduct = _orderItems
            .SingleOrDefault(o => o.ProductId == productId);

        if (existingOrderForProduct != null)
        {
            existingOrderForProduct.AddUnits(units);
        }
        else
        {
            _orderItems.Add(new OrderItem(
                productId, productName, unitPrice, discount, pictureUrl, units));
        }
    }

    // ── Macchina a stati dell'ordine ────────────────────────────

    public void SetAwaitingValidationStatus()
    {
        if (OrderStatus == OrderStatus.Submitted)
        {
            AddDomainEvent(new OrderStatusChangedToAwaitingValidationDomainEvent(
                Id, _orderItems));
            OrderStatus = OrderStatus.AwaitingValidation;
        }
    }

    public void SetStockConfirmedStatus()
    {
        if (OrderStatus == OrderStatus.AwaitingValidation)
        {
            AddDomainEvent(new OrderStatusChangedToStockConfirmedDomainEvent(Id));
            OrderStatus = OrderStatus.StockConfirmed;
        }
    }

    public void SetPaidStatus()
    {
        if (OrderStatus == OrderStatus.StockConfirmed)
        {
            AddDomainEvent(new OrderStatusChangedToPaidDomainEvent(
                Id, OrderItems));
            OrderStatus = OrderStatus.Paid;
        }
    }

    public void SetShippedStatus()
    {
        if (OrderStatus != OrderStatus.Paid)
            StatusChangeException(OrderStatus.Shipped);

        OrderStatus = OrderStatus.Shipped;
        Description = "The order was shipped.";
        AddDomainEvent(new OrderShippedDomainEvent(this));
    }

    public void SetCancelledStatus()
    {
        if (OrderStatus == OrderStatus.Paid || OrderStatus == OrderStatus.Shipped)
            StatusChangeException(OrderStatus.Cancelled);

        OrderStatus = OrderStatus.Cancelled;
        Description = $"The order was cancelled.";
        AddDomainEvent(new OrderCancelledDomainEvent(this));
    }

    public decimal GetTotal() => _orderItems.Sum(o => o.Units * o.UnitPrice);
}
```

### 8.4 La Macchina a Stati dell'Ordine

```
                            ┌──────────────────┐
                            │    Submitted     │ ← Ordine appena creato
                            │      (1)         │
                            └────────┬─────────┘
                                     │ SetAwaitingValidationStatus()
                            ┌────────▼─────────┐
                 ┌──────────│  Awaiting        │
                 │          │  Validation (2)  │
                 │          └────────┬─────────┘
                 │                   │ SetStockConfirmedStatus()
                 │          ┌────────▼─────────┐
                 │          │ Stock Confirmed  │──────────────┐
                 │          │      (3)         │              │
                 │          └────────┬─────────┘              │
                 │                   │ SetPaidStatus()        │
                 │          ┌────────▼─────────┐              │
                 │          │      Paid        │              │
                 │          │      (4)         │              │
                 │          └────────┬─────────┘              │
                 │                   │ SetShippedStatus()     │
                 │          ┌────────▼─────────┐              │
                 │          │    Shipped       │              │
                 │          │      (5)         │              │
                 │          └──────────────────┘              │
                 │                                            │
                 │ SetCancelledStatus()   SetCancelledStatus() │
                 │          ┌──────────────────┐              │
                 └─────────▶│   Cancelled      │◀─────────────┘
                            │      (6)         │
                            └──────────────────┘
```

> **Regole della macchina a stati:**
> - Solo `Submitted` → `AwaitingValidation`
> - Solo `AwaitingValidation` → `StockConfirmed`
> - Solo `StockConfirmed` → `Paid`
> - Solo `Paid` → `Shipped`
> - `Submitted` o `AwaitingValidation` o `StockConfirmed` → `Cancelled`
> - `Paid` e `Shipped` **NON** possono essere annullati

### 8.5 Domain Events

Ogni transizione di stato genera un **Domain Event** che viene gestito all'interno dello stesso bounded context:

```csharp
// Evento generato quando un nuovo ordine viene creato
public class OrderStartedDomainEvent(
    Order order,
    string userId,
    string userName,
    int cardTypeId,
    string cardNumber,
    string cardSecurityNumber,
    string cardHolderName,
    DateTime cardExpiration) : INotification
{
    public Order Order { get; } = order;
    public string UserId { get; } = userId;
    public string UserName { get; } = userName;
    // ... altri campi
}

// Handler del domain event → crea/verifica il Buyer
public class ValidateOrAddBuyerAggregateWhenOrderStartedDomainEventHandler(
    IBuyerRepository buyerRepository)
    : INotificationHandler<OrderStartedDomainEvent>
{
    public async Task Handle(OrderStartedDomainEvent domainEvent, 
        CancellationToken cancellationToken)
    {
        // Cerca il buyer esistente o ne crea uno nuovo
        var buyer = await buyerRepository.FindAsync(domainEvent.UserId);
        bool buyerExisted = buyer is not null;

        if (!buyerExisted)
        {
            buyer = new Buyer(domainEvent.UserId, domainEvent.UserName);
        }

        // Verifica/aggiunge il metodo di pagamento
        buyer.VerifyOrAddPaymentMethod(
            domainEvent.CardTypeId,
            $"Payment Method on {DateTime.UtcNow}",
            domainEvent.CardNumber,
            domainEvent.CardSecurityNumber,
            domainEvent.CardHolderName,
            domainEvent.CardExpiration,
            domainEvent.Order.Id);

        var buyerUpdated = buyerExisted
            ? buyerRepository.Update(buyer)
            : buyerRepository.Add(buyer);

        await buyerRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
    }
}
```

---

## 9. Ordering.Infrastructure — Il Layer di Persistenza

### 9.1 OrderingContext — DbContext con Unit of Work

```csharp
public class OrderingContext : DbContext, IUnitOfWork
{
    public const string DEFAULT_SCHEMA = "ordering";

    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<PaymentMethod> Payments { get; set; }
    public DbSet<Buyer> Buyers { get; set; }
    public DbSet<CardType> CardTypes { get; set; }

    // Gestione transazioni
    private IDbContextTransaction _currentTransaction;
    public IDbContextTransaction GetCurrentTransaction() => _currentTransaction;
    public bool HasActiveTransaction => _currentTransaction != null;

    // ⚡ SaveEntitiesAsync: dispatcha i domain events PRIMA del SaveChanges
    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        // I domain events vengono dispatchati tramite MediatR
        await _mediator.DispatchDomainEventsAsync(this);

        // Dopo il dispatch degli eventi, salva le modifiche al DB
        await base.SaveChangesAsync(cancellationToken);
        return true;
    }

    // Gestione esplicita delle transazioni (BEGIN / COMMIT / ROLLBACK)
    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        if (_currentTransaction != null) return null;
        _currentTransaction = await Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted);
        return _currentTransaction;
    }

    public async Task CommitTransactionAsync(IDbContextTransaction transaction)
    {
        try
        {
            await SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            RollbackTransaction();
            throw;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }
}
```

### 9.2 Dispatch dei Domain Events (MediatorExtension)

```csharp
static class MediatorExtension
{
    // Estrae i domain events dagli aggregati e li pubblica tramite MediatR
    public static async Task DispatchDomainEventsAsync(
        this IMediator mediator, OrderingContext ctx)
    {
        // 1. Trova tutte le entità con domain events pendenti
        var domainEntities = ctx.ChangeTracker
            .Entries<Entity>()
            .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Any());

        // 2. Raccoglie tutti gli eventi
        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        // 3. Pulisce gli eventi dalle entità (evita re-dispatch)
        domainEntities.ToList().ForEach(entity => entity.Entity.ClearDomainEvents());

        // 4. Pubblica ogni evento tramite MediatR
        foreach (var domainEvent in domainEvents)
        {
            await mediator.Publish(domainEvent);  // → Notification handlers
        }
    }
}
```

> **Didattica**: Il dispatch dei domain events avviene **prima** del `SaveChanges`. Questo garantisce che:
> 1. Gli handler dei domain events possano modificare altri aggregati nello stesso contesto
> 2. Tutte le modifiche vengano salvate in una **singola transazione**
> 3. Se un handler fallisce, l'intera operazione viene rollbackata

### 9.3 Entity Configurations (Fluent API)

```csharp
// Esempio di configurazione per l'entità Order
class OrderEntityTypeConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders", OrderingContext.DEFAULT_SCHEMA);
        builder.HasKey(o => o.Id);

        // Value Object mappato come Owned Entity
        builder.OwnsOne(o => o.Address, a =>
        {
            a.WithOwner();
            // Mappato come colonne nella stessa tabella:
            // Address_Street, Address_City, Address_State, etc.
        });

        // Navigazione verso OrderItems (collezione privata)
        builder.HasMany(o => o.OrderItems)
            .WithOne()
            .HasForeignKey("OrderId");

        // Smart Enumeration mappata come relazione
        builder.HasOne(o => o.OrderStatus)
            .WithMany()
            .HasForeignKey("OrderStatusId");
    }
}
```

---

## 10. OrderProcessor — Worker di Background

L'OrderProcessor è un **background worker** che gestisce le transizioni di stato automatiche degli ordini (ad es. il "grace period" dopo la creazione).

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.AddBasicServiceDefaults();    // Senza autenticazione (servizio interno)
builder.AddApplicationServices();
var app = builder.Build();
app.MapDefaultEndpoints();
await app.RunAsync();
```

L'OrderProcessor:
- Ascolta gli eventi dal bus RabbitMQ
- Gestisce il periodo di grazia (grace period) prima di procedere con la validazione dello stock
- Non ha API esterne (è un worker puro)
- Dipende dal database degli ordini
- **Attende** che l'Ordering API sia pronto (per le migrazioni del DB)

```csharp
// Nell'AppHost:
builder.AddProject<Projects.OrderProcessor>("order-processor")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(orderDb)
    .WaitFor(orderingApi);  // ← Attende le migrazioni EF dell'Ordering API
```

---

## 11. PaymentProcessor — Processore di Pagamenti Event-Driven

Il PaymentProcessor è interamente **event-driven**. Non espone API e non ha database.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Si sottoscrive SOLO all'evento "Ordine con stock confermato"
builder.AddRabbitMqEventBus("EventBus")
    .AddSubscription<OrderStatusChangedToStockConfirmedIntegrationEvent,
                      OrderStatusChangedToStockConfirmedIntegrationEventHandler>();

// Opzioni per simulare successo/fallimento del pagamento
builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration(nameof(PaymentOptions));

var app = builder.Build();
app.MapDefaultEndpoints();
await app.RunAsync();
```

### Flusso del pagamento:

```
Ordering.API                     RabbitMQ                  PaymentProcessor
     │                              │                              │
     │ OrderStatusChangedTo         │                              │
     │ StockConfirmed ──publish────▶│                              │
     │                              │──deliver──────────────────▶ │
     │                              │                   ┌──────────┤
     │                              │                   │ Simula   │
     │                              │                   │ pagamento│
     │                              │                   └──────────┤
     │                              │◀────publish──────────────── │
     │ OrderPaymentSucceeded        │    (o OrderPaymentFailed)    │
     │ (o Failed) ◀─────deliver─────│                              │
```

---

## 12. Identity.API — Autenticazione e Autorizzazione (OpenID Connect)

L'Identity API è il **Security Token Service (STS)** che gestisce l'autenticazione degli utenti tramite il protocollo **OpenID Connect/OAuth 2.0**.

### 12.1 Architettura di Sicurezza

```
┌─────────┐       ┌─────────────┐       ┌─────────────┐
│  WebApp  │──────▶│ Identity API│◀──────│ Basket API  │
│ (client) │ OIDC  │   (STS)     │ JWT   │ (resource)  │
└──────┬──┘       └──────┬──────┘       └─────────────┘
       │                  │                      ▲
       │ 1. Redirect      │ 2. Login             │ 4. Valida
       │    to login      │    + consent         │    JWT
       │                  │                      │
       │ 3. Riceve        │                      │
       │    JWT token ◄───┘                      │
       │                                         │
       └─────── 4. Chiama con JWT ──────────────┘
```

### 12.2 Configurazione del Server

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// PostgreSQL per i dati degli utenti
builder.AddNpgsqlDbContext<ApplicationDbContext>("identitydb");
builder.Services.AddMigration<ApplicationDbContext, UsersSeed>();

// ASP.NET Identity (gestione utenti, ruoli, password)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Duende IdentityServer (OpenID Connect / OAuth 2.0)
builder.Services.AddIdentityServer(options =>
{
    options.Authentication.CookieLifetime = TimeSpan.FromHours(2);
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseInformationEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseSuccessEvents = true;
})
    .AddInMemoryIdentityResources(Config.GetResources())     // OpenID scopes
    .AddInMemoryApiScopes(Config.GetApiScopes())             // API scopes
    .AddInMemoryApiResources(Config.GetApis())               // API resources
    .AddInMemoryClients(Config.GetClients(builder.Configuration))  // Registered clients
    .AddAspNetIdentity<ApplicationUser>()
    .AddProfileService<ProfileService>();

// MVC per le pagine di login/consent
builder.Services.AddControllersWithViews();
```

### 12.3 Configurazione JWT nei Microservizi

Ogni microservizio che richiede autenticazione usa la stessa configurazione JWT:

```csharp
// In eShop.ServiceDefaults/AuthenticationExtensions.cs
public static IServiceCollection AddDefaultAuthentication(
    this IHostApplicationBuilder builder)
{
    var identitySection = builder.Configuration.GetSection("Identity");
    if (!identitySection.Exists()) return builder.Services;

    // Previene il mapping del claim "sub" verso "nameidentifier"
    JsonWebTokenHandler.DefaultInboundClaimTypeMap.Remove("sub");

    builder.Services.AddAuthentication().AddJwtBearer(options =>
    {
        var identityUrl = identitySection.GetRequiredValue("Url");
        var audience = identitySection.GetRequiredValue("Audience");

        options.Authority = identityUrl;        // URL dell'Identity Server
        options.RequireHttpsMetadata = false;   // Permette HTTP in dev
        options.Audience = audience;            // Audience del servizio
        options.TokenValidationParameters.ValidIssuers = [identityUrl];
    });

    builder.Services.AddAuthorization();
    return builder.Services;
}
```

> **Flusso di autenticazione:**
> 1. L'utente accede alla WebApp
> 2. Viene reindirizzato all'Identity API per il login
> 3. Dopo il login, riceve un **JWT (JSON Web Token)**
> 4. Il JWT viene inviato come header `Authorization: Bearer <token>` nelle richieste ai microservizi
> 5. Ogni microservizio **valida il JWT** indipendentemente (senza contattare l'Identity API)

---

## 13. WebApp — Frontend Blazor Server

La WebApp è il frontend utente costruito con **Blazor Server**, un framework che esegue la logica UI sul server e comunica con il browser tramite SignalR.

### 13.1 Configurazione

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Blazor Server con componenti Razor interattivi
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.AddApplicationServices();

var app = builder.Build();

// Mappa i componenti Blazor con Server-Side Rendering
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

// Proxy delle immagini prodotto verso il Catalog API
app.MapForwarder("/product-images/{id}", "http://catalog-api", 
    "/api/catalog/items/{id}/pic");
```

### 13.2 Comunicazione con i Microservizi

La WebApp comunica con i servizi backend attraverso **client HTTP tipizzati** e **gRPC client**:

```csharp
// Comunicazione gRPC con Basket.API
public class BasketService(GrpcBasketClient basketClient)
{
    // Recupera il carrello dell'utente corrente
    public async Task<IReadOnlyCollection<BasketQuantity>> GetBasketAsync()
    {
        var result = await basketClient.GetBasketAsync(new());
        return MapToBasket(result);
    }

    // Aggiorna il carrello
    public async Task UpdateBasketAsync(IReadOnlyCollection<BasketQuantity> basket)
    {
        var updatePayload = new UpdateBasketRequest();
        foreach (var item in basket)
        {
            updatePayload.Items.Add(new GrpcBasketItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
            });
        }
        await basketClient.UpdateBasketAsync(updatePayload);
    }
}

// Comunicazione HTTP con Ordering.API
public class OrderingService(HttpClient httpClient)
{
    private readonly string remoteServiceBaseUrl = "/api/Orders/";

    public Task<OrderRecord[]> GetOrders()
    {
        return httpClient.GetFromJsonAsync<OrderRecord[]>(remoteServiceBaseUrl)!;
    }

    public Task CreateOrder(CreateOrderRequest request, Guid requestId)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, remoteServiceBaseUrl);
        requestMessage.Headers.Add("x-requestid", requestId.ToString());
        requestMessage.Content = JsonContent.Create(request);
        return httpClient.SendAsync(requestMessage);
    }
}
```

### 13.3 Gestione dello Stato del Carrello (BasketState)

La classe `BasketState` implementa il pattern **State Management** per Blazor:

```csharp
public class BasketState(
    BasketService basketService,
    CatalogService catalogService,
    OrderingService orderingService,
    AuthenticationStateProvider authenticationStateProvider) : IBasketState
{
    // Cache locale del carrello
    private Task<IReadOnlyCollection<BasketItem>>? _cachedBasket;

    // Sottoscrittori per notifiche di cambio stato
    private HashSet<BasketStateChangedSubscription> _changeSubscriptions = new();

    // Aggiunge un prodotto al carrello
    public async Task AddAsync(CatalogItem item)
    {
        var items = (await FetchBasketItemsAsync())
            .Select(i => new BasketQuantity(i.ProductId, i.Quantity)).ToList();

        // Cerca se il prodotto è già nel carrello
        bool found = false;
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].ProductId == item.Id)
            {
                items[i] = items[i] with { Quantity = items[i].Quantity + 1 };
                found = true;
                break;
            }
        }
        if (!found)
        {
            items.Add(new BasketQuantity(item.Id, 1));
        }

        // Invalida la cache e aggiorna via gRPC
        _cachedBasket = null;
        await basketService.UpdateBasketAsync(items);

        // Notifica i componenti UI che lo stato è cambiato
        await NotifyChangeSubscribersAsync();
    }

    // Checkout: crea l'ordine e svuota il carrello
    public async Task CheckoutAsync(BasketCheckoutInfo checkoutInfo)
    {
        var buyerId = await authenticationStateProvider.GetBuyerIdAsync();
        var orderItems = await FetchBasketItemsAsync();

        var request = new CreateOrderRequest(
            UserId: buyerId,
            UserName: userName,
            City: checkoutInfo.City!,
            // ... altri campi
            Items: [.. orderItems]);

        // Chiama l'Ordering API per creare l'ordine
        await orderingService.CreateOrder(request, checkoutInfo.RequestId);

        // Svuota il carrello
        await DeleteBasketAsync();
    }

    // Lazy loading con cache
    private Task<IReadOnlyCollection<BasketItem>> FetchBasketItemsAsync()
    {
        return _cachedBasket ??= FetchCoreAsync();

        async Task<IReadOnlyCollection<BasketItem>> FetchCoreAsync()
        {
            // 1. Prende le quantità dal Basket API (gRPC)
            var quantities = await basketService.GetBasketAsync();

            // 2. Arricchisce con i dati del catalogo (HTTP)
            var productIds = quantities.Select(row => row.ProductId);
            var catalogItems = (await catalogService.GetCatalogItems(productIds))
                .ToDictionary(k => k.Id, v => v);

            // 3. Combina i dati per la UI
            var basketItems = new List<BasketItem>();
            foreach (var item in quantities)
            {
                var catalogItem = catalogItems[item.ProductId];
                basketItems.Add(new BasketItem
                {
                    ProductId = catalogItem.Id,
                    ProductName = catalogItem.Name,
                    UnitPrice = catalogItem.Price,
                    Quantity = item.Quantity,
                });
            }
            return basketItems;
        }
    }
}
```

> **Didattica**: Questo è un esempio eccellente di come un frontend deve comporre dati da più microservizi. Il carrello (Basket API) contiene solo `ProductId` e `Quantity`, mentre nome e prezzo vengono dal Catalog API. Questa composizione avviene **nel frontend** (o nel BFF), mai con join cross-database.

---

## 14. WebAppComponents — Libreria Componenti Condivisi

Il progetto `WebAppComponents` è una **Razor Class Library** (RCL) condivisa tra WebApp e potenzialmente altri frontend:

```
WebAppComponents/
├── Catalog/
│   ├── CatalogItem.cs              # Record DTO per i dati del catalogo
│   ├── CatalogResult.cs            # DTO per risultati paginati
│   ├── CatalogBrand.cs             # DTO brand
│   ├── CatalogItemType.cs          # DTO tipo
│   ├── CatalogListItem.razor       # Componente Blazor per la card prodotto
│   └── CatalogSearch.razor         # Componente Blazor per la ricerca
├── Item/
│   └── ItemHelper.cs               # Helper per formattazione prezzi
└── Services/
    ├── ICatalogService.cs           # Interfaccia servizio catalogo
    ├── CatalogService.cs            # Implementazione con HttpClient
    └── IProductImageUrlProvider.cs  # Interfaccia per URL immagini
```

### Servizio Catalogo condiviso

```csharp
public class CatalogService(HttpClient httpClient) : ICatalogService
{
    private readonly string remoteServiceBaseUrl = "/api/catalog/";

    public Task<CatalogResult> GetCatalogItems(int pageIndex, int pageSize, 
        int? brand, int? type)
    {
        var uri = $"{remoteServiceBaseUrl}items?pageIndex={pageIndex}&pageSize={pageSize}";
        if (brand.HasValue) uri += $"&brand={brand}";
        if (type.HasValue) uri += $"&type={type}";

        return httpClient.GetFromJsonAsync<CatalogResult>(uri)!;
    }

    public Task<List<CatalogItem>> GetCatalogItems(IEnumerable<int> ids)
    {
        var uri = $"{remoteServiceBaseUrl}items/by?ids={string.Join("&ids=", ids)}";
        return httpClient.GetFromJsonAsync<List<CatalogItem>>(uri)!;
    }

    // ... altri metodi
}
```

---

## 15. Mobile.Bff.Shopping — Backend-for-Frontend con YARP

Il **BFF (Backend-for-Frontend)** è un pattern che crea un API gateway specifico per un tipo di client (in questo caso, le app mobile).

### 15.1 Cos'è YARP?

**YARP** (Yet Another Reverse Proxy) è un reverse proxy ad alte prestazioni sviluppato da Microsoft, configurabile in C# e JSON:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Configura YARP con service discovery di Aspire
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();  // ← Risolve automaticamente gli URL

var app = builder.Build();
app.MapReverseProxy();   // Mappa tutte le route definite nella configurazione
```

### 15.2 Configurazione del Routing

```json
{
  "ReverseProxy": {
    "Routes": {
      "catalog": {
        "ClusterId": "catalog",
        "Match": { "Path": "api/catalog/{**remainder}" }
      },
      "orders": {
        "ClusterId": "ordering",
        "Match": { "Path": "api/orders/{**remainder}" }
      },
      "basket": {
        "ClusterId": "basket",
        "Match": { "Path": "api/basket/{**remainder}" }
      }
    },
    "Clusters": {
      "catalog": {
        "Destinations": {
          "destination0": { "Address": "http://catalog-api" }
        }
      },
      "ordering": {
        "Destinations": {
          "destination0": { "Address": "http://ordering-api" }
        }
      },
      "basket": {
        "Destinations": {
          "destination0": { "Address": "http://basket-api" }
        }
      }
    }
  }
}
```

> **Didattica**: Il pattern BFF risolve diversi problemi:
> - **Aggregazione**: Un'app mobile potrebbe aver bisogno di dati da più servizi in una singola richiesta
> - **Trasformazione**: I dati possono essere adattati alle esigenze specifiche del client mobile
> - **Sicurezza**: Riduce il numero di endpoint esposti all'esterno
> - **Performance**: Il BFF può cachare, comprimere e ottimizzare le risposte

---

## 16. Webhooks.API e WebhookClient

### Webhooks.API

Il servizio Webhooks permette a sistemi esterni di **sottoscriversi a eventi** e ricevere notifiche HTTP quando accadono:

```csharp
builder.AddServiceDefaults();
builder.AddApplicationServices();

var webHooks = app.NewVersionedApi("Web Hooks");
webHooks.MapWebHooksApiV1().RequireAuthorization();  // Richiede autenticazione
```

### WebhookClient

Il WebhookClient è un'applicazione web dimostrativa che si sottoscrive ai webhook e mostra le notifiche ricevute.

---

## 17. Comunicazione tra Microservizi — Event Bus

### 17.1 L'Astrazione EventBus

L'Event Bus è il **sistema nervoso** dell'applicazione, che permette ai microservizi di comunicare **in modo asincrono e disaccoppiato**.

```
┌────────────────────────────────────────────────────────────────┐
│                       Event Bus Layer                          │
│                                                                │
│  ┌────────────────┐    ┌────────────────────────────────────┐  │
│  │    EventBus     │    │       EventBusRabbitMQ            │  │
│  │  (Abstractions) │    │      (Implementation)             │  │
│  │                 │    │                                    │  │
│  │  IEventBus      │◄───│  RabbitMQEventBus                 │  │
│  │  IntegrationEvent│   │  • Direct Exchange                │  │
│  │  IIntegrationEvent│  │  • Polly Resilience               │  │
│  │    Handler       │   │  • OpenTelemetry Traces           │  │
│  └────────────────┘    └────────────────────────────────────┘  │
│                                                                │
│  Registrazione:                                                │
│  builder.AddRabbitMqEventBus("eventbus")                       │
│      .AddSubscription<TEvent, THandler>();                     │
└────────────────────────────────────────────────────────────────┘
```

### 17.2 Contratti Base

```csharp
// Interfaccia dell'Event Bus — Single method per pubblicazione
public interface IEventBus
{
    Task PublishAsync(IntegrationEvent @event);
}

// Base class per tutti gli Integration Events
public record IntegrationEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime CreationDate { get; } = DateTime.UtcNow;
}

// Interfaccia per gli handler degli eventi
public interface IIntegrationEventHandler<in TIntegrationEvent>
    where TIntegrationEvent : IntegrationEvent
{
    Task Handle(TIntegrationEvent @event);
}
```

### 17.3 Registrazione Flexibile

Il pattern Builder rende la registrazione degli eventi fluente:

```csharp
public interface IEventBusBuilder
{
    // Permette di sottoscrivere un handler ad un evento specifico
    IEventBusBuilder AddSubscription<T, TH>()
        where T : IntegrationEvent
        where TH : class, IIntegrationEventHandler<T>;
}

// Implementazione: registra handler come Keyed DI Services
public static IEventBusBuilder AddSubscription<T, 
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TH>(
    this IEventBusBuilder eventBusBuilder)
    where T : IntegrationEvent
    where TH : class, IIntegrationEventHandler<T>
{
    // Registra il tipo di evento per la deserializzazione
    eventBusBuilder.Services.TryAddKeyedTransient<IIntegrationEventHandler, TH>(typeof(T));
    return eventBusBuilder;
}

// Utilizzo nei servizi:
builder.AddRabbitMqEventBus("eventbus")
    .AddSubscription<OrderStartedIntegrationEvent, 
                     OrderStartedIntegrationEventHandler>()
    .AddSubscription<ProductPriceChangedIntegrationEvent, 
                     ProductPriceChangedIntegrationEventHandler>();
```

### 17.4 Implementazione RabbitMQ

La classe `RabbitMQEventBus` è il cuore della comunicazione asincrona:

```
┌───────────────┐    publish    ┌──────────────────────┐
│   Publisher    │─────────────▶│  RabbitMQ Exchange    │
│ (any service) │              │  "eshop_event_bus"   │
└───────────────┘              │  (type: direct)      │
                               └──────────┬───────────┘
                                          │
                    ┌─────────────────────┤ routing key = event type name
                    │                     │
           ┌────────▼───────┐    ┌────────▼───────┐
           │   Queue        │    │   Queue        │
           │ "Basket.API"   │    │ "Catalog.API"  │
           └────────┬───────┘    └────────┬───────┘
                    │                     │
           ┌────────▼───────┐    ┌────────▼───────┐
           │   Consumer     │    │   Consumer     │
           │ OrderStarted   │    │ PriceChanged   │
           │ EventHandler   │    │ EventHandler   │
           └────────────────┘    └────────────────┘
```

Caratteristiche chiave dell'implementazione:

- **Direct Exchange** (`eshop_event_bus`): Ogni evento viene routato alla coda corretta tramite il nome del tipo
- **Polly Resilience**: Retry automatico con exponential backoff per la pubblicazione
- **OpenTelemetry**: Propagazione automatica del contesto di trace nelle intestazioni dei messaggi
- **Hosted Service**: Il consumer viene avviato come `IHostedService` e gira per tutta la vita dell'applicazione

```csharp
// Esempio di come il trace context viene propagato:
// Quando si pubblica un evento, il RabbitMQEventBus:
// 1. Crea un'Activity (span) OpenTelemetry
// 2. Serializza il trace context nelle intestazioni del messaggio RabbitMQ
// 3. Il consumer dall'altro lato legge le intestazioni e ricostruisce il contesto

// Questo permette di vedere l'intera catena di eventi nel dashboard di tracing:
// WebApp → Ordering.API → [RabbitMQ] → Catalog.API (verifica stock)
//                                    → Basket.API (svuota carrello)
//                                    → PaymentProcessor (pagamento)
```

---

## 18. Integration Events e Outbox Pattern

### 18.1 Il Problema della Consistenza Distribuita

In un'architettura a microservizi, quando un servizio deve:
1. Aggiornare il proprio database
2. Pubblicare un evento sul bus

...si presenta un problema: **cosa succede se il database viene aggiornato ma la pubblicazione dell'evento fallisce?** O viceversa?

### 18.2 La Soluzione: Outbox Pattern

L'**Outbox Pattern** risolve questo problema salvando l'evento nella **stessa transazione** del database:

```
┌─────────────────────────────────────────────────────────────────┐
│                    Outbox Pattern Flow                           │
│                                                                 │
│  1. BEGIN TRANSACTION                                           │
│     ├── UPDATE CatalogItems SET Price = @newPrice               │
│     └── INSERT INTO IntegrationEventLog (EventType, Content)    │
│  2. COMMIT TRANSACTION                                          │
│                                                                 │
│  3. SELECT FROM IntegrationEventLog WHERE State = 'NotPublished'│
│  4. UPDATE IntegrationEventLog SET State = 'InProgress'         │
│  5. PUBLISH to RabbitMQ                                         │
│  6. UPDATE IntegrationEventLog SET State = 'Published'          │
└─────────────────────────────────────────────────────────────────┘
```

### 18.3 Implementazione nel Catalog.API

```csharp
public class CatalogIntegrationEventService(
    ILogger<CatalogIntegrationEventService> logger,
    IEventBus eventBus,
    CatalogContext catalogContext,
    IIntegrationEventLogService integrationEventLogService)
    : ICatalogIntegrationEventService
{
    // Salva atomicamente: modifiche DB + evento nell'outbox
    public async Task SaveEventAndCatalogContextChangesAsync(IntegrationEvent evt)
    {
        // ResilientTransaction garantisce che entrambe le operazioni
        // siano nella stessa transazione DB
        await ResilientTransaction.New(catalogContext).ExecuteAsync(async () =>
        {
            // 1. Salva le modifiche al catalogo (es. nuovo prezzo)
            await catalogContext.SaveChangesAsync();

            // 2. Salva l'evento nella tabella IntegrationEventLog
            //    usando la STESSA transazione
            await integrationEventLogService.SaveEventAsync(
                evt, catalogContext.Database.CurrentTransaction);
        });
    }

    // Pubblica l'evento sul bus (può essere ritentata in caso di errore)
    public async Task PublishThroughEventBusAsync(IntegrationEvent evt)
    {
        try
        {
            // Marca l'evento come "in corso di pubblicazione"
            await integrationEventLogService.MarkEventAsInProgressAsync(evt.Id);

            // Pubblica su RabbitMQ
            await eventBus.PublishAsync(evt);

            // Marca come "pubblicato" — successo!
            await integrationEventLogService.MarkEventAsPublishedAsync(evt.Id);
        }
        catch (Exception ex)
        {
            // Se la pubblicazione fallisce, l'evento rimane nel log
            // e può essere ripubblicato successivamente
            await integrationEventLogService.MarkEventAsFailedAsync(evt.Id);
        }
    }
}
```

### 18.4 IntegrationEventLogEntry

```csharp
public class IntegrationEventLogEntry
{
    public Guid EventId { get; private set; }        // ID univoco dell'evento
    public string EventTypeName { get; private set; } // Nome completo del tipo
    public string Content { get; private set; }       // JSON serializzato dell'evento
    public EventStateEnum State { get; set; }         // Stato dell'evento
    public int TimesSent { get; set; }                // Tentativi di invio
    public DateTime CreationTime { get; private set; }
    public string TransactionId { get; private set; } // ID della transazione DB
}

public enum EventStateEnum
{
    NotPublished = 0,    // Salvato ma non ancora pubblicato
    InProgress = 1,      // Pubblicazione in corso
    Published = 2,       // Pubblicato con successo
    PublishedFailed = 3  // Pubblicazione fallita (può essere ritentata)
}
```

### 18.5 Mappa Completa degli Integration Events

| Evento | Publisher | Subscriber(s) | Azione |
|--------|----------|---------------|--------|
| `OrderStartedIntegrationEvent` | Ordering.API | Basket.API | Svuota il carrello dell'utente |
| `OrderStatusChangedToAwaitingValidationIntegrationEvent` | Ordering.API | Catalog.API | Verifica disponibilità stock |
| `OrderStockConfirmedIntegrationEvent` | Catalog.API | Ordering.API | Conferma stock disponibile |
| `OrderStockRejectedIntegrationEvent` | Catalog.API | Ordering.API | Stock non disponibile → annulla ordine |
| `OrderStatusChangedToStockConfirmedIntegrationEvent` | Ordering.API | PaymentProcessor | Processa il pagamento |
| `OrderPaymentSucceededIntegrationEvent` | PaymentProcessor | Ordering.API | Pagamento riuscito → `Paid` |
| `OrderPaymentFailedIntegrationEvent` | PaymentProcessor | Ordering.API | Pagamento fallito → `Cancelled` |
| `OrderStatusChangedToPaidIntegrationEvent` | Ordering.API | Catalog.API | Decrementa lo stock |
| `ProductPriceChangedIntegrationEvent` | Catalog.API | — | Notifica cambio prezzo |
| `GracePeriodConfirmedIntegrationEvent` | OrderProcessor | Ordering.API | Periodo di grazia terminato |

---

## 19. Flusso Completo di un Ordine (Caso d'Uso End-to-End)

Questo diagramma mostra l'intero flusso dall'aggiunta al carrello alla spedizione:

```
Utente         WebApp         Basket.API       Ordering.API      OrderProcessor
  │               │               │                 │                 │
  │ 1. Add to     │               │                 │                 │
  │    cart ──────▶│               │                 │                 │
  │               │ 2. gRPC ─────▶│                 │                 │
  │               │   UpdateBasket│                 │                 │
  │               │               │ 3. Save ───▶Redis               │
  │               │◀──────────────│                 │                 │
  │◀──────────────│               │                 │                 │
  │               │               │                 │                 │
  │ 4. Checkout──▶│               │                 │                 │
  │               │ 5. HTTP POST ─────────────────▶│                 │
  │               │   CreateOrder │                 │                 │
  │               │               │                 │                 │
  │               │               │   ┌─────────────┤                 │
  │               │               │   │6. Create    │                 │
  │               │               │   │  Order      │                 │
  │               │               │   │  Aggregate  │                 │
  │               │               │   │  + Domain   │                 │
  │               │               │   │    Events   │                 │
  │               │               │   └──────┬──────┤                 │
  │               │               │          │      │                 │
  │               │               │          │ 7. Save to DB          │
  │               │               │          │ 8. Dispatch Domain Events
  │               │               │          │    → Create Buyer      │
  │               │               │          │    → Verify Payment    │
  │               │               │          │                        │
  │               │               │          │ 9. Publish Integration Events
  │               │               │          │                        │
```

```
Ordering.API     RabbitMQ       Basket.API     Catalog.API    PaymentProcessor
  │                 │               │               │                │
  │ 10. Publish     │               │               │                │
  │ OrderStarted───▶│               │               │                │
  │                 │──deliver────▶ │               │                │
  │                 │              │11. Delete      │                │
  │                 │              │    basket      │                │
  │                 │              │    from Redis  │                │
  │                 │               │               │                │
  │ 12. Grace period│               │               │                │
  │     timer ──────│───────────────│───────────────│────────────────│
  │                 │               │               │                │
OrderProcessor      │               │               │                │
  │ 13. Grace       │               │               │                │
  │ period done ───▶│               │               │                │
  │                 │──deliver────────────────────▶ │                │
  │                 │               │  14. Set      │                │
  │                 │               │  AwaitingValid│                │
  │                 │               │               │                │
  │ 15. Publish     │               │               │                │
  │ AwaitingValid──▶│               │               │                │
  │                 │──deliver────────────────────▶ │                │
  │                 │               │  16. Check    │                │
  │                 │               │      stock    │                │
  │                 │               │  17. Publish  │                │
  │                 │◀─────────────────StockConfirmed                │
  │                 │──deliver─────▶│               │                │
  │ 18. Set         │               │               │                │
  │ StockConfirmed  │               │               │                │
  │                 │               │               │                │
  │ 19. Publish     │               │               │                │
  │ StockConfirmed─▶│               │               │                │
  │                 │──deliver──────│───────────────│──────────────▶ │
  │                 │               │               │   20. Process  │
  │                 │               │               │       payment  │
  │                 │               │               │   21. Publish  │
  │                 │◀──────────────│───────────────│──PaymentSucceeded
  │                 │──deliver─────▶│               │                │
  │ 22. SetPaid     │               │               │                │
  │ 23. Publish     │               │               │                │
  │ PaidEvent ─────▶│               │               │                │
  │                 │──deliver────────────────────▶ │                │
  │                 │               │  24. Decrement│                │
  │                 │               │      stock    │                │
```

### Riepilogo delle fasi:

1. **Aggiunta al carrello** (WebApp → Basket.API via gRPC → Redis)
2. **Checkout** (WebApp → Ordering.API via HTTP)
3. **Creazione ordine** (Command → Handler → Aggregate → Domain Events)
4. **Svuotamento carrello** (Integration Event → Basket.API)
5. **Grace Period** (OrderProcessor attende un periodo configurabile)
6. **Validazione stock** (Ordering → Catalog API → verifica disponibilità)
7. **Confirmazione stock** (Catalog → Ordering)
8. **Processamento pagamento** (Ordering → PaymentProcessor → conferma/rifiuto)
9. **Ordine pagato** (Payment → Ordering → decremento stock nel Catalog)
10. **Spedizione** (manuale, tramite endpoint `/ship`)

---

## 20. Cross-Cutting Concerns

### 20.1 eShop.ServiceDefaults — Il Progetto dei Default

Questo progetto centralizza configurazioni condivise da **tutti i microservizi**:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <IsAspireSharedProject>true</IsAspireSharedProject>
  </PropertyGroup>
  <ItemGroup>
    <!-- OpenTelemetry -->
    <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" />
    <PackageReference Include="OpenTelemetry.Extensions.Hosting" />
    <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" />
    <PackageReference Include="OpenTelemetry.Instrumentation.GrpcNetClient" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Http" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" />
    <!-- Auth -->
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
    <!-- API Versioning -->
    <PackageReference Include="Asp.Versioning.Http" />
    <PackageReference Include="Asp.Versioning.Mvc.ApiExplorer" />
    <!-- OpenAPI -->
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
    <PackageReference Include="Scalar.AspNetCore" />
    <!-- Resilience -->
    <PackageReference Include="Microsoft.Extensions.Http.Resilience" />
    <PackageReference Include="Microsoft.Extensions.ServiceDiscovery" />
  </ItemGroup>
</Project>
```

### 20.2 OpenTelemetry — Osservabilità Distribuita

Ogni servizio è strumentato con OpenTelemetry per tre segnali:

```csharp
// Configurato in AddServiceDefaults() / AddBasicServiceDefaults()
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()     // Trace delle richieste HTTP
            .AddGrpcClientInstrumentation()     // Trace delle chiamate gRPC
            .AddHttpClientInstrumentation()     // Trace delle chiamate HTTP in uscita
            .AddSource("RabbitMQ.Client.*");    // Trace di RabbitMQ
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation();       // Metriche GC, thread pool, etc.
    })
    .UseOtlpExporter();                        // Esporta verso il collector OTLP
```

```
┌─────────┐  ┌─────────┐  ┌──────────┐
│ Traces  │  │ Metrics │  │   Logs   │    3 Pillar dell'Osservabilità
└────┬────┘  └────┬────┘  └────┬─────┘
     │            │            │
     └────────────┼────────────┘
                  │
         ┌────────▼────────┐
         │  OTLP Exporter  │     OpenTelemetry Protocol
         └────────┬────────┘
                  │
         ┌────────▼────────┐
         │ Aspire Dashboard│     Visualizzazione centralizzata
         │  (o Jaeger,     │     di trace, metriche e log
         │   Grafana...)   │
         └─────────────────┘
```

> **Didattica**: La **traccia distribuita** permette di seguire una singola richiesta attraverso tutti i microservizi. Ad esempio, quando un utente crea un ordine, si può vedere la catena:
> `WebApp → Ordering.API → [RabbitMQ publish] → Catalog.API (stock check) → [RabbitMQ publish] → PaymentProcessor`
> ...tutto in un'unica vista, con tempi di esecuzione per ogni hop.

### 20.3 Health Checks

```csharp
// Ogni servizio espone endpoint di health check
app.MapDefaultEndpoints();

// Che include:
// /health      → liveness check (il servizio è vivo?)
// /alive       → readiness check (il servizio è pronto a ricevere traffico?)

// L'AppHost può usarli per orchestrare l'avvio:
orderingApi.WithHttpHealthCheck("/health");
```

### 20.4 Service Discovery

.NET Aspire gestisce automaticamente la **service discovery**. I servizi si riferiscono l'uno all'altro tramite nomi logici:

```csharp
// Nell'AppHost:
var catalogApi = builder.AddProject<Projects.Catalog_API>("catalog-api");

// Nei servizi consumer, l'URL viene risolto automaticamente:
// "http://catalog-api" → "http://localhost:5234" (in dev)
// "http://catalog-api" → "http://catalog-api.namespace.svc.cluster.local" (in K8s)
```

### 20.5 Migrazioni Database Automatiche

I database vengono migrati automaticamente all'avvio tramite un `BackgroundService`:

```csharp
// In Shared/MigrateDbContextExtensions.cs
public static IServiceCollection AddMigration<TContext, TDbSeeder>(
    this IHostApplicationBuilder builder)
    where TContext : DbContext
    where TDbSeeder : class, IDbSeeder<TContext>
{
    // Registra un BackgroundService che:
    // 1. Attende che il database sia raggiungibile
    // 2. Esegue le migrazioni EF Core pendenti
    // 3. Esegue il seeder per i dati iniziali
    // 4. Tutto con OpenTelemetry tracing
    builder.Services.AddHostedService<MigrationHostedService<TContext>>();
    builder.Services.AddScoped<TDbSeeder>();
    return builder.Services;
}
```

### 20.6 API Versioning e OpenAPI/Scalar

Ogni API REST include:
- **Versionamento** tramite `Asp.Versioning`
- **Documentazione interattiva** con Scalar (alternativa moderna a Swagger UI)

```csharp
// Nel Program.cs di ogni servizio REST:
var withApiVersioning = builder.Services.AddApiVersioning();
builder.AddDefaultOpenApi(withApiVersioning);

// Le API sono mappate con versione:
app.NewVersionedApi("Catalog").MapCatalogApiV1();

// Scalar genera UI interattiva accessibile su /scalar/v1
app.UseDefaultOpenApi();
// Root "/" redirect a /scalar/v1 in development
```

---

## 21. Testing

### 21.1 Strategia di Test

```
tests/
├── Basket.UnitTests/              # Unit test del servizio Basket
├── Catalog.FunctionalTests/       # Test funzionali del Catalog API
├── ClientApp.UnitTests/           # Unit test dell'app mobile (MAUI)
├── Ordering.FunctionalTests/      # Test funzionali dell'Ordering API
├── Ordering.UnitTests/            # Unit test del dominio Ordering
└── e2e/                          # Test end-to-end con Playwright
```

### 21.2 Unit Test — Ordering Domain

```csharp
// Test dell'aggregato Order
public class OrderAggregateTest
{
    [Fact]
    public void Create_order_item_success()
    {
        // Arrange
        var productId = 1;
        var productName = "FakeProductName";
        var unitPrice = 12;
        var discount = 15;
        var pictureUrl = "FakeUrl";
        var units = 5;

        // Act
        var fakeOrderItem = new OrderItem(
            productId, productName, unitPrice, discount, pictureUrl, units);

        // Assert
        Assert.NotNull(fakeOrderItem);
    }

    [Fact]
    public void Invalid_number_of_units()
    {
        // Arrange & Act & Assert
        Assert.Throws<OrderingDomainException>(() => 
            new OrderItem(1, "FakeProduct", 12, 15, "FakeUrl", units: 0));
        // L'ordine con 0 unità viola l'invariante del dominio
    }
}
```

### 21.3 Test Funzionali — Catalog API

```csharp
public class CatalogApiTests : IClassFixture<CatalogApiFixture>
{
    [Fact]
    public async Task GetCatalogItems_ReturnsSuccess()
    {
        // Arrange — CatalogApiFixture crea un WebApplicationFactory
        var client = _fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/catalog/items?pageSize=10&pageIndex=0");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedItems<CatalogItem>>(content);
        Assert.NotEmpty(result.Data);
    }
}
```

### 21.4 Test End-to-End — Playwright

```typescript
// e2e/AddItemTest.spec.ts
test('add item to basket', async ({ page }) => {
    // Naviga alla home page
    await page.goto('/');
    
    // Clicca sul primo prodotto
    await page.click('.catalog-item:first-child .add-to-cart');
    
    // Verifica che il carrello contenga l'item
    await expect(page.locator('.basket-count')).toHaveText('1');
});
```

---

## 22. Gestione Centralizzata delle Dipendenze

### Directory.Packages.props

eShop utilizza il **Central Package Management** di NuGet per gestire centralmente tutte le versioni dei pacchetti:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
    
    <!-- Versioni definite come proprietà per consistency -->
    <AspnetVersion>9.0.0</AspnetVersion>
    <AspireVersion>9.0.0</AspireVersion>
    <GrpcVersion>2.67.0-pre1</GrpcVersion>
    <DuendeVersion>7.0.6</DuendeVersion>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- Ogni pacchetto è definito UNA SOLA volta con la versione -->
    <PackageVersion Include="MediatR" Version="12.4.1" />
    <PackageVersion Include="FluentValidation.AspNetCore" Version="11.3.0" />
    <PackageVersion Include="Polly.Core" Version="8.4.2" />
    <PackageVersion Include="Dapper" Version="2.1.35" />
    <PackageVersion Include="Yarp.ReverseProxy" Version="2.2.0" />
    <!-- ... molti altri -->
  </ItemGroup>
</Project>
```

> **Didattica**: Nei progetti con molti microservizi, è fondamentale evitare conflitti di versione. Con Central Package Management:
> - I file `.csproj` dei singoli progetti specificano solo il nome del pacchetto (senza versione)
> - La versione è definita **una sola volta** nel file `Directory.Packages.props`
> - L'aggiornamento di un pacchetto richiede la modifica in **un solo posto**

```xml
<!-- Nei .csproj dei servizi: SENZA versione -->
<PackageReference Include="MediatR" />
<PackageReference Include="Dapper" />
```

---

## 23. Pattern Architetturali Utilizzati — Riepilogo

| Pattern | Dove | Descrizione |
|---------|------|-------------|
| **Microservizi** | Intera architettura | Decomposizione in servizi indipendenti |
| **Database-per-Service** | Ogni servizio | Ogni servizio possiede il proprio database |
| **CQRS** | Ordering.API | Separazione lettura/scrittura |
| **DDD (Domain-Driven Design)** | Ordering.Domain | Aggregati, entità, value objects, domain events |
| **Event-Driven Architecture** | RabbitMQ Event Bus | Comunicazione asincrona tra servizi |
| **Outbox Pattern** | Catalog, Ordering | Consistenza tra DB e pubblicazione eventi |
| **Saga (Coreografia)** | Flusso dell'ordine | Transazione distribuita tramite eventi |
| **REPR (Request-Endpoint-Response)** | Minimal API | Un endpoint per handler |
| **Mediator** | MediatR | Disaccoppiamento command → handler |
| **Repository** | Ordering, Basket | Astrazione dell'accesso ai dati |
| **Unit of Work** | OrderingContext | Transazioni atomiche |
| **Smart Enumeration** | OrderStatus, CardType | Enum come classi con logica |
| **Value Object** | Address | Oggetto immutabile senza identità |
| **Aggregate Root** | Order, Buyer | Entry point per le modifiche dell'aggregato |
| **Idempotent Commands** | IdentifiedCommand | Previene l'elaborazione duplicata |
| **Pipeline Behavior** | MediatR Behaviors | Cross-cutting concerns nel pipeline CQRS |
| **Backend-for-Frontend (BFF)** | Mobile.Bff.Shopping | API gateway specifico per client tipo |
| **Service Discovery** | .NET Aspire | Risoluzione automatica degli URL |
| **Strangler Fig** | Webhook pattern | Estensibilità tramite eventi esterni |
| **API Versioning** | Tutti i REST API | Evoluzione API senza breaking changes |
| **Health Check** | Tutti i servizi | Monitoring della disponibilità |
| **Centralized Config** | Aspire AppHost | Configurazione centralizzata delle risorse |

---

## 24. Glossario

| Termine | Definizione |
|---------|-------------|
| **Aggregate** | Un cluster di entità e value objects trattato come un'unità per le modifiche ai dati. Solo l'Aggregate Root è accessibile dall'esterno. |
| **Aggregate Root** | L'entità principale di un aggregate, unico punto di accesso per le modifiche. |
| **Bounded Context** | Un confine esplicito all'interno del quale un modello di dominio è definito e applicabile. |
| **CQRS** | Command Query Responsibility Segregation — separazione dei modelli di lettura e scrittura. |
| **DDD** | Domain-Driven Design — approccio allo sviluppo software centrato sul dominio di business. |
| **Domain Event** | Un evento che si verifica all'interno di un bounded context e viene gestito nello stesso contesto. |
| **Integration Event** | Un evento che attraversa i confini dei bounded context, comunicato tramite un message broker. |
| **Eventual Consistency** | Garanzia che, dato un tempo sufficiente senza nuove modifiche, tutti i nodi convergeranno allo stesso stato. |
| **gRPC** | Google Remote Procedure Call — framework RPC ad alte prestazioni con Protocol Buffers. |
| **Idempotenza** | Proprietà per cui un'operazione produce lo stesso risultato indipendentemente dal numero di volte che viene eseguita. |
| **Minimal API** | Approccio leggero di ASP.NET Core per definire endpoint HTTP senza controller. |
| **Outbox Pattern** | Pattern che garantisce la consistenza tra l'aggiornamento del database e la pubblicazione di eventi. |
| **Protocol Buffers** | Formato di serializzazione binario e linguaggio di definizione delle interfacce di Google. |
| **Saga** | Pattern per gestire transazioni distribuite tramite una sequenza di transazioni locali coordinate da eventi. |
| **Service Discovery** | Meccanismo per trovare automaticamente gli indirizzi di rete dei servizi. |
| **STS** | Security Token Service — servizio che emette token di sicurezza (JWT). |
| **Unit of Work** | Pattern che mantiene una lista di oggetti modificati e coordina il salvataggio delle modifiche. |
| **Value Object** | Oggetto di dominio senza identità, definito dai suoi attributi. Due VO con gli stessi valori sono uguali. |
| **YARP** | Yet Another Reverse Proxy — reverse proxy ad alte prestazioni di Microsoft. |

---

> **Nota**: Questa documentazione è stata creata a scopo didattico, basata sull'analisi del codice sorgente dell'applicazione eShop Reference Application di Microsoft (.NET 9 / .NET Aspire 9.0.0). Per la documentazione ufficiale, fare riferimento al [repository GitHub](https://github.com/dotnet/eShop) e alla [documentazione .NET Aspire](https://learn.microsoft.com/dotnet/aspire/).
