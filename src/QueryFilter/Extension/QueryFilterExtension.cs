using System;
using System.Collections.Generic;
using System.Linq;

namespace QueryFilter
{
    using Newtonsoft.Json.Linq;
    using QueryFilter;
    using System.Linq.Dynamic.Core;

    public static class QueryFilterExtension
    {
        private static IQueryable setSorting<TEntity>(QueryFilterModel queryFilterCommand, IQueryable query)
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
                    query = query.OrderBy(queryFilterCommand.GroupDescriptors.FirstOrDefault().Member);
            }

            return query;
        }

        public static PagedList<TEntity>QueryFilter<TEntity>(this IEnumerable<TEntity> entities, QueryFilterModel queryFilterCommand)
            where TEntity : class
        {
            int totalCount = 0;
            IEnumerable<TEntity> result;

            var entityFilterModel = queryFilterCommand.ToEntityFilterModel<TEntity>();
            var query = entities.AsQueryable<TEntity>();

            if (entityFilterModel.Filter != null)
                query = query.Where<TEntity>(entityFilterModel.Filter);

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

                groupResult = setSorting<TEntity>(queryFilterCommand, groupResult);
                totalCount = groupResult.Count();

                var transformToEntity = groupResult.Skip(entityFilterModel.Skip).Take(entityFilterModel.Take) as IEnumerable<object>;

                result = transformToEntity.Select((iitem) =>
                {
                    var obj = Activator.CreateInstance<TEntity>();

                    //maybe another 
                    return JObject.FromObject(iitem).ToObject< TEntity>();
                }).ToList();
            }
            else
            {
                query = (IQueryable<TEntity>)setSorting<TEntity>(queryFilterCommand, query);

                totalCount = query.Count();
                result = query.Skip(entityFilterModel.Skip).Take(entityFilterModel.Take).OfType<TEntity>().ToList();
            }

            return new PagedList<TEntity>(result, totalCount);
        }

        public static IQueryable<TEntity> Query<TEntity>(this IEnumerable<TEntity> entities, QueryFilterModel queryFilterCommand)
            where TEntity : class
        {
            var entityFilterModel = queryFilterCommand.ToEntityFilterModel<TEntity>();
            var query = entities.AsQueryable();

            if (entityFilterModel.Filter != null)
                query = query.Where<TEntity>(entityFilterModel.Filter);

            return query;
        }

        public static PagedList<TEntity> QueryFilter<TEntity>(this IEnumerable<TEntity> entities, string queryFilter)
            where TEntity : class
        {
            return entities.QueryFilter(QueryFilterModel.Parse(queryFilter));
        }


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
    }
}