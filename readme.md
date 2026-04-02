# QueryFilter

OData benzeri sorgu dizimi ile `IQueryable` ve `IEnumerable` koleksiyonları uzerinde filtreleme, siralama, sayfalama ve gruplama islemleri yapmanizi saglayan bir .NET kutuphanesidir.

[![NuGet](https://img.shields.io/nuget/v/SmartCreative.QueryFilter.svg)](https://www.nuget.org/packages/SmartCreative.QueryFilter/)
[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.1-blue.svg)]()

## Kurulum

```bash
dotnet add package SmartCreative.QueryFilter
```

## Hizli Baslangiç

```csharp
var studentList = new List<StudentModel> {
    new StudentModel { Name = "Nancy", LastName = "Fuller", Age = 35 },
    new StudentModel { Name = "Andrew", LastName = "Leverling", Age = 33 },
    new StudentModel { Name = "Janet", LastName = "Peacock", Age = 32 }
};

var queryFilterModel = QueryFilterModel.Parse("$filter=Name~eq~'Nancy'");
var result = studentList.QueryFilter(queryFilterModel);
// => Nancy Fuller
```

## Sorgu Parametreleri

Birden fazla parametre icin `&` karakteri kullanilir.

| Parametre   | Aciklama                                  | Ornek                          |
|-------------|-------------------------------------------|--------------------------------|
| `$filter`   | Filtreleme kosullari                      | `Name~eq~'Nancy'`             |
| `$top`      | Dondurulecek kayit sayisi                 | `$top=10`                      |
| `$skip`     | Atlanacak kayit sayisi (sayfalama)        | `$skip=20`                     |
| `$orderby`  | Siralama                                  | `Name-asc`, `Age-desc`        |
| `$select`   | Secilecek kolonlar                        | `$select=Name,Age`            |
| `$from`     | Tablo/entity adi                          | `$from=Products`              |
| `$groupby`  | Gruplama                                  | `$groupby=Status`             |

## Operatorler

Filtre ifadelerinde ayrac olarak `~` karakteri kullanilir.

### Karsilastirma

| Operator         | Kisa Kod       | Aciklama                | Ornek                              |
|------------------|----------------|-------------------------|------------------------------------|
| equal            | `eq`           | Esit                    | `Name~eq~'Nancy'`                 |
| not equal        | `ne`           | Esit degil              | `Name~ne~'Nancy'`                 |
| less than        | `lt`           | Kucuk                   | `Age~lt~30`                        |
| less or equal    | `le`           | Kucuk veya esit         | `Age~le~30`                        |
| greater than     | `gt`           | Buyuk                   | `Age~gt~30`                        |
| greater or equal | `ge`           | Buyuk veya esit         | `Age~ge~30`                        |

### String Fonksiyonlari

| Operator         | Kisa Kod          | Aciklama                | Ornek                              |
|------------------|-------------------|-------------------------|------------------------------------|
| contains         | `contains`        | Iceren                  | `Name~contains~'an'`              |
| not contains     | `necontains`      | Icermeyen               | `Name~necontains~'an'`            |
| starts with      | `startswith`      | Ile baslayan            | `Name~startswith~'Na'`            |
| not starts with  | `notstartswith`   | Ile baslamayan          | `Name~notstartswith~'Na'`         |
| ends with        | `endswith`        | Ile biten               | `Name~endswith~'cy'`              |
| not ends with    | `notendswith`     | Ile bitmeyen            | `Name~notendswith~'cy'`           |

### Kume Operatorleri

| Operator         | Kisa Kod   | Aciklama                | Ornek                              |
|------------------|------------|-------------------------|------------------------------------|
| in               | `in`       | Listede bulunan         | `Age~in~[32,33]`                  |
| not in           | `notin`    | Listede bulunmayan      | `Age~notin~[32,33]`              |

### Mantiksal Operatorler

| Operator | Aciklama   | Ornek                                        |
|----------|------------|----------------------------------------------|
| `and`    | Ve         | `Name~eq~'Nancy'~and~Age~gt~30`             |
| `or`     | Veya       | `Name~eq~'Nancy'~or~Name~eq~'Andrew'`       |

## Desteklenen Veri Tipleri

| Tip        | Soz Dizimi                               | Ornek                                           |
|------------|------------------------------------------|-------------------------------------------------|
| String     | Tek tirnak ile sarili                    | `Name~eq~'Nancy'`                               |
| Number     | Dogrudan                                 | `Age~gt~30`                                      |
| DateTime   | `datetime` on eki                        | `Birth~eq~datetime'2020-11-20'`                 |
| TimeSpan   | `time` on eki                            | `Time~gt~time'09:00:00'`                        |
| Boolean    | `true` / `false`                         | `IsActive~eq~true`                               |
| Null       | `null`                                   | `Name~eq~null`                                   |
| GUID       | Tek tirnak ile sarili                    | `Id~eq~'f55b2c58-bd48-4ace-aaf4-cddc3fc00e13'` |
| Array      | Koseli parantez                          | `Age~in~[32,33,35]`                             |

## Kullanim Ornekleri

### Temel Filtreleme

```csharp
// String esitlik
var qf = QueryFilterModel.Parse("$filter=Name~eq~'Nancy'");
var result = studentList.QueryFilter(qf);

// Sayisal karsilastirma
var qf = QueryFilterModel.Parse("$filter=Age~gt~32");
var result = studentList.QueryFilter(qf);

// Null kontrolu
var qf = QueryFilterModel.Parse("$filter=Name~eq~null");
var result = studentList.QueryFilter(qf);
```

### Sayfalama ve Siralama

```csharp
var qf = QueryFilterModel.Parse("$filter=Age~gt~20&$top=10&$skip=0&$orderby=Age-desc");
var result = studentList.QueryFilter(qf);
// result.Items  => Filtrelenmis kayitlar
// result.TotalCount => Toplam kayit sayisi
```

### Birlesik (Composite) Filtreler

Parantez ile gruplama ve `and`/`or` operatorleri desteklenir:

```csharp
// AND + OR birlesimi
var qf = QueryFilterModel.Parse(
    "$filter=(Name~eq~null~and~Age~in~[93])~or~((Name~eq~'Nancy'~and~Age~in~[35]))"
);
var result = studentList.QueryFilter(qf);
```

### Nested Property Erisimi

Nokta notasyonu ile ic ice property'lere erisim:

```csharp
var qf = QueryFilterModel.Parse("$filter=Fields.OrderNumber.keyword~eq~'L591-2461481851'");
```

### IQueryable ile Kullanim (Entity Framework)

```csharp
// DbContext ile
var qf = QueryFilterModel.Parse("$filter=Status~eq~'Active'&$top=20&$orderby=CreatedAt-desc");
var expression = ExpressionMethodHelper.GetExpression<Product>(qf.FilterDescriptors);
var result = dbContext.Products.Where(expression).ToList();
```

### QueryAdditional ile Ozel Kosullar

`IQueryAdditional<T>` arayuzu ile standart filtrelerin uzerine ek kosullar ekleyebilirsiniz:

```csharp
public class ActiveProductFilter : IQueryAdditional<Product>
{
    public IQueryable<Product> Apply(IQueryable<Product> query)
        => query.Where(p => p.IsActive && p.Stock > 0);

    public Expression<Func<Product, bool>> GetExpression()
        => p => p.IsActive && p.Stock > 0;
}

var qf = QueryFilterModel.Parse("$filter=Category~eq~'Electronics'");
qf.AddQueryAdditional(new ActiveProductFilter());
var result = dbContext.Products.AsQueryable().ApplyFilter(qf);
```

### Programatik Filtre Olusturma

Sorgu dizimi yerine dogrudan model uzerinden filtre tanimlayabilirsiniz:

```csharp
var qf = new QueryFilterModel();
qf.FilterDescriptors.Add(new FilterDescriptor
{
    Member = nameof(StudentModel.Age),
    Operator = FilterOperator.IsContainedIn,
    Value = new int[] { 32, 20 }
});
var result = studentList.QueryFilter(qf);
```

## SQL Formatter

Sorgu ifadelerini ham SQL'e donusturebilirsiniz:

### Standart SQL

```csharp
var qf = QueryFilterModel.Parse("$filter=Name~eq~'Nancy'&$from=Students&$top=10");
var formatter = new SQLFormatter(qf);
var sql = formatter.Format();
// SELECT * FROM "Students" WHERE "Name" = @Name ORDER BY ... OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY
```

### PostgreSQL (JSONB Destegi)

```csharp
var qf = QueryFilterModel.Parse("$filter=Name~eq~'Nancy'&$from=Students");
qf.JsonbColumns = new List<string> { "Metadata" };
qf.JsonbArrayColumns = new List<string> { "Tags" };

var formatter = new PostgreSQLFormatter(qf);
var sql = formatter.Format();
```

PostgreSQL formatter'in JSONB ozellikleri:
- `@>` (contains), `<@` (contained in) operatorleri
- `?|` (any of), `?&` (all of) dizi operatorleri
- Nested JSON path destegi (nokta notasyonu ile)
- JSONPath ifadeleri (`@@` operatoru)
- Dizi icinde nesne arama

## Locale-Safe String Karsilastirma

String karsilastirmalari `ToLowerInvariant()` kullanir. Bu sayede Turkce `I/i/I/i` gibi locale'e bagli karakter donusumleri veritabani ile uyumsuzluk yaratmaz.

```
// Olusan expression:
x.Property != null AND lower(trim(x.Property)) == 'deger'
```

Veritabani tarafinda SQL `LOWER()` fonksiyonu, C# tarafinda `ToLowerInvariant()` kullanilir. Her iki taraf da ayni sonucu uretir.

## Proje Yapisi

```
src/
  QueryFilter/                  # Ana kutuphane (.NET Standard 2.1)
    Descriptor/                 # FilterDescriptor, SortDescriptor, vb.
      Filtering/Parsing/        # FilterLexer, FilterParser
    Extension/                  # ExpressionExtension
    Formatter/                  # SQLFormatter, PostgreSQLFormatter
    Helper/                     # ExpressionMethodHelper
  QueryFilter.Test/             # Test projesi (NUnit, .NET 6.0)
```

## Bagimliliklar

- Newtonsoft.Json 13.0.1
- System.Linq.Dynamic.Core 1.3.0

## Lisans

GPL v3 - Detaylar icin [LICENSE](LICENSE) dosyasina bakiniz.

## Katkida Bulunma

Katkida bulunmak icin pull request gonderebilirsiniz. Buyuk degisiklikler icin once bir issue acmaniz onerilir.
