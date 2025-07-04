# Architecture Overview

Htmx.Components is built around a modular architecture that leverages ASP.NET Core's dependency injection and MVC patterns to provide a seamless HTMX integration experience.

## Core Components

### 1. Model Handlers ([`ModelHandler<T, TKey>`](../../api/Htmx.Components.Models.ModelHandler-2.html))

Model handlers are the central abstraction that define how data models are processed, displayed, and manipulated:

```csharp
public class ModelHandler<T, TKey> : ModelHandler
    where T : class
{
    public Expression<Func<T, TKey>> KeySelector { get; set; }
    public Func<IQueryable<T>>? GetQueryable { get; internal set; }
    public Func<T, Task<Result<T>>>? CreateModel { get; internal set; }
    public Func<T, Task<Result<T>>>? UpdateModel { get; internal set; }
    public Func<TKey, Task<Result>>? DeleteModel { get; internal set; }
}
```

**Key Features:**
- CRUD operation definitions
- Table model building and data fetching
- Input model configuration
- Integration with authorization system

### 2. Table System

The table system provides a comprehensive data grid solution:

#### TableModel ([`TableModel<T, TKey>`](../../api/Htmx.Components.Table.Models.TableModel-2.html))
- Represents the complete table structure
- Contains columns, rows, pagination state
- Supports sorting, filtering, and CRUD operations

#### TableColumnModel ([`TableColumnModel<T, TKey>`](../../api/Htmx.Components.Table.Models.TableColumnModel-2.html))
- Defines individual column behavior
- Supports custom rendering, filtering, and actions
- Type-safe property binding via expressions

#### TableProvider ([`ITableProvider`](../../api/Htmx.Components.Table.ITableProvider.html))
- Handles data fetching and query building
- Applies filtering, sorting, and pagination
- Works with Entity Framework Core queryables

### 3. Navigation System

#### INavProvider
The navigation system provides authorization and context-aware navigation and supports two approaches to defining navigation actions:

**Attribute-Based Navigation ([`AttributeNavProvider`](../../api/Htmx.Components.NavBar.AttributeNavProvider.html)):**
```csharp
[NavActionGroup(Order = 1, DisplayName = "Admin", Icon = "fas fa-cog")]
public class AdminController : Controller
{
    [NavAction(Order = 1, DisplayName = "Users", Icon = "fas fa-users")]
    public IActionResult Users() => View();
}
```

**Builder-Based Navigation ([`BuilderBasedNavProvider`](../../api/Htmx.Components.NavBar.BuilderBasedNavProvider.html)):**
```csharp
services.AddHtmxComponents(options =>
{
    options.WithNavBuilder(nav =>
    {
        nav.AddAction(a => a
            .WithLabel("Dashboard")
            .WithIcon("fas fa-dashboard")
            .WithHxGet("/Dashboard"));
    });
});
```

### 4. State Management

#### PageState ([`IPageState`](../../api/Htmx.Components.State.IPageState.html))
Provides encrypted, client-side state management that enables stateful interactions while maintaining RESTful principles:

```csharp
public interface IPageState
{
    T? Get<T>(string partition, string key);
    void Set<T>(string partition, string key, T value);
    T GetOrCreate<T>(string partition, string key, Func<T> factory);
    string Encrypted { get; }
    bool IsDirty { get; }
}
```

The PageState system bridges the gap between stateless HTTP and stateful user interfaces by providing server-side state that travels with HTMX requests. This approach aligns with HATEOAS (Hypermedia as the Engine of Application State) principles, where the server provides both data and the possible actions/transitions available to the client based on the current state of the client.

Unlike traditional session-based state management, PageState is:
- **Client-carried**: State travels in HTTP headers, eliminating server-side session storage
- **Encrypted**: All state data is encrypted before being sent to the client
- **Tamper-proof**: Clients cannot modify state without server knowledge
- **Request-scoped**: State is available throughout the entire request pipeline

