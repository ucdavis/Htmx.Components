using System.Globalization;
using System.Text.RegularExpressions;

namespace Htmx.Components.Extensions;

/// <summary>
/// Provides extension methods for string manipulation and formatting in the HTMX Components library.
/// </summary>
/// <remarks>
/// These extensions provide utilities for HTML attribute formatting, template processing,
/// and type-specific string conversion that are commonly needed in web component scenarios.
/// </remarks>
public static class StringExtensions
{
    /// <summary>
    /// Sanitizes a string to be safe for use as an HTML element ID or CSS class name.
    /// </summary>
    /// <param name="input">The string to sanitize.</param>
    /// <returns>A sanitized string safe for use in HTML attributes.</returns>
    /// <remarks>
    /// This method replaces any character that is not a letter, digit, hyphen, underscore,
    /// or colon with an underscore. This ensures the resulting string can be safely used
    /// as an HTML ID or CSS class name according to HTML specifications.
    /// </remarks>
    /// <example>
    /// <code>
    /// string unsafeId = "user name!@#";
    /// string safeId = unsafeId.SanitizeForHtmlId(); // Returns "user_name___"
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is null.</exception>
    public static string SanitizeForHtmlId(this string input)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        
        // Replace invalid characters with underscores
        return string.Concat(input.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == ':' ? c : '_'));
    }

    /// <summary>
    /// Formats a Serilog-style message template with a single parameter and additional parameters.
    /// </summary>
    /// <param name="messageTemplate">The message template with placeholder markers.</param>
    /// <param name="parameter">The first parameter to substitute.</param>
    /// <param name="additionalParameters">Additional parameters to substitute in order.</param>
    /// <returns>The formatted message with parameters substituted.</returns>
    /// <remarks>
    /// This is a simplified template formatting utility that handles basic placeholder
    /// substitution. It expects the number of parameters to match the number of placeholders
    /// in the template.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when the number of parameters doesn't match the number of template placeholders.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="messageTemplate"/> is null.</exception>
    internal static string FormatTemplate(this string messageTemplate, object parameter, params object[] additionalParameters)
    {
        if (messageTemplate == null) throw new ArgumentNullException(nameof(messageTemplate));
        
        return FormatTemplate(messageTemplate, new[] { parameter }.Concat(additionalParameters));
    }

    /// <summary>
    /// Formats a Serilog-style message template with the provided parameters.
    /// </summary>
    /// <param name="messageTemplate">The message template with placeholder markers.</param>
    /// <param name="parameters">The parameters to substitute in the template.</param>
    /// <returns>The formatted message with parameters substituted.</returns>
    /// <remarks>
    /// This method provides basic template formatting by replacing placeholder patterns
    /// with the corresponding parameter values. It's designed for simple logging scenarios
    /// and doesn't support the full feature set of Serilog's template processing.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when the number of parameters doesn't match the number of template placeholders.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="messageTemplate"/> or <paramref name="parameters"/> is null.</exception>
    internal static string FormatTemplate(this string messageTemplate, IEnumerable<object> parameters)
    {
        if (messageTemplate == null) throw new ArgumentNullException(nameof(messageTemplate));
        if (parameters == null) throw new ArgumentNullException(nameof(parameters));
        
        var objects = parameters as object[] ?? parameters.ToArray();

        if (objects.Length == 0)
        {
            return messageTemplate;
        }

        if (objects.Length != Regex.Matches(messageTemplate, "{.*?}").Count)
        {
            throw new ArgumentException("Number of arguments does not match number of template parameters");
        }

        var i = 0;
        return Regex.Replace(messageTemplate, "{.*?}", _ => objects[i++]?.ToString() ?? string.Empty);
    }

    /// <summary>
    /// Converts a value to its string representation suitable for HTML input elements.
    /// </summary>
    /// <typeparam name="T">The type of the value to convert.</typeparam>
    /// <param name="value">The value to convert to a string.</param>
    /// <returns>A string representation of the value formatted for HTML inputs.</returns>
    /// <remarks>
    /// <para>
    /// This method provides type-specific formatting for common .NET types to ensure
    /// they are represented correctly in HTML input elements. It handles:
    /// </para>
    /// <list type="bullet">
    /// <item><description>DateTime values in ISO format</description></item>
    /// <item><description>DateTimeOffset values in date format</description></item>
    /// <item><description>TimeSpan values in time format</description></item>
    /// <item><description>Boolean values as "true"/"false"</description></item>
    /// <item><description>Enum values as string representations</description></item>
    /// <item><description>Numeric values using invariant culture</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// DateTime date = new DateTime(2023, 12, 25);
    /// string dateStr = date.ConvertToInputString(); // Returns "2023-12-25T00:00:00"
    /// 
    /// bool flag = true;
    /// string flagStr = flag.ConvertToInputString(); // Returns "true"
    /// </code>
    /// </example>
    public static string ConvertToInputString<T>(this T value)
    {
        if (value == null)
            return string.Empty;

        var type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        // If T is object, use the runtime type of value
        if (type == typeof(object))
            type = value.GetType();

        if (type == typeof(DateTime))
            return ((DateTime)(object)value).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        if (type == typeof(DateTimeOffset))
            return ((DateTimeOffset)(object)value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (type == typeof(TimeSpan))
            return ((TimeSpan)(object)value).ToString("HH\\:mm\\:ss", CultureInfo.InvariantCulture);
        if (type == typeof(Guid))
            return ((Guid)(object)value).ToString();
        if (type == typeof(DateOnly))
            return ((DateOnly)(object)value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (type == typeof(TimeOnly))
            return ((TimeOnly)(object)value).ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        if (type == typeof(bool))
            return ((bool)(object)value) ? "true" : "false";
        if (type.IsEnum)
            return ((Enum)(object)value).ToString();

        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }
}