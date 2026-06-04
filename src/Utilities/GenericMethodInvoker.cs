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
    private static readonly ConcurrentDictionary<InvocationCacheKey, Delegate> _delegateCache = new();
    private static readonly Type NullArgumentType = typeof(NullArgument);

    private static Delegate GetOrAddDelegate(
        Type targetType,
        string methodName,
        Type[] genericTypes,
        Type[] paramTypes,
        bool isStatic,
        Type? expectedReturnType = null)
    {
        var cacheKey = new InvocationCacheKey(
            targetType,
            methodName,
            genericTypes.ToArray(),
            paramTypes.ToArray(),
            isStatic,
            expectedReturnType);

        return _delegateCache.GetOrAdd(cacheKey, _ =>
        {
            var method = ResolveMethod(targetType, methodName, genericTypes, paramTypes, isStatic, expectedReturnType);

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

    private static MethodInfo ResolveMethod(
        Type targetType,
        string methodName,
        Type[] genericTypes,
        Type[] paramTypes,
        bool isStatic,
        Type? expectedReturnType)
    {
        var flags = BindingFlags.NonPublic | BindingFlags.Public | FlattenHierarchyFor(isStatic) |
                    (isStatic ? BindingFlags.Static : BindingFlags.Instance);

        var matches = targetType
            .GetMethods(flags)
            .Where(method => method.Name == methodName)
            .Select(method => TryCloseGenericMethod(method, genericTypes))
            .Where(method => method is not null)
            .Cast<MethodInfo>()
            .Where(method => ReturnTypeMatches(method, expectedReturnType))
            .Select(method => new MethodMatch(method, GetParameterMatchScore(method.GetParameters(), paramTypes)))
            .Where(match => match.Score is not null)
            .ToArray();

        if (matches.Length == 0)
        {
            throw new InvalidOperationException(
                $"Method '{methodName}' with {genericTypes.Length} generic argument(s) and {paramTypes.Length} parameter(s) was not found on {targetType.FullName}.");
        }

        var bestScore = matches.Min(match => match.Score!.Value);
        var bestMatches = matches.Where(match => match.Score == bestScore).ToArray();

        return bestMatches.Length == 1
            ? bestMatches[0].Method
            : throw new InvalidOperationException(
                $"Method '{methodName}' is ambiguous for the supplied generic argument(s) and parameter(s) on {targetType.FullName}.");
    }

    private static BindingFlags FlattenHierarchyFor(bool isStatic)
    {
        return isStatic ? BindingFlags.FlattenHierarchy : 0;
    }

    private static MethodInfo? TryCloseGenericMethod(MethodInfo method, Type[] genericTypes)
    {
        if (!method.IsGenericMethodDefinition)
        {
            return genericTypes.Length == 0 ? method : null;
        }

        if (method.GetGenericArguments().Length != genericTypes.Length)
        {
            return null;
        }

        try
        {
            return method.MakeGenericMethod(genericTypes);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static bool ReturnTypeMatches(MethodInfo method, Type? expectedReturnType)
    {
        return expectedReturnType is null ||
               expectedReturnType == typeof(object) ||
               expectedReturnType.IsAssignableFrom(method.ReturnType) ||
               method.ReturnType == typeof(void) && expectedReturnType == typeof(void);
    }

    private static int? GetParameterMatchScore(ParameterInfo[] parameters, Type[] argumentTypes)
    {
        if (parameters.Length != argumentTypes.Length)
        {
            return null;
        }

        var score = 0;
        foreach (var (parameter, argumentType) in parameters.Zip(argumentTypes))
        {
            var parameterScore = GetParameterScore(parameter.ParameterType, argumentType);
            if (parameterScore is null)
            {
                return null;
            }

            score += parameterScore.Value;
        }

        return score;
    }

    private static int? GetParameterScore(Type parameterType, Type argumentType)
    {
        if (argumentType == NullArgumentType)
        {
            return !parameterType.IsValueType || Nullable.GetUnderlyingType(parameterType) is not null
                ? 0
                : null;
        }

        if (parameterType == argumentType)
        {
            return 0;
        }

        if (!parameterType.IsAssignableFrom(argumentType))
        {
            return null;
        }

        var distance = 1;
        for (var current = argumentType.BaseType; current is not null; current = current.BaseType)
        {
            if (current == parameterType)
            {
                return distance;
            }

            distance++;
        }

        return distance;
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
        var paramTypes = GetParameterTypes(parameters);
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
        var paramTypes = GetParameterTypes(parameters);
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

    private static Type[] GetParameterTypes(object?[] parameters)
    {
        return parameters.Select(parameter => parameter?.GetType() ?? NullArgumentType).ToArray();
    }

    private sealed class NullArgument;

    private readonly record struct MethodMatch(MethodInfo Method, int? Score);

    private readonly record struct InvocationCacheKey(
        Type TargetType,
        string MethodName,
        Type[] GenericTypes,
        Type[] ParameterTypes,
        bool IsStatic,
        Type? ExpectedReturnType)
    {
        public bool Equals(InvocationCacheKey other)
        {
            return TargetType == other.TargetType &&
                   MethodName == other.MethodName &&
                   GenericTypes.SequenceEqual(other.GenericTypes) &&
                   ParameterTypes.SequenceEqual(other.ParameterTypes) &&
                   IsStatic == other.IsStatic &&
                   ExpectedReturnType == other.ExpectedReturnType;
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(TargetType);
            hash.Add(MethodName);
            foreach (var genericType in GenericTypes)
            {
                hash.Add(genericType);
            }

            foreach (var parameterType in ParameterTypes)
            {
                hash.Add(parameterType);
            }

            hash.Add(IsStatic);
            hash.Add(ExpectedReturnType);
            return hash.ToHashCode();
        }
    }
}