**Features:**
- Partitioned state organization for logical grouping
- Automatic encryption/decryption using secure algorithms
- Change tracking
- HTTP header-based transport

### 5. Filter System

The framework includes sophisticated result filters for HTMX integration:

#### OobResultFilterBase
Base class for out-of-band (OOB) content injection:

```csharp
public abstract class OobResultFilterBase<T> : IAsyncResultFilter
    where T : Attribute
{
    protected abstract Task UpdateMultiSwapViewResultAsync(
        T attribute, 
        MultiSwapViewResult multiSwapViewResult, 
        ResultExecutingContext context);
}
```

#### Specialized Filters
- [`TableOobRefreshFilter`](../../api/Htmx.Components.Table.Internal.TableOobRefreshFilter.html): Updates table components
- [`TableOobEditFilter`](../../api/Htmx.Components.Table.Internal.TableOobEditFilter.html): Handles inline editing
- [`NavActionResultFilter`](../../api/Htmx.Components.NavBar.Internal.NavActionResultFilter.html): Updates navigation state
- [`AuthStatusUpdateFilter`](../../api/Htmx.Components.AuthStatus.Internal.AuthStatusUpdateFilter.html): Refreshes authentication status
- [`PageStateOobInjectorFilter`](../../api/Htmx.Components.Filters.PageStateOobInjectorFilter.html): Manages state synchronization

### 6. Authorization Integration

#### IAuthorizationRequirementFactory
Creates authorization requirements that integrate with ASP.NET Core's authorization system:

```csharp
public interface IAuthorizationRequirementFactory
{
    IAuthorizationRequirement ForOperation(string resource, string operation);
    IAuthorizationRequirement ForRoles(params string[] roles);
}
```

**Integration with ASP.NET Core Authorization:**

The factory creates `IAuthorizationRequirement` instances that are consumed by ASP.NET Core's `IAuthorizationService`. This allows Htmx.Components to leverage the full power of the built-in authorization system:

```csharp
// Example implementation that creates operation-based requirements
public class OperationAuthorizationRequirementFactory : IAuthorizationRequirementFactory
{
    public IAuthorizationRequirement ForOperation(string resource, string operation)
    {
        return new OperationAuthorizationRequirement 
        { 
            Name = $"{resource}.{operation}" 
        };
    }

    public IAuthorizationRequirement ForRoles(params string[] roles)
    {
        return new RolesAuthorizationRequirement(roles);
    }
}
```

**Authorization Flow:**
1. Model handlers register resource-operation pairs during configuration
2. The factory creates appropriate `IAuthorizationRequirement` instances
3. ASP.NET Core's `IAuthorizationService.AuthorizeAsync()` evaluates requirements
4. Authorization handlers (registered in DI) process the requirements
5. Results are cached by `AuthorizationMetadataService` for performance

**Custom Authorization Handlers:**
You can register custom authorization handlers that work with the requirements:

```csharp
services.AddScoped<IAuthorizationHandler, ProjectOperationHandler>();

public class ProjectOperationHandler : AuthorizationHandler<OperationAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement)
    {
        // Custom authorization logic
        if (UserCanPerformOperation(context.User, requirement.Name))
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}
```

This approach allows the framework to integrate seamlessly with existing ASP.NET Core authorization policies, handlers, and middleware while providing the abstractions needed for dynamic resource-operation authorization.

#### IResourceOperationRegistry
Registers resource-operation pairs with the authorization system:

```csharp
public interface IResourceOperationRegistry
{
    Task Register(string resource, string operation);
}
```

#### AuthorizationMetadataService
Provides caching and evaluation of authorization metadata by bridging controller attributes with ASP.NET Core's authorization system:

```csharp
public class AuthorizationMetadataService : IAuthorizationMetadataService
{
    public async Task<bool> IsAuthorizedAsync(
        ControllerActionDescriptor descriptor, 
        ClaimsPrincipal user);
}
```

**How it integrates with ASP.NET Core Authorization:**

The service extracts authorization metadata from controller actions and evaluates them using the standard ASP.NET Core authorization pipeline:

