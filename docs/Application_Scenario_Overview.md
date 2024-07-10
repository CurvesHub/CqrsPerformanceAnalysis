# Scenario Overview

## Scenario 1: Traditional

This scenario includes the `Traditional.Api` C# project. It uses various common business dependencies like:

- [Entity Framework Core 8](https://learn.microsoft.com/en-us/ef/core/)
- [Npgsql.EntityFrameworkCore.PostgreSQL](https://www.npgsql.org/efcore/)
- [Serilog](https://serilog.net/)
- [FluentValidation](https://fluentvalidation.net/)
- [ErrorOr](https://github.com/amantinband/error-or#a-simple-fluent-discriminated-union-of-an-error-or-a-result)
- ... #TODOs complete the list

## Scenario 2: CQRS

It includes the `Cqrs.Api` C# project. It uses the same common business dependencies like [Scenario 1: Traditional](Application_Scenario_Overview.md#scenario-1-traditional) but new ones are included because a company would likely use those packages for the implementation of CQRS in a large scale application. 

The implementation of the CQRS pattern can be done by hand but also be simplified with the Mediator pattern. Especially if you want to use other functionality from the package and reduce your coupling even more with The Mediator Pattern. The nuget package [MediatR](https://github.com/jbogard/MediatR) provides a useful API to get started quickly. But it uses reflection under the hood, which can bring some overhead. The nuget package [Mediator.SourceGenerator](https://github.com/martinothamar/Mediator) uses [source generating](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview) to generate the code which then can be optimized further by the [.NET Compiler (Roslyn)](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/).

Since the APIs of both projects are very similar they can be swapped out for another which leads to:

- Scenario 3.1: Implementing it by hand
- Scenario 3.2: Using MediatR
- Scenario 3.3: Using Mediator.SourceGenerator

### Optimizations enabled by CQRS

Since CQRS proposes the separation of the responsibility of commands and queries. We can split the Entity Framework Core `DbContext` into a separate `WriteDbContext` for commands and a `ReadDbContext` for queries. This enables further optimizations like configuring all queries as `NoTracking` so the `ChangeTracker` doesn't have to track the entities, which leads to less memory allocation and processing.
This scenario is the same as [Scenario 1: Traditional](Application_Scenario_Overview.md#scenario-1-traditional) but the implementation has changes following now the [CQRS pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs).

## Common facts for all scenarios

### Endpoint definition in minimal APIs

This project uses [C# 12](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12) as well as the Frameworks [.NET 8](https://learn.microsoft.com/en-us/dotnet/) and [ASP.NET 8](https://learn.microsoft.com/en-us/aspnet/overview). All endpoints in the Web API project are defined using the [minimal API](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-8.0) approach instead of [controllers](https://learn.microsoft.com/en-us/aspnet/mvc/overview/older-versions-1/controllers-and-routing/aspnet-mvc-controllers-overview-cs). Here is simple example:

We define a `/hello` endpoint which returns `Hello World`.

```http
GET /hello
```
```text/plain
Hello World!
```

With minimal APIs:

```csharp
var app = WebApplication.Create(args);

app.MapGet("/hello", () => "Hello World!");

app.Run();
```

With controllers:

```csharp
var builder = WebApplication.CreateBuilder(args);  
  
builder.Services.AddControllers();  
  
var app = builder.Build();  
  
app.MapControllers();  
  
app.Run();  
  
[Route("/hello")]  
public class HelloWorldController : Controller  
{  
    [HttpGet]
    public string Get() => "Hello World!";
}
```

### Exception handling with ASP.NET

This Web API uses structured error responses conforming to the [RFC 7807](https://tools.ietf.org/html/rfc7807) standard for expressing problems and errors encountered during API operations. Clients should expect and handle these structured error responses gracefully. This is achieved by using the [exception handling middle ware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-8.0#exception-handler-page) provided by [ASP.NET](https://learn.microsoft.com/en-us/aspnet/overview). Here is a simple example:

We define a `/boom` endpoint to trigger the exception handler. It them redirects our call the the `/error` endpoint.

```HTTP
GET /boom
```
```HTTP
GET /error
```

```csharp
var app = WebApplication.Create(args);  
  
app.MapGet("/boom", () =>  
{  
    throw new Exception("Boom!");  
});  
  
app.MapGet("/error", () => Results.Problem("An error occurred", statusCode: 500));  
  
app.UseExceptionHandler("/error");  
  
app.Run();
```

When the `/boom` endpoint is called we get the a `Content-Type: application/problem+json` response with the status code 500 Internal Sever error. The stack trace and other sensitive information's doesn't get leaked to the user. In this case no other information about the exception are returned to the user but the `/error` endpoint could for example log the exceptions with the request trace id and return additional information's like a support contact to the user.

```HTTP
HTTP/1.1 500 Internal Server Error
Content-Type: application/problem+json
```
```json
{
    "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
    "title": "An error occurred while processing your request.",
    "status": 500,
    "detail": "An error occurred"
}
```

### Caching in memory

This project uses the `IMemoryCache` abstraction for caching the result of different database queries.

#TODOs
- [ ] Add code snippets

It also uses request output caching for same requests which hits the API multiple times. This functionality caches the hole HTTP response with the header and everything else.

## Common Analysis Technics

### Focus of the analysis

The projects include endpoints with simple and complex queries. Therefore the complexity of each endpoint and the resulting database queries is evaluated accordingly. A simple query would e.g. be selecting all `Categories` associated with one `Article`. A complex query would be selecting all child `Categories` recursively out of a tree structure. The Analysis is focused on the complex read queries as those are suspected to benefit the most of the [[CQRS]] redesign.

### Collected metrics

The analysis focuses on the **HTTP response times** but interesting anomalies in other metrics will be analysed as well.

- HTTP response times
- HTTP error rates (unsuccessful status codes)
- Scalability e.g. max requests per sec/min
- (CPU and Memory usage)
- (Database latency and query analysis)

## Used technologies

The following tools were used:

Database: [PostgreSQL](https://www.postgresql.org/docs/) Version: [16.2](https://www.postgresql.org/about/news/postgresql-162-156-1411-1314-and-1218-released-2807/)
Deployment: [Docker](https://docs.docker.com/)
Testing: [K6](https://k6.io/docs/examples/tutorials/get-started-with-k6/) [(Grafana docs)](https://grafana.com/docs/k6/latest/)
Visualizations: [Grafana](https://grafana.com/docs/grafana/latest/)