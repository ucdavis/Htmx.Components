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
    [HttpGet("Products")]
    [NavAction(DisplayName = "Products", Icon = "fas fa-boxes-stacked", Order = 0, PushUrl = true, ViewName = "_Products")]
    public IActionResult Products()
    {
        // Your logic here
        return Ok(new { });
    }
    
    [HttpGet("Users")]
    [NavAction(DisplayName = "Users", Icon = "fas fa-users-cog", Order = 1, PushUrl = true, ViewName = "_Users")]
    public async Task<IActionResult> Users()
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

## Example Groups

### Home Controller
```csharp
public class HomeController : Controller
{
    [NavAction(DisplayName = "Home", Icon = "fas fa-home", Order = 0, PushUrl = true, ViewName = "_Content")]
    public IActionResult Index()
    {
        return Ok(new { });
    }
}
```

### Catalog Controller Group
```csharp
[NavActionGroup(DisplayName = "Catalog", Icon = "fas fa-boxes-stacked", Order = 1)]
public class CatalogController : Controller
{
    [NavAction(DisplayName = "Products", Icon = "fas fa-box", Order = 1, PushUrl = true, ViewName = "_Products")]
    public IActionResult Products()
    {
        var tableModel = new { };
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
    [HttpGet("Products")]
    [NavAction(DisplayName = "Products", Icon = "fas fa-box", Order = 0, PushUrl = true, ViewName = "_Products")]
    public async Task<IActionResult> Products()
    {
        var modelHandler = await _modelHandlerFactory.Get<Product, int>(nameof(Product), ModelUI.Table);
        var tableModel = await modelHandler.BuildTableModelAndFetchPageAsync();
        return Ok(tableModel);
    }

    [HttpGet("Users")]
    [Authorize(Policy = "CanManageUsers")]
    [NavAction(DisplayName = "Users", Icon = "fas fa-users-cog", Order = 1, PushUrl = true, ViewName = "_Users")]
    public async Task<IActionResult> Users()
    {
        var modelHandler = await _modelHandlerFactory.Get<UserSummary, int>(nameof(UserSummary), ModelUI.Table);
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
[Authorize(Policy = "CanManageUsers")]
[NavAction(DisplayName = "Users", Icon = "fas fa-users-cog")]
public IActionResult Users() => Ok(new { });
```

Only navigation items the user is authorized to access will be displayed in the navigation menu.

## How It Works

1. The `NavBar` ViewComponent automatically discovers all controller actions marked with `[NavAction]`
2. Actions are grouped by their controller's `[NavActionGroup]` attribute (if present)
3. Navigation items are sorted by the `Order` property
4. Authorization policies are automatically applied to filter visible items
5. HTMX is used for smooth navigation between views

## Next Steps

- **[Tables](tables.md)**: Learn about implementing data tables with model handlers
- **[Authentication](authentication.md)**: Configure authentication and the AuthStatus component
- **[Authorization](authorization.md)**: Set up authorization policies and permissions
