using Htmx.Components.Table.Models;

namespace Htmx.Components.Models;

public class ViewPaths
{
    public TableViewPaths Table { get; set; } = new();
    public string NavBar { get; set; } = "Default";
    public string AuthStatus { get; set; } = "Default";
    public string Input { get; set; } = "_Input";
    public string DefaultNavContent { get; set; } = "_DefaultNavContent";
}