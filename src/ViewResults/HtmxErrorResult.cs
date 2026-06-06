using Htmx.Components.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Htmx.Components.ViewResults;

/// <summary>
/// Returns a structured, user-safe HTMX error fragment.
/// </summary>
public sealed class HtmxErrorResult : ContentResult
{
    /// <summary>
    /// Initializes a new structured HTMX error result.
    /// </summary>
    /// <param name="statusCode">The HTTP status code for the response.</param>
    /// <param name="error">The user-safe error fragment to render.</param>
    public HtmxErrorResult(int statusCode, HtmxErrorFragment error)
    {
        StatusCode = statusCode;
        ContentType = "text/html";
        Content = error.ToHtml();
    }

    /// <summary>
    /// Creates a validation error response.
    /// </summary>
    /// <param name="message">The user-safe validation message.</param>
    /// <param name="componentId">The optional component id that owns the error.</param>
    public static HtmxErrorResult Validation(string message, string? componentId = null)
        => new(StatusCodes.Status400BadRequest, new HtmxErrorFragment(
            "Check your entry",
            message,
            HtmxErrorKinds.Validation,
            componentId));

    /// <summary>
    /// Creates a CRUD operation error response.
    /// </summary>
    /// <param name="message">The user-safe operation message.</param>
    /// <param name="componentId">The optional component id that owns the error.</param>
    public static HtmxErrorResult Crud(string message, string? componentId = null)
        => new(StatusCodes.Status400BadRequest, new HtmxErrorFragment(
            "Request not completed",
            message,
            HtmxErrorKinds.Crud,
            componentId));

    /// <summary>
    /// Creates an error response for an unavailable model handler.
    /// </summary>
    /// <param name="componentId">The optional component id that owns the error.</param>
    public static HtmxErrorResult MissingHandler(string? componentId = null)
        => new(StatusCodes.Status400BadRequest, new HtmxErrorFragment(
            "Request not available",
            "This action is not available for the selected content.",
            HtmxErrorKinds.MissingHandler,
            componentId));

    /// <summary>
    /// Creates an unauthenticated error response.
    /// </summary>
    /// <param name="componentId">The optional component id that owns the error.</param>
    public static HtmxErrorResult Unauthorized(string? componentId = null)
        => new(StatusCodes.Status401Unauthorized, new HtmxErrorFragment(
            "Sign in required",
            "Please sign in and try again.",
            HtmxErrorKinds.Authorization,
            componentId));

    /// <summary>
    /// Creates a forbidden error response.
    /// </summary>
    /// <param name="componentId">The optional component id that owns the error.</param>
    public static HtmxErrorResult Forbidden(string? componentId = null)
        => new(StatusCodes.Status403Forbidden, new HtmxErrorFragment(
            "Access denied",
            "You do not have permission to perform this action.",
            HtmxErrorKinds.Authorization,
            componentId));

    /// <summary>
    /// Creates an internal server error response.
    /// </summary>
    /// <param name="componentId">The optional component id that owns the error.</param>
    public static HtmxErrorResult InternalServerError(string? componentId = null)
        => new(StatusCodes.Status500InternalServerError, new HtmxErrorFragment(
            "Something went wrong",
            "The request could not be completed. Please try again.",
            HtmxErrorKinds.General,
            componentId));
}
