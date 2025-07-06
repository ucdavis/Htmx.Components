# Future Directions

Htmx.Components is an evolving framework with opportunities for enhancement and extension. This document outlines potential future directions and suggests how the framework could be enhanced to support additional scenarios.

> **⚠️ Important**: This document discusses potential future enhancements, not current capabilities. The framework is actively evolving, and these suggestions may influence future development. Always refer to the current API documentation for actual capabilities.

## Enhanced Model Handler Extensibility

Currently, <xref:Htmx.Components.Models.ModelHandler`2> is configured through a builder pattern using delegates. Future enhancements could provide more extensibility points.

### Suggested: Event Hooks for CRUD Operations

```csharp
// Potential future enhancement
public class ModelHandlerBuilder<T, TKey>
{
    public ModelHandlerBuilder<T, TKey> OnBeforeCreate(Func<T, Task<ValidationResult>> beforeCreate)
    {
        // Could enable pre-create validation, auditing, etc.
        return this;
    }
    
    public ModelHandlerBuilder<T, TKey> OnAfterCreate(Func<T, Task> afterCreate)
    {
        // Could enable post-create notifications, logging, etc.
        return this;
    }
    
    public ModelHandlerBuilder<T, TKey> OnBeforeUpdate(Func<T, T, Task<ValidationResult>> beforeUpdate)
    {
        // Could compare old vs new values for auditing
        return this;
    }
    
    public ModelHandlerBuilder<T, TKey> OnBeforeDelete(Func<TKey, Task<ValidationResult>> beforeDelete)
    {
        // Could enable soft delete logic
        return this;
    }
}
```

### Suggested: Middleware-Style Pipeline

```csharp
// Potential future enhancement
public interface IModelHandlerMiddleware<T, TKey>
{
    Task<Result<T>> ProcessCreateAsync(T model, Func<T, Task<Result<T>>> next);
    Task<Result<T>> ProcessUpdateAsync(T model, Func<T, Task<Result<T>>> next);
    Task<Result> ProcessDeleteAsync(TKey key, Func<TKey, Task<Result>> next);
}

public class AuditingMiddleware<T, TKey> : IModelHandlerMiddleware<T, TKey>
    where T : class, IAuditable
{
    public async Task<Result<T>> ProcessCreateAsync(T model, Func<T, Task<Result<T>>> next)
    {
        model.CreatedDate = DateTime.UtcNow;
        model.CreatedBy = GetCurrentUser();
        return await next(model);
    }
    
    // Similar implementations for Update and Delete
}
```

## Advanced Table Customization

### Suggested: Plugin Architecture for Table Features

```csharp
// Potential future enhancement
public interface ITablePlugin<T, TKey>
{
    void ConfigureColumns(TableModelBuilder<T, TKey> builder);
    void ConfigureFiltering(TableModelBuilder<T, TKey> builder);
    void ConfigureActions(TableModelBuilder<T, TKey> builder);
}

public class AuditableTablePlugin<T, TKey> : ITablePlugin<T, TKey>
    where T : IAuditable
{
    public void ConfigureColumns(TableModelBuilder<T, TKey> builder)
    {
        builder.Column(x => x.CreatedDate).WithLabel("Created");
        builder.Column(x => x.CreatedBy).WithLabel("Created By");
    }
}
```

### Suggested: Custom Column Types

```csharp
// Potential future enhancement
public abstract class CustomColumnType<T, TValue>
{
    public abstract void Configure(TableColumnModelBuilder<T, TKey> column);
    public abstract string RenderCell(T model, TValue value);
    public abstract string RenderFilter();
}

public class ImageColumnType<T> : CustomColumnType<T, string>
{
    public override void Configure(TableColumnModelBuilder<T, TKey> column)
    {
        column.WithCellPartial("_ImageCell");
    }
    
    // Implementation details...
}
```

## Enhanced ViewComponent Integration

### Suggested: Declarative OOB Updates

```csharp
// Potential future enhancement
[AttributeUsage(AttributeTargets.Method)]
public class UpdateComponentsAttribute : Attribute
{
    public string[] ComponentNames { get; set; }
    
    public UpdateComponentsAttribute(params string[] componentNames)
    {
        ComponentNames = componentNames;
    }
}

[HttpPost]
[UpdateComponents("NavBar", "AuthStatus", "Notifications")]
public IActionResult UpdateUser(User user)
{
    // Framework automatically updates specified components
    return Ok();
}
```

### Suggested: Component Dependency Management

```csharp
// Potential future enhancement
public class ComponentDependencyRegistry
{
    public void RegisterDependency<TComponent>(string triggerEvent, params string[] dependentComponents);
}

