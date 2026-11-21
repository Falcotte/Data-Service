using System.Collections.Generic;
using System.Text.RegularExpressions;
using AngryKoala.Data.Tests.Sample;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AngryKoala.Data.Tests
{
    public sealed class DataPropertyGeneratorTests
    {
        [Test]
        public void GetTypeName_ForInt_ReturnsKeyword()
        {
            Assert.AreEqual("int", DataPropertyGenerator.GetTypeName(typeof(int)));
        }

        [Test]
        public void GetTypeName_ForString_ReturnsKeyword()
        {
            Assert.AreEqual("string", DataPropertyGenerator.GetTypeName(typeof(string)));
        }

        [Test]
        public void GetTypeName_ForEnumInOtherNamespace_ReturnsGlobalName()
        {
            Assert.AreEqual("global::AngryKoala.Data.Tests.Sample.TestGeneratedMode",
                DataPropertyGenerator.GetTypeName(typeof(TestGeneratedMode)));
        }

        [Test]
        public void GetTypeName_ForNestedType_SeparatesWithDot()
        {
            Assert.AreEqual("global::AngryKoala.Data.Tests.TestGeneratedData.Flavor",
                DataPropertyGenerator.GetTypeName(typeof(TestGeneratedData.Flavor)));
        }

        [Test]
        public void GetTypeName_ForArray_AppendsBrackets()
        {
            Assert.AreEqual("global::UnityEngine.Vector3[]", DataPropertyGenerator.GetTypeName(typeof(Vector3[])));
        }

        [Test]
        public void GetTypeName_ForListOfInt_WritesGenericArgument()
        {
            Assert.AreEqual("global::System.Collections.Generic.List<int>",
                DataPropertyGenerator.GetTypeName(typeof(List<int>)));
        }

        [Test]
        public void GetTypeName_ForNestedGenerics_FormatsArgumentsRecursively()
        {
            Assert.AreEqual(
                "global::System.Collections.Generic.Dictionary<string, " +
                "global::System.Collections.Generic.List<global::UnityEngine.Vector3>>",
                DataPropertyGenerator.GetTypeName(typeof(Dictionary<string, List<Vector3>>)));
        }

        [Test]
        public void GenerateCode_ForContainer_WritesNamespaceAndPartialClass()
        {
            string code = DataPropertyGenerator.GenerateCode(typeof(TestGeneratedData));

            StringAssert.Contains("namespace AngryKoala.Data.Tests", code);
            StringAssert.Contains("public partial class TestGeneratedData", code);
        }

        [Test]
        public void GenerateCode_ForContainer_WritesOnePropertyPerVisibleField()
        {
            string code = DataPropertyGenerator.GenerateCode(typeof(TestGeneratedData));

            StringAssert.Contains("public int Level", code);
            StringAssert.Contains("get => _level;", code);
            StringAssert.Contains("set => _level = value;", code);
            StringAssert.Contains("public global::System.Collections.Generic.List<string> Names", code);
            StringAssert.Contains("public global::AngryKoala.Data.Tests.Sample.TestGeneratedMode Mode", code);
        }

        [Test]
        public void GenerateCode_ForHiddenField_WritesNoProperty()
        {
            string code = DataPropertyGenerator.GenerateCode(typeof(TestGeneratedData));

            StringAssert.DoesNotContain("HiddenValue", code);
            StringAssert.DoesNotContain("_hiddenValue", code);
        }

        [Test]
        public void GenerateCode_WithHandWrittenPropertyAndNoGeneratedFile_SkipsTheField()
        {
            string code = DataPropertyGenerator.GenerateCode(typeof(TestHandWrittenData), new HashSet<string>());

            StringAssert.DoesNotContain("public int Speed", code);
            StringAssert.Contains("public int Level", code);
        }

        [Test]
        public void GenerateCode_WithHandWrittenPropertyAndGeneratedFile_StillSkipsTheField()
        {
            string code = DataPropertyGenerator.GenerateCode(typeof(TestHandWrittenData),
                new HashSet<string> { "Level" });

            StringAssert.DoesNotContain("public int Speed", code);
            StringAssert.Contains("public int Level", code);
        }

        [Test]
        public void GenerateCode_WithPropertyDeclaredByGeneratedFile_GeneratesItAgain()
        {
            string code = DataPropertyGenerator.GenerateCode(typeof(TestHandWrittenData),
                new HashSet<string> { "Speed", "Level" });

            StringAssert.Contains("public int Speed", code);
            StringAssert.Contains("public int Level", code);
        }

        [Test]
        public void GenerateCode_WithoutNames_GeneratesEveryField()
        {
            string code = DataPropertyGenerator.GenerateCode(typeof(TestHandWrittenData));

            StringAssert.Contains("public int Speed", code);
            StringAssert.Contains("public int Level", code);
        }

        [Test]
        public void GenerateCode_WhenPropertyNameIsTheClassName_SkipsTheFieldWithWarning()
        {
            LogAssert.Expect(LogType.Warning, new Regex("TestClassNameClashData[.]_testClassNameClashData"));

            string code = DataPropertyGenerator.GenerateCode(typeof(TestClassNameClashData));

            StringAssert.DoesNotContain("get => _testClassNameClashData;", code);
            StringAssert.Contains("public int Other", code);
        }

        [Test]
        public void GenerateCode_WhenTwoFieldsMapToOneProperty_KeepsTheFirstWithWarning()
        {
            LogAssert.Expect(LogType.Warning, new Regex("TestDuplicateMappingData[.]value"));

            string code = DataPropertyGenerator.GenerateCode(typeof(TestDuplicateMappingData));

            StringAssert.Contains("get => _value;", code);
            StringAssert.DoesNotContain("get => value;", code);
            StringAssert.Contains("public int Other", code);
        }

        [Test]
        public void GenerateCode_WhenPropertyNameIsNotAnIdentifier_SkipsTheFieldWithWarning()
        {
            LogAssert.Expect(LogType.Warning, new Regex("TestInvalidIdentifierData[.]_2nd"));

            string code = DataPropertyGenerator.GenerateCode(typeof(TestInvalidIdentifierData));

            StringAssert.DoesNotContain("2nd", code);
            StringAssert.Contains("public int Other", code);
        }

        [Test]
        public void GenerateCode_WhenPropertyNameIsInherited_SkipsTheFieldWithWarning()
        {
            LogAssert.Expect(LogType.Warning, new Regex("TestInheritedClashData[.]_version"));

            string code = DataPropertyGenerator.GenerateCode(typeof(TestInheritedClashData));

            StringAssert.DoesNotContain("get => _version;", code);
            StringAssert.Contains("public string Name", code);
        }

        [Test]
        public void GetDeclaredPropertyNames_ForGeneratedCode_ReturnsThePropertyNames()
        {
            string code = DataPropertyGenerator.GenerateCode(typeof(TestGeneratedData));

            HashSet<string> names = DataPropertyGenerator.GetDeclaredPropertyNames(code);

            CollectionAssert.AreEquivalent(new[] { "Level", "Names", "Mode" }, names);
        }

        [Test]
        public void GetDeclaredPropertyNames_ForCodeWithWindowsLineEndings_ReturnsThePropertyNames()
        {
            string code = DataPropertyGenerator.GenerateCode(typeof(TestGeneratedData)).Replace("\n", "\r\n");

            HashSet<string> names = DataPropertyGenerator.GetDeclaredPropertyNames(code);

            CollectionAssert.AreEquivalent(new[] { "Level", "Names", "Mode" }, names);
        }

        [Test]
        public void AreEqualIgnoringLineEndings_ForSameTextWithOtherLineEndings_ReturnsTrue()
        {
            Assert.IsTrue(DataPropertyGenerator.AreEqualIgnoringLineEndings("a\nb\n", "a\r\nb\r\n"));
        }

        [Test]
        public void AreEqualIgnoringLineEndings_ForDifferentText_ReturnsFalse()
        {
            Assert.IsFalse(DataPropertyGenerator.AreEqualIgnoringLineEndings("a\nb\n", "a\r\nc\r\n"));
        }
    }
}
