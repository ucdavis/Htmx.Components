namespace Htmx.Components.State;

/// <summary>
/// Contains constant string keys used for page state management throughout the Htmx Components framework.
/// These constants ensure consistent naming and provide centralized management of state keys.
/// </summary>
public static class PageStateConstants
{
    /// <summary>
    /// Contains constant keys used for form-related page state management.
    /// These keys are used to store and retrieve form editing state information.
    /// </summary>
    public static class FormStateKeys
    {
        /// <summary>
        /// The partition key used to organize form-related state data.
        /// This helps separate form state from other types of page state.
        /// </summary>
        public const string Partition = "Form";
        
        /// <summary>
        /// The key used to store the item currently being edited in a form.
        /// This typically contains the model instance that the user is modifying.
        /// </summary>
        public const string EditingItem = "EditingItem";
        
        /// <summary>
        /// The key used to indicate whether the form is editing an existing record or creating a new one.
        /// This boolean flag helps determine the appropriate form behavior and validation rules.
        /// </summary>
        public const string EditingExistingRecord = "EditingExistingRecord";
    }

    /// <summary>
    /// Contains constant keys used for table-related page state management.
    /// These keys are used to store and retrieve table display and interaction state.
    /// </summary>
    public static class TableStateKeys
    {
        /// <summary>
        /// The partition key used to organize table-related state data.
        /// This helps separate table state from other types of page state.
        /// </summary>
        public const string Partition = "Table";
        
        /// <summary>
        /// The key used to store the current table state including pagination, sorting, and filtering information.
        /// This maintains the user's table interaction preferences across requests.
        /// </summary>
        public const string TableState = "TableState";
    }
}