namespace QueryFilter
{
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Globalization;

    internal static class ConversionExtensions
    {
        #region Object

        public static T Convert<T>(this object value) => (T)Convert(value, typeof(T));

        public static T Convert<T>(this object value, CultureInfo culture) => (T)Convert(value, typeof(T), culture);

        public static object Convert(this object value, Type to) => value.Convert(to, CultureInfo.CurrentCulture);

        public static object Convert(this object value, Type to, CultureInfo culture)
        {
            if (value == null || to.IsInstanceOfType(value))
            {
                return value;
            }

            // array conversion results in four cases, as below
            var valueAsArray = value as Array;
            if (to.IsArray)
            {
                var destinationElementType = to.GetElementType();
                if (valueAsArray != null)
                {
                    // case 1: both destination + source type are arrays, so convert each element
                    var valueAsList = (IList)valueAsArray;
                    IList converted = Array.CreateInstance(destinationElementType, valueAsList.Count);
                    for (var i = 0; i < valueAsList.Count; i++)
                    {
                        converted[i] = valueAsList[i].Convert(destinationElementType, culture);
                    }
                    return converted;
                }
                else
                {
                    // case 2: destination type is array but source is single element, so wrap element in array + convert
                    var element = value.Convert(destinationElementType, culture);
                    IList converted = Array.CreateInstance(destinationElementType, 1);
                    converted[0] = element;
                    return converted;
                }
            }
            else if (valueAsArray != null)
            {
                // case 3: destination type is single element but source is array, so extract first element + convert
                var valueAsList = (IList)valueAsArray;
                if (valueAsList.Count > 0)
                {
                    value = valueAsList[0];
                }
                // .. fallthrough to case 4
            }
            // case 4: both destination + source type are single elements, so convert

            var fromType = value.GetType();

            if (to.IsInterface || to.IsGenericTypeDefinition || to.IsAbstract)
            {
                throw new System.Exception(string.Concat("to", "Target type '{0}' is not a value type or a non-abstract class.", to.FullName));
            }

            // use Convert.ChangeType if both types are IConvertible
            if (value is IConvertible && typeof(IConvertible).IsAssignableFrom(to))
            {
                if (to.IsEnum)
                {
                    if (value is string)
                    {
                        return Enum.Parse(to, value.ToString(), true);
                    }
                    else if (fromType.IsInteger())
                    {
                        return Enum.ToObject(to, value);
                    }
                }

                return System.Convert.ChangeType(value, to, culture);
            }

            if (value is DateTime time && to == typeof(DateTimeOffset))
            {
                return new DateTimeOffset(time);
            }

            if (value is string v && to == typeof(Guid))
            {
                return new Guid(v);
            }

            // see if source or target types have a TypeConverter that converts between the two
            var toConverter = TypeDescriptor.GetConverter(fromType);

            if (toConverter?.CanConvertTo(to) == true)
            {
                return toConverter.ConvertTo(null, culture, value, to);
            }

            var fromConverter = TypeDescriptor.GetConverter(to);
            if (fromConverter?.CanConvertFrom(fromType) == true)
            {
                return fromConverter.ConvertFrom(null, culture, value);
            }

            //Modified for Nullable fields
            //https://stackoverflow.com/questions/42768023/getting-error-the-binary-operator-equal-is-not-defined-for-the-types-system-g

            return toConverter.ConvertFromInvariantString(null, value.ToString()); // 3

            throw new System.Exception("invalid convert types");
        }

        public static bool IsInteger(this Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    return true;

                default:
                    return false;
            }
        }

        #endregion Object
    }
}
