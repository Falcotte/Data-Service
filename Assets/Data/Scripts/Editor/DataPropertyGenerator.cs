using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace AngryKoala.Data
{
    public static class DataPropertyGenerator
    {
        private const string _generatedFileSuffix = ".Properties.Generated.cs";
        private const string _assetsFolderPrefix = "Assets/";

        private static readonly Dictionary<Type, string> _keywordNames = new()
        {
            { typeof(bool), "bool" },
            { typeof(byte), "byte" },
            { typeof(sbyte), "sbyte" },
            { typeof(char), "char" },
            { typeof(decimal), "decimal" },
            { typeof(double), "double" },
            { typeof(float), "float" },
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(long), "long" },
            { typeof(ulong), "ulong" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(string), "string" },
            { typeof(object), "object" }
        };

        // A line of the generated file that declares a property: "public <type> <Name>".
        private static readonly Regex _declaredPropertyRegex =
            new(@"^[ \t]*public[ \t]+(?!partial\b)\S.*[ \t](\w+)[ \t]*\r?$", RegexOptions.Multiline);

        [MenuItem("Angry Koala/Data/Generate Properties")]
        private static void GenerateProperties()
        {
            int writtenCount = 0;
            int unchangedCount = 0;
            int skippedCount = 0;

            foreach (Type type in TypeCache.GetTypesDerivedFrom<DataContainer>())
            {
                if (type.IsAbstract || type.IsGenericType || type.IsNested)
                {
                    continue;
                }

                string scriptPath = GetScriptPath(type);

                if (string.IsNullOrEmpty(scriptPath) ||
                    !scriptPath.StartsWith(_assetsFolderPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!IsDeclaredPartial(scriptPath, type.Name))
                {
                    skippedCount++;
                    continue;
                }

                string generatedPath = Path.Combine(Path.GetDirectoryName(scriptPath) ?? string.Empty,
                    type.Name + _generatedFileSuffix);
                bool hasGeneratedFile = File.Exists(generatedPath);
                string existingCode = hasGeneratedFile ? File.ReadAllText(generatedPath) : string.Empty;
                string code = GenerateCode(type, GetDeclaredPropertyNames(existingCode));

                if (hasGeneratedFile && AreEqualIgnoringLineEndings(existingCode, code))
                {
                    unchangedCount++;
                    continue;
                }

                File.WriteAllText(generatedPath, code, new UTF8Encoding(false));
                writtenCount++;
            }

            if (writtenCount > 0)
            {
                AssetDatabase.Refresh();
            }

            Debug.Log($"DataPropertyGenerator: {writtenCount} written, {unchangedCount} unchanged, " +
                      $"{skippedCount} skipped because the class is not partial.");
        }

        // Writes a property for every field, hand-written properties included.
        internal static string GenerateCode(Type containerType)
        {
            return GenerateCodeInternal(containerType, null);
        }

        // Skips a field whose property name is a public property of the class that the generated file does not
        // declare, because that property is hand-written.
        internal static string GenerateCode(Type containerType, ISet<string> generatedPropertyNames)
        {
            if (generatedPropertyNames == null)
            {
                throw new ArgumentNullException(nameof(generatedPropertyNames));
            }

            return GenerateCodeInternal(containerType, generatedPropertyNames);
        }

        internal static HashSet<string> GetDeclaredPropertyNames(string generatedCode)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);

            if (string.IsNullOrEmpty(generatedCode))
            {
                return names;
            }

            foreach (Match match in _declaredPropertyRegex.Matches(generatedCode))
            {
                names.Add(match.Groups[1].Value);
            }

            return names;
        }

        internal static bool AreEqualIgnoringLineEndings(string first, string second)
        {
            return string.Equals(NormalizeLineEndings(first), NormalizeLineEndings(second), StringComparison.Ordinal);
        }

        internal static string GetTypeName(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (type.IsArray)
            {
                return GetTypeName(type.GetElementType()) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
            }

            if (_keywordNames.TryGetValue(type, out string keyword))
            {
                return keyword;
            }

            return GetNamedTypeName(type);
        }

        private static string GenerateCodeInternal(Type containerType, ISet<string> generatedPropertyNames)
        {
            if (containerType == null)
            {
                throw new ArgumentNullException(nameof(containerType));
            }

            bool hasNamespace = !string.IsNullOrEmpty(containerType.Namespace);
            string indent = hasNamespace ? "    " : string.Empty;
            string accessModifier = containerType.IsPublic ? "public" : "internal";

            var builder = new StringBuilder();

            AppendLine(builder, "// This file is generated by DataPropertyGenerator.");
            AppendLine(builder, "// Any changes will be overwritten.");
            AppendLine(builder, string.Empty);

            if (hasNamespace)
            {
                AppendLine(builder, $"namespace {containerType.Namespace}");
                AppendLine(builder, "{");
            }

            AppendLine(builder, $"{indent}{accessModifier} partial class {containerType.Name}");
            AppendLine(builder, $"{indent}{{");

            bool isFirstProperty = true;

            HashSet<string> inheritedNames = GetInheritedMemberNames(containerType);
            var usedNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (FieldInfo field in GetPropertyFields(containerType))
            {
                string propertyName = DataContainerFields.GetPropertyName(field.Name);
                string skipReason = GetSkipReason(containerType, field, propertyName, inheritedNames);

                if (skipReason != null)
                {
                    Debug.LogWarning($"DataPropertyGenerator: {containerType.Name}.{field.Name} is skipped, " +
                                     skipReason);
                    continue;
                }

                if (generatedPropertyNames != null && !generatedPropertyNames.Contains(propertyName) &&
                    HasPublicProperty(containerType, propertyName))
                {
                    continue;
                }

                if (!usedNames.Add(propertyName))
                {
                    Debug.LogWarning($"DataPropertyGenerator: {containerType.Name}.{field.Name} is skipped, " +
                                     $"an earlier field already produces the property \"{propertyName}\".");
                    continue;
                }

                if (!isFirstProperty)
                {
                    AppendLine(builder, string.Empty);
                }

                isFirstProperty = false;

                AppendLine(builder, $"{indent}    public {GetTypeName(field.FieldType)} {propertyName}");
                AppendLine(builder, $"{indent}    {{");
                AppendLine(builder, $"{indent}        get => {field.Name};");
                AppendLine(builder, $"{indent}        set => {field.Name} = value;");
                AppendLine(builder, $"{indent}    }}");
            }

            AppendLine(builder, $"{indent}}}");

            if (hasNamespace)
            {
                AppendLine(builder, "}");
            }

            return builder.ToString();
        }

        private static List<FieldInfo> GetPropertyFields(Type containerType)
        {
            const BindingFlags bindingFlags =
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            var fields = new List<FieldInfo>();

            foreach (FieldInfo field in containerType.GetFields(bindingFlags))
            {
                if (!field.IsPrivate || !field.IsDefined(typeof(SerializeField), inherit: false) ||
                    field.IsDefined(typeof(HideInInspector), inherit: false))
                {
                    continue;
                }

                if (field.Name.StartsWith("<", StringComparison.Ordinal) ||
                    field.Name.StartsWith("m_", StringComparison.Ordinal))
                {
                    continue;
                }

                // A public property cannot have a type that is not public.
                if (!field.FieldType.IsVisible)
                {
                    continue;
                }

                fields.Add(field);
            }

            return fields;
        }

        private static string GetSkipReason(Type containerType, FieldInfo field, string propertyName,
            HashSet<string> inheritedNames)
        {
            if (!IsValidIdentifier(propertyName))
            {
                return $"\"{propertyName}\" is not a valid C# identifier.";
            }

            if (string.Equals(propertyName, containerType.Name, StringComparison.Ordinal))
            {
                return $"the property name \"{propertyName}\" is the name of the class.";
            }

            if (string.Equals(propertyName, field.Name, StringComparison.Ordinal))
            {
                return $"the property name \"{propertyName}\" is the name of the field.";
            }

            if (inheritedNames.Contains(propertyName))
            {
                return $"the property name \"{propertyName}\" is already a member of a base class.";
            }

            return null;
        }

        private static bool IsValidIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier) || !(char.IsLetter(identifier[0]) || identifier[0] == '_'))
            {
                return false;
            }

            foreach (char character in identifier)
            {
                if (!char.IsLetterOrDigit(character) && character != '_')
                {
                    return false;
                }
            }

            return true;
        }

        // The names of the public and protected instance members that the class inherits.
        private static HashSet<string> GetInheritedMemberNames(Type containerType)
        {
            const BindingFlags bindingFlags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            var names = new HashSet<string>(StringComparer.Ordinal);

            for (Type baseType = containerType.BaseType; baseType != null; baseType = baseType.BaseType)
            {
                foreach (MemberInfo member in baseType.GetMembers(bindingFlags))
                {
                    if (IsVisibleToDerivedClasses(member))
                    {
                        names.Add(member.Name);
                    }
                }
            }

            return names;
        }

        private static bool IsVisibleToDerivedClasses(MemberInfo member)
        {
            return member switch
            {
                FieldInfo field => field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly,
                MethodBase method => IsAccessibleToDerivedClasses(method),
                PropertyInfo property => Array.Exists(property.GetAccessors(true), IsAccessibleToDerivedClasses),
                EventInfo eventInfo => IsAccessibleToDerivedClasses(eventInfo.GetAddMethod(true)),
                _ => false
            };
        }

        private static bool IsAccessibleToDerivedClasses(MethodBase method)
        {
            return method != null && (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly);
        }

        private static bool HasPublicProperty(Type containerType, string propertyName)
        {
            return containerType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public) != null;
        }

        private static string NormalizeLineEndings(string text)
        {
            return text.Replace("\r\n", "\n");
        }

        private static string GetNamedTypeName(Type type)
        {
            var chain = new List<Type>();

            for (Type current = type; current != null; current = current.DeclaringType)
            {
                chain.Insert(0, current);
            }

            Type[] arguments = type.IsGenericType ? type.GetGenericArguments() : Type.EmptyTypes;

            var builder = new StringBuilder("global::");

            if (!string.IsNullOrEmpty(chain[0].Namespace))
            {
                builder.Append(chain[0].Namespace).Append('.');
            }

            int argumentIndex = 0;

            for (int i = 0; i < chain.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append('.');
                }

                string typeName = chain[i].Name;
                int backtickIndex = typeName.IndexOf('`');

                builder.Append(backtickIndex >= 0 ? typeName.Substring(0, backtickIndex) : typeName);

                // A nested type of a generic class repeats the arguments of the classes around it.
                int ownArgumentCount = chain[i].GetGenericArguments().Length -
                                       (i > 0 ? chain[i - 1].GetGenericArguments().Length : 0);

                if (ownArgumentCount <= 0)
                {
                    continue;
                }

                var argumentNames = new List<string>();

                for (int j = 0; j < ownArgumentCount; j++)
                {
                    argumentNames.Add(GetTypeName(arguments[argumentIndex++]));
                }

                builder.Append('<').Append(string.Join(", ", argumentNames)).Append('>');
            }

            return builder.ToString();
        }

        private static bool IsDeclaredPartial(string scriptPath, string className)
        {
            string scriptText = File.ReadAllText(scriptPath);

            return Regex.IsMatch(scriptText, $@"partial\s+class\s+{Regex.Escape(className)}\b");
        }

        private static string GetScriptPath(Type containerType)
        {
            ScriptableObject instance = ScriptableObject.CreateInstance(containerType);

            if (instance == null)
            {
                return null;
            }

            try
            {
                MonoScript script = MonoScript.FromScriptableObject(instance);

                return script != null ? AssetDatabase.GetAssetPath(script) : null;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void AppendLine(StringBuilder builder, string line)
        {
            builder.Append(line).Append('\n');
        }
    }
}
