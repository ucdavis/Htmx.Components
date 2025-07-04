using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using System.Reflection;

namespace Htmx.Components.Configuration;

/// <summary>
/// Expands view locations to look for ViewComponent views within their respective component folders.
/// This enables a self-contained folder structure where each ViewComponent can have its views
/// co-located with its C# code.
/// </summary>
public class ComponentViewLocationExpander : IViewLocationExpander
{
    private static readonly Lazy<HashSet<string>> _componentNames = new(ScanForViewComponents);

    public void PopulateValues(ViewLocationExpanderContext context)
    {
        // Add a key to identify this expander
        context.Values["ComponentExpander"] = "true";
    }

    public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
    {
        var locations = viewLocations.ToList();
        var viewName = context.ViewName;
        
        if (string.IsNullOrEmpty(viewName))
            return locations;

        var componentLocations = new List<string>();
        
        // Check if this is a ViewComponent request by looking at the view name pattern
        if (viewName.StartsWith("Components/"))
        {
            // Extract the component name from the view name (e.g., "Components/Table/Default" -> "Table")
            var parts = viewName.Split('/');
            if (parts.Length >= 3)
            {
                var componentName = parts[1];
                var actualViewName = parts[2];
                
                componentLocations.AddRange(new[]
                {
                    $"/src/Components/{componentName}/Views/{actualViewName}.cshtml",
                    $"/src/Components/{componentName}/Views/Shared/{actualViewName}.cshtml"
                });
            }
        }
        else
        {
            // For partial views, try all discovered ViewComponents
            var componentNames = _componentNames.Value;
            foreach (var componentName in componentNames)
            {
                componentLocations.AddRange(new[]
                {
                    $"/src/Components/{componentName}/Views/{viewName}.cshtml",
                    $"/src/Components/{componentName}/Views/Shared/{viewName}.cshtml"
                });
            }
        }
        
        // Insert component locations at the beginning for highest priority
        if (componentLocations.Any())
        {
            locations.InsertRange(0, componentLocations);
        }
        
        return locations;
    }
    
    private static HashSet<string> ScanForViewComponents()
    {
        var componentNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        // Get the current assembly (Htmx.Components)
        var assembly = Assembly.GetExecutingAssembly();
        
        // Find all types that inherit from ViewComponent
        var viewComponentTypes = assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && IsViewComponent(type));
        
        foreach (var type in viewComponentTypes)
        {
            // Extract component name by removing "ViewComponent" suffix
            var componentName = type.Name;
            if (componentName.EndsWith("ViewComponent", StringComparison.OrdinalIgnoreCase))
            {
                componentName = componentName.Substring(0, componentName.Length - "ViewComponent".Length);
            }
            
            if (!string.IsNullOrEmpty(componentName))
            {
                componentNames.Add(componentName);
            }
        }
        
        return componentNames;
    }
    
    private static bool IsViewComponent(Type type)
    {
        // Check if the type inherits from ViewComponent
        var baseType = type.BaseType;
        while (baseType != null)
        {
            if (baseType.Name == "ViewComponent" && baseType.Namespace == "Microsoft.AspNetCore.Mvc")
            {
                return true;
            }
            baseType = baseType.BaseType;
        }
        
        // Also check for IViewComponent interface (though ViewComponent base class is more common)
        return type.GetInterfaces().Any(i => i.Name == "IViewComponent");
    }
}
