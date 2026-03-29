using Serializer = MiniJsonSerializer.MiniJsonSerializer;

namespace MiniJsonSerializer.Tests;

public class SerializeTests
{
    [Fact]
    public void Serialize_Null_ReturnsNullLiteral()
    {
        var result = Serializer.Serialize(null);
        Assert.Equal("null", result);
    }

    [Fact]
    public void Serialize_True_ReturnsTrueLiteral()
    {
        var result = Serializer.Serialize(true);
        Assert.Equal("true", result);
    }

    [Fact]
    public void Serialize_False_ReturnsFalseLiteral()
    {
        var result = Serializer.Serialize(false);
        Assert.Equal("false", result);
    }

    [Fact]
    public void Serialize_String_ReturnsQuotedString()
    {
        var result = Serializer.Serialize("hello");
        Assert.Equal("\"hello\"", result);
    }

    [Fact]
    public void Serialize_EmptyString_ReturnsEmptyQuotedString()
    {
        var result = Serializer.Serialize("");
        Assert.Equal("\"\"", result);
    }

    [Fact]
    public void Serialize_StringWithEscapeCharacters_EscapesCorrectly()
    {
        var result = Serializer.Serialize("line1\nline2\r\ttab\\slash\"quote");
        Assert.Equal("\"line1\\nline2\\r\\ttab\\\\slash\\\"quote\"", result);
    }

    [Fact]
    public void Serialize_Int_ReturnsNumber()
    {
        var result = Serializer.Serialize(42);
        Assert.Equal("42", result);
    }

    [Fact]
    public void Serialize_NegativeInt_ReturnsNegativeNumber()
    {
        var result = Serializer.Serialize(-7);
        Assert.Equal("-7", result);
    }

    [Fact]
    public void Serialize_Long_ReturnsNumber()
    {
        var result = Serializer.Serialize(9876543210L);
        Assert.Equal("9876543210", result);
    }

    [Fact]
    public void Serialize_Float_ReturnsDecimalNumber()
    {
        var result = Serializer.Serialize(3.14f);
        Assert.Equal("3.14", result);
    }

    [Fact]
    public void Serialize_Double_ReturnsDecimalNumber()
    {
        var result = Serializer.Serialize(2.718281828);
        Assert.Equal("2.718281828", result);
    }

    [Fact]
    public void Serialize_Decimal_ReturnsDecimalNumber()
    {
        var result = Serializer.Serialize(99.99m);
        Assert.Equal("99.99", result);
    }

    [Fact]
    public void Serialize_EmptyArray_ReturnsEmptyJsonArray()
    {
        var result = Serializer.Serialize(new int[] { });
        Assert.Equal("[]", result);
    }

    [Fact]
    public void Serialize_IntArray_ReturnsJsonArray()
    {
        var result = Serializer.Serialize(new[] { 1, 2, 3 });
        Assert.Equal("[1,2,3]", result);
    }

    [Fact]
    public void Serialize_StringArray_ReturnsJsonArray()
    {
        var result = Serializer.Serialize(new[] { "a", "b", "c" });
        Assert.Equal("[\"a\",\"b\",\"c\"]", result);
    }

    [Fact]
    public void Serialize_MixedList_ReturnsJsonArray()
    {
        var list = new List<object> { 1, "two", true, null! };
        var result = Serializer.Serialize(list);
        Assert.Equal("[1,\"two\",true,null]", result);
    }

    [Fact]
    public void Serialize_EmptyDictionary_ReturnsEmptyJsonObject()
    {
        var dict = new Dictionary<string, object>();
        var result = Serializer.Serialize(dict);
        Assert.Equal("{}", result);
    }

    [Fact]
    public void Serialize_Dictionary_ReturnsJsonObject()
    {
        var dict = new Dictionary<string, object>
        {
            ["name"] = "Alice",
            ["age"] = 30
        };
        var result = Serializer.Serialize(dict);
        Assert.Equal("{\"name\":\"Alice\",\"age\":30}", result);
    }

