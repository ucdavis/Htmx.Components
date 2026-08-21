# Htmx.Components

[![NuGet](https://img.shields.io/nuget/v/Htmx.Components.svg)](https://www.nuget.org/packages/Htmx.Components/)
[![Documentation](https://img.shields.io/badge/docs-latest-blue.svg)](https://ucdavis.github.io/Htmx.Components/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A comprehensive ASP.NET Core library for building interactive web applications with **server-side rendering** and **HTMX integration**. Get dynamic UIs with minimal JavaScript through reusable components, state management, and built-in authorization.

## ✨ What You Get

- 🚀 **Ready-to-use components**: Tables, navigation, auth status, forms
- 🔧 **HTMX integration**: Scoped out-of-band updates, multi-swap responses, page state
- 🛡️ **Authorization system**: Resource-based permissions with ASP.NET Core integration  
- 📱 **Responsive design**: Built for DaisyUI/Tailwind CSS
- ⚡ **Automatic partial updates**: Smart filters and a packaged browser runtime handle HTMX updates behind the scenes

## 🚀 Quick Example

```csharp
// Program.cs - Minimal setup
builder.Services.AddHtmxComponents();
builder.Services.AddControllersWithViews()
    .AddHtmxComponentsApplicationPart();

app.UseHtmxPageState();
app.UseAuthentication();
app.UseAuthorization();
```

```csharp
// Controllers - Attribute-based navigation and tables
[NavActionGroup(DisplayName = "Admin")]
public class AdminController : Controller
{
    [NavAction(DisplayName = "Users", Icon = "fas fa-users")]
    public async Task<IActionResult> Users()
    {
        var tableModel = await _modelHandler.BuildTableModelAsync();
        // Note that we are returning an ObjectResult here rather than a ViewResult.
        // Htmx.Components makes use of result filters and action context to determine whether to return
        // a ViewResult or a htmx-specific MultiSwapViewResult. For htmx requests, filters determine
        // what partial views and models go into building the response.
        return Ok(tableModel);
    }
}
```

```html
<!-- Layout - Components just work -->
@await Component.InvokeAsync("NavBar")
@await Component.InvokeAsync("AuthStatus")
@await Component.InvokeAsync("Table", Model)

<htmx-runtime></htmx-runtime> <!-- Packaged runtime behaviors -->
<htmx-page-state></htmx-page-state> <!-- State management -->
```

**Result**: Dynamic navigation, interactive tables, auth integration - all with server-side rendering.

## 📖 Documentation

| Guide | Description |
|-------|-------------|
| **[🚀 Getting Started](https://ucdavis.github.io/Htmx.Components/articles/getting-started.html)** | Installation, setup, and first working example |
| **[👤 User Guide](https://ucdavis.github.io/Htmx.Components/articles/user-guide/basic-usage.html)** | Usage patterns, components, and examples |  
| **[🔧 Developer Guide](https://ucdavis.github.io/Htmx.Components/articles/developer-guide/architecture.html)** | Architecture, extension points, and customization |
| **[📚 API Reference](https://ucdavis.github.io/Htmx.Components/api/)** | Complete API documentation |

## 💻 Requirements

- .NET 10.0+
- ASP.NET Core
- HTMX 2.0+

## 🤝 Contributing

Contributions welcome! See our [Contributing Guide](https://ucdavis.github.io/Htmx.Components/articles/developer-guide/contributing.html) for development setup and guidelines.

## 📄 License

[MIT License](LICENSE) - see LICENSE file for details.
