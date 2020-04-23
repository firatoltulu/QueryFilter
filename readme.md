#QueryFilter

QueryFilter, OData benzeri LINQ veya EntityFramework üzerinde çalışabilen sorgu kümelerine filtreleme veya gruplama işlemi yapabileceğiniz bir araçtır. 



## Özellikler (v1.0.0)


* `IQueryable` veya `IEnumerable` üzerinde filtreleme veya gruplama yapabilirsiniz
* Desteklenen Operatörler
  * in
  * equal
  * not equal
  * less
  * less or equal
  * greater
  * greater or equal
  * starts with
  * ends with
  * contains
 


## Başlayalım
ClientSide veya WebApi tarafından gelen **`QueryFilter`** söz dizimleri aşağıdaki parametler ile gönderilmelidir. Birden fazla ifade kullanımı için **&** karakterini kullanabilirsiniz.

`data:`

```csharp
var studentList =  new List<StudentModel> {
        new StudentModel { Name="Nancy",LastName="Fuller",Age=35 },
        new StudentModel { Name="Andrew",LastName="Leverling",Age=33 },
        new StudentModel { Name="Janet",LastName="Peacock",Age=32 }
};
```


### $filter

```csharp
    
     var queryFilterModel = QueryFilterModel.Parse("$filter=Name~eq~'Nancy'");
     var result = studentModels.ApplyQueryFilter(queryFilterModel);

   //Nancy Fuller         
```

### $top

Bir sıranın başından itibaren belirtilen sayıda bitişik öğeyi döndürür.


```csharp
    
     var queryFilterModel = QueryFilterModel.Parse("$filter=Age~gt~20&$top=1");
     var result = studentModels.ApplyQueryFilter(queryFilterModel);

   //Nancy Fuller         
```


### $skip

Bir dizide belirtilen sayıda öğeyi atlar ve ardından kalan öğeleri döndürür.


```csharp
    
     var queryFilterModel = QueryFilterModel.Parse("$filter=Age~gt~20&$top=1&$skip=1");
     var result = studentModels.ApplyQueryFilter(queryFilterModel);

   //Andrew Leverling         
```


## Kullanabileceğiniz Operatörler

*`$filter`* söz dizimi içinde aşağıdaki ifade ve operatörleri kullanabilirsiniz. Söz dizimi içinde ayraç olarak **`~`** karakteri kullanılmıştır. 2 veya daha fazla koşulu oluşturmak için yine ayraç **`~`** karakterini kullacağız. `$filter=Age~eq~0~and~Name~eq~'Nancy'`


### Karşılaştırma ve Fonksiyon

| Operatör          |  Karşılığı           | Açıklama  |
| -------------     |:-------------|:-----|
| equal             | eq           |   eşit |
| not equal         | ne           |   eşit değil |
| less              | lt           |   küçük |
| less or equal     | le           |   küçük ve eşit |
| greater           | ge           |   büyük |
| greater or equal  | gt           |   büyük ve eşit |
| ends with         | endswith     |   ile biten |
| starts with       | startswith   |   ile başlayan |
| contains          | contains     |   içeren |

### Mantıksal

| Operatör          |  Karşılığı  | Açıklama  |
| -------------     |:-------------|:-----|
| and               | and      |      |
| or                | or       |      |


### İfade

| Operatör          |  Karşılığı  | Açıklama  |
| -------------     |:-------------|:-----|
| true              | true      |      |
| false             | false       |      |

  
## Kullanım Biçimleri








## Contributions