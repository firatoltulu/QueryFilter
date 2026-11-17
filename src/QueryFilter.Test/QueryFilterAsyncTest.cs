namespace QueryFilter.Test
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    [TestFixture]
    public class QueryFilterAsyncTest
    {
        public class AsyncTestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string City { get; set; }
            public int Age { get; set; }
        }

        private List<AsyncTestEntity> _data;

        // Bellek içi IQueryable için ORM'in CountAsync/ToListAsync'ini taklit eden fonksiyonlar.
        private static Task<int> CountAsync(IQueryable<AsyncTestEntity> q, CancellationToken ct)
            => Task.FromResult(q.Count());

        private static Task<List<AsyncTestEntity>> ToListAsync(IQueryable<AsyncTestEntity> q, CancellationToken ct)
            => Task.FromResult(q.ToList());

        [SetUp]
        public void Setup()
        {
            var cities = new[] { "Ankara", "Istanbul", "Izmir", "Bursa" };
            _data = Enumerable.Range(1, 25)
                .Select(i => new AsyncTestEntity
                {
                    Id = i,
                    Name = "Person " + i,
                    City = cities[i % cities.Length],
                    Age = 18 + (i * 7 % 40)
                })
                .ToList();
        }

        public static IEnumerable<TestCaseData> EquivalenceModels()
        {
            yield return new TestCaseData("$filter=Age~gt~30&$top=100").SetName("Async_Equals_Sync_Filtered");
            yield return new TestCaseData("$orderby=Age-desc,Id-asc&$top=100").SetName("Async_Equals_Sync_Sorted");
            yield return new TestCaseData("$skip=10&$top=5").SetName("Async_Equals_Sync_Paged");
            yield return new TestCaseData("$filter=City~eq~'Ankara'~or~Age~lt~30&$orderby=Name-desc&$skip=2&$top=4")
                .SetName("Async_Equals_Sync_FilteredSortedPaged");
        }

        [TestCaseSource(nameof(EquivalenceModels))]
        public async Task QueryFilterAsync_ReturnsSameResultAsSync(string queryString)
        {
            var model = QueryFilterModel.Parse(queryString);

            var expected = _data.AsQueryable().QueryFilter(model);
            var actual = await _data.AsQueryable().QueryFilterAsync(model, CountAsync, ToListAsync);

            Assert.AreEqual(expected.TotalCount, actual.TotalCount, "TotalCount");
            CollectionAssert.AreEqual(expected.Items.Select(x => x.Id).ToList(), actual.Items.Select(x => x.Id).ToList(), "Items order/page");
        }

        [Test]
        public async Task QueryFilterAsync_ResultIsActuallyFilteredSortedPaged()
        {
            // Senkron yolla karşılaştırmanın yanında bağımsız bir beklenti: iki yol aynı hatayı paylaşsa da yakalansın.
            var model = QueryFilterModel.Parse("$filter=Age~gt~30&$orderby=Id-desc&$skip=1&$top=3");

            var actual = await _data.AsQueryable().QueryFilterAsync(model, CountAsync, ToListAsync);

            var filtered = _data.Where(x => x.Age > 30).OrderByDescending(x => x.Id).ToList();
            Assert.AreEqual(filtered.Count, actual.TotalCount);
            CollectionAssert.AreEqual(filtered.Skip(1).Take(3).Select(x => x.Id).ToList(), actual.Items.Select(x => x.Id).ToList());
        }

        [Test]
        public async Task QueryFilterAsync_StringOverload_ReturnsSameResultAsSync()
        {
            const string queryString = "$filter=Age~gt~30&$orderby=Name-asc&$skip=1&$top=3";

            var expected = _data.AsQueryable().QueryFilter(queryString);
            var actual = await _data.AsQueryable().QueryFilterAsync(queryString, CountAsync, ToListAsync);

            Assert.AreEqual(expected.TotalCount, actual.TotalCount);
            CollectionAssert.AreEqual(expected.Items.Select(x => x.Id).ToList(), actual.Items.Select(x => x.Id).ToList());
        }

        [Test]
        public async Task QueryFilterAsync_CallsEachFunctionExactlyOnce()
        {
            var countCalls = 0;
            var listCalls = 0;
            IQueryable<AsyncTestEntity> countQuery = null;
            IQueryable<AsyncTestEntity> pageQuery = null;

            var model = QueryFilterModel.Parse("$filter=Age~gt~30&$skip=2&$top=3");

            var result = await _data.AsQueryable().QueryFilterAsync(
                model,
                async (q, ct) => { countCalls++; countQuery = q; await Task.Yield(); return 999; },
                async (q, ct) => { listCalls++; pageQuery = q; await Task.Yield(); return new List<AsyncTestEntity> { new AsyncTestEntity { Id = -1 } }; });

            Assert.AreEqual(1, countCalls, "countAsync call count");
            Assert.AreEqual(1, listCalls, "toListAsync call count");

            // Sonuç gerçekten fonksiyonların dönüşünden gelmeli.
            Assert.AreEqual(999, result.TotalCount);
            CollectionAssert.AreEqual(new[] { -1 }, result.Items.Select(x => x.Id).ToArray());

            // countAsync sayfalanmamış, toListAsync sayfalanmış sorguyu almalı.
            Assert.AreEqual(_data.Count(x => x.Age > 30), countQuery.Count());
            Assert.AreEqual(3, pageQuery.Count());
        }

        [Test]
        public void QueryFilterAsync_NullCountAsync_Throws()
        {
            var model = QueryFilterModel.Parse("$top=5");
            var ex = Assert.Throws<ArgumentNullException>(() => _data.AsQueryable().QueryFilterAsync(model, null, ToListAsync));
            Assert.AreEqual("countAsync", ex.ParamName);
        }

        [Test]
        public void QueryFilterAsync_NullToListAsync_Throws()
        {
            var model = QueryFilterModel.Parse("$top=5");
            var ex = Assert.Throws<ArgumentNullException>(() => _data.AsQueryable().QueryFilterAsync(model, CountAsync, null));
            Assert.AreEqual("toListAsync", ex.ParamName);
        }

        [Test]
        public void QueryFilterAsync_StringOverload_NullFunctions_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => _data.AsQueryable().QueryFilterAsync("$top=5", null, ToListAsync));
            Assert.Throws<ArgumentNullException>(() => _data.AsQueryable().QueryFilterAsync("$top=5", CountAsync, null));
        }

        [Test]
        public void QueryFilterAsync_CancelledToken_ReachesCountAsync()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var model = QueryFilterModel.Parse("$top=5");

            Assert.ThrowsAsync<OperationCanceledException>(() => _data.AsQueryable().QueryFilterAsync(
                model,
                (q, ct) => { ct.ThrowIfCancellationRequested(); return Task.FromResult(q.Count()); },
                ToListAsync,
                cts.Token));
        }

        [Test]
        public void QueryFilterAsync_CancelledToken_ReachesToListAsync()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var model = QueryFilterModel.Parse("$top=5");

            Assert.ThrowsAsync<OperationCanceledException>(() => _data.AsQueryable().QueryFilterAsync(
                model,
                CountAsync, // belirteci yok sayar; iptal toListAsync'te görülmeli
                (q, ct) => { ct.ThrowIfCancellationRequested(); return Task.FromResult(q.ToList()); },
                cts.Token));
        }

        [Test]
        public async Task QueryFilterAsync_PassesSameTokenToBothFunctions()
        {
            using var cts = new CancellationTokenSource();
            CancellationToken countToken = default, listToken = default;

            await _data.AsQueryable().QueryFilterAsync(
                QueryFilterModel.Parse("$top=5"),
                (q, ct) => { countToken = ct; return CountAsync(q, ct); },
                (q, ct) => { listToken = ct; return ToListAsync(q, ct); },
                cts.Token);

            Assert.AreEqual(cts.Token, countToken);
            Assert.AreEqual(cts.Token, listToken);
        }

        [Test]
        public async Task QueryFilterAsync_Grouped_ReturnsSameResultAsSync_AndFallsBackToSyncPath()
        {
            var model = QueryFilterModel.Parse("$groupby=City&$top=10");
            Assert.That(model.GroupDescriptors.Count, Is.GreaterThan(0));

            var expected = _data.AsQueryable().QueryFilter(model);

            var countCalls = 0;
            var listCalls = 0;
            var actual = await _data.AsQueryable().QueryFilterAsync(
                model,
                (q, ct) => { countCalls++; return CountAsync(q, ct); },
                (q, ct) => { listCalls++; return ToListAsync(q, ct); });

            Assert.AreEqual(4, expected.TotalCount, "sanity: 4 distinct cities");
            Assert.AreEqual(expected.TotalCount, actual.TotalCount);
            CollectionAssert.AreEqual(expected.Items.Select(x => x.City).ToList(), actual.Items.Select(x => x.City).ToList());

            // Gruplu yol bu sürümde senkron çalışır; async fonksiyonlar kullanılmaz.
            Assert.AreEqual(0, countCalls);
            Assert.AreEqual(0, listCalls);
        }
    }
}
