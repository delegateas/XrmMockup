using Microsoft.Xrm.Sdk;
using System;

namespace DG.Tools.XrmMockup
{
    internal static class ValueConverter
    {
        public static object ConvertToComparableObject(object obj)
        {
            if (obj is EntityReference entityReference)
                return entityReference.Id;

            else if (obj is Money money)
                return money.Value;

            else if (obj is AliasedValue aliasedValue)
                return ConvertToComparableObject(aliasedValue.Value);

            else if (obj is OptionSetValue optionSetValue)
                return optionSetValue.Value;

            else if (obj != null && obj.GetType().IsEnum)
                return (int)obj;

            else
                return obj;
        }


        public static object ConvertTo(object value, Type targetType)
        {
            // If the value, or target type, are null, nothing to convert, return the value
            if (targetType is null || value is null)
            {
                return value;
            }

            var valueType = value.GetType();
            if (valueType == targetType || (Nullable.GetUnderlyingType(targetType) != null && valueType == Nullable.GetUnderlyingType(targetType)))
            {
                // If the types match, just return the object
                return value;
            }

            // We might be trying to convert a string 0, or 1 to a bool
            if ((targetType == typeof(bool) || targetType == typeof(bool?)) && (value is string str && decimal.TryParse(str, out var numericValue)))
            {
                return numericValue != 0;
            }

            // Can we convert from the value's type converter to the target type?
            var valueConverter = System.ComponentModel.TypeDescriptor.GetConverter(valueType);
            if (valueConverter.CanConvertTo(targetType))
            {
                return valueConverter.ConvertTo(value, targetType);
            }

            // Can we convert to the target's type using the target type converter?
            var targetConverter = System.ComponentModel.TypeDescriptor.GetConverter(targetType);
            if (targetConverter.CanConvertFrom(valueType))
            {
                return targetConverter.ConvertFrom(value);
            }

            // Fallback to Convert.ChangeType which handles most IConvertible types
            return Convert.ChangeType(value, targetType);
        }
    }
}