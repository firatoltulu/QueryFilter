using System;
using System.Linq.Expressions;

namespace QueryFilter
{
    public class EntityFilterModel<TEntity>
    {
        public Expression<Func<TEntity, bool>> Filter { get; set; }

        public string Sorts { get; set; }

        public int Take { get; set; }

        public int Skip { get; set; }
    }
}