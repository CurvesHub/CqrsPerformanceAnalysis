# Database Information

The data for this project is stored in a open source object-relational database called [PostgreSQL](https://www.postgresql.org/docs/) (Version: [16.2](https://www.postgresql.org/about/news/postgresql-162-156-1411-1314-and-1218-released-2807/)).


## Database scheme

#TODOs
Add docs for the database scheme

<div style="text-align: center;"><img src="./assets/Traditional%20Database%20Scheme.png" alt="Traditional Database 20Scheme" width="500"/></div>


## Optimizations

#TODOs 
- [ ] Clean up this section!
- [ ] Question: does ef core calls `vacum` or `reindex` on the db (e.g. for full text indices) or does the db never gets clean up?

### Indexing

Like most Applications this one is read heavy. Therefore, indices got placed to improved readability. The overhead on writing the data shouldn't be a problem.

- mostly relevant for tables with a large amount of data
- Binary trees / lookup tables
- is configured in the db context model configurations

Treads to Validity:
> The last way out for Indexing:
> index(email) can be quite large but if you save the email as hash code then the index would be smaller and perhaps faster
> Downside: The conversion has to happen in the code maybe in the model or even db context. Conversion may be so slow that there is no benefit of the index. 

### Views / materialized Views

> Views can store a sql query similar to prepared statements or stored procedures.

> Can be a performance improvement for queries with a lot of joins, but doesn't need to be since joining on indices is already quit efficient.

- ⚠ materialized views need to be refreshed, which is not handles by the db automatically
- e.g. refresh when a put request comes in via events or pipeline behavior (Treads to Validity)

### Table structure improvements

> The structure of a table matters the get the last bit of performance out of the database.
> 
- e.g. **int before varchar**

> fixed data types first then later the variable data types like string (since it has a different length every time)


### Tips & Tricks

- explain analyze before query will show the execution plan



## Database scenarios

#TODOs 
- [ ] Clean up this section!

> Example GitHub repository with docker compose files for replicas: 
> https://github.com/dreamsofcode-io/postgres-brrr


> Pitfall: extra complexity in the system and it must be set up and monitored that the read replicas are working as intended

**Tools**
- PGBouncer
- PgPool-II

### 1. One standard PostgreSQL DB

### 2. One standard PostgreSQL DB with one read replica

- asynchron/synchron streaming (DDL Command)

Is the best and fastes way (there is also log shipping, and logical transfer, and full data transfer) -> can have a little amount of lag but not much even on very busy transaction servers

- Can be changes symcron / asyncon
- Async is default

- **2.1 both with standard config**
- **2.2 read replica with read optimized config (memory etc.)**

### Separate Dbs

>Two separate databases which are not liked in any we. The synchronization would be handled by a self implemented C# service which can listen on events and update the read DB accordingly.
- [ ] Reason why this is probably worse then a read replica with streaming which is optimized for this scenario (Treads to Validity), but mention it as an alternative. (Maybe the db doesnt support read replicas or you want to use a SQL and NoSQL database)

### Load balancing
> Only mention that it is recommended if one read replica is not enough the use load balancing toll like "PgPool or HAProxy (open source)" with multiple replicas. Note: One main db can support multiple read replicas
