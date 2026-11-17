# QueryFilter

> The full documentation is currently in Turkish: see [readme.md](readme.md).

## Async Pagination (linq2db / EF Core)

`QueryFilterAsync` applies exactly the same filter, sorting, `Skip` and `Take` as `QueryFilter`, but runs the total count and the page query through async functions you supply. The library does not depend on linq2db or EF Core; you pass your ORM's `CountAsync` / `ToListAsync`. The `CancellationToken` is forwarded to both functions unchanged.

```csharp
// linq2db
var page = await table.QueryFilterAsync(model,
    (q, ct) => LinqToDB.AsyncExtensions.CountAsync(q, ct),
    (q, ct) => LinqToDB.AsyncExtensions.ToListAsync(q, ct), ct);

// EF Core
var page = await dbContext.Products.QueryFilterAsync("$filter=Price~gt~100&$top=20",
    (q, ct) => q.CountAsync(ct),
    (q, ct) => q.ToListAsync(ct), ct);
```

> Note: queries with `$groupby` are not async in this version; the synchronous path runs and its result is returned via `Task.FromResult` (the supplied functions are not called).
