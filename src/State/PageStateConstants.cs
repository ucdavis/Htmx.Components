namespace Htmx.Components.State;

public static class PageStateConstants
{
    public static class FormStateKeys
    {
        public const string Partition = "Form";
        public const string EditingItem = "EditingItem";
        public const string EditingExistingRecord = "EditingExistingRecord";
    }

    public static class TableStateKeys
    {
        public const string Partition = "Table";
        public const string TableState = "TableState";
    }
}