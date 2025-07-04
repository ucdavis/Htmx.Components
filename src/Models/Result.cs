using System.Runtime.CompilerServices;
using Htmx.Components.Extensions;
using Serilog;
using Serilog.Context;
using Serilog.Events;

namespace Htmx.Components.Models;

/// <summary>
/// Represents the result of an operation that returns a value, with optional success/error status and message.
/// This generic result type extends the base Result class to include a typed value.
/// </summary>
/// <typeparam name="T">The type of the value returned by the operation</typeparam>
public class Result<T> : Result
{
    /// <summary>
    /// Gets the value returned by the operation.
    /// This value may be null or default if the operation failed.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Initializes a new instance of the Result class with the specified value and optional message.
    /// </summary>
    /// <param name="value">The value returned by the operation</param>
    /// <param name="message">An optional message describing the result</param>
    public Result(T value, string message = "") : base(message)
    {
        Value = value;
    }

    /// <summary>
    /// Implicitly converts a ResultError to a failed Result&lt;T&gt;.
    /// This allows for easy creation of error results without explicitly constructing the Result&lt;T&gt; instance.
    /// </summary>
    /// <param name="error">The error information to convert to a failed result</param>
    /// <returns>A failed Result&lt;T&gt; with the error message</returns>
    public static implicit operator Result<T>(Result.ResultError error) => new Result<T>(default!) { Message = error.ErrorMessage, IsError = true };
}

/// <summary>
/// Represents the result of an operation with success/error status and an optional message.
/// This class provides a way to return both success and error states with descriptive messages,
/// along with structured logging capabilities for error scenarios.
/// </summary>
/// <remarks>
/// This design helps avoid exceptions for expected error conditions and provides better
/// type inference compared to always using the generic Result&lt;T&gt; class.
/// </remarks>
public class Result
{
    /// <summary>
    /// Gets a value indicating whether this result represents an error condition.
    /// </summary>
    public bool IsError { get; protected set; }
    
    /// <summary>
    /// Gets the message associated with this result, which may describe success or error details.
    /// </summary>
    public string Message { get; protected set; } = "";

    /// <summary>
    /// Initializes a new instance of the Result class with the specified message.
    /// </summary>
    /// <param name="message">The message describing the result</param>
    public Result(string message)
    {
        Message = message;
    }

    /// <summary>
    /// Implicitly converts a ResultError to a failed Result.
    /// This allows for easy creation of error results without explicitly constructing the Result instance.
    /// </summary>
    /// <param name="error">The error information to convert to a failed result</param>
    /// <returns>A failed Result with the error message</returns>
    public static implicit operator Result(ResultError error) => new Result(error.ErrorMessage) { IsError = true };

    /// <summary>
    /// Creates an error result with structured logging capabilities.
    /// Logs the error message at the specified level and captures caller information for debugging.
    /// </summary>
    /// <param name="messageTemplate">The message template for structured logging</param>
    /// <param name="logLevel">The log level for the error (defaults to Error)</param>
    /// <param name="callerFilePath">The file path of the caller (automatically captured)</param>
    /// <param name="callerLineNumber">The line number of the caller (automatically captured)</param>
    /// <returns>A ResultError that can be implicitly converted to a failed Result</returns>
    public static ResultError Error(string messageTemplate,
        LogEventLevel logLevel = LogEventLevel.Error,
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        using var _ = LogContext.PushProperty("FileName", Path.GetFileName(callerFilePath));
        LogContext.PushProperty("LineNumber", callerLineNumber);
        Log.Write(logLevel, messageTemplate);
        return new ResultError(messageTemplate);
    }

