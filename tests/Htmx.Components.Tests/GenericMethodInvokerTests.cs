using Htmx.Components.Utilities;

namespace Htmx.Components.Tests;

public class GenericMethodInvokerTests
{
    [Fact]
    public void Invoke_ResolvesOverloadByParameterType()
    {
        var target = new GenericInvocationTarget();

        var text = GenericMethodInvoker.Invoke<string>(
            target,
            nameof(GenericInvocationTarget.Echo),
            [],
            "alpha");
        var number = GenericMethodInvoker.Invoke<string>(
            target,
            nameof(GenericInvocationTarget.Echo),
            [],
            42);

        Assert.Equal("string:alpha", text);
        Assert.Equal("int:42", number);
    }

    [Fact]
    public async Task InvokeAsync_ResolvesGenericTaskByClosedParameterType()
    {
        var target = new GenericInvocationTarget();

        var result = await GenericMethodInvoker.InvokeAsync<string>(
            target,
            nameof(GenericInvocationTarget.DescribeAsync),
            [typeof(Widget), typeof(int)],
            new GenericBox<Widget, int>(new Widget { Id = 7, Name = "gear" }, 7));

        Assert.Equal("Widget:7", result);
    }

    [Fact]
    public void Invoke_ThrowsForAmbiguousNullArgument()
    {
        var target = new GenericInvocationTarget();

        var ex = Assert.Throws<InvalidOperationException>(() => GenericMethodInvoker.Invoke<string>(
            target,
            nameof(GenericInvocationTarget.Echo),
            [],
            (object?)null!));

        Assert.Contains("ambiguous", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class GenericInvocationTarget
    {
        public string Echo(string value) => $"string:{value}";
        public string Echo(int value) => $"int:{value}";
        public string Echo(object value) => $"object:{value}";

        public Task<string> DescribeAsync<T, TKey>(GenericBox<T, TKey> box)
            where T : class
        {
            return Task.FromResult($"{typeof(T).Name}:{box.Key}");
        }
    }

    private sealed record GenericBox<T, TKey>(T Value, TKey Key)
        where T : class;
}
