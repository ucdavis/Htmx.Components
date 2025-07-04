# HTMX Components

A powerful .NET library that provides server-side components for building dynamic web applications with HTMX. This library simplifies the creation of interactive UIs with table management, navigation, authentication status, and form handling - all with minimal JavaScript.

## Key Features

- **Table Components**: Full-featured data tables with sorting, filtering, pagination, and inline editing
- **Navigation System**: Attribute-based or programmatic navigation with authorization integration
- **Authentication Integration**: Built-in authentication status components and OIDC support
- **Form Handling**: Seamless form processing with page state management
- **Authorization**: Fine-grained permission system with resource-operation based access control
- **Out-of-Band Updates**: Efficient partial page updates using HTMX's OOB functionality
- **JavaScript Behaviors**: Server-generated JavaScript behaviors for enhanced interactivity

## Quick Start

Get up and running quickly with the essentials:

```bash
dotnet add package Htmx.Components
```

```csharp
// Minimal setup in Program.cs
builder.Services.AddHtmxComponents();
builder.Services.AddMvc().AddHtmxComponentsApplicationPart();

app.UseHtmxPageState();
app.UseAuthentication();
app.UseAuthorization();
```

**📖 [Complete Setup Guide →](articles/getting-started.md)**

## What's Inside

### [Getting Started](articles/getting-started.md)
Learn how to set up HTMX Components in your ASP.NET Core application.

### [User Guide](articles/user-guide/basic-usage.md)
Comprehensive guides for using all the library features:
- Basic usage patterns
- Navigation setup
- Table management
- Authentication and authorization

### [Developer Guide](articles/developer-guide/architecture.md)
Deep dive into the library architecture and extension points:
- [Architecture overview](articles/developer-guide/architecture.md)
- [Component architecture](articles/developer-guide/component-architecture.md)
- [JavaScript architecture](articles/developer-guide/javascript-architecture.md)
- [Design choices and rationale](articles/developer-guide/design-choices.md)
- [Future directions and extensibility](articles/developer-guide/future-directions.md)
- [Building custom providers](articles/developer-guide/custom-providers.md)
- [Contributing guidelines](articles/developer-guide/contributing.md)

## Community

- **Source Code**: [GitHub Repository](https://github.com/your-org/htmx-components)
- **Issues**: Report bugs and request features
- **Discussions**: Community support and questions

## License

This project is licensed under the MIT License - see the LICENSE file for details.