    [Fact]
    public void Serialize_DictionaryWithNullValue_ReturnsNullInJson()
    {
        var dict = new Dictionary<string, object>
        {
            ["key"] = null!
        };
        var result = Serializer.Serialize(dict);
        Assert.Equal("{\"key\":null}", result);
    }

    [Fact]
    public void Serialize_AnonymousObject_ReturnsJsonObject()
    {
        var obj = new { Name = "Bob", Age = 25 };
        var result = Serializer.Serialize(obj);
        Assert.Equal("{\"Name\":\"Bob\",\"Age\":25}", result);
    }

    [Fact]
    public void Serialize_ObjectWithBoolProperty_SerializesCorrectly()
    {
        var obj = new { Active = true, Deleted = false };
        var result = Serializer.Serialize(obj);
        Assert.Equal("{\"Active\":true,\"Deleted\":false}", result);
    }

    [Fact]
    public void Serialize_ObjectWithNullProperty_SerializesNull()
    {
        var obj = new { Value = (string?)null };
        var result = Serializer.Serialize(obj);
        Assert.Equal("{\"Value\":null}", result);
    }

    [Fact]
    public void Serialize_NestedObject_SerializesRecursively()
    {
        var obj = new
        {
            Name = "Parent",
            Child = new { Name = "Child", Age = 5 }
        };
        var result = Serializer.Serialize(obj);
        Assert.Equal("{\"Name\":\"Parent\",\"Child\":{\"Name\":\"Child\",\"Age\":5}}", result);
    }

    [Fact]
    public void Serialize_ObjectWithArrayProperty_SerializesCorrectly()
    {
        var obj = new { Tags = new[] { "a", "b" } };
        var result = Serializer.Serialize(obj);
        Assert.Equal("{\"Tags\":[\"a\",\"b\"]}", result);
    }

    [Fact]
    public void Serialize_NestedDictionary_SerializesRecursively()
    {
        var dict = new Dictionary<string, object>
        {
            ["outer"] = new Dictionary<string, object>
            {
                ["inner"] = 42
            }
        };
        var result = Serializer.Serialize(dict);
        Assert.Equal("{\"outer\":{\"inner\":42}}", result);
    }

    [Fact]
    public void Serialize_ArrayOfObjects_SerializesCorrectly()
    {
        var array = new[]
        {
            new { Id = 1, Name = "A" },
            new { Id = 2, Name = "B" }
        };
        var result = Serializer.Serialize(array);
        Assert.Equal("[{\"Id\":1,\"Name\":\"A\"},{\"Id\":2,\"Name\":\"B\"}]", result);
    }

    [Fact]
    public void Serialize_ArrayWithNulls_SerializesCorrectly()
    {
        var array = new object?[] { "a", null, 1 };
        var result = Serializer.Serialize(array);
        Assert.Equal("[\"a\",null,1]", result);
    }

    [Fact]
    public void Serialize_DictionaryWithNestedArray_SerializesCorrectly()
    {
        var dict = new Dictionary<string, object>
        {
            ["items"] = new[] { 1, 2, 3 }
        };
        var result = Serializer.Serialize(dict);
        Assert.Equal("{\"items\":[1,2,3]}", result);
    }

    [Fact]
    public void Serialize_ClassWithReadOnlyProperty_IncludesIt()
    {
        var obj = new PersonWithReadOnly("Alice", 30);
        var result = Serializer.Serialize(obj);
        Assert.Contains("\"Name\":\"Alice\"", result);
        Assert.Contains("\"Age\":30", result);
    }

    [Fact]
    public void Serialize_ZeroInt_ReturnsZero()
    {
        var result = Serializer.Serialize(0);
        Assert.Equal("0", result);
    }

    [Fact]
    public void Serialize_StringWithOnlySpecialChars_EscapesAll()
    {
        var result = Serializer.Serialize("\"\\\n\r\t");
        Assert.Equal("\"\\\"\\\\\\n\\r\\t\"", result);
    }

    [Fact]
    public void Serialize_SingleElementArray_ReturnsJsonArray()
    {
        var result = Serializer.Serialize(new[] { 42 });
        Assert.Equal("[42]", result);
    }

