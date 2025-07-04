using System.Linq.Expressions;

namespace Htmx.Components.Extensions;

/// <summary>
/// Provides extension methods for working with LINQ expressions in the HTMX Components library.
/// </summary>
/// <remarks>
/// These extensions simplify common expression manipulation tasks such as extracting
/// property names and types from lambda expressions used in table column definitions
/// and input model building.
/// </remarks>
public static class ExpressionExtensions
{
    /// <summary>
    /// Extracts the property name from an expression like x => x.Property
    /// </summary>
    /// <typeparam name="T">The type containing the property.</typeparam>
    /// <param name="expression">The lambda expression that accesses a property.</param>
    /// <returns>The name of the property referenced in the expression.</returns>
    /// <remarks>
    /// This method supports both direct member access (x => x.Property) and
    /// member access with type conversion (x => (object)x.Property).
    /// </remarks>
    /// <example>
    /// <code>
    /// Expression&lt;Func&lt;User, object&gt;&gt; expr = u => u.Name;
    /// string propertyName = expr.GetPropertyName(); // Returns "Name"
    /// </code>
    /// </example>
    /// <exception cref="ArgumentException">Thrown when the expression is not a member access expression.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="expression"/> is null.</exception>
    public static string GetPropertyName<T>(this Expression<Func<T, object>> expression)
    {
        if (expression == null) throw new ArgumentNullException(nameof(expression));
        
        if (expression.Body is UnaryExpression unary && unary.Operand is MemberExpression memberUnary)
        {
            return memberUnary.Member.Name;
        }
        else if (expression.Body is MemberExpression member)
        {
            return member.Member.Name;
        }

        throw new ArgumentException("Expression must be a member access", nameof(expression));
    }

    /// <summary>
    /// Extracts the property name from a strongly-typed expression like x => x.Property
    /// </summary>
    /// <typeparam name="T">The type containing the property.</typeparam>
    /// <typeparam name="TProp">The type of the property being accessed.</typeparam>
    /// <param name="expression">The lambda expression that accesses a property.</param>
    /// <returns>The name of the property referenced in the expression.</returns>
    /// <remarks>
    /// This overload provides type safety by avoiding boxing and allows for
    /// more specific property type handling.
    /// </remarks>
    /// <example>
    /// <code>
    /// Expression&lt;Func&lt;User, string&gt;&gt; expr = u => u.Name;
    /// string propertyName = expr.GetPropertyName(); // Returns "Name"
    /// </code>
    /// </example>
    /// <exception cref="ArgumentException">Thrown when the expression is not a member access expression.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="expression"/> is null.</exception>
    public static string GetPropertyName<T, TProp>(this Expression<Func<T, TProp>> expression)
    {
        if (expression == null) throw new ArgumentNullException(nameof(expression));
        
        if (expression.Body is UnaryExpression unary && unary.Operand is MemberExpression memberUnary)
        {
            return memberUnary.Member.Name;
        }
        else if (expression.Body is MemberExpression member)
        {
            return member.Member.Name;
        }

        throw new ArgumentException("Expression must be a member access", nameof(expression));
    }

    /// <summary>
    /// Extracts the type of the property from an expression like x => x.Property
    /// </summary>
    /// <typeparam name="T">The type containing the property.</typeparam>
    /// <param name="expression">The lambda expression that accesses a property.</param>
    /// <returns>The CLR type of the property, with nullable types unwrapped to their underlying type.</returns>
    /// <remarks>
    /// This method automatically unwraps nullable types to return the underlying type.
    /// For example, if the property is of type int?, this method returns typeof(int).
    /// This is useful for type-specific processing such as input control selection
    /// or formatting operations.
    /// </remarks>
    /// <example>
    /// <code>
    /// Expression&lt;Func&lt;User, object&gt;&gt; expr = u => u.Age; // where Age is int?
    /// Type propertyType = expr.GetMemberType(); // Returns typeof(int)
    /// </code>
    /// </example>
    /// <exception cref="ArgumentException">Thrown when the expression is not a member access expression.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="expression"/> is null.</exception>
    public static Type GetMemberType<T>(this Expression<Func<T, object>> expression)
        where T : class
    {
        if (expression == null) throw new ArgumentNullException(nameof(expression));
        
        Type type;
        if (expression.Body is UnaryExpression unary && unary.Operand is MemberExpression memberUnary)
        {
            type = memberUnary.Type;
        }
        else if (expression.Body is MemberExpression member)
        {
            type = member.Type;
        }
        else
        {
            throw new ArgumentException("Expression must be a member access", nameof(expression));
        }

        // If the type is nullable, get the underlying type
        return Nullable.GetUnderlyingType(type) ?? type;
    }
}