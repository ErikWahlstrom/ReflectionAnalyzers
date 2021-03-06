namespace ReflectionAnalyzers.Tests.Helpers.Reflection
{
    using System.Linq;
    using NUnit.Framework;

    public class GenericTypeNameTests
    {
        [TestCase("System.Int32")]
        [TestCase("System.Nullable`1")]
        [TestCase("System.Nullable`1[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]")]
        [TestCase("System.Nullable`1 [System.Int32]")]
        [TestCase("System.Collections.Generic.KeyValuePair`2 [System.Int32,System.String]")]
        public void TryGetGenericWhenFalse(string name)
        {
            Assert.AreEqual(false, GenericTypeName.TryParse(name, out _));
        }

        [TestCase("System.Nullable`1[System.Int32]", "System.Int32")]
        [TestCase("System.Nullable`1[[System.Int32]]", "System.Int32")]
        [TestCase("System.Nullable`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]", "System.Int32")]
        public void TryGetGenericWhenNullable(string name, string arg)
        {
            Assert.AreEqual(true, GenericTypeName.TryParse(name, out var generic));
            Assert.AreEqual("System.Nullable`1", generic.MetadataName);
            var typeArgument = generic.TypeArguments.Single();
            Assert.AreEqual(arg, typeArgument.MetadataName);
            Assert.AreEqual(null, typeArgument.TypeArguments);
        }

        [TestCase("System.Collections.Generic.KeyValuePair`2[System.Int32,System.String]", "System.Int32", "System.String")]
        [TestCase("System.Collections.Generic.KeyValuePair`2[System.Int32, System.String]", "System.Int32", "System.String")]
        [TestCase("System.Collections.Generic.KeyValuePair`2[System.Int32,  System.String]", "System.Int32", "System.String")]
        [TestCase("System.Collections.Generic.KeyValuePair`2[ System.Int32,System.String]", "System.Int32", "System.String")]
        [TestCase("System.Collections.Generic.KeyValuePair`2[ System.Int32, System.String]", "System.Int32", "System.String")]
        [TestCase("System.Collections.Generic.KeyValuePair`2[System.Int32,System.String ]", "System.Int32", "System.String ")]
        [TestCase("System.Collections.Generic.KeyValuePair`2[System.Int32 ,System.String]", "System.Int32 ", "System.String")]
        [TestCase("System.Collections.Generic.KeyValuePair`2[ System.Int32, System.String ]", "System.Int32", "System.String ")]
        [TestCase("System.Collections.Generic.KeyValuePair`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]", "System.Int32", "System.String")]
        public void TryGetGenericWhenKeyValuePair(string name, string arg0, string arg1)
        {
            Assert.AreEqual(true, GenericTypeName.TryParse(name, out var generic));
            Assert.AreEqual("System.Collections.Generic.KeyValuePair`2", generic.MetadataName);
            var typeArguments = generic.TypeArguments;
            Assert.AreEqual(2, typeArguments.Length);
            Assert.AreEqual(arg0, typeArguments[0].MetadataName);
            Assert.AreEqual(null, typeArguments[0].TypeArguments);
            Assert.AreEqual(arg1, typeArguments[1].MetadataName);
            Assert.AreEqual(null, typeArguments[1].TypeArguments);
        }

        [TestCase("System.Nullable`1[System.Collections.Generic.KeyValuePair`2[System.Int32,System.String]]", "System.Int32", "System.String")]
        [TestCase("System.Nullable`1[[System.Collections.Generic.KeyValuePair`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]", "System.Int32", "System.String")]
        public void TryGetGenericWhenNullableKeyValuePair(string name, string arg0, string arg1)
        {
            Assert.AreEqual(true, GenericTypeName.TryParse(name, out var generic));
            Assert.AreEqual("System.Nullable`1", generic.MetadataName);
            var typeArgument = generic.TypeArguments.Single();
            Assert.AreEqual("System.Collections.Generic.KeyValuePair`2", typeArgument.MetadataName);
            var genericArguments = typeArgument.TypeArguments;
            Assert.AreEqual(2, genericArguments.Count);
            Assert.AreEqual(arg0, genericArguments[0].MetadataName);
            Assert.AreEqual(null, genericArguments[0].TypeArguments);
            Assert.AreEqual(arg1, genericArguments[1].MetadataName);
            Assert.AreEqual(null, genericArguments[1].TypeArguments);
        }
    }
}
