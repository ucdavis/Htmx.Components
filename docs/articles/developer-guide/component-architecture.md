# Component Architecture and Self-Contained Structure

This guide covers the technical details of the component architecture in Htmx.Components. For setup and basic usage, see the **[Getting Started Guide](../getting-started.md)** and **[User Guide](../user-guide/basic-usage.md)**.

Htmx.Components uses a self-contained component architecture where each ViewComponent keeps all its related files co-located for better maintainability and discoverability.

## Self-Contained Component Structure

Each component is organized with all related files in a single folder:

```
src/Components/
├── AuthStatus/
│   ├── AuthStatusViewComponent.cs
│   ├── AuthStatusViewModel.cs
│   ├── IAuthStatusProvider.cs
│   ├── DefaultAuthStatusProvider.cs
│   ├── AuthStatusUpdateAttribute.cs      ← Component-specific attribute
│   ├── Internal/
│   │   └── AuthStatusUpdateFilter.cs    ← Internal infrastructure
│   └── Views/
│       └── Default.cshtml
├── Table/
│   ├── TableViewComponent.cs
│   ├── TableProvider.cs
│   ├── TableActionAttributes.cs          ← Component-specific attributes
│   ├── Internal/
│   │   ├── TableOobEditFilter.cs        ← Internal infrastructure
│   │   └── TableOobRefreshFilter.cs     ← Internal infrastructure
│   └── Views/
│       ├── _Table.cshtml
│       ├── _TableBody.cshtml
│       └── ... (other table partials)
└── NavBar/
    ├── NavBarViewComponent.cs
    ├── AttributeNavProvider.cs
    ├── NavActionAttribute.cs             ← Component-specific attributes
    ├── Internal/
    │   └── NavActionResultFilter.cs      ← Internal infrastructure
    └── Views/
        └── Default.cshtml
```

## Benefits of Self-Contained Structure

- **Cohesion**: All files related to a component are in one place
- **Discoverability**: Easy to find all aspects of a component
- **Maintainability**: Changes to a component are localized
- **Reusability**: Components can be easily copied or extracted
- **Component-specific attributes**: Attributes are co-located with their components
- **Internal infrastructure**: Component-specific filters and internal types are organized in Internal subfolders

## Component-Specific Namespaces

With the self-contained structure, component-specific attributes now use their component's namespace:

### AuthStatus Component
```csharp
using Htmx.Components.AuthStatus;

[HttpPost]
[AuthStatusUpdate]  // Now in AuthStatus namespace
public IActionResult Login() => Ok();
```

### Navigation Component
```csharp
using Htmx.Components.NavBar;

[NavActionGroup(DisplayName = "Admin", Icon = "fas fa-cogs")]
public class AdminController : Controller
{
    [NavAction(DisplayName = "Users", Icon = "fas fa-users")]
    public IActionResult Users() => View();
}
```

### Table Component
Internal table attributes and filters remain in the Table namespace:
```csharp
using Htmx.Components.Table;
using Htmx.Components.Table.Internal;

// Internal attributes used by the framework
[TableEditAction]      // In Table namespace
[TableRefreshAction]   // In Table namespace

// Internal filters (not used directly by consumers)
public class TableOobEditFilter      // In Table.Internal namespace
public class TableOobRefreshFilter   // In Table.Internal namespace
```

### General Attributes
Non-component-specific attributes remain in the general namespace:
```csharp
using Htmx.Components.Attributes;

[ModelConfig("users")]
private void ConfigureUserModel(ModelHandlerBuilder<User, int> builder)
{
    // Configuration logic
}
```

## View Location Discovery

The self-contained structure is enabled by a custom [`ComponentViewLocationExpander`](../../api/Htmx.Components.Configuration.ComponentViewLocationExpander.html) that automatically looks for views in component folders:

- `/src/Components/{ComponentName}/Views/{ViewName}.cshtml`
- `/src/Components/{ComponentName}/Views/Shared/{ViewName}.cshtml`

This works alongside the traditional view locations, so existing components continue to work without changes.

## Internal Infrastructure Pattern

Component-specific internal types (filters, utilities, etc.) are organized in `Internal` subfolders with corresponding `.Internal` namespaces:

- **Namespace**: `Htmx.Components.{ComponentName}.Internal`
- **Location**: `/src/Components/{ComponentName}/Internal/`
- **Purpose**: Framework infrastructure not intended for direct consumer use
- **Examples**: Result filters, internal attributes, component-specific utilities

## Creating New Components

When creating new components, follow the self-contained pattern:

1. Create a folder under `/src/Components/`
2. Place the ViewComponent class and related files in the folder
3. Create a `Views/` subfolder for Razor views
4. Place component-specific attributes in the same folder
5. Use the component's namespace for attributes

This pattern ensures your components are well-organized and easy to maintain.

## Related Documentation

- **[Design Choices](design-choices.md)**: Understand the rationale behind the self-contained component architecture
- **[Architecture Overview](architecture.md)**: Complete architectural overview including component integration
- **[Future Directions](future-directions.md)**: Potential enhancements and extension patterns
