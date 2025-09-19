using System;
using System.Collections.Generic;
using System.Linq;
using ParadoxDataLib.Core.Common;
using Xunit;

namespace ParadoxDataLib.Tests
{
    /// <summary>
    /// Unit tests for the ParadoxNode class
    /// </summary>
    public class ParadoxNodeTests
    {
        [Fact]
        public void CreateScalar_WithValidData_CreatesScalarNode()
        {
            var node = ParadoxNode.CreateScalar("test_key", "test_value");

            Assert.Equal("test_key", node.Key);
            Assert.Equal("test_value", node.Value);
            Assert.Equal(NodeType.Scalar, node.Type);
            Assert.Empty(node.Children);
            Assert.Empty(node.Items);
        }

        [Fact]
        public void CreateObject_WithValidKey_CreatesObjectNode()
        {
            var node = ParadoxNode.CreateObject("test_object");

            Assert.Equal("test_object", node.Key);
            Assert.Null(node.Value);
            Assert.Equal(NodeType.Object, node.Type);
            Assert.NotNull(node.Children);
            Assert.Empty(node.Children);
            Assert.Empty(node.Items);
        }

        [Fact]
        public void CreateList_WithValidKey_CreatesListNode()
        {
            var node = ParadoxNode.CreateList("test_list");

            Assert.Equal("test_list", node.Key);
            Assert.Null(node.Value);
            Assert.Equal(NodeType.List, node.Type);
            Assert.Empty(node.Children);
            Assert.NotNull(node.Items);
            Assert.Empty(node.Items);
        }

        [Fact]
        public void CreateDate_WithValidData_CreatesDateNode()
        {
            var date = new DateTime(1444, 11, 11);
            var node = ParadoxNode.CreateDate("1444.11.11", date);

            Assert.Equal("1444.11.11", node.Key);
            Assert.Equal(date, node.Value);
            Assert.Equal(NodeType.Date, node.Type);
            Assert.NotNull(node.Children);
            Assert.Empty(node.Items);
        }

        [Fact]
        public void AddChild_ToObjectNode_AddsChildSuccessfully()
        {
            var parent = ParadoxNode.CreateObject("parent");
            var child = ParadoxNode.CreateScalar("child", "value");

            parent.AddChild(child);

            Assert.Single(parent.Children);
            Assert.True(parent.Children.ContainsKey("child"));
            Assert.Equal(child, parent.Children["child"]);
        }

        [Fact]
        public void AddChild_ToDateNode_AddsChildSuccessfully()
        {
            var date = new DateTime(1444, 11, 11);
            var parent = ParadoxNode.CreateDate("1444.11.11", date);
            var child = ParadoxNode.CreateScalar("owner", "FRA");

            parent.AddChild(child);

            Assert.Single(parent.Children);
            Assert.True(parent.Children.ContainsKey("owner"));
            Assert.Equal(child, parent.Children["owner"]);
        }

        [Fact]
        public void AddChild_DuplicateKey_ReplacesExistingChild()
        {
            var parent = ParadoxNode.CreateObject("parent");
            var child1 = ParadoxNode.CreateScalar("child", "value1");
            var child2 = ParadoxNode.CreateScalar("child", "value2");

            parent.AddChild(child1);
            parent.AddChild(child2);

            Assert.Single(parent.Children);
            Assert.Equal(child2, parent.Children["child"]);
            Assert.Equal("value2", parent.Children["child"].Value);
        }

        [Fact]
        public void AddItem_ToListNode_AddsItemSuccessfully()
        {
            var list = ParadoxNode.CreateList("test_list");
            var item = ParadoxNode.CreateScalar("", "item1");

            list.AddItem(item);

            Assert.Single(list.Items);
            Assert.Equal(item, list.Items[0]);
        }

        [Fact]
        public void HasChild_ExistingChild_ReturnsTrue()
        {
            var parent = ParadoxNode.CreateObject("parent");
            var child = ParadoxNode.CreateScalar("child", "value");
            parent.AddChild(child);

            Assert.True(parent.HasChild("child"));
        }