```csharp
// Extracts metadata from controller attributes
[Authorize(Policy = "CanEditUsers")]
[Authorize(Roles = "Admin,Manager")]
public class UserController : Controller
{
    public IActionResult Edit(int id) => View();
}

// The service processes these attributes and calls:
// 1. IAuthorizationService.AuthorizeAsync() for policies
// 2. IRoleService.UserHasAnyRoleAsync() for role checks
// 3. Caches results for performance
```

**Authorization Evaluation Process:**
1. **Metadata Extraction**: Parses `[Authorize]` and `[AllowAnonymous]` attributes from controllers and actions
2. **Policy Evaluation**: Uses `IAuthorizationService.AuthorizeAsync()` to evaluate each policy requirement
3. **Role Checking**: Delegates role validation to the configured `IRoleService` implementation
4. **Result Caching**: Caches authorization decisions per user/policy combination to improve performance
5. **Composition Logic**: Combines policy results (AND semantics) with role results (OR semantics)

**Caching Strategy:**
- **Metadata Cache**: Controller action metadata cached for 10 minutes (attribute parsing is expensive)
- **Authorization Cache**: User-specific authorization results cached for 2 minutes
- **Cache Keys**: Uses user ID claim and policy/role identifiers for precise cache invalidation

This approach ensures that all authorization decisions flow through ASP.NET Core's standard authorization system while providing the performance optimizations needed for high-frequency operations like navigation building and table filtering.

### 7. JavaScript Architecture

The framework includes a sophisticated JavaScript delivery system that enables server-side configuration and dynamic script inclusion through the `HtmxScriptsTagHelper`.

#### Key Components

**HtmxScriptsTagHelper**: Central TagHelper that manages JavaScript behavior inclusion:
```html
<htmx-scripts include="page-state,table-behavior"></htmx-scripts>
```

**Script Behaviors**: Modular JavaScript functionalities delivered as Razor partial views:
- `PageStateBehavior`: Automatic page state management
- `TableBehavior`: Table interaction and editing behaviors
- `BlurSaveCoordination`: Form coordination and race condition prevention
- `HtmxAuthRetry`: Authentication retry handling

**Benefits:**
- **Server-side Generation**: Scripts can include dynamically generated URLs and configuration
- **Selective Inclusion**: Choose which behaviors to include per page
- **Maintainability**: Each behavior is isolated and testable
- **Performance**: Inline delivery eliminates additional HTTP requests

For detailed information, see [JavaScript Architecture](javascript-architecture.md).

## Data Flow Diagrams

### 1. Request Processing

```
HTTP Request → Middleware → Controller → ModelHandler → View Components → Response
```

1. **PageStateMiddleware** extracts encrypted state from headers
2. **Controller actions** process requests using ModelHandlers
3. **Result filters** transform responses for HTMX requests
4. **View rendering** follows different paths:
   - **Non-HTMX requests**: Standard MVC rendering with ViewComponents
   - **HTMX requests**: Result filters determine which partial views are rendered; ViewComponents only render if explicitly called by those partials
5. **MultiSwapViewResult** coordinates multiple content updates for HTMX

### 2. Table Data Flow

```
Query → TableProvider → EF Core → Filtering/Sorting → Pagination → TableModel → Views
```

1. **ModelHandler** provides base queryable
2. **TableProvider** applies state-based transformations
3. **EF Core** executes optimized queries
4. **TableModel** structures data for rendering
5. **Partial views** render table fragments

### 3. State Synchronization

```
Client State → HTTP Header → PageStateMiddleware → IPageState → Result Filters → OOB Updates
```

1. Client sends encrypted state (from hidden input) in headers
2. Middleware deserializes state into [`IPageState`](../../api/Htmx.Components.State.IPageState.html)
3. Controllers modify state during processing
4. Result filter detects state changes
5. OOB updates sync client-side state

## Extension Points

### 1. Custom Model Handlers

Create specialized model handlers for complex scenarios:

