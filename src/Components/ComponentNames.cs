namespace Htmx.Components;

/// <summary>
/// Provides constant names for built-in view components in the HTMX Components library.
/// </summary>
/// <remarks>
/// These constants are used throughout the library for component identification,
/// out-of-band targeting, and view resolution. Applications can reference these
/// constants when working with component-specific functionality.
/// </remarks>
public static class ComponentNames
{
    /// <summary>
    /// The name of the authentication status view component.
    /// </summary>
    public const string AuthStatus = "AuthStatus";

    /// <summary>
    /// The name of the navigation bar view component.
    /// </summary>
    public const string NavBar = "NavBar";

    /// <summary>
    /// The name of the table view component.
    /// </summary>
    public const string Table = "Table";
}