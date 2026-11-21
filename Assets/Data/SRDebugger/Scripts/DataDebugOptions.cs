using System;
using System.Collections.Generic;
using System.Reflection;
using AngryKoala.Services;
using SRDebugger;
using SRF.Helpers;
using UnityEngine;

namespace AngryKoala.Data
{
    /// <summary>
    /// Shows the fields ticked "Add to SROptions" on the SRDebugger options page, for as long as a data service
    /// is registered. Compiled only when the project defines SRDEBUGGER.
    /// </summary>
    internal static class DataDebugOptions
    {
        private static DynamicOptionContainer _optionContainer;
        private static IDataService _dataService;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _optionContainer = null;
            _dataService = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            ServiceLocator.OnServiceRegistered += OnServiceRegistered;
            ServiceLocator.OnServiceDeregistered += OnServiceDeregistered;

            if (ServiceLocator.TryGet(out IDataService dataService))
            {
                AddOptions(dataService);
            }
        }

        private static void OnServiceRegistered(Type type, IService service)
        {
            if (service is IDataService dataService)
            {
                AddOptions(dataService);
            }
        }

        private static void OnServiceDeregistered(Type type, IService service)
        {
            if (ReferenceEquals(service, _dataService))
            {
                RemoveOptions();
            }
        }

        private static void AddOptions(IDataService dataService)
        {
            RemoveOptions();

            var containers = new List<DataContainer>();
            dataService.GetContainers(containers);

            var optionContainer = new DynamicOptionContainer();
            BuildOptions(containers, optionContainer);

            // SRDebugger is left alone when no field is ticked, so the module does not start it by itself.
            if (optionContainer.Options.Count == 0)
            {
                return;
            }

            _dataService = dataService;
            _optionContainer = optionContainer;

            SRDebug.Instance.AddOptionContainer(_optionContainer);
        }

        private static void RemoveOptions()
        {
            if (_optionContainer != null && SRDebug.IsInitialized)
            {
                SRDebug.Instance.RemoveOptionContainer(_optionContainer);
            }

            _optionContainer = null;
            _dataService = null;
        }

        /// <summary>
        /// Adds one option per enabled entry whose field still exists on its container.
        /// </summary>
        internal static void BuildOptions(IReadOnlyList<DataContainer> containers,
            DynamicOptionContainer optionContainer)
        {
            if (containers == null)
            {
                throw new ArgumentNullException(nameof(containers));
            }

            if (optionContainer == null)
            {
                throw new ArgumentNullException(nameof(optionContainer));
            }

            var fields = new List<FieldInfo>();

            foreach (DataContainer container in containers)
            {
                if (container == null)
                {
                    continue;
                }

                DataContainerFields.GetOptionFields(container.GetType(), fields);

                foreach (SRDataOptionEntry entry in container.SRDataOptions)
                {
                    if (entry?.Settings == null || !entry.Settings.Enabled)
                    {
                        continue;
                    }

                    FieldInfo field = FindField(fields, entry.PropertyName);

                    if (field != null)
                    {
                        optionContainer.AddOption(CreateOption(container, field, entry));
                    }
                }
            }
        }

        private static FieldInfo FindField(List<FieldInfo> fields, string propertyName)
        {
            foreach (FieldInfo field in fields)
            {
                if (string.Equals(DataContainerFields.GetPropertyName(field.Name), propertyName,
                        StringComparison.Ordinal))
                {
                    return field;
                }
            }

            return null;
        }

        private static OptionDefinition CreateOption(DataContainer container, FieldInfo field,
            SRDataOptionEntry entry)
        {
            SRDataOptionSettings settings = entry.Settings;
            Type fieldType = field.FieldType;

            var attributes = new List<Attribute>();

            if (DataContainerFields.IsNumericType(fieldType))
            {
                SRDataOptionRange numberRange = settings.NumberRange;

                if (numberRange != null && numberRange.Enabled)
                {
                    attributes.Add(new NumberRangeAttribute(numberRange.Min, numberRange.Max));
                }

                bool hasIncrement = Math.Abs(settings.Increment) > double.Epsilon &&
                                    Math.Abs(settings.Increment - 1d) > double.Epsilon;

                if (hasIncrement)
                {
                    attributes.Add(new IncrementAttribute(settings.Increment));
                }
            }

            var property = new PropertyReference(
                fieldType,
                getter: () => field.GetValue(container),
                setter: value => field.SetValue(container, Convert(value, fieldType)),
                attributes.ToArray());

            string name = string.IsNullOrEmpty(settings.DisplayName) ? entry.PropertyName : settings.DisplayName;
            string category = string.IsNullOrEmpty(settings.Category) ? container.GetType().Name : settings.Category;

            return new OptionDefinition(name, category, settings.Sort, property);
        }

        private static object Convert(object value, Type fieldType)
        {
            if (value == null || fieldType.IsInstanceOfType(value))
            {
                return value;
            }

            if (fieldType.IsEnum)
            {
                return Enum.ToObject(fieldType, value);
            }

            return System.Convert.ChangeType(value, fieldType);
        }
    }
}
