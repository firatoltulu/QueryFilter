using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace QueryFilter.Helper
{
    public static class MapperUtility
    {
        /*passing values to given object*/
        public static TTarget MapTo<TSource, TTarget>(this TSource aSource, TTarget aTarget)
        {
            const BindingFlags flags = BindingFlags.Public |
                                     BindingFlags.Instance | BindingFlags.NonPublic;

            /*TODO: find fields*/
            var srcFields = (from PropertyInfo aProp in typeof(TSource).GetProperties(flags)
                             where aProp.CanRead     //check if prop is readable
                             select new
                             {
                                 Name = aProp.Name,
                                 Type = Nullable.GetUnderlyingType(aProp.PropertyType) ??
                                                                        aProp.PropertyType
                             }).ToList();
            var trgFields = (from PropertyInfo aProp in aTarget.GetType().GetProperties(flags)
                             where aProp.CanWrite   //check if prop is writeable
                             select new
                             {
                                 Name = aProp.Name,
                                 Type = Nullable.GetUnderlyingType(aProp.PropertyType) ??
                                                                     aProp.PropertyType
                             }).ToList();

            /*TODO: common fields where name and type same*/
            var commonFields = srcFields.Intersect(trgFields).ToList();

            /*assign values*/
            foreach (var aField in commonFields)
            {
                var value = aSource.GetType().GetProperty(aField.Name).GetValue(aSource, null);
                PropertyInfo propertyInfos = aTarget.GetType().GetProperty(aField.Name);
                propertyInfos.SetValue(aTarget, value, null);
            }
            return aTarget;
        }

        /*returns new object with mapping*/
        public static TTarget CreateMapped<TSource, TTarget>(this TSource aSource) where TTarget : new()
        {
            return aSource.MapTo(new TTarget());
        }
    }
}
