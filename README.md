# CQRS Performance Analysis

## Disclaimer

This project is currently under development 🏗👷‍♂️🔨. Therefore, the documentation 📝 is not complete yet and may have outdated information. You can view 🔍 the open tasks 📋 and eventually planned features at [TODOs](./docs/TODOs.md).

TODO: Change readme to have:
- A quick introduction
- links to
    - solution structure
    - Use cases
    - Scenarios
    - Businesses scenario
    - Endpoints (Request and Response)
- Requirements to get started
    - Docker
    - Dotnet sdk
- How to run the tests manual and automated

TODO: Anleitung wie man die endpoints ausprobiert, die app in docker started, die test local, in docker und automatisiert ausführt.
TODO: Use click to expand in Readme
TODO: link to the database scheme and put it on a page with some information about the database

TODO: Put this into a sh file

## Build the performance test project:
- `cd .\repositories\DemoProjects\Bachelor\CqrsPerformanceAnalysis`
- `rm .\src\Traditional\Traditional.Api\appsettings.Development.json`
- `rm .\src\Traditional\Traditional.Api\appsettings.json`
- `cd .\tests\Common\PerformanceTests\`
- `dotnet restore PerformanceTests.csproj --no-cache`
- `dotnet build PerformanceTests.csproj -c Release --no-cache`
- `dotnet publish PerformanceTests.csproj -c Release /p:UseAppHost=false`
- `cd ..\..\..\`
- `git checkout -- .\src\Traditional\Traditional.Api\appsettings.json`
- `git checkout -- .\src\Traditional\Traditional.Api\appsettings.Development.json`

## Run the performance test project:
- `cd .\repositories\DemoProjects\Bachelor\CqrsPerformanceAnalysis\tests\Common\PerformanceTests\`
- `dotnet C:\Users\jens.richter\repositories\DemoProjects\Bachelor\CqrsPerformanceAnalysis\tests\Common\PerformanceTests\bin\Release\net8.0\publish\PerformanceTests.dll --urls="http://localhost:5017"`
- `curl 'http://localhost:5017'`
- `curl 'http://localhost:5017/K6Tests/allOfBothApis?checkElastic=true&withWarmUp=true&saveMinimalResults=true'`

## Overview

This project aims to provide different complex scenarios. Those are used to evaluate if restructuring a project with CQRS will bring performance benefits.

See also the specific documentation for each scenario:
- [Scenario 1: Traditional](./docs/Application_Scenario_Overview.md#scenario-1-traditional)
- [Scenario 2: CQRS](./docs/Application_Scenario_Overview.md#scenario-2-cqrs)
- [Database Information](./docs/Database_Scheme_Overview.md)

## Business scenario

This Web API provides access to article content data like categories and attributes. The user can provide new data for the articles which will be saved in a database.

#TODOs
- [ ] Provide more information about the use cases.
- This API writes and reads data about articles, categories and category specific attributes from an internal company database.
- The company's content team uses this API to manage the attributes and categories of the articles.
- This services does not sync the data with any external services (marketplaces). This is done by another service.

### Endpoints

#TODOs
- [ ] List all routes endpoints here

These are all the endpoints which are available in the API:
```http
GET /attributes
PUT /attributes
GET /attributes/subAttributes
GET /attributes/leafAttributes
GET /categories
PUT /categories
GET /categories/children
GET /categories/search
GET /rootCategories
GET /error
```
Try the endpoints yourself by using http requests in the [requests](./requests) folder. You can fire a request right away in JetBrains Rider or in Visual Studio Code with the [REST Client](https://marketplace.visualstudio.com/items?itemName=humao.rest-client) extension.
