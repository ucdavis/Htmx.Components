namespace Htmx.Components.Authorization;

/// <summary>
/// Provides constant values for common authorization operations and resources used throughout
/// the HTMX Components library.
/// </summary>
public static class AuthConstants
{
    /// <summary>
    /// Defines standard CRUD operation names for authorization requirements.
    /// </summary>
    /// <remarks>
    /// These constants are used with <see cref="IAuthorizationRequirementFactory.ForOperation"/>
    /// to create authorization requirements for data access operations.
    /// </remarks>
    public static class CrudOperations
    {
        /// <summary>
        /// The "Read" operation for reading/viewing data.
        /// </summary>
        public const string Read = "Read";

        /// <summary>
        /// The "Create" operation for creating new data.
        /// </summary>
        public const string Create = "Create";

        /// <summary>
        /// The "Update" operation for modifying existing data.
        /// </summary>
        public const string Update = "Update";

        /// <summary>
        /// The "Delete" operation for removing data.
        /// </summary>
        public const string Delete = "Delete";
    }
}
