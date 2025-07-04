# Htmx.Components

A comprehensive ASP.NET Core library that provides HTMX-enabled UI components for building interactive web applications with server-side rendering.

## Features

### 🚀 Core Components
- **Table Component**: Interactive data tables with pagination, sorting, filtering, and CRUD operations
- **Navigation Bar**: Attribute-based or builder-pattern navigation with authorization support
- **Authentication Status**: Configurable authentication UI with profile display
- **Form Controls**: HTMX-enabled input components with validation support

### 🔧 HTMX Integration
- **Multi-Swap View Results**: Return multiple HTMX views in a single response
- **Out-of-Band Updates**: Automatic injection of component updates
- **Page State Management**: Encrypted client-side state with server-side validation
- **Authentication Retry**: Seamless re-authentication for expired sessions
- **JavaScript Behaviors**: Dynamically generated JavaScript behaviors via TagHelper system
- **HTMX Extensions**: Built-in table editing and coordination behaviors

### 🛡️ Authorization & Security
- **Resource-based Authorization**: Fine-grained permissions with custom requirements
- **Role-based Access Control**: Configurable role services
- **Secure Page State**: Encrypted state management with data protection

### 📱 Responsive Design
- **DaisyUI Integration**: Beautiful, responsive components out of the box
- **Tailwind CSS Support**: Utility-first styling approach
- **Mobile-friendly**: Touch-optimized interactions

## Quick Start

### 1. Installation

```bash
dotnet add package Htmx.Components
```

### 2. Configuration

```csharp
// Program.cs
builder.Services.AddHtmxComponents(options =>
{
    options.WithModelHandlerRegistry((registry, serviceProvider) =>
    {
        // Register your model handlers
        ModelHandlerAttributeRegistrar.RegisterAll(registry);
    });
    
    options.WithAuthorizationRequirementFactory<YourPermissionFactory>();
    options.WithResourceOperationRegistry<YourResourceRegistry>();
    options.WithUserIdClaimType("your-claim-type");
});

builder.Services.AddControllersWithViews()
    .AddHtmxComponentsApplicationPart();

// Configure middleware
app.UseHtmxPageState();
app.UseAuthentication();
app.UseAuthorization();
```

### 3. Basic Usage

#### Table Component

```csharp
// Controller
[Route("Users")]
public class UsersController : Controller
{
    [NavAction(DisplayName = "Users", Icon = "fas fa-users")]
    public async Task<IActionResult> Index()
    {
        var tableModel = await _modelHandler.BuildTableModelAsync();
        return View(tableModel);
    }
}
```

```cshtml
<!-- View -->
@model ITableModel
@await Component.InvokeAsync("Table", Model)
```

#### Navigation with Attributes

```csharp
[NavActionGroup(DisplayName = "Administration", Order = 10)]
public class AdminController : Controller
{
    [NavAction(DisplayName = "Users", Icon = "fas fa-users", Order = 1)]
    public IActionResult Users() => View();
    
    [NavAction(DisplayName = "Settings", Icon = "fas fa-cog", Order = 2)]
    public IActionResult Settings() => View();
}
```

#### Authentication Status

```cshtml
<!-- Layout -->
<div class="navbar-end">
    @await Component.InvokeAsync("NavBar")
    @await Component.InvokeAsync("AuthStatus")
</div>
```

#### JavaScript Behaviors

Include JavaScript behaviors in your layout:

```html
<!-- Include all behaviors -->
<htmx-scripts></htmx-scripts>

<!-- Include specific behaviors only -->
<htmx-scripts include="page-state,table-behavior"></htmx-scripts>

<!-- Exclude specific behaviors -->
<htmx-scripts exclude="auth-retry"></htmx-scripts>
```

Available behaviors:
- **page-state**: Automatic page state management
- **table-behavior**: Table editing interactions
- **blur-save-coordination**: Form coordination
- **auth-retry**: Authentication retry handling

## Advanced Features

### Multi-Swap Responses

```csharp
public IActionResult UpdateData()
{
    return new MultiSwapViewResult()
        .WithMainContent("_UpdatedContent", model)
        .WithOobContent("NavBar", navigationModel)
        .WithOobContent("AuthStatus", authModel);
}
```

