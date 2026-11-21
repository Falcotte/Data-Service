using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace AngryKoala.Data
{
    // Finds the fields of a container class that can be shown as debug options, and names them.
    internal static class DataContainerFields
    {
        private static readonly HashSet<Type> _optionTypes = new()
        {
            typeof(bool),
            typeof(int),
            typeof(uint),
            typeof(short),
            typeof(ushort),
            typeof(byte),
            typeof(sbyte),
            typeof(float),
            typeof(double),
            typeof(string)
        };

        // The private serialized fields of the class and its base classes up to DataContainer, base class first,
        // in the order they are written.
        public static void GetOptionFields(Type containerType, List<FieldInfo> results)
        {
            if (containerType == null)
            {
                throw new ArgumentNullException(nameof(containerType));
            }

            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();

            var types = new List<Type>();

            for (Type type = containerType; type != null && type != typeof(DataContainer); type = type.BaseType)
            {
                types.Insert(0, type);
            }

            const BindingFlags bindingFlags =
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            foreach (Type type in types)
            {
                foreach (FieldInfo field in type.GetFields(bindingFlags))
                {
                    if (IsOptionField(field))
                    {
                        results.Add(field);
                    }
                }
            }
        }

        public static bool IsOptionType(Type type) => _optionTypes.Contains(type) || type.IsEnum;

        public static bool IsNumericType(Type type) =>
            type != typeof(bool) && type != typeof(string) && !type.IsEnum && _optionTypes.Contains(type);

        public static string GetPropertyName(string fieldName)
        {
            string cleanedFieldName = fieldName.TrimStart('_');

            if (string.IsNullOrEmpty(cleanedFieldName))
            {
                return fieldName;
            }

            return char.ToUpperInvariant(cleanedFieldName[0]) + cleanedFieldName.Substring(1);
        }

        private static bool IsOptionField(FieldInfo field)
        {
            if (!field.IsPrivate || field.GetCustomAttribute<SerializeField>() == null)
            {
                return false;
            }

            if (field.GetCustomAttribute<HideInInspector>() != null)
            {
                return false;
            }

            return IsOptionType(field.FieldType);
        }
    }
}
