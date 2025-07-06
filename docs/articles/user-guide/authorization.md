# Authorization

Htmx.Components integrates with ASP.NET Core's authorization framework and provides additional features for resource-based permissions.

## Basic Authorization Setup

Htmx.Components works with standard ASP.NET Core authorization. Here's how to set up policies and configure authorization:

### Service Registration

Configure authorization in Program.cs:

```csharp
builder.Services.AddAuthorization(options =>
{
    // Define access policies directly
    options.AddPolicy("SystemAccess", policy => 
        policy.RequireRole("System"));
    
    options.AddPolicy("AdminAccess", policy => 
        policy.RequireRole("System", "Admin"));
    
    options.AddPolicy("UserAccess", policy => 
        policy.RequireAuthenticatedUser());
});

// Register authorization handler if using custom requirements
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();

// Configure Htmx.Components authorization
builder.Services.AddHtmxComponents(htmxOptions =>
{
    htmxOptions.WithAuthorizationRequirementFactory<PermissionRequirementFactory>();
    htmxOptions.WithResourceOperationRegistry<ResourceOperationRegistry>();
});
```

## Using Authorization in Controllers

### NavAction Authorization

Navigation items are automatically filtered based on authorization:

```csharp
[Route("Admin")]
[NavActionGroup(DisplayName = "Admin", Icon = "fas fa-cogs", Order = 2)]
public class AdminController : Controller
{
    [HttpGet("AdminUsers")]
    [Authorize(Policy = "SystemAccess")]  // Only System role can access
    [NavAction(DisplayName = "Admin Users", Icon = "fas fa-users-cog", Order = 1)]
    public async Task<IActionResult> AdminUsers()
    {
        var modelHandler = await _modelHandlerFactory.Get<AdminUserModel, int>(nameof(AdminUserModel), ModelUI.Table);
        var tableModel = await modelHandler.BuildTableModelAndFetchPageAsync();
        return Ok(tableModel);
    }

    [HttpGet("Repos")]
    [NavAction(DisplayName = "Repos", Icon = "fas fa-database", Order = 0)]
    public async Task<IActionResult> Repos()
    {
        // No authorization - visible to all authenticated users
        var modelHandler = await _modelHandlerFactory.Get<Repo, int>(nameof(Repo), ModelUI.Table);
        var tableModel = await modelHandler.BuildTableModelAndFetchPageAsync();
        return Ok(tableModel);
    }
}
```

### Standard Authorization Attributes

Use standard ASP.NET Core authorization attributes:

```csharp
[Authorize]  // Requires authentication
public class DashboardController : Controller
{
    [NavAction(DisplayName = "Dashboard", Icon = "fas fa-tachometer-alt")]
    public IActionResult Index()
    {
        return Ok(new { });
    }
}

[Authorize(Policy = "AdminAccess")]  // Requires specific policy
public class ReportsController : Controller
{
    [NavAction(DisplayName = "Financial Reports")]
    public IActionResult FinancialReports()
    {
        return Ok();
    }
}

[Authorize(Roles = "Admin,Manager")]  // Requires specific roles
public class UserManagementController : Controller
{
    [NavAction(DisplayName = "Manage Users")]
    public IActionResult Index()
    {
        return Ok();
    }
}
```

## How Authorization Works with Navigation

1. **Automatic Filtering**: The `NavBar` component automatically discovers controller actions with `[NavAction]` attributes
2. **Authorization Check**: Each navigation item is checked against the user's permissions
3. **Filtered Display**: Only items the user is authorized to access are displayed
4. **Real-time Updates**: When authentication status changes, navigation updates automatically

## Resource-Based Authorization (Advanced)

For more complex scenarios, Htmx.Components supports resource-operation based authorization:

### Authorization Requirement Factory

```csharp
public class PermissionRequirementFactory : IAuthorizationRequirementFactory
{
    public IAuthorizationRequirement ForOperation(string resource, string operation)
    {
        return new OperationAuthorizationRequirement 
        { 
            Name = $"{resource}:{operation}" 
        };
    }

    public IAuthorizationRequirement ForRoles(params string[] roles)
    {
        return new RolesAuthorizationRequirement(roles);
    }
}
```

### Resource Operation Registry

```csharp
public class ResourceOperationRegistry : IResourceOperationRegistry
{
    private readonly HashSet<string> _registeredOperations = new();

    public Task Register(string resource, string operation)
    {
        _registeredOperations.Add($"{resource}:{operation}");
        return Task.CompletedTask;
    }

    public IEnumerable<string> GetRegisteredOperations() => _registeredOperations;
}
```

## Table Authorization

Tables automatically respect authorization rules when CRUD operations are enabled:

```csharp
[ModelConfig(nameof(AdminUserModel))]
private void ConfigureAdminUser(ModelHandlerBuilder<AdminUserModel, int> builder)
{
    builder
        .WithKeySelector(u => u.Id)
        .WithQueryable(() => _dbContext.Users.Where(/* authorized users only */))
        .WithTable(table => table
            .WithCrudActions()  // CRUD actions respect controller authorization
            .AddSelectorColumn(x => x.Name)
            .AddSelectorColumn(x => x.Email, config => config.WithEditable())
            .AddCrudDisplayColumn());
}
```