### Custom Authorization

```csharp
public class CustomPermissionFactory : IAuthorizationRequirementFactory
{
    public IAuthorizationRequirement ForOperation(string operation, Type resourceType)
    {
        return new CustomPermissionRequirement(operation, resourceType);
    }
}
```

### Page State Management

```csharp
public class MyController : Controller
{
    private readonly IPageState _pageState;
    
    public IActionResult SaveFilter(string filterValue)
    {
        _pageState.Set("filters", "current", filterValue);
        return Ok();
    }
    
    public IActionResult LoadFilter()
    {
        var filter = _pageState.Get<string>("filters", "current");
        return Ok(filter);
    }
}
```

### Filter Attributes

```csharp
[TableEditAction] // Automatically injects table updates
[AuthStatusUpdate] // Updates auth status on completion
public async Task<IActionResult> CreateUser(CreateUserModel model)
{
    // Your logic here
    return Ok(result);
}
```

## Architecture

The Htmx.Components library follows a **self-contained component architecture** where each ViewComponent is organized with all its related code in a single folder structure:

```
Components/
├── AuthStatus/
│   ├── Models/
│   │   └── AuthStatusViewModel.cs
│   ├── Internal/
│   │   └── AuthStatusUpdateFilter.cs
│   ├── Views/
│   ├── AuthStatusViewComponent.cs
│   ├── AuthStatusUpdateAttribute.cs
│   └── IAuthStatusProvider.cs
├── NavBar/
│   ├── Internal/
│   │   └── NavActionResultFilter.cs
│   ├── Views/
│   ├── NavBarViewComponent.cs
│   ├── NavActionAttribute.cs
│   └── AttributeNavProvider.cs
└── Table/
    ├── Models/
    │   ├── TableModel.cs
    │   ├── TableColumnModel.cs
    │   ├── TableState.cs
    │   └── ...
    ├── Internal/
    │   ├── TableOobEditFilter.cs
    │   └── TableOobRefreshFilter.cs
    ├── Views/
    ├── TableViewComponent.cs
    ├── TableActionAttributes.cs
    └── TableProvider.cs
```

### Self-Contained Components

Each component folder contains:

- **Models/**: ViewComponent-specific models and view models
- **Internal/**: Infrastructure code like filters and internal services
- **Views/**: Razor views specific to the component  
- **Attributes**: Component-specific attributes
- **Services**: Component providers and services

### Shared Infrastructure

General-purpose models that are used across multiple components remain in the main `Models/` namespace:

- `ActionModel`, `ActionSet`, `ActionGroup` - Used by NavBar, Table, and other components
- `BuilderBase`, `ActionBuilders` - Shared builder infrastructure
- `Result`, `ModelHandler` - Cross-cutting concerns

This architecture promotes:
- **High Cohesion**: Related code is co-located
- **Clear Boundaries**: Each component is self-contained
- **Reusable Infrastructure**: Shared models remain accessible
- **Maintainability**: Easy to understand and modify components

## Dependencies

- ASP.NET Core 6.0+
- HTMX 1.8+
- System.Text.Json
- Microsoft.AspNetCore.DataProtection

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Update documentation if needed (see [docs/README.md](docs/README.md) for documentation setup)
6. Submit a pull request

### Documentation

The project uses DocFX to generate documentation from C# XML comments and markdown articles. The documentation is automatically built and deployed to GitHub Pages on every push to main.

- **Local documentation development**: See [docs/README.md](docs/README.md)
- **Published documentation**: https://sweber.github.io/Htmx.Components/
- **Adding API documentation**: Add XML documentation comments to your C# code
- **Adding articles**: Create markdown files in `docs/articles/`

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

- 📖 [Documentation](https://sweber.github.io/Htmx.Components/)
- 🐛 [Issues](https://github.com/sweber/Htmx.Components/issues)
- 💬 [Discussions](https://github.com/sweber/Htmx.Components/discussions)

## Acknowledgments

- Built with [HTMX](https://htmx.org/)
- Styled with [DaisyUI](https://daisyui.com/) and [Tailwind CSS](https://tailwindcss.com/)
- Inspired by modern component-based architectures