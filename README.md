# CQRS Performance Analysis

## Overview

This project is used to compare a traditional API with a CQRS API. The goal is to evaluate if restructuring a project with CQRS will bring performance benefits.

The solution includes the following projects:
- `Traditional.Api`: A traditional API which uses a single `DbContext` for all queries and commands.
- `Cqrs.Api`: A CQRS API which uses two `DbContexts` for queries and commands.
- `main-MediatR branch`: The CQRS API uses the MediatR package to implement the CQRS pattern.

## Business scenario

This Web API provides access to article content data like categories and attributes. The user can provide new data for the articles which will be saved in a database.

- This API writes and reads data about articles, categories and category specific attributes from an internal company database.
- The company's content team uses this API to manage the attributes and categories of the articles.
- This services does not sync the data with any external services (marketplaces). This is done by another service.

## Endpoints

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

## Used technologies

The following tools were used:

Database: [PostgreSQL](https://www.postgresql.org/docs/) Version: [16.2](https://www.postgresql.org/about/news/postgresql-162-156-1411-1314-and-1218-released-2807/)
Deployment: [Docker](https://docs.docker.com/)
Testing: [K6](https://k6.io/docs/examples/tutorials/get-started-with-k6/) [(Grafana docs)](https://grafana.com/docs/k6/latest/)
Visualizations: [Grafana](https://grafana.com/docs/grafana/latest/)