## Common Patterns

### Role Hierarchy

```csharp
builder.Services.AddAuthorization(options =>
{
    // Role hierarchy: System > Admin > User
    options.AddPolicy("ReadAccess", policy => 
        policy.RequireRole("User", "Admin", "System"));
    
    options.AddPolicy("WriteAccess", policy => 
        policy.RequireRole("Admin", "System"));
    
    options.AddPolicy("AdminAccess", policy => 
        policy.RequireRole("System"));
});
```

### Feature-Based Authorization

```csharp
[Authorize(Policy = "CanViewReports")]
[NavAction(DisplayName = "Reports")]
public IActionResult Reports() => Ok();

[Authorize(Policy = "CanManageUsers")]
[NavAction(DisplayName = "User Management")]
public IActionResult UserManagement() => Ok();
```

## Next Steps

- **[Authentication](authentication.md)**: Learn about setting up authentication that works with authorization
- **[Navigation](navigation.md)**: Understand how navigation integrates with authorization
- **[Tables](tables.md)**: See how tables respect authorization in CRUD operations
```

## Authorization Handlers

### Custom Authorization Handlers

Create handlers for your custom requirements:

```csharp
public class UserManagementHandler : AuthorizationHandler<UserManagementRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        UserManagementRequirement requirement)
    {
        var user = context.User;
        
        // Check if user has admin role
        if (user.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
        
        // Check specific permissions based on operation
        var hasPermission = requirement.Operation switch
        {
            "read" => user.HasClaim("permission", "users:read") || 
                     user.IsInRole("UserViewer"),
            "create" => user.HasClaim("permission", "users:create") || 
                       user.IsInRole("UserManager"),
            "update" => user.HasClaim("permission", "users:update") || 
                       user.IsInRole("UserManager"),
            "delete" => user.HasClaim("permission", "users:delete") || 
                       user.IsInRole("UserAdmin"),
            _ => false
        };
        
        if (hasPermission)
            context.Succeed(requirement);
            
        return Task.CompletedTask;
    }
}

// Register the handler
builder.Services.AddScoped<IAuthorizationHandler, UserManagementHandler>();
```

### Resource-Specific Handlers

Create handlers that work with specific resources:

```csharp
public class OrderAccessHandler : AuthorizationHandler<OrderAccessRequirement, Order>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OrderAccessHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrderAccessRequirement requirement,
        Order resource)
    {
        var user = context.User;
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        // Users can always access their own orders
        if (resource.CustomerId == userId)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
        
        // Admins can access all orders
        if (user.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
        
        // Sales team can read all orders
        if (requirement.Operation == "read" && user.IsInRole("Sales"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
        
        return Task.CompletedTask;
    }
}
```

## Role-Based Authorization

### Role Service

Implement [`IRoleService`](../../api/Htmx.Components.Services.IRoleService.html) for role-based authorization:

```csharp
public class DatabaseRoleService : IRoleService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public DatabaseRoleService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<bool> UserHasAnyRoleAsync(ClaimsPrincipal user, IEnumerable<string> requiredRoles)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return false;
            
        var appUser = await _userManager.FindByIdAsync(userId);
        if (appUser == null)
            return false;
            
        var userRoles = await _userManager.GetRolesAsync(appUser);
        return requiredRoles.Any(role => userRoles.Contains(role));
    }
}

// Register the service
builder.Services.AddHtmxComponents(options =>
{
    options.WithRoleService<DatabaseRoleService>();
});
```

### Custom Role Provider

Create a more sophisticated role system:

```csharp
public class HierarchicalRoleService : IRoleService
{
    private readonly IUserService _userService;
    private readonly IRoleHierarchyService _roleHierarchyService;

    public async Task<bool> UserHasAnyRoleAsync(ClaimsPrincipal user, IEnumerable<string> requiredRoles)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRoles = await _userService.GetUserRolesAsync(userId);
        
        // Check direct role membership
        if (requiredRoles.Any(role => userRoles.Contains(role)))
            return true;
            
        // Check role hierarchy (e.g., Admin includes Manager includes User)
        var expandedRoles = await _roleHierarchyService.ExpandRolesAsync(userRoles);
        return requiredRoles.Any(role => expandedRoles.Contains(role));
    }
}
```

## Model Handler Authorization

### Automatic Authorization

Model handlers automatically register their operations:

```csharp
[ModelConfig("products")]
private void ConfigureProductModel(ModelHandlerBuilder<Product, int> builder)
{
    builder.WithKeySelector(p => p.Id)
           .WithQueryable(() => _context.Products)
           .WithCreate(CreateProduct)    // Registers "products:create"
           .WithUpdate(UpdateProduct)    // Registers "products:update"  
           .WithDelete(DeleteProduct);   // Registers "products:delete"
                                        // Read is registered with WithQueryable
}
```

### Manual Authorization Checks

Perform authorization checks in controllers:

```csharp
public async Task<IActionResult> SpecialReport()
{
    var requirement = _permissionFactory.ForOperation("reports", "special");
    var authResult = await _authorizationService.AuthorizeAsync(User, null, requirement);
    
    if (!authResult.Succeeded)
        return Forbid();
        
    // Generate report
    var report = await _reportService.GenerateSpecialReportAsync();
    return View(report);
}
```

## Navigation Authorization

### Attribute-Based Authorization

Navigation items are automatically filtered based on authorization:

```csharp
using Htmx.Components.NavBar;