    /// <summary>
    /// Creates an error result with structured logging capabilities and three template parameters.
    /// Logs the error message at the specified level and captures caller information for debugging.
    /// </summary>
    /// <param name="messageTemplate">The message template for structured logging with placeholders</param>
    /// <param name="prop0">The first property value for the message template</param>
    /// <param name="prop1">The second property value for the message template</param>
    /// <param name="prop2">The third property value for the message template</param>
    /// <param name="logLevel">The log level for the error (defaults to Error)</param>
    /// <param name="callerFilePath">The file path of the caller (automatically captured)</param>
    /// <param name="callerLineNumber">The line number of the caller (automatically captured)</param>
    /// <returns>A ResultError that can be implicitly converted to a failed Result</returns>
    public static ResultError Error(string messageTemplate, object prop0, object prop1, object prop2,
        LogEventLevel logLevel = LogEventLevel.Error,
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        using var _ = LogContext.PushProperty("FileName", Path.GetFileName(callerFilePath));
        LogContext.PushProperty("LineNumber", callerLineNumber);
        Log.Write(logLevel, messageTemplate, prop0, prop1, prop2);
        return new ResultError(messageTemplate.FormatTemplate(prop0, prop1, prop2));
    }

    /// <summary>
    /// Creates an error result with structured logging capabilities and two template parameters.
    /// Logs the error message at the specified level and captures caller information for debugging.
    /// </summary>
    /// <param name="messageTemplate">The message template for structured logging with placeholders</param>
    /// <param name="prop0">The first property value for the message template</param>
    /// <param name="prop1">The second property value for the message template</param>
    /// <param name="logLevel">The log level for the error (defaults to Error)</param>
    /// <param name="callerFilePath">The file path of the caller (automatically captured)</param>
    /// <param name="callerLineNumber">The line number of the caller (automatically captured)</param>
    /// <returns>A ResultError that can be implicitly converted to a failed Result</returns>
    public static ResultError Error(string messageTemplate, object prop0, object prop1,
        LogEventLevel logLevel = LogEventLevel.Error,
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        using var _ = LogContext.PushProperty("FileName", Path.GetFileName(callerFilePath));
        LogContext.PushProperty("LineNumber", callerLineNumber);
        Log.Write(logLevel, messageTemplate, prop0, prop1);
        return new ResultError(messageTemplate.FormatTemplate(prop0, prop1));
    }

    /// <summary>
    /// Creates an error result with structured logging capabilities and one template parameter.
    /// Logs the error message at the specified level and captures caller information for debugging.
    /// </summary>
    /// <param name="messageTemplate">The message template for structured logging with placeholders</param>
    /// <param name="prop0">The property value for the message template</param>
    /// <param name="logLevel">The log level for the error (defaults to Error)</param>
    /// <param name="callerFilePath">The file path of the caller (automatically captured)</param>
    /// <param name="callerLineNumber">The line number of the caller (automatically captured)</param>
    /// <returns>A ResultError that can be implicitly converted to a failed Result</returns>
    public static ResultError Error(string messageTemplate, object prop0,
        LogEventLevel logLevel = LogEventLevel.Error,
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        using var _ = LogContext.PushProperty("FileName", Path.GetFileName(callerFilePath));
        LogContext.PushProperty("LineNumber", callerLineNumber);
        Log.Write(logLevel, messageTemplate, prop0);
        return new ResultError(messageTemplate.FormatTemplate(prop0));
    }

    /// <summary>
    /// Creates a successful result with a typed value and optional message.
    /// This method provides type inference to avoid having to specify the generic type parameter explicitly.
    /// </summary>
    /// <typeparam name="T">The type of the value (inferred from the parameter)</typeparam>
    /// <param name="value">The value to wrap in a successful result</param>
    /// <param name="message">An optional message describing the successful result</param>
    /// <returns>A successful Result&lt;T&gt; containing the specified value</returns>
    public static Result<T> Value<T>(T value, string message = "") => new Result<T>(value, message);

    /// <summary>
    /// Creates a successful result with an optional message.
    /// </summary>
    /// <param name="message">An optional message describing the successful result</param>
    /// <returns>A successful Result with the specified message</returns>
    public static Result Ok(string message = "") => new Result(message);

    /// <summary>
    /// Represents error information that can be implicitly converted to a failed Result.
    /// This class encapsulates error messages for structured error handling.
    /// </summary>
    public class ResultError
    {
        /// <summary>
        /// Gets the error message describing what went wrong.
        /// </summary>
        public string ErrorMessage { get; }
        
        /// <summary>
        /// Initializes a new instance of the ResultError class with the specified error message.
        /// </summary>
        /// <param name="errorMessage">The error message describing what went wrong</param>
        public ResultError(string errorMessage) => ErrorMessage = errorMessage;
    }

}