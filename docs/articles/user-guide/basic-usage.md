# Basic Usage Patterns

This guide covers common usage patterns for Htmx.Components. For installation and initial setup, see the **[Getting Started Guide](../getting-started.md)**.

## Prerequisites

- Htmx.Components installed and configured (see [Getting Started](../getting-started.md))
- Basic understanding of ASP.NET Core MVC and HTMX

## Layout Integration

For setup instructions, see the **[Getting Started Guide](../getting-started.md)**. This section covers usage patterns.

### JavaScript Behaviors

The `<htmx-scripts>` TagHelper provides a unified way to include JavaScript behaviors:

```html
<!-- Include all behaviors (default) -->
<htmx-scripts></htmx-scripts>

<!-- Include only specific behaviors -->
<htmx-scripts include="page-state,table-behavior"></htmx-scripts>

<!-- Exclude specific behaviors -->
<htmx-scripts exclude="auth-retry"></htmx-scripts>
```

**Available Behaviors:**
- `page-state`: Automatic page state management for HTMX requests
- `table-behavior`: Enhanced table interactions and editing
- `blur-save-coordination`: Prevents form submission race conditions
- `auth-retry`: Handles authentication retry with popup windows

The behaviors are delivered as server-generated inline JavaScript, allowing for dynamic configuration and eliminating additional HTTP requests.

## 3. Set Up Tailwind CSS (Recommended)

Htmx.Components is designed to work with Tailwind CSS for optimal styling. Here's how to set it up using the modern CSS directives approach:

### Create Tools Directory Structure

Create a `Tools` directory in your project root with the following files:

**Tools/package.json:**
```json
{
    "name": "build",
    "version": "1.0.0",
    "main": "index.js",
    "scripts": {
        "build:css": "npx tailwindcss -i input.css -o ../wwwroot/css/site.css --minify",
        "watch:css": "npx tailwindcss -i input.css -o ../wwwroot/css/site.css --watch"
    },
    "keywords": [],
    "author": "",
    "license": "ISC",
    "description": "",
    "devDependencies": {
        "@tailwindcss/cli": "^4.0.15",
        "daisyui": "^5.0.9",
        "tailwindcss": "^4.0.15"
    },
    "dependencies": {}
}
```

**Tools/input.css:**
```css
@import "tailwindcss" source(none);
@source "../Views/**/*.{html,cshtml}";
@source "../wwwroot/**/*.{html,cshtml}";
@source "../../Htmx.Components/content/extracted-css-classes.txt";
@plugin "daisyui";
```

### Configure MSBuild Integration

Add the following to your `.csproj` file to automatically build Tailwind CSS during compilation.

```xml
  <!-- Tailwind CSS Build -->
  <PropertyGroup>
    <TailwindOutputFile>wwwroot/css/site.css</TailwindOutputFile>
    <TailwindInputFile>Tools/input.css</TailwindInputFile>
  </PropertyGroup>
  <Target Name="BuildTailwind" AfterTargets="ResolveProjectReferences" Inputs="$(TailwindInputFile)" Outputs="$(TailwindOutputFile)">
    <Exec Command="cd $(ProjectDir)Tools &amp;&amp; npm run build:css" />
    <Touch Files="$(TailwindOutputFile)" AlwaysCreate="true" />
  </Target>
```

You might also want to ensure that npm packages are installed before building Tailwind CSS. Add the following target to your `.csproj` file:

```xml
  <!-- Only run npm install when package.json has been modified or .install-stamp doesn't exist -->
  <PropertyGroup>
    <NpmInstallStampFile>Tools/node_modules/.install-stamp</NpmInstallStampFile>
  </PropertyGroup>
  <Target Name="EnsureNpmPackages" BeforeTargets="BuildTailwind" Inputs="Tools\package.json" Outputs="$(NpmInstallStampFile)">
    <Exec Command="npm install" WorkingDirectory="Tools" />
    <Touch Files="$(NpmInstallStampFile)" AlwaysCreate="true" />
  </Target>
```

### Manual Build Commands

You can also run Tailwind CSS manually:

```bash
# Navigate to Tools directory
cd Tools

# Install dependencies (first time only)
npm install

# Build CSS once
npm run build:css

# Watch for changes and rebuild automatically
npm run watch:css
```

## 4. Create Your First Controller

Create a controller with navigation attributes:

```csharp
using Htmx.Components.NavBar;
using Microsoft.AspNetCore.Mvc;

public class HomeController : Controller
{
    [NavAction(DisplayName = "Home", Icon = "fas fa-home", Order = 0, PushUrl = true, ViewName = "_Content")]
    public IActionResult Index()
    {
        return Ok(new { message = "Welcome to your app!" });
    }
}

[Route("Admin")]
[NavActionGroup(DisplayName = "Admin", Icon = "fas fa-cogs", Order = 1)]
public class AdminController : Controller
{
    [HttpGet("Users")]
    [NavAction(DisplayName = "Users", Icon = "fas fa-users", Order = 0, PushUrl = true, ViewName = "_Users")]
    public IActionResult Users()
    {
        // Your logic here
        return Ok(new { });
    }
}
```

## 5. Create Views

Create corresponding view files:

**Views/Home/_Content.cshtml:**
```html
<div id="main-content">
    <h1>Welcome to Your App</h1>
    <p>This content is loaded via HTMX!</p>
</div>
```

**Views/Admin/_Users.cshtml:**
```html
<div id="admin-users">
    <h2>User Management</h2>
    <!-- Your user management UI here -->
</div>
```

## What You Get

With this setup, you automatically get:

- **Dynamic Navigation**: The `NavBar` component automatically discovers your `[NavAction]` and `[NavActionGroup]` decorated controller actions
- **HTMX Integration**: Navigation uses HTMX for smooth page transitions without full reloads
- **Authentication Support**: The `AuthStatus` component handles login/logout states
- **Page State Management**: Browser history and URL updates work seamlessly
- **Table Components**: Ready-to-use data table functionality (see advanced guides)

## Next Steps

- **[Getting Started](../getting-started.md)** - Complete installation and configuration guide
- **[Tables](tables.md)** - Interactive data tables with CRUD operations
- **[Navigation](navigation.md)** - Advanced navigation patterns
- **[Authorization](authorization.md)** - Permission-based access control
- **[Authentication](authentication.md)** - OIDC and auth status management

For technical details and extension points, see the **[Developer Guide](../developer-guide/architecture.md)**.

## Key Features

### Automatic Navigation Registration
Controller actions marked with `[NavAction]` are automatically registered in the navigation system. Group related actions using `[NavActionGroup]`.

### ViewComponent Integration
Components like `NavBar` and `AuthStatus` integrate seamlessly with ASP.NET Core's ViewComponent system.

### HTMX Enhancement
The library provides out-of-the-box HTMX integration for dynamic content updates, form handling, and navigation.