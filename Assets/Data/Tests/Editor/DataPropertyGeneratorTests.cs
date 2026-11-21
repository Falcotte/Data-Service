using System.Collections.Generic;
using AngryKoala.Data.Tests.Sample;
using NUnit.Framework;
using UnityEngine;

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
    }
}
