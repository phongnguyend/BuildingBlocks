using Serializer = MiniJsonSerializer.MiniJsonSerializer;

namespace MiniJsonSerializer.Tests;

public class DeserializeTests
{
    [Fact]
    public void Deserialize_Null_ReturnsNull()
    {
        var result = Serializer.Deserialize("null");
        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_True_ReturnsTrue()
    {
        var result = Serializer.Deserialize("true");
        Assert.Equal(true, result);
    }

    [Fact]
    public void Deserialize_False_ReturnsFalse()
    {
        var result = Serializer.Deserialize("false");
        Assert.Equal(false, result);
    }

    [Fact]
    public void Deserialize_String_ReturnsString()
    {
        var result = Serializer.Deserialize("\"hello\"");
        Assert.Equal("hello", result);
    }

    [Fact]
    public void Deserialize_EmptyString_ReturnsEmptyString()
    {
        var result = Serializer.Deserialize("\"\"");
        Assert.Equal("", result);
    }

    [Fact]
    public void Deserialize_StringWithEscapes_UnescapesCorrectly()
    {
        var result = Serializer.Deserialize("\"line1\\nline2\\r\\ttab\\\\slash\\\"quote\"");
        Assert.Equal("line1\nline2\r\ttab\\slash\"quote", result);
    }

    [Fact]
    public void Deserialize_Int_ReturnsInt()
    {
        var result = Serializer.Deserialize("42");
        Assert.IsType<int>(result);
        Assert.Equal(42, result);
    }

    [Fact]
    public void Deserialize_NegativeInt_ReturnsNegativeInt()
    {
        var result = Serializer.Deserialize("-7");
        Assert.IsType<int>(result);
        Assert.Equal(-7, result);
    }

    [Fact]
    public void Deserialize_Zero_ReturnsZero()
    {
        var result = Serializer.Deserialize("0");
        Assert.IsType<int>(result);
        Assert.Equal(0, result);
    }

    [Fact]
    public void Deserialize_Long_ReturnsLong()
    {
        var result = Serializer.Deserialize("9876543210");
        Assert.IsType<long>(result);
        Assert.Equal(9876543210L, result);
    }

    [Fact]
    public void Deserialize_Double_ReturnsDouble()
    {
        var result = Serializer.Deserialize("3.14");
        Assert.IsType<double>(result);
        Assert.Equal(3.14, result);
    }

    [Fact]
    public void Deserialize_ScientificNotation_ReturnsDouble()
    {
        var result = Serializer.Deserialize("1.5e2");
        Assert.IsType<double>(result);
        Assert.Equal(150.0, result);
    }

    [Fact]
    public void Deserialize_EmptyArray_ReturnsEmptyList()
    {
        var result = Serializer.Deserialize("[]");
        var list = Assert.IsType<List<object?>>(result);
        Assert.Empty(list);
    }

    [Fact]
    public void Deserialize_IntArray_ReturnsList()
    {
        var result = Serializer.Deserialize("[1,2,3]");
        var list = Assert.IsType<List<object?>>(result);
        Assert.Equal(3, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(2, list[1]);
        Assert.Equal(3, list[2]);
    }

    [Fact]
    public void Deserialize_StringArray_ReturnsList()
    {
        var result = Serializer.Deserialize("[\"a\",\"b\",\"c\"]");
        var list = Assert.IsType<List<object?>>(result);
        Assert.Equal(["a", "b", "c"], list);
    }

    [Fact]
    public void Deserialize_MixedArray_ReturnsList()
    {
        var result = Serializer.Deserialize("[1,\"two\",true,null]");
        var list = Assert.IsType<List<object?>>(result);
        Assert.Equal(4, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal("two", list[1]);
        Assert.Equal(true, list[2]);
        Assert.Null(list[3]);
    }

    [Fact]
    public void Deserialize_EmptyObject_ReturnsEmptyDictionary()
    {
        var result = Serializer.Deserialize("{}");
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Empty(dict);
    }

    [Fact]
    public void Deserialize_Object_ReturnsDictionary()
    {
        var result = Serializer.Deserialize("{\"name\":\"Alice\",\"age\":30}");
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal("Alice", dict["name"]);
        Assert.Equal(30, dict["age"]);
    }

    [Fact]
    public void Deserialize_ObjectWithNullValue_ReturnsDictionaryWithNull()
    {
        var result = Serializer.Deserialize("{\"key\":null}");
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Null(dict["key"]);
    }

    [Fact]
    public void Deserialize_NestedObject_ReturnsDictionaryWithNestedDictionary()
    {
        var result = Serializer.Deserialize("{\"outer\":{\"inner\":42}}");
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        var inner = Assert.IsType<Dictionary<string, object?>>(dict["outer"]);
        Assert.Equal(42, inner["inner"]);
    }

    [Fact]
    public void Deserialize_DeeplyNested_ParsesCorrectly()
    {
        var result = Serializer.Deserialize("{\"level1\":{\"level2\":{\"level3\":\"deep\"}}}");
        var l1 = Assert.IsType<Dictionary<string, object?>>(result);
        var l2 = Assert.IsType<Dictionary<string, object?>>(l1["level1"]);
        var l3 = Assert.IsType<Dictionary<string, object?>>(l2["level2"]);
        Assert.Equal("deep", l3["level3"]);
    }

    [Fact]
    public void Deserialize_ArrayOfObjects_ParsesCorrectly()
    {
        var result = Serializer.Deserialize("[{\"Id\":1,\"Name\":\"A\"},{\"Id\":2,\"Name\":\"B\"}]");
        var list = Assert.IsType<List<object?>>(result);
        Assert.Equal(2, list.Count);
        var first = Assert.IsType<Dictionary<string, object?>>(list[0]);
        Assert.Equal(1, first["Id"]);
        Assert.Equal("A", first["Name"]);
    }

    [Fact]
    public void Deserialize_WithWhitespace_ParsesCorrectly()
    {
        var json = "{\r\n  \"name\": \"Alice\",\r\n  \"age\": 30\r\n}";
        var result = Serializer.Deserialize(json);
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal("Alice", dict["name"]);
        Assert.Equal(30, dict["age"]);
    }

    [Fact]
    public void Deserialize_UnicodeEscape_ParsesCorrectly()
    {
        var result = Serializer.Deserialize("\"\\u0048\\u0065\\u006C\\u006C\\u006F\"");
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void Deserialize_InvalidJson_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => Serializer.Deserialize("{invalid}"));
    }

    [Fact]
    public void Deserialize_TrailingContent_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => Serializer.Deserialize("42 extra"));
    }

    [Fact]
    public void Deserialize_UnterminatedString_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => Serializer.Deserialize("\"unterminated"));
    }

    [Fact]
    public void Deserialize_Generic_ToInt_ReturnsTypedValue()
    {
        var result = Serializer.Deserialize<int>("42");
        Assert.Equal(42, result);
    }

    [Fact]
    public void Deserialize_Generic_ToString_ReturnsTypedValue()
    {
        var result = Serializer.Deserialize<string>("\"hello\"");
        Assert.Equal("hello", result);
    }

    [Fact]
    public void Deserialize_Generic_ToBool_ReturnsTypedValue()
    {
        var result = Serializer.Deserialize<bool>("true");
        Assert.True(result);
    }

    [Fact]
    public void Deserialize_Generic_ToDouble_ReturnsTypedValue()
    {
        var result = Serializer.Deserialize<double>("3.14");
        Assert.Equal(3.14, result);
    }

    [Fact]
    public void Deserialize_Generic_ToIntArray_ReturnsTypedArray()
    {
        var result = Serializer.Deserialize<int[]>("[1,2,3]");
        Assert.NotNull(result);
        Assert.Equal([1, 2, 3], result);
    }

    [Fact]
    public void Deserialize_Generic_ToStringArray_ReturnsTypedArray()
    {
        var result = Serializer.Deserialize<string[]>("[\"a\",\"b\"]");
        Assert.NotNull(result);
        Assert.Equal(["a", "b"], result);
    }

    [Fact]
    public void Deserialize_Generic_ToListOfInt_ReturnsTypedList()
    {
        var result = Serializer.Deserialize<List<int>>("[1,2,3]");
        Assert.NotNull(result);
        Assert.Equal([1, 2, 3], result);
    }

    [Fact]
    public void Deserialize_Generic_ToClass_SetsProperties()
    {
        var result = Serializer.Deserialize<Person>("{\"Name\":\"Bob\",\"Age\":25}");
        Assert.NotNull(result);
        Assert.Equal("Bob", result.Name);
        Assert.Equal(25, result.Age);
    }

    [Fact]
    public void Deserialize_Generic_ToClass_IgnoresUnknownProperties()
    {
        var result = Serializer.Deserialize<Person>("{\"Name\":\"Bob\",\"Age\":25,\"Unknown\":true}");
        Assert.NotNull(result);
        Assert.Equal("Bob", result.Name);
        Assert.Equal(25, result.Age);
    }

    [Fact]
    public void Deserialize_Generic_ToClass_MissingPropertyUsesDefault()
    {
        var result = Serializer.Deserialize<Person>("{\"Name\":\"Bob\"}");
        Assert.NotNull(result);
        Assert.Equal("Bob", result.Name);
        Assert.Equal(0, result.Age);
    }

    [Fact]
    public void Deserialize_Generic_ToNullableInt_ReturnsNull()
    {
        var result = Serializer.Deserialize<int?>("null");
        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_Generic_ToNullableInt_ReturnsValue()
    {
        var result = Serializer.Deserialize<int?>("42");
        Assert.Equal(42, result);
    }

    [Fact]
    public void Deserialize_Generic_ToClassWithNestedObject_SetsNestedProperties()
    {
        var result = Serializer.Deserialize<Parent>("{\"Name\":\"Parent\",\"Child\":{\"Name\":\"Child\",\"Age\":5}}");
        Assert.NotNull(result);
        Assert.Equal("Parent", result.Name);
        Assert.NotNull(result.Child);
        Assert.Equal("Child", result.Child.Name);
        Assert.Equal(5, result.Child.Age);
    }

    [Fact]
    public void Deserialize_RoundTrip_Object()
    {
        var original = new Person { Name = "Alice", Age = 30 };
        var json = Serializer.Serialize(original);
        var deserialized = Serializer.Deserialize<Person>(json);
        Assert.NotNull(deserialized);
        Assert.Equal(original.Name, deserialized.Name);
        Assert.Equal(original.Age, deserialized.Age);
    }

    [Fact]
    public void Deserialize_RoundTrip_Dictionary()
    {
        var original = new Dictionary<string, object>
        {
            ["name"] = "Alice",
            ["age"] = 30
        };
        var json = Serializer.Serialize(original);
        var result = Serializer.Deserialize(json);
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal("Alice", dict["name"]);
        Assert.Equal(30, dict["age"]);
    }

    [Fact]
    public void Deserialize_RoundTrip_Array()
    {
        var original = new[] { 1, 2, 3 };
        var json = Serializer.Serialize(original);
        var result = Serializer.Deserialize<int[]>(json);
        Assert.NotNull(result);
        Assert.Equal(original, result);
    }

    public class Person
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    public class Parent
    {
        public string? Name { get; set; }
        public Person? Child { get; set; }
    }
}
