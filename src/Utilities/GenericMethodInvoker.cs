using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;

namespace Htmx.Components.Utilities;

/// <summary>
/// Provides high-performance, cached invocation of generic methods using compiled expressions.
/// This utility class optimizes the performance of generic method calls by caching compiled delegates,
/// making it ideal for scenarios where the same generic methods are called repeatedly.
/// </summary>
public static class GenericMethodInvoker
{
    private static readonly ConcurrentDictionary<string, Delegate> _delegateCache = new();

    private static Delegate GetOrAddDelegate(
        Type targetType,
        string methodName,
        Type[] genericTypes,
        Type[] paramTypes,
        bool isStatic,
        Type? expectedReturnType = null)
    {
        var cacheKey = $"{targetType.FullName}|{methodName}|{string.Join("|", genericTypes.Select(t => t.FullName))}|{string.Join("|", paramTypes.Select(t => t.FullName))}|{expectedReturnType?.FullName}";
        return _delegateCache.GetOrAdd(cacheKey, _ =>
        {
            var flags = BindingFlags.NonPublic | BindingFlags.Public | (isStatic ? BindingFlags.Static : BindingFlags.Instance);
            var method = targetType.GetMethod(methodName, flags);
            if (method == null)
                throw new InvalidOperationException($"Method '{methodName}' not found on {targetType.Name}.");

            if (genericTypes.Length > 0)
                method = method.MakeGenericMethod(genericTypes);

            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var argsParam = Expression.Parameter(typeof(object[]), "args");

            var callParams = method.GetParameters()
                .Select((p, i) =>
                    Expression.Convert(
                        Expression.ArrayIndex(argsParam, Expression.Constant(i)),
                        p.ParameterType))
                .ToArray();

            Expression callExpr = method.IsStatic
                ? Expression.Call(method, callParams)
                : Expression.Call(Expression.Convert(instanceParam, targetType), method, callParams);

            // Handle void return
            if (method.ReturnType == typeof(void))
            {
                var lambda = Expression.Lambda<Action<object, object[]>>(callExpr, instanceParam, argsParam);
                return lambda.CompileFast();
            }

            // Handle all other return types
            var converted = Expression.Convert(callExpr, typeof(object));
            var lambda2 = Expression.Lambda<Func<object, object[], object>>(converted, instanceParam, argsParam);
            return lambda2.CompileFast();
        });
    }

    /// <summary>
    /// Invokes a generic method that returns void using cached compiled expressions for optimal performance.
    /// The method is located by name and generic type parameters, then invoked with the provided arguments.
    /// </summary>
    /// <param name="instance">The object instance on which to invoke the method</param>
    /// <param name="methodName">The name of the method to invoke</param>
    /// <param name="genericTypes">Array of types for the generic type parameters</param>
    /// <param name="parameters">Arguments to pass to the method</param>
    /// <exception cref="InvalidOperationException">Thrown when the method is not found or delegate type is incompatible</exception>
    public static void InvokeVoid(
        object instance,
        string methodName,
        Type[] genericTypes,
        params object[] parameters)
    {
        var type = instance.GetType();
        var paramTypes = parameters.Select(p => p?.GetType() ?? typeof(object)).ToArray();
        var del = GetOrAddDelegate(type, methodName, genericTypes, paramTypes, false, typeof(void));
        if (del is Action<object, object[]> action)
            action(instance, parameters);
        else
            throw new InvalidOperationException("Delegate type not supported for void method.");
    }

    /// <summary>
    /// Invokes a generic method that returns a value using cached compiled expressions for optimal performance.
    /// The method is located by name and generic type parameters, then invoked with the provided arguments.
    /// </summary>
    /// <typeparam name="TReturn">The expected return type of the method</typeparam>
    /// <param name="instance">The object instance on which to invoke the method</param>
    /// <param name="methodName">The name of the method to invoke</param>
    /// <param name="genericTypes">Array of types for the generic type parameters</param>
    /// <param name="parameters">Arguments to pass to the method</param>
    /// <returns>The result of the method invocation cast to the specified return type</returns>
    /// <exception cref="InvalidOperationException">Thrown when the method is not found or delegate type is incompatible</exception>
    public static TReturn Invoke<TReturn>(
        object instance,
        string methodName,
        Type[] genericTypes,
        params object[] parameters)
    {
        var type = instance.GetType();
        var paramTypes = parameters.Select(p => p?.GetType() ?? typeof(object)).ToArray();
        var del = GetOrAddDelegate(type, methodName, genericTypes, paramTypes, false, typeof(TReturn));
        if (del is Func<object, object[], object> func)
            return (TReturn)func(instance, parameters)!;
        throw new InvalidOperationException("Delegate type not supported for value-returning method.");
    }

    /// <summary>
    /// Asynchronously invokes a generic method that returns a Task using cached compiled expressions for optimal performance.
    /// The method is located by name and generic type parameters, then invoked with the provided arguments.
    /// </summary>
    /// <param name="instance">The object instance on which to invoke the method</param>
    /// <param name="methodName">The name of the method to invoke</param>
    /// <param name="genericTypes">Array of types for the generic type parameters</param>
    /// <param name="parameters">Arguments to pass to the method</param>
    /// <returns>A task representing the asynchronous method execution</returns>
    /// <exception cref="InvalidOperationException">Thrown when the method is not found or does not return a Task</exception>
    public static async Task InvokeAsync(
        object instance,
        string methodName,
        Type[] genericTypes,
        params object[] parameters)
    {
        var result = Invoke<object>(instance, methodName, genericTypes, parameters);
        if (result is Task task)
            await task;
        else
            throw new InvalidOperationException("Method does not return Task.");
    }

    // Async Task<TResult>-returning methods
    /// <summary>
    /// Asynchronously invokes a generic method that returns a <see cref="Task{TResult}"/> with the specified parameters.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the task.</typeparam>
    /// <param name="instance">The object instance on which to invoke the method.</param>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="genericTypes">An array of types for generic parameters.</param>
    /// <param name="parameters">The parameters to pass to the method.</param>
    /// <returns>A task that represents the asynchronous operation, containing the result of the method invocation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the method does not return a <see cref="Task{TResult}"/>.</exception>
    public static async Task<TResult> InvokeAsync<TResult>(
        object instance,
        string methodName,
        Type[] genericTypes,
        params object[] parameters)
    {
        var result = Invoke<object>(instance, methodName, genericTypes, parameters);
        if (result is Task<TResult> task)
            return await task;
        throw new InvalidOperationException("Method does not return Task<TResult>.");
    }
}