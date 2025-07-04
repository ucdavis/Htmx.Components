# Navigation

Htmx.Components provides a flexible navigation system based on controller and action attributes that automatically builds navigation menus.

## Attribute-Based Navigation

The primary way to create navigation is using attributes on your controllers and actions.

### NavAction Attribute

Mark controller actions that should appear in navigation:

```csharp
using Htmx.Components.NavBar;

[NavAction(DisplayName = "Dashboard", Icon = "fas fa-tachometer-alt", Order = 0, PushUrl = true, ViewName = "_Content")]
public IActionResult Index()
{
    return Ok(new { });
}
```

### NavActionGroup Attribute

Group related actions together:

```csharp
[Route("Admin")]
[NavActionGroup(DisplayName = "Admin", Icon = "fas fa-cogs", Order = 2)]
public class AdminController : Controller
{
    [HttpGet("Repos")]
    [NavAction(DisplayName = "Repos", Icon = "fas fa-database", Order = 0, PushUrl = true, ViewName = "_Repos")]
    public async Task<IActionResult> Repos()
    {
        // Your logic here
        return Ok(new { });
    }
    
    [HttpGet("AdminUsers")]
    [NavAction(DisplayName = "Admin Users", Icon = "fas fa-users-cog", Order = 1, PushUrl = true, ViewName = "_AdminUsers")]
    public async Task<IActionResult> AdminUsers()
    {
        // Your logic here
        return Ok(new { });
    }
}
```

### Attribute Properties

| Property | Description | Example |
|----------|-------------|---------|
| `DisplayName` | Text shown in navigation | "Dashboard" |
| `Icon` | CSS class for icon (FontAwesome) | "fas fa-home" |
| `Order` | Sort order within group | 0, 1, 2... |
| `PushUrl` | Whether to update browser URL | true/false |
| `ViewName` | View to render for the action | "_Content" |

## Real-World Examples

Here are actual examples from CruSibyl.Web:

### Dashboard Controller
```csharp
public class DashboardController : Controller
{
    [NavAction(DisplayName = "Dashboard", Icon = "fas fa-tachometer-alt", Order = 0, PushUrl = true, ViewName = "_Content")]
    public IActionResult Index()
    {
        return Ok(new { });
    }
}
```

### Reports Controller Group
```csharp
[NavActionGroup(DisplayName = "Reports", Icon = "fas fa-chart-bar", Order = 1)]
public class ReportsController : Controller
{
    [NavAction(Icon = "fas fa-database", Order = 1, PushUrl = true, ViewName = "_PackageVersions")]
    public async Task<IActionResult> PackageVersions()
    {
        // Report logic here
        return Ok(tableModel);
    }
}
```

### Admin Controller Group
```csharp
[Route("Admin")]
[NavActionGroup(DisplayName = "Admin", Icon = "fas fa-cogs", Order = 2)]
public class AdminController : Controller
{
    [HttpGet("Repos")]
    [NavAction(DisplayName = "Repos", Icon = "fas fa-database", Order = 0, PushUrl = true, ViewName = "_Repos")]
    public async Task<IActionResult> Repos()
    {
        var modelHandler = await _modelHandlerFactory.Get<Repo, int>(nameof(Repo), ModelUI.Table);
        var tableModel = await modelHandler.BuildTableModelAndFetchPageAsync();
        return Ok(tableModel);
    }

    [HttpGet("AdminUsers")]
    [Authorize(Policy = AccessPolicies.SystemAccess)]
    [NavAction(DisplayName = "Admin Users", Icon = "fas fa-users-cog", Order = 1, PushUrl = true, ViewName = "_AdminUsers")]
    public async Task<IActionResult> AdminUsers()
    {
        var modelHandler = await _modelHandlerFactory.Get<AdminUserModel, int>(nameof(AdminUserModel), ModelUI.Table);
        var tableModel = await modelHandler.BuildTableModelAndFetchPageAsync();
        return Ok(tableModel);
    }
}
```

## Using the NavBar Component

Include the navigation in your layout:

```html
<div class="navbar-end">
    @await Component.InvokeAsync("NavBar")
    @await Component.InvokeAsync("AuthStatus")
</div>
```

## Authorization Integration

Navigation items are automatically filtered based on user permissions using standard ASP.NET Core authorization:

```csharp
[Authorize(Policy = AccessPolicies.SystemAccess)]
[NavAction(DisplayName = "Admin Users", Icon = "fas fa-users-cog")]
public IActionResult AdminUsers() => Ok(new { });
```

Only navigation items the user is authorized to access will be displayed in the navigation menu.

## How It Works

1. The `NavBar` ViewComponent automatically discovers all controller actions marked with `[NavAction]`
2. Actions are grouped by their controller's `[NavActionGroup]` attribute (if present)
3. Navigation items are sorted by the `Order` property
4. Authorization policies are automatically applied to filter visible items
5. HTMX is used for smooth navigation between views

## Best Practices

1. **Use Clear Display Names**: Choose descriptive names that users will understand
2. **Consistent Icons**: Use a consistent icon library (like FontAwesome) throughout your application
3. **Logical Ordering**: Use the `Order` property to organize navigation logically
4. **Authorization**: Always apply appropriate authorization attributes to protect sensitive actions
5. **ViewNames**: Use descriptive view names that indicate their purpose

## Next Steps

- **[Tables](tables.md)**: Learn about implementing data tables with model handlers
- **[Authentication](authentication.md)**: Configure authentication and the AuthStatus component
- **[Authorization](authorization.md)**: Set up authorization policies and permissions