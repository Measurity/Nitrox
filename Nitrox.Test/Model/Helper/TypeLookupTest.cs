using System;
using System.Reflection;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NitroxClient.Debuggers.Drawer;

namespace NitroxModel.Helper;

[TestClass]
public class TypeLookupTest
{
    [TestMethod]
    public void ShouldFindImplementationsForInterface()
    {
        TypeLookup<IDrawer<object>> lookup = TypeLookup<IDrawer<object>>.Create<TestWrapper<object>>(typeof(TypeLookupTest).GetNestedTypes(BindingFlags.NonPublic));
        lookup.Should().NotBeEmpty();
        lookup.Should().ContainKey(typeof(int))
              .WhoseValue.Should().BeOfType<TestWrapper<int>>()
              .Which.Inner.Should().BeOfType<TestIntAndStringDrawer>();
        lookup[typeof(int)].Invoking(drawer => drawer.Draw("oops")).Should().Throw<InvalidCastException>();
        lookup[typeof(int)].Should().BeOfType<TestWrapper<int>>().Which.Inner.Should().BeSameAs((lookup[typeof(string)] as TestWrapper<string>)?.Inner).And.BeOfType<TestIntAndStringDrawer>();

        lookup.Should().ContainKey(typeof(float))
              .WhoseValue.Should().BeOfType<TestWrapper<float>>()
              .Which.Inner.Should().BeOfType<TestChainingDrawer>();
        lookup[typeof(float)].Draw(5f).Should().Be(5f);
    }

    private sealed record TestWrapper<T>(IDrawer<T> Inner) : IDrawer<object>
    {
        public object Draw(object target) => Inner.Draw((T)target);
    }

    private sealed class TestIntAndStringDrawer : IDrawer<int>, IDrawer<string>
    {
        public int Draw(int target) => target;
        public string Draw(string target) => target;
    }

    private sealed record TestChainingDrawer(TestIntAndStringDrawer IntStringDrawer) : IDrawer<float>
    {
        public float Draw(float target) => IntStringDrawer.Draw((int)target);
    }
}
