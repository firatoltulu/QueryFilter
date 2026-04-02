# CLAUDE.md

## Project Overview

QueryFilter is a .NET library that provides OData-like query filtering, sorting, paging, and grouping over `IQueryable` and `IEnumerable` collections. Published as NuGet package `SmartCreative.QueryFilter`.

## Build & Test

```bash
# Build
cd src && dotnet build

# Run all tests
cd src && dotnet test

# Run specific test class
cd src && dotnet test --filter "FullyQualifiedName~TurkishLocaleTest"
```

- Solution file: `src/QueryFilter.sln`
- Main library: `src/QueryFilter/QueryFilter.csproj` (netstandard2.1)
- Test project: `src/QueryFilter.Test/QueryFilter.Test.csproj` (net6.0, NUnit)

## Architecture

### Key Components

- **Parsing**: `FilterLexer` tokenizes query strings, `FilterParser` builds expression trees (recursive descent)
- **Descriptors**: `FilterDescriptor`, `CompositeFilterDescriptor`, `SortDescriptor`, `SelectDescriptor`, `GroupDescriptor`
- **Expression Building**: `ExpressionMethodHelper.GetExpression<T>()` converts filter descriptors to `Expression<Func<T, bool>>`
- **Extensions**: `ExpressionExtension.TrimToLower()` normalizes strings for case-insensitive comparison
- **SQL Formatters**: `SQLFormatter` (standard SQL), `PostgreSQLFormatter` (JSONB support)
- **QueryAdditional**: `IQueryAdditional<T>` interface for custom filter conditions beyond standard query syntax

### Query Syntax

Delimiter: `~` (tilde). Parameters: `$filter`, `$top`, `$skip`, `$orderby`, `$select`, `$from`, `$groupby`.

Example: `$filter=Name~eq~'Nancy'~and~Age~gt~30&$top=10&$orderby=Name-asc`

### String Comparison Strategy

All string comparisons use `ToLowerInvariant()` to avoid locale-specific issues (Turkish-I problem). The member (DB column) side uses `ToLowerInvariant()` in the expression tree which ORM providers translate to SQL `LOWER()`. The constant (parameter) side is evaluated directly in C# using `ToLowerInvariant()`.

## Code Conventions

- Language: C# with .NET Standard 2.1
- Namespace: `QueryFilter`
- Tests: NUnit with `[TestFixture]`, `[Test]`, `[TestCaseSource]`
- Expression trees are used extensively; member expressions go to SQL, constant expressions are evaluated in C#
- JSON support targets PostgreSQL JSONB columns specifically

## Important Patterns

- `ExpressionExtension.TrimToLower()` has 3 overloads: `MemberExpression` (DB side), `ConstantExpression` (C# side), `UnaryExpression` (C# side). The DB side builds an expression tree node; the C# side evaluates the value directly.
- `ExpressionMethodHelper.In()` handles string containment separately with its own `ToLowerInvariant` logic for both single and multi-value scenarios.
- Null checks are prepended via `AddNullCheck()` for string comparisons.

## Dependencies

- Newtonsoft.Json 13.0.1
- System.Linq.Dynamic.Core 1.3.0