        [Fact]
        public void HasChild_NonExistentChild_ReturnsFalse()
        {
            var parent = ParadoxNode.CreateObject("parent");

            Assert.False(parent.HasChild("nonexistent"));
        }

        [Fact]
        public void GetChild_ExistingChild_ReturnsChild()
        {
            var parent = ParadoxNode.CreateObject("parent");
            var child = ParadoxNode.CreateScalar("child", "value");
            parent.AddChild(child);

            var result = parent.GetChild("child");

            Assert.NotNull(result);
            Assert.Equal(child, result);
        }

        [Fact]
        public void GetChild_NonExistentChild_ReturnsNull()
        {
            var parent = ParadoxNode.CreateObject("parent");

            var result = parent.GetChild("nonexistent");

            Assert.Null(result);
        }

        [Fact]
        public void GetChildren_MultipleChildrenWithSameKey_ReturnsAllMatching()
        {
            var parent = ParadoxNode.CreateObject("parent");
            var child1 = ParadoxNode.CreateScalar("add_core", "FRA");
            var child2 = ParadoxNode.CreateScalar("add_core", "ENG");

            // Simulate multiple entries with same key (stored as list)
            var coresList = ParadoxNode.CreateList("add_core");
            coresList.AddItem(child1);
            coresList.AddItem(child2);
            parent.AddChild(coresList);

            var results = parent.GetChildren("add_core").ToList();

            Assert.Equal(2, results.Count); // Returns the items in the list
            Assert.Equal("FRA", results[0].Value);
            Assert.Equal("ENG", results[1].Value);
        }

        [Fact]
        public void GetValue_StringType_ReturnsCorrectValue()
        {
            var node = ParadoxNode.CreateObject("parent");
            var child = ParadoxNode.CreateScalar("test", "string_value");
            node.AddChild(child);

            var result = node.GetValue<string>("test");

            Assert.Equal("string_value", result);
        }

        [Fact]
        public void GetValue_IntType_ReturnsCorrectValue()
        {
            var node = ParadoxNode.CreateObject("parent");
            var child = ParadoxNode.CreateScalar("test", 42);
            node.AddChild(child);

            var result = node.GetValue<int>("test");

            Assert.Equal(42, result);
        }

        [Fact]
        public void GetValue_FloatType_ReturnsCorrectValue()
        {
            var node = ParadoxNode.CreateObject("parent");
            var child = ParadoxNode.CreateScalar("test", 3.14f);
            node.AddChild(child);

            var result = node.GetValue<float>("test");

            Assert.Equal(3.14f, result);
        }

        [Fact]
        public void GetValue_BoolType_ReturnsCorrectValue()
        {
            var node = ParadoxNode.CreateObject("parent");
            var child = ParadoxNode.CreateScalar("test", true);
            node.AddChild(child);

            var result = node.GetValue<bool>("test");

            Assert.True(result);
        }

        [Fact]
        public void GetValue_WithDefaultValue_NonExistentKey_ReturnsDefault()
        {
            var node = ParadoxNode.CreateObject("parent");

            var result = node.GetValue<string>("nonexistent", "default_value");

            Assert.Equal("default_value", result);
        }

        [Fact]
        public void GetValue_WithoutDefaultValue_NonExistentKey_ReturnsTypeDefault()
        {
            var node = ParadoxNode.CreateObject("parent");

            var stringResult = node.GetValue<string>("nonexistent");
            var intResult = node.GetValue<int>("nonexistent");
            var boolResult = node.GetValue<bool>("nonexistent");

            Assert.Null(stringResult);
            Assert.Equal(0, intResult);
            Assert.False(boolResult);
        }

        [Fact]
        public void GetValue_TypeConversion_StringToInt_ReturnsConvertedValue()
        {
            var node = ParadoxNode.CreateObject("parent");
            var child = ParadoxNode.CreateScalar("test", "123");
            node.AddChild(child);

            var result = node.GetValue<int>("test");

            Assert.Equal(123, result);
        }

