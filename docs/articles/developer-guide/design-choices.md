# Design Choices in Htmx.Components

This document outlines the key architectural and design decisions made in Htmx.Components, providing context for developers working with or extending the library.

## Outline

1. [Self-Contained Component Architecture](#self-contained-component-architecture)
2. [Model Handler Pattern](#model-handler-pattern) 
3. [Multi-Swap View Results](#multi-swap-view-results)
4. [Encrypted Client-Side State Management](#encrypted-client-side-state-management)
5. [Result Filter-Based HTMX Integration](#result-filter-based-htmx-integration)
6. [Dual Navigation Provider Architecture](#dual-navigation-provider-architecture)
7. [CSS Class Extraction System](#css-class-extraction-system)
8. [Authorization Integration Strategy](#authorization-integration-strategy)
9. [ViewComponent-Centric Design](#viewcomponent-centric-design)

---

## Self-Contained Component Architecture

**Decision**: Organize each component with all related files co-located in a single folder structure.

```
Components/AuthStatus/
├── AuthStatusViewComponent.cs
├── AuthStatusUpdateAttribute.cs
├── Internal/AuthStatusUpdateFilter.cs
└── Views/Default.cshtml
```

**Rationale**:
- **Cohesion**: All aspects of a component (logic, attributes, views, filters) are in one place
- **Maintainability**: Changes to a component are localized, reducing cross-cutting modifications
- **Discoverability**: Developers can find all component-related code without searching the entire codebase
- **Reusability**: Components can be easily extracted or copied to other projects

**Trade-offs**:
- Slightly deeper folder nesting compared to flat organization
- May seem unfamiliar to developers used to traditional MVC folder structures

[↑ Back to outline](#outline)

---

## Model Handler Pattern

**Decision**: Use a centralized <xref:Htmx.Components.Models.ModelHandler`2> abstraction for CRUD operations and table building.

```csharp
[ModelConfig("users")]
private void ConfigureUserModel(ModelHandlerBuilder<User, int> builder)
{
    builder.WithQueryable(() => _dbContext.Users)
           .WithTable(table => table.AddSelectorColumn(x => x.Name));
}
```

**Rationale**:
- **Consistency**: Provides a uniform way to configure HTMX components and make them aware of app data models
- **Declarative Configuration**: Uses builder pattern for readable, fluent configuration
- **Authorization Integration**: Automatically integrates with the authorization system

**Trade-offs**:
- Additional abstraction layer over direct Entity Framework usage
- Learning curve for developers unfamiliar with the pattern

[↑ Back to outline](#outline)

---

## Multi-Swap View Results

**Decision**: Implement <xref:Htmx.Components.ViewResults.MultiSwapViewResult> to return multiple HTMX view updates in a single response.

```csharp
return new MultiSwapViewResult()
    .WithMainContent("_Table", tableModel)
    .WithOob("_AuthStatus", authModel);
```

**Rationale**:
- **Performance**: Reduces HTTP round trips by batching multiple UI updates
- **Consistency**: Ensures related UI components stay synchronized
- **HTMX Compatibility**: Leverages HTMX's out-of-band (OOB) swap capabilities
- **Developer Experience**: Provides a clean API for complex UI updates

**Trade-offs**:
- More complex than simple view returns
- Requires understanding of HTMX OOB concepts

**Alternative Considered**: Separate HTMX requests for each component update, which would increase network overhead.

[↑ Back to outline](#outline)

---

## Encrypted Client-Side State Management

**Decision**: Store component state encrypted in HTTP headers and decrypt server-side.

```csharp
public interface IPageState
{
    T? Get<T>(string partition, string key);
    void Set<T>(string partition, string key, T value);
    string Encrypted { get; }
}
```

**Rationale**:
- **Security**: State is encrypted using ASP.NET Core Data Protection, preventing client tampering
- **Scalability**: Avoids server-side session storage, enabling stateless server architecture
- **Partitioning**: Organizes state by logical partitions (e.g., "table", "filters")
- **HTMX Integration**: Automatically included in HTMX requests via headers

**Trade-offs**:
- HTTP header size limitations for large state objects
- Encryption/decryption overhead on each request

**Alternatives Considered**: 
- **Component-specific hidden form fields**: Complexity and duplication concerns across multiple components
- **Server-side session storage**: Scalability concerns in distributed environments
- **Unencrypted client state**: Security concerns with client-side tampering
- **Database state storage**: Performance overhead and complexity for temporary state

[↑ Back to outline](#outline)

---

## Result Filter-Based HTMX Integration

**Decision**: Use ASP.NET Core result filters to automatically inject HTMX-specific content.

```csharp
[TableRefresh(TargetId = "user-table")]
public async Task<IActionResult> UpdateUser(int id) { }
```

**Rationale**:
- **Declarative**: Attributes clearly indicate HTMX behavior without cluttering action logic
- **Automatic**: Out-of-band updates happen automatically based on attributes and action context
- **Composable**: Multiple filters can be applied to a single action
- **Framework Integration**: Leverages ASP.NET Core's built-in filter pipeline

**Trade-offs**:
- Magic behavior that may not be immediately obvious to developers
- Possible debugging complexity when multiple filters are involved

**Alternative Considered**: Manually specify every OOB view in each action method, which would be more verbose and error-prone.

[↑ Back to outline](#outline)

---

## Dual Navigation Provider Architecture

**Decision**: Support both attribute-based and builder-based navigation configuration, with the attribute-based approach using the builder-based configuration internally for consistency.

**Attribute-Based**:
```csharp
[NavActionGroup(DisplayName = "Admin", Icon = "fas fa-cogs")]
public class AdminController : Controller
{
    [NavAction(DisplayName = "Users", Icon = "fas fa-users")]
    public IActionResult Users() => View();
}
```

**Builder-Based**:
```csharp
options.WithNavBuilder(nav => nav.AddAction(a => a.WithLabel("Dashboard")));
```

**Rationale**:
- **Flexibility**: Supports different developer preferences and use cases
- **Co-location**: Attributes keep navigation logic close to controller actions
- **Centralization**: Builders allow centralized navigation configuration
- **Unified Implementation**: Attribute-based navigation internally uses the builder pattern, ensuring consistency

**Trade-offs**:
- Increased complexity with two configuration methods
- Potential for inconsistent navigation patterns within a project


[↑ Back to outline](#outline)

---

## CSS Class Extraction System

**Decision**: Extract CSS classes from C# code using MSBuild tasks and regular expressions.

```xml
<CssExtractorPatterns>
  \.WithClass\s*\(\s*"([^"]+)"\s*\);
  \.WithIcon\s*\(\s*"([^"]+)"\s*\);
</CssExtractorPatterns>
```

**Rationale**:
- **Tailwind Integration**: Ensures dynamically referenced CSS classes are included in Tailwind builds
- **Pattern Flexibility**: Regular expressions allow extraction from various code patterns
- **NuGet Distribution**: Extracted classes are packaged and distributed with the library

**Trade-offs**:
- Build process complexity with custom MSBuild tasks
- Maintenance overhead for extraction patterns

[↑ Back to outline](#outline)

---

## Authorization Integration Strategy

**Decision**: Integrate with ASP.NET Core's authorization system by providing a customizable authorization requirement factory and resource operation registry.

```csharp
options.WithAuthorizationRequirementFactory<AuthorizationRequirementFactory>();
options.WithResourceOperationRegistry<ResourceOperationRegistry>();
```

**Rationale**:
- **Flexibility**: Different applications can use different authorization models
- **Testability**: Authorization logic can be easily mocked or replaced
- **Resource-Based**: Supports fine-grained, resource-based authorization

**Trade-offs**:
- Additional abstraction layer over standard ASP.NET Core authorization
- Learning curve for understanding the factory and registry patterns

[↑ Back to outline](#outline)

---

## ViewComponent-Centric Design

**Decision**: Use ViewComponents as the primary UI rendering mechanism, integrated with result filters to handle HTMX responses and mitigate the complexity of managing the selection of partial views.

```csharp
@await Component.InvokeAsync("Table", tableModel)
```

**Rationale**:
- **Dynamic View Selection**: The partial views that may be used in an HTMX response are highly dynamic and context-specific
- **Server-Side Logic**: ViewComponents can contain complex server-side logic
- **Dependency Injection**: Full access to DI container for services and configuration
- **Testability**: ViewComponents can be unit tested independently
- **ASP.NET Core Integration**: Leverages built-in ViewComponent infrastructure

**HTMX Integration**:
ViewComponents alone don't automatically handle HTMX responses. The framework integrates them through:
- **Result Filters**: Automatically render ViewComponents for OOB updates based on attributes
- **MultiSwapViewResult**: Can render ViewComponents by name when given to `WithOobContent()`
- **Controller Detection**: Controllers can manually detect HTMX requests and use <xref:Htmx.Components.ViewResults.MultiSwapViewResult>

**Trade-offs**:
- More heavyweight than simple partial views
- Requires understanding of ViewComponent lifecycle and conventions
- HTMX integration requires additional infrastructure (result filters or manual controller logic)

**Alternative Considered**: Using tag helpers or direct partial view rendering, which would be simpler but more error prone for context-specific HTMX responses and would require more manual HTMX integration work.


[↑ Back to outline](#outline)

---

## Questions for Consideration

The following design decisions would benefit from additional context for a complete understanding:

1. **State Management Header Size**: Have there been practical limitations encountered with HTTP header size for state objects? What strategies are recommended for handling large state?

2. **Filter Pipeline Debugging**: What tooling or practices are recommended for debugging complex scenarios where multiple result filters are involved?

[↑ Back to outline](#outline)

---

For additional technical details, see:
- [Architecture Overview](architecture.md)
- [Component Architecture](component-architecture.md)
- [Future Directions](future-directions.md)