// Usage
registry.RegisterDependency<UserTableComponent>("user.updated", "NavBar", "UserCount");
```

## State Management Enhancements

### Suggested: State Persistence Options

```csharp
// Potential future enhancement
public enum StatePersistence
{
    Page,         // Current behavior - Persisted in the page
    Session,      // Persisted in server-side session
    LocalStorage, // Persisted in browser local storage
    Database      // Persisted in database for user
}

builder.Services.ConfigurePageState(options =>
{
    options.DefaultPersistence = StatePersistence.Session;
    options.Configure("table", "filters", StatePersistence.LocalStorage);
});
```

## Advanced Result Filter Patterns

### Suggested: Conditional Result Filters

```csharp
// Potential future enhancement
public abstract class ConditionalResultFilterBase<T> : OobResultFilterBase<T>
    where T : Attribute
{
    protected abstract Task<bool> ShouldApplyAsync(T attribute, ResultExecutingContext context);
    
    protected override async Task UpdateMultiSwapViewResultAsync(
        T attribute, 
        MultiSwapViewResult multiSwapViewResult, 
        ResultExecutingContext context)
    {
        if (await ShouldApplyAsync(attribute, context))
        {
            await ApplyUpdateAsync(attribute, multiSwapViewResult, context);
        }
    }
    
    protected abstract Task ApplyUpdateAsync(
        T attribute, 
        MultiSwapViewResult multiSwapViewResult, 
        ResultExecutingContext context);
}
```

### Suggested: Filter Composition

```csharp
// Potential future enhancement
[AttributeUsage(AttributeTargets.Method)]
public class CompositeFilterAttribute : Attribute
{
    public Type[] FilterTypes { get; set; }
    
    public CompositeFilterAttribute(params Type[] filterTypes)
    {
        FilterTypes = filterTypes;
    }
}

[HttpPost]
[CompositeFilter(typeof(TableRefreshFilter), typeof(NotificationFilter), typeof(AuditFilter))]
public IActionResult UpdateData()
{
    // Multiple filters applied automatically
    return Ok();
}
```

## Performance and Optimization Opportunities

### Suggested: Component-Level Caching

```csharp
// Potential future enhancement
[AttributeUsage(AttributeTargets.Class)]
public class CacheableComponentAttribute : Attribute
{
    public TimeSpan Duration { get; set; }
    public string[] VaryByParameters { get; set; }
    
    public CacheableComponentAttribute(int durationMinutes, params string[] varyByParameters)
    {
        Duration = TimeSpan.FromMinutes(durationMinutes);
        VaryByParameters = varyByParameters;
    }
}

[CacheableComponent(5, "userId", "roleId")]
public class UserDashboardViewComponent : ViewComponent
{
    // Automatically cached based on userId and roleId for 5 minutes
}
```

## Integration Patterns

### Suggested: External Service Integration

```csharp
// Potential future enhancement
public interface IExternalServiceConnector<T, TKey>
{
    Task SyncAfterCreateAsync(T entity);
    Task SyncAfterUpdateAsync(T entity);
    Task SyncAfterDeleteAsync(TKey key);
}

public class SalesforceConnector<T, TKey> : IExternalServiceConnector<T, TKey>
    where T : ISalesforceEntity
{
    public async Task SyncAfterCreateAsync(T entity)
    {
        // Sync new entity to Salesforce
    }
}
```

## Testing and Development Support

### Suggested: Testing Utilities

```csharp
// Potential future enhancement
public class ModelHandlerTestBuilder<T, TKey>
{
    public ModelHandlerTestBuilder<T, TKey> WithMockData(IEnumerable<T> data);
    public ModelHandlerTestBuilder<T, TKey> WithMockUser(ClaimsPrincipal user);
    public ModelHandlerTestBuilder<T, TKey> WithMockServices(Action<IServiceCollection> configure);
    public Task<ModelHandler<T, TKey>> BuildAsync();
}

// Usage in tests
var handler = await new ModelHandlerTestBuilder<User, int>()
    .WithMockData(GetTestUsers())
    .WithMockUser(GetTestUser())
    .BuildAsync();
```

### Suggested: Development-Time Diagnostics

```csharp
// Potential future enhancement
[Conditional("DEBUG")]
public class DiagnosticsMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.ContainsKey("X-Htmx-Diagnostics"))
        {
            // Add diagnostic information to response headers
            context.Response.Headers.Add("X-Htmx-Query-Count", queryCount.ToString());
            context.Response.Headers.Add("X-Htmx-Render-Time", renderTime.ToString());
        }
        
        await _next(context);
    }
}
```

## Related Documentation

- **[Design Choices](design-choices.md)**: Understand the current architectural decisions
- **[Architecture Overview](architecture.md)**: Current framework architecture
- **[Component Architecture](component-architecture.md)**: Current component patterns

These future directions represent potential enhancements that could make Htmx.Components even more powerful and flexible while maintaining its core principles of simplicity and convention over configuration.