[Authorize(Policy = "CanManageUsers")]
[NavAction(DisplayName = "User Management")]
public IActionResult Users() => View();

[Authorize(Roles = "Admin")]
[NavAction(DisplayName = "System Settings")]
public IActionResult Settings() => View();
```

### Programmatic Authorization

Check authorization in programmatic navigation:

```csharp
builder.Services.AddHtmxComponents(options =>
{
    options.WithNavBuilder(async nav =>
    {
        // Always show home
        nav.AddAction(action => action
            .WithLabel("Home")
            .WithHxGet("/"));
        
        // Check if user can manage users
        var userManagementReq = permissionFactory.ForOperation("users", "read");
        var canManageUsers = await authService.AuthorizeAsync(user, userManagementReq);
        
        if (canManageUsers.Succeeded)
        {
            nav.AddAction(action => action
                .WithLabel("Users")
                .WithHxGet("/Users"));
        }
    });
});
```

## Claims-Based Authorization

### Custom Claims

Add custom claims during authentication:

```csharp
public async Task<ClaimsIdentity> CreateClaimsIdentityAsync(User user)
{
    var identity = new ClaimsIdentity();
    
    // Basic claims
    identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
    identity.AddClaim(new Claim(ClaimTypes.Name, user.UserName));
    
    // Permission claims
    var permissions = await _permissionService.GetUserPermissionsAsync(user.Id);
    foreach (var permission in permissions)
    {
        identity.AddClaim(new Claim("permission", permission));
    }
    
    // Department claim
    if (!string.IsNullOrEmpty(user.Department))
    {
        identity.AddClaim(new Claim("department", user.Department));
    }
    
    return identity;
}
```

### Claims-Based Requirements

Create requirements that check specific claims:

```csharp
public class DepartmentAccessRequirement : IAuthorizationRequirement
{
    public string Department { get; }
    public DepartmentAccessRequirement(string department) => Department = department;
}

public class DepartmentAccessHandler : AuthorizationHandler<DepartmentAccessRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DepartmentAccessRequirement requirement)
    {
        var userDepartment = context.User.FindFirst("department")?.Value;
        
        if (userDepartment == requirement.Department || 
            context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
        }
        
        return Task.CompletedTask;
    }
}
```

## Multi-Tenant Authorization

### Tenant-Aware Authorization

Implement authorization that considers tenant context:

```csharp
public class TenantAwarePermissionFactory : IAuthorizationRequirementFactory
{
    public IAuthorizationRequirement ForOperation(string resource, string operation)
    {
        return new TenantResourceRequirement(resource, operation);
    }

    public IAuthorizationRequirement ForRoles(params string[] roles)
    {
        return new TenantRolesRequirement(roles);
    }
}

public class TenantResourceRequirement : IAuthorizationRequirement
{
    public string Resource { get; }
    public string Operation { get; }
    
    public TenantResourceRequirement(string resource, string operation)
    {
        Resource = resource;
        Operation = operation;
    }
}

public class TenantResourceHandler : AuthorizationHandler<TenantResourceRequirement>
{
    private readonly ITenantService _tenantService;

    public TenantResourceHandler(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantResourceRequirement requirement)
    {
        var tenantId = await _tenantService.GetCurrentTenantIdAsync();
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        // Check if user belongs to the current tenant
        var userTenant = context.User.FindFirst("tenant")?.Value;
        if (userTenant != tenantId)
        {
            return; // User doesn't belong to current tenant
        }
        
        // Check tenant-specific permissions
        var hasPermission = await _tenantService.UserHasPermissionAsync(
            userId, tenantId, requirement.Resource, requirement.Operation);
            
        if (hasPermission)
            context.Succeed(requirement);
    }
}
```

## Testing Authorization

### Unit Testing Requirements

```csharp
[Test]
public async Task UserManagementHandler_AdminUser_ShouldSucceed()
{
    // Arrange
    var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.Role, "Admin")
    }));
    
    var context = new AuthorizationHandlerContext(
        new[] { new UserManagementRequirement("read") }, user, null);
    
    var handler = new UserManagementHandler();
    
    // Act
    await handler.HandleAsync(context);
    
    // Assert
    Assert.IsTrue(context.HasSucceeded);
}
```

### Integration Testing

```csharp
[Test]
public async Task GetUsers_UnauthorizedUser_ShouldReturnForbidden()
{
    // Arrange
    var client = _factory.CreateClient();
    
    // Act
    var response = await client.GetAsync("/Users");
    
    // Assert
    Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
}
```

This comprehensive authorization system provides fine-grained control over access to resources while maintaining flexibility and performance. The system integrates seamlessly with ASP.NET Core's built-in authorization framework while adding powerful resource-operation based permissions specifically designed for HTMX applications.