        [Fact]
        public void GetValue_TypeConversion_StringToFloat_ReturnsConvertedValue()
        {
            var node = ParadoxNode.CreateObject("parent");
            var child = ParadoxNode.CreateScalar("test", "3.14");
            node.AddChild(child);

            var result = node.GetValue<float>("test");

            Assert.Equal(3.14f, result, 2);
        }

        [Fact]
        public void GetValue_TypeConversion_InvalidConversion_ReturnsDefault()
        {
            var node = ParadoxNode.CreateObject("parent");
            var child = ParadoxNode.CreateScalar("test", "not_a_number");
            node.AddChild(child);

            var result = node.GetValue<int>("test", 999);

            Assert.Equal(999, result);
        }

        [Fact]
        public void GetValues_ListNode_ReturnsAllValues()
        {
            var parent = ParadoxNode.CreateObject("parent");
            var listNode = ParadoxNode.CreateList("cores");
            listNode.AddItem(ParadoxNode.CreateScalar("", "FRA"));
            listNode.AddItem(ParadoxNode.CreateScalar("", "ENG"));
            listNode.AddItem(ParadoxNode.CreateScalar("", "CAS"));
            parent.AddChild(listNode);

            var result = parent.GetValues<string>("cores").ToList();

            Assert.Equal(3, result.Count);
            Assert.Contains("FRA", result);
            Assert.Contains("ENG", result);
            Assert.Contains("CAS", result);
        }

        [Fact]
        public void GetValues_SingleValue_ReturnsListWithOneItem()
        {
            var parent = ParadoxNode.CreateObject("parent");
            var child = ParadoxNode.CreateScalar("core", "FRA");
            parent.AddChild(child);

            var result = parent.GetValues<string>("core").ToList();

            Assert.Single(result);
            Assert.Equal("FRA", result[0]);
        }

        [Fact]
        public void GetValues_NonExistentKey_ReturnsEmptyList()
        {
            var parent = ParadoxNode.CreateObject("parent");

            var result = parent.GetValues<string>("nonexistent");

            Assert.Empty(result);
        }

        [Fact]
        public void GetValues_TypeConversion_ReturnsConvertedValues()
        {
            var parent = ParadoxNode.CreateObject("parent");
            var listNode = ParadoxNode.CreateList("numbers");
            listNode.AddItem(ParadoxNode.CreateScalar("", "1"));
            listNode.AddItem(ParadoxNode.CreateScalar("", "2"));
            listNode.AddItem(ParadoxNode.CreateScalar("", "3"));
            parent.AddChild(listNode);

            var result = parent.GetValues<int>("numbers").ToList();

            Assert.Equal(3, result.Count);
            Assert.Equal(1, result[0]);
            Assert.Equal(2, result[1]);
            Assert.Equal(3, result[2]);
        }

        [Theory]
        [InlineData("yes", true)]
        [InlineData("true", true)]
        [InlineData("no", false)]
        [InlineData("false", false)]
        public void GetValue_BooleanStrings_ReturnsCorrectBooleanValue(string input, bool expected)
        {
            var node = ParadoxNode.CreateObject("parent");
            var child = ParadoxNode.CreateScalar("test", input);
            node.AddChild(child);

            var result = node.GetValue<bool>("test");

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetValue_NullableInt_WithValue_ReturnsValue()
        {
            var node = ParadoxNode.CreateObject("parent");
            var child = ParadoxNode.CreateScalar("test", 42);
            node.AddChild(child);

            var result = node.GetValue<int?>("test");

            Assert.Equal(42, result);
        }

        [Fact]
        public void GetValue_NullableInt_NonExistent_ReturnsNull()
        {
            var node = ParadoxNode.CreateObject("parent");

            var result = node.GetValue<int?>("nonexistent");

            Assert.Null(result);
        }
    }
}