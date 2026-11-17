namespace QueryFilter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Dynamic.Core;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;

    public static class QueryFilterExtension
    {
        private static IQueryable SetSorting<TEntity>(QueryFilterModel queryFilterCommand, IQueryable query)
        {
            if (queryFilterCommand.SortDescriptors.Count > 0)
            {
                query = query.OrderBy(string.Join(" ,", queryFilterCommand.SortDescriptors.Select(sort => sort.ToString())));
            }
            else
            {
                if (queryFilterCommand.GroupDescriptors.Count == 0)
                {
                    var propertyName = typeof(TEntity).GetProperties()[0].Name;
                    query = query.OrderBy(propertyName);
                }
                else
                {
                    query = query.OrderBy(queryFilterCommand.GroupDescriptors.FirstOrDefault().Member);
                }
            }

            return query;
        }

        /// <summary>
        /// Senkron ve async yolların ortak kullandığı adım: model dönüşümü ve filtre (Where).
        /// </summary>
        private static IQueryable<TEntity> BuildFilteredQuery<TEntity>(
            IQueryable<TEntity> query,
            QueryFilterModel queryFilterCommand,
            out EntityFilterModel<TEntity> entityFilterModel)
            where TEntity : class
        {
            entityFilterModel = queryFilterCommand.ToEntityFilterModel<TEntity>();

            if (entityFilterModel.Filter != null)
            {
                query = query.Where(entityFilterModel.Filter);
            }

            return query;
        }

        /// <summary>
        /// Gruplamasız yolda sıralamayı uygular (senkron ve async yol aynı kodu kullanır).
        /// </summary>
        private static IQueryable<TEntity> ApplySorting<TEntity>(IQueryable<TEntity> query, QueryFilterModel queryFilterCommand)
            where TEntity : class
            => (IQueryable<TEntity>)SetSorting<TEntity>(queryFilterCommand, query);

        public static IQueryable<TEntity> Query<TEntity>(this IEnumerable<TEntity> entities, QueryFilterModel queryFilterCommand)
            where TEntity : class
        {
            return BuildFilteredQuery(entities.AsQueryable(), queryFilterCommand, out _);
        }

        public static PagedList<TEntity> QueryFilter<TEntity>(this IEnumerable<TEntity> entities, QueryFilterModel queryFilterCommand)
                  where TEntity : class
        {
            var totalCount = 0;
            IEnumerable<TEntity> result;

            var query = BuildFilteredQuery(entities.AsQueryable(), queryFilterCommand, out var entityFilterModel);

            if (queryFilterCommand.GroupDescriptors.Count > 0)
            {
                var arrFields = queryFilterCommand.GroupDescriptors.Select(field => field.Member);
                var selectVisitor = queryFilterCommand.GroupDescriptors.Select(field => field.ToLinq()).ToList();

                if (queryFilterCommand.AggDescriptors.Count > 0)
                {
                    selectVisitor.AddRange(queryFilterCommand.AggDescriptors.Select(agg => agg.ToLinq()));
                }

                var groupResult = query.GroupBy($"new ({string.Join(",", arrFields)})", "it")
                    .Select($"new({string.Join(",", selectVisitor)})");

                groupResult = SetSorting<TEntity>(queryFilterCommand, groupResult);
                totalCount = groupResult.Count();

                var transformToEntity = groupResult.Skip(entityFilterModel.Skip).Take(entityFilterModel.Take) as IEnumerable<object>;

                result = transformToEntity.Select((iitem) => JObject.FromObject(iitem).ToObject<TEntity>()).ToList();
            }
            else
            {
                query = ApplySorting(query, queryFilterCommand);

                totalCount = query.Count();
                result = query.Skip(entityFilterModel.Skip).Take(entityFilterModel.Take).OfType<TEntity>().ToList();
            }

            return new PagedList<TEntity>(result, totalCount);
        }

        public static PagedList<TEntity> QueryFilter<TEntity>(this IEnumerable<TEntity> entities, string queryFilter)
            where TEntity : class => entities.QueryFilter(QueryFilterModel.Parse(queryFilter));

        public static PagedList<TResult> QueryFilter<TSource, TResult>(
            this IEnumerable<TSource> entities,
            Func<TSource, TResult> selector,
            QueryFilterModel queryFilterCommand
            )
            where TResult : class
            where TSource : class
        {
            var result = entities.QueryFilter(queryFilterCommand);
            var convertedResult = result.Items.Select(selector).ToList();
            return new PagedList<TResult>(convertedResult, result.TotalCount);
        }
        /// <summary>
        /// <see cref="QueryFilter{TEntity}(IEnumerable{TEntity}, QueryFilterModel)"/> ile aynı filtre, sıralama,
        /// Skip ve Take'i uygular; sayımı ve sayfa sorgusunu verilen async fonksiyonlarla çalıştırır.
        /// Kütüphane hiçbir ORM'e bağımlı değildir; linq2db / EF Core'un CountAsync ve ToListAsync
        /// metotlarını çağıran fonksiyonları siz verirsiniz.
        /// </summary>
        /// <remarks>
        /// Gruplu sorgularda (<see cref="QueryFilterModel.GroupDescriptors"/> dolu) bu sürümde async çalıştırma
        /// yoktur: dinamik LINQ ve JObject kullanan senkron yol çalışır ve sonuç <see cref="Task.FromResult{TResult}"/>
        /// ile döner. Bu durumda <paramref name="countAsync"/> ve <paramref name="toListAsync"/> çağrılmaz.
        /// <para>
        /// <paramref name="cancellationToken"/> iki fonksiyona da aynen geçirilir; iptal istisnası yutulmaz.
        /// </para>
        /// </remarks>
        /// <param name="query">Kaynak sorgu.</param>
        /// <param name="queryFilterCommand">Filtre modeli.</param>
        /// <param name="countAsync">Sıralanmış ve filtrelenmiş sorgunun toplam kayıt sayısını async döner.</param>
        /// <param name="toListAsync">Sayfa sorgusunu (Skip/Take uygulanmış) async listeye çevirir.</param>
        /// <param name="cancellationToken">İptal belirteci.</param>
        /// <exception cref="ArgumentNullException"><paramref name="countAsync"/> ya da <paramref name="toListAsync"/> null.</exception>
        public static Task<PagedList<TEntity>> QueryFilterAsync<TEntity>(
            this IQueryable<TEntity> query,
            QueryFilterModel queryFilterCommand,
            Func<IQueryable<TEntity>, CancellationToken, Task<int>> countAsync,
            Func<IQueryable<TEntity>, CancellationToken, Task<List<TEntity>>> toListAsync,
            CancellationToken cancellationToken = default)
            where TEntity : class
        {
            if (countAsync == null)
            {
                throw new ArgumentNullException(nameof(countAsync));
            }

            if (toListAsync == null)
            {
                throw new ArgumentNullException(nameof(toListAsync));
            }

            if (queryFilterCommand.GroupDescriptors.Count > 0)
            {
                return Task.FromResult(query.QueryFilter(queryFilterCommand));
            }

            return QueryFilterAsyncCore(query, queryFilterCommand, countAsync, toListAsync, cancellationToken);
        }

        /// <summary>
        /// <see cref="QueryFilterAsync{TEntity}(IQueryable{TEntity}, QueryFilterModel, Func{IQueryable{TEntity}, CancellationToken, Task{int}}, Func{IQueryable{TEntity}, CancellationToken, Task{List{TEntity}}}, CancellationToken)"/>
        /// ile aynıdır; sorgu metnini <see cref="QueryFilterModel.Parse(string)"/> ile çözer.
        /// Gruplu sorgularda senkron yol çalışır (bkz. diğer overload).
        /// </summary>
        public static Task<PagedList<TEntity>> QueryFilterAsync<TEntity>(
            this IQueryable<TEntity> query,
            string queryFilter,
            Func<IQueryable<TEntity>, CancellationToken, Task<int>> countAsync,
            Func<IQueryable<TEntity>, CancellationToken, Task<List<TEntity>>> toListAsync,
            CancellationToken cancellationToken = default)
            where TEntity : class
            => query.QueryFilterAsync(QueryFilterModel.Parse(queryFilter), countAsync, toListAsync, cancellationToken);

        private static async Task<PagedList<TEntity>> QueryFilterAsyncCore<TEntity>(
            IQueryable<TEntity> source,
            QueryFilterModel queryFilterCommand,
            Func<IQueryable<TEntity>, CancellationToken, Task<int>> countAsync,
            Func<IQueryable<TEntity>, CancellationToken, Task<List<TEntity>>> toListAsync,
            CancellationToken cancellationToken)
            where TEntity : class
        {
            var query = BuildFilteredQuery(source, queryFilterCommand, out var entityFilterModel);
            query = ApplySorting(query, queryFilterCommand);

            var totalCount = await countAsync(query, cancellationToken).ConfigureAwait(false);
            var items = await toListAsync(query.Skip(entityFilterModel.Skip).Take(entityFilterModel.Take), cancellationToken).ConfigureAwait(false);

            return new PagedList<TEntity>(items, totalCount);
        }

        public static QueryFilterModel WithJsonbColumns(this QueryFilterModel model, params string[] columns)
        {
            model.JsonbColumns = columns.ToList();
            return model;
        }
    }
}
