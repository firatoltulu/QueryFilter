using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;

namespace QueryFilter
{
    internal static class ConversionExtensions
    {
        #region Object

        public static T Convert<T>(this object value)
        {
            return (T)Convert(value, typeof(T));
        }

        public static T Convert<T>(this object value, CultureInfo culture)
        {
            return (T)Convert(value, typeof(T), culture);
        }

        public static object Convert(this object value, Type to)
        {
            return value.Convert(to, CultureInfo.CurrentCulture);
        }

        public static object Convert(this object value, Type to, CultureInfo culture)
        {
            if (value == null || to.IsInstanceOfType(value))
            {
                return value;
            }

            // array conversion results in four cases, as below
            Array valueAsArray = value as Array;
            if (to.IsArray)
            {
                Type destinationElementType = to.GetElementType();
                if (valueAsArray != null)
                {
                    // case 1: both destination + source type are arrays, so convert each element
                    IList valueAsList = (IList)valueAsArray;
                    IList converted = Array.CreateInstance(destinationElementType, valueAsList.Count);
                    for (int i = 0; i < valueAsList.Count; i++)
                    {
                        converted[i] = valueAsList[i].Convert(destinationElementType, culture);
                    }
                    return converted;
                }
                else
                {
                    // case 2: destination type is array but source is single element, so wrap element in array + convert
                    object element = value.Convert(destinationElementType, culture);
                    IList converted = Array.CreateInstance(destinationElementType, 1);
                    converted[0] = element;
                    return converted;
                }
            }
            else if (valueAsArray != null)
            {
                // case 3: destination type is single element but source is array, so extract first element + convert
                IList valueAsList = (IList)valueAsArray;
                if (valueAsList.Count > 0)
                {
                    value = valueAsList[0];
                }
                // .. fallthrough to case 4
            }
            // case 4: both destination + source type are single elements, so convert

            Type fromType = value.GetType();

            if (to.IsInterface || to.IsGenericTypeDefinition || to.IsAbstract)
                throw new System.Exception(string.Concat("to", "Target type '{0}' is not a value type or a non-abstract class.", to.FullName));

            // use Convert.ChangeType if both types are IConvertible
            if (value is IConvertible && typeof(IConvertible).IsAssignableFrom(to))
            {
                if (to.IsEnum)
                {
                    if (value is string)
                        return Enum.Parse(to, value.ToString(), true);
                    else if (fromType.IsInteger())
                        return Enum.ToObject(to, value);
                }

                return System.Convert.ChangeType(value, to, culture);
            }

            if (value is DateTime && to == typeof(DateTimeOffset))
                return new DateTimeOffset((DateTime)value);

            if (value is string && to == typeof(Guid))
                return new Guid((string)value);

            // see if source or target types have a TypeConverter that converts between the two
            TypeConverter toConverter = TypeDescriptor.GetConverter(fromType);

            if (toConverter != null && toConverter.CanConvertTo(to))
            {
                return toConverter.ConvertTo(null, culture, value, to);
            }

            TypeConverter fromConverter = TypeDescriptor.GetConverter(to);

            if (fromConverter != null && fromConverter.CanConvertFrom(fromType))
            {
                return fromConverter.ConvertFrom(null, culture, value);
            }

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