    [Fact]
    public void Serialize_SingleKeyDictionary_ReturnsJsonObject()
    {
        var dict = new Dictionary<string, object> { ["only"] = "one" };
        var result = Serializer.Serialize(dict);
        Assert.Equal("{\"only\":\"one\"}", result);
    }

    [Fact]
    public void Serialize_DeeplyNestedStructure_SerializesCorrectly()
    {
        var dict = new Dictionary<string, object>
        {
            ["level1"] = new Dictionary<string, object>
            {
                ["level2"] = new Dictionary<string, object>
                {
                    ["level3"] = "deep"
                }
            }
        };
        var result = Serializer.Serialize(dict);
        Assert.Equal("{\"level1\":{\"level2\":{\"level3\":\"deep\"}}}", result);
    }

    private record PersonWithReadOnly(string Name, int Age);

    [Fact]
    public void Serialize_WriteIndented_Object_FormatsWithIndentation()
    {
        var obj = new { Name = "Bob", Age = 25 };
        var options = new JsonSerializerOptions { WriteIndented = true };
        var result = Serializer.Serialize(obj, options);
        var expected =
            "{\r\n" +
            "  \"Name\": \"Bob\",\r\n" +
            "  \"Age\": 25\r\n" +
            "}";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Serialize_WriteIndented_Array_FormatsWithIndentation()
    {
        var array = new[] { 1, 2, 3 };
        var options = new JsonSerializerOptions { WriteIndented = true };
        var result = Serializer.Serialize(array, options);
        var expected =
            "[\r\n" +
            "  1,\r\n" +
            "  2,\r\n" +
            "  3\r\n" +
            "]";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Serialize_WriteIndented_Dictionary_FormatsWithIndentation()
    {
        var dict = new Dictionary<string, object>
        {
            ["name"] = "Alice",
            ["age"] = 30
        };
        var options = new JsonSerializerOptions { WriteIndented = true };
        var result = Serializer.Serialize(dict, options);
        var expected =
            "{\r\n" +
            "  \"name\": \"Alice\",\r\n" +
            "  \"age\": 30\r\n" +
            "}";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Serialize_WriteIndented_NestedObject_FormatsWithIndentation()
    {
        var obj = new
        {
            Name = "Parent",
            Child = new { Name = "Child", Age = 5 }
        };
        var options = new JsonSerializerOptions { WriteIndented = true };
        var result = Serializer.Serialize(obj, options);
        var expected =
            "{\r\n" +
            "  \"Name\": \"Parent\",\r\n" +
            "  \"Child\": {\r\n" +
            "    \"Name\": \"Child\",\r\n" +
            "    \"Age\": 5\r\n" +
            "  }\r\n" +
            "}";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Serialize_WriteIndented_EmptyObject_NoIndentation()
    {
        var dict = new Dictionary<string, object>();
        var options = new JsonSerializerOptions { WriteIndented = true };
        var result = Serializer.Serialize(dict, options);
        Assert.Equal("{}", result);
    }

    [Fact]
    public void Serialize_WriteIndented_EmptyArray_NoIndentation()
    {
        var array = new int[] { };
        var options = new JsonSerializerOptions { WriteIndented = true };
        var result = Serializer.Serialize(array, options);
        Assert.Equal("[]", result);
    }

    [Fact]
    public void Serialize_WriteIndented_False_ProducesCompactOutput()
    {
        var obj = new { Name = "Bob", Age = 25 };
        var options = new JsonSerializerOptions { WriteIndented = false };
        var result = Serializer.Serialize(obj, options);
        Assert.Equal("{\"Name\":\"Bob\",\"Age\":25}", result);
    }

    [Fact]
    public void Serialize_WriteIndented_DeeplyNested_FormatsCorrectly()
    {
        var dict = new Dictionary<string, object>
        {
            ["level1"] = new Dictionary<string, object>
            {
                ["level2"] = new Dictionary<string, object>
                {
                    ["level3"] = "deep"
                }
            }
        };
        var options = new JsonSerializerOptions { WriteIndented = true };
        var result = Serializer.Serialize(dict, options);
        var expected =
            "{\r\n" +
            "  \"level1\": {\r\n" +
            "    \"level2\": {\r\n" +
            "      \"level3\": \"deep\"\r\n" +
            "    }\r\n" +
            "  }\r\n" +
            "}";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Serialize_WriteIndented_ArrayOfObjects_FormatsCorrectly()
    {
        var array = new[]
        {
            new { Id = 1, Name = "A" },
            new { Id = 2, Name = "B" }
        };
        var options = new JsonSerializerOptions { WriteIndented = true };
        var result = Serializer.Serialize(array, options);
        var expected =
            "[\r\n" +
            "  {\r\n" +
            "    \"Id\": 1,\r\n" +
            "    \"Name\": \"A\"\r\n" +
            "  },\r\n" +
            "  {\r\n" +
            "    \"Id\": 2,\r\n" +
            "    \"Name\": \"B\"\r\n" +
            "  }\r\n" +
            "]";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Serialize_MaxDepth_ExceedsLimit_ThrowsInvalidOperationException()
    {
        var obj = new
        {
            Child = new
            {
                Child = new { Name = "deep" }
            }
        };
        var options = new JsonSerializerOptions { MaxDepth = 2 };
        Assert.Throws<InvalidOperationException>(() => Serializer.Serialize(obj, options));
    }

    [Fact]
    public void Serialize_MaxDepth_ExactLimit_Succeeds()
    {
        var obj = new
        {
            Child = new { Name = "leaf" }
        };
        var options = new JsonSerializerOptions { MaxDepth = 2 };
        var result = Serializer.Serialize(obj, options);
        Assert.Contains("\"Name\":\"leaf\"", result);
    }

    [Fact]
    public void Serialize_MaxDepth_FlatObject_Succeeds()
    {
        var obj = new { Name = "Bob", Age = 25 };
        var options = new JsonSerializerOptions { MaxDepth = 1 };
        var result = Serializer.Serialize(obj, options);
        Assert.Equal("{\"Name\":\"Bob\",\"Age\":25}", result);
    }

    [Fact]
    public void Serialize_MaxDepth_NestedArray_ExceedsLimit_Throws()
    {
        var array = new object[] { new object[] { new object[] { 1 } } };
        var options = new JsonSerializerOptions { MaxDepth = 2 };
        Assert.Throws<InvalidOperationException>(() => Serializer.Serialize(array, options));
    }

    [Fact]
    public void Serialize_MaxDepth_NestedDictionary_ExceedsLimit_Throws()
    {
        var dict = new Dictionary<string, object>
        {
            ["a"] = new Dictionary<string, object>
            {
                ["b"] = new Dictionary<string, object>
                {
                    ["c"] = 1
                }
            }
        };
        var options = new JsonSerializerOptions { MaxDepth = 2 };
        Assert.Throws<InvalidOperationException>(() => Serializer.Serialize(dict, options));
    }

    [Fact]
    public void Serialize_MaxDepth_Default64_DeeplyNestedSucceeds()
    {
        var dict = new Dictionary<string, object>
        {
            ["level1"] = new Dictionary<string, object>
            {
                ["level2"] = new Dictionary<string, object>
                {
                    ["level3"] = "deep"
                }
            }
        };
        var options = new JsonSerializerOptions();
        var result = Serializer.Serialize(dict, options);
        Assert.Equal("{\"level1\":{\"level2\":{\"level3\":\"deep\"}}}", result);
    }

    [Fact]
    public void Serialize_WithoutOptions_NoDepthLimit()
    {
        var dict = new Dictionary<string, object>
        {
            ["level1"] = new Dictionary<string, object>
            {
                ["level2"] = new Dictionary<string, object>
                {
                    ["level3"] = "deep"
                }
            }
        };
        var result = Serializer.Serialize(dict);
        Assert.Equal("{\"level1\":{\"level2\":{\"level3\":\"deep\"}}}", result);
    }
}