```csharp
public class AuditableModelHandler<T, TKey> : ModelHandler<T, TKey>
    where T : class, IAuditable
{
    // Add audit-specific functionality
}
```

### 2. Custom Table Columns

Implement specialized column types:

```csharp
public class ImageColumnBuilder<T, TKey> : TableColumnModelBuilder<T, TKey>
{
    public ImageColumnBuilder<T, TKey> WithImageRenderer(string imageUrlProperty)
    {
        // Custom image rendering logic
        return this;
    }
}
```

### 3. Custom View Components

Create reusable UI components that integrate with HTMX:

```csharp
public class CustomWidgetViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(CustomWidgetModel model)
    {
        return View(model);
    }
}
```

**Important**: ViewComponents alone don't automatically handle HTMX responses. For HTMX integration, you need one of these approaches:

**Option 1: Custom Result Filter**
```csharp
public class CustomWidgetOobFilter : OobResultFilterBase<CustomWidgetRefreshAttribute>
{
    protected override async Task UpdateMultiSwapViewResultAsync(
        CustomWidgetRefreshAttribute attribute,
        MultiSwapViewResult multiSwapViewResult,
        ResultExecutingContext context)
    {
        var model = // ... build your model
        multiSwapViewResult.WithOobContent("CustomWidget", model); // ViewComponent name
    }
}
```

**Option 2: Controller-Level HTMX Detection**
```csharp
public class CustomWidgetController : Controller
{
    public async Task<IActionResult> Refresh()
    {
        var model = // ... build your model
        var navModel = // ... build your navigation model
        var sidebarModel = // ... build your sidebar model
        
        if (Request.IsHtmx())
        {
            return new MultiSwapViewResult()
                .WithOobContent("CustomWidget", model)
                .WithOobContent("NavBar", navModel)
                .WithOobContent("Sidebar", sidebarModel);
        }
        
        return View(model); // Full page for non-HTMX
    }
}
```

> **⚠️ Scalability Warning**: This approach is only viable for simple applications. As your app grows and more UI concerns need to be addressed by a single response (navigation updates, notifications, state synchronization, etc.), controller actions can become unwieldy and difficult to maintain. For production applications, **Option 1 (Custom Result Filters)** is strongly recommended as it provides better separation of concerns and allows each filter to handle its specific responsibility independently.

**Option 3: Partial View Integration**
ViewComponents can be called from partial views that are rendered by existing result filters:
```html
<!-- In a partial view rendered by TableOobRefreshFilter -->
@await Component.InvokeAsync("CustomWidget", model)
```

### 4. Custom Authorization

Implement domain-specific authorization:

```csharp
public class ProjectBasedAuthRequirementFactory : IAuthorizationRequirementFactory
{
    public IAuthorizationRequirement ForOperation(string resource, string operation)
    {
        return new ProjectOperationRequirement(resource, operation);
    }
}
```

## Performance Considerations

### 1. Caching Strategy

- **Authorization metadata**: Cached for 10 minutes
- **Authorization results**: Cached for 2 minutes per user/policy
- **Compiled expressions**: Cached indefinitely via FastExpressionCompiler

### 2. Query Optimization

- Uses `IQueryable<T>` for deferred execution
- Applies filtering before pagination
- Leverages EF Core query compilation

### 3. State Management

- Encrypted state prevents tampering
- Change tracking minimizes unnecessary updates

## Testing Strategies

> **TODO**: This section will be expanded once comprehensive test suites are established for the framework. Testing patterns and examples will be added to cover unit testing of components, integration testing of HTMX flows, and authorization testing strategies.

## Related Documentation

- **[Design Choices](design-choices.md)**: Deep dive into the architectural decisions and rationale behind key design patterns
- **[Component Architecture](component-architecture.md)**: Detailed exploration of the self-contained component structure
- **[Future Directions](future-directions.md)**: Potential enhancements and extension patterns

This architecture provides a solid foundation for building complex HTMX applications while maintaining separation of concerns and extensibility.