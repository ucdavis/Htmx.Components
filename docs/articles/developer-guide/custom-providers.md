# Custom Providers

The Htmx.Components framework is built around a provider pattern that allows you to customize core functionality. This guide covers implementing custom providers for various framework components.

## Navigation Providers

Navigation providers control how navigation menus are built and rendered.

### Custom Navigation Provider

```csharp
public class DatabaseNavProvider : INavProvider
{
    private readonly INavigationRepository _navRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;

    public DatabaseNavProvider(
        INavigationRepository navRepository,
        IAuthorizationService authorizationService,
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider)
    {
        _navRepository = navRepository;
        _authorizationService = authorizationService;
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
    }

    public async Task<IActionSet> BuildAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var builder = new ActionSetBuilder(_serviceProvider);

        // Load navigation structure from database
        var navigationItems = await _navRepository.GetNavigationStructureAsync();

        foreach (var item in navigationItems.Where(x => x.ParentId == null).OrderBy(x => x.Order))
        {
            await BuildNavigationItem(builder, item, navigationItems, user);
        }

        return await builder.BuildAsync();
    }

    private async Task BuildNavigationItem(
        ActionSetBuilder builder, 
        NavigationItem item, 
        List<NavigationItem> allItems, 
        ClaimsPrincipal user)
    {
        // Check authorization for this item
        if (!string.IsNullOrEmpty(item.RequiredPolicy))
        {
            var authResult = await _authorizationService.AuthorizeAsync(user, item.RequiredPolicy);
            if (!authResult.Succeeded)
                return;
        }

        var children = allItems.Where(x => x.ParentId == item.Id).OrderBy(x => x.Order).ToList();

        if (children.Any())
        {
            // This is a group
            builder.AddGroup(async group =>
            {
                group.WithLabel(item.DisplayName)
                     .WithIcon(item.Icon)
                     .WithClass(item.CssClass);

                foreach (var child in children)
                {
                    await BuildChildActionAsync(group, child, user);
                }
            });
        }
        else
        {
            // This is a direct action
            builder.AddAction(action => BuildActionModel(action, item));
        }
    }

    private async Task BuildChildActionAsync(
        ActionGroupBuilder group, 
        NavigationItem child, 
        ClaimsPrincipal user)
    {
        if (!string.IsNullOrEmpty(child.RequiredPolicy))
        {
            var authResult = await _authorizationService.AuthorizeAsync(user, child.RequiredPolicy);
            if (!authResult.Succeeded)
                return;
        }

        group.AddAction(action => BuildActionModel(action, child));
    }

    private ActionModelBuilder BuildActionModel(ActionModelBuilder action, NavigationItem item)
    {
        var currentPath = _httpContextAccessor.HttpContext?.Request.Path.Value ?? "";
        var isActive = currentPath.StartsWith(item.Url, StringComparison.OrdinalIgnoreCase);

        return action
            .WithLabel(item.DisplayName)
            .WithIcon(item.Icon)
            .WithClass(item.CssClass)
            .WithIsActive(isActive)
            .WithHxGet(item.Url)
            .WithHxPushUrl(item.PushUrl.ToString().ToLowerInvariant());
    }
}

// Entity model for database storage
public class NavigationItem
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string DisplayName { get; set; } = "";
    public string Url { get; set; } = "";
    public string Icon { get; set; } = "";
    public string CssClass { get; set; } = "";
    public string RequiredPolicy { get; set; } = "";
    public int Order { get; set; }
    public bool PushUrl { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// Repository interface
public interface INavigationRepository
{
    Task<List<NavigationItem>> GetNavigationStructureAsync();
    Task<NavigationItem> CreateNavigationItemAsync(NavigationItem item);
    Task<NavigationItem> UpdateNavigationItemAsync(NavigationItem item);
    Task DeleteNavigationItemAsync(int id);
}
```

## Authentication Status Providers

Custom authentication status providers allow you to control how user authentication information is displayed.

### OAuth/OpenID Connect Provider

```csharp
public class OidcAuthStatusProvider : IAuthStatusProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public OidcAuthStatusProvider(
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    public Task<AuthStatusViewModel> GetAuthStatusAsync(ClaimsPrincipal user)
    {
        var isAuthenticated = user.Identity?.IsAuthenticated ?? false;
        
        var model = new AuthStatusViewModel
        {
            IsAuthenticated = isAuthenticated,
            LoginUrl = "/Account/Login"
        };

        if (isAuthenticated)
        {
            // Extract user information from claims
            model.UserName = GetDisplayName(user);
            model.ProfileImageUrl = GetProfileImageUrl(user);
            
            // Add custom properties
            model.Email = user.FindFirst(ClaimTypes.Email)?.Value;
            model.Roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            model.LastLoginTime = GetLastLoginTime(user);
        }

        return Task.FromResult(model);
    }

    private string GetDisplayName(ClaimsPrincipal user)
    {
        // Try different claim types for display name
        return user.FindFirst("preferred_username")?.Value ??
               user.FindFirst("name")?.Value ??
               user.FindFirst(ClaimTypes.Name)?.Value ??
               user.FindFirst("email")?.Value ??
               "Unknown User";
    }

    private string GetProfileImageUrl(ClaimsPrincipal user)
    {
        // Try to get profile image from claims
        var pictureUrl = user.FindFirst("picture")?.Value;
        if (!string.IsNullOrEmpty(pictureUrl))
            return pictureUrl;

        // Fallback to Gravatar
        var email = user.FindFirst(ClaimTypes.Email)?.Value;
        if (!string.IsNullOrEmpty(email))
        {
            var hash = CreateMD5Hash(email.ToLowerInvariant());
            return $"https://www.gravatar.com/avatar/{hash}?s=80&d=identicon";
        }

        return null;
    }

    private DateTime? GetLastLoginTime(ClaimsPrincipal user)
    {
        var lastLoginClaim = user.FindFirst("last_login")?.Value;
        if (DateTime.TryParse(lastLoginClaim, out var lastLogin))
            return lastLogin;
        
        return null;
    }

    private string CreateMD5Hash(string input)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}

// Extended view model
public class ExtendedAuthStatusViewModel : AuthStatusViewModel
{
    public string Email { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime? LastLoginTime { get; set; }
    public string TimeZone { get; set; }
    public string PreferredLanguage { get; set; }
}
```

### Database-Driven Auth Status Provider

```csharp
public class DatabaseAuthStatusProvider : IAuthStatusProvider
{
    private readonly IUserRepository _userRepository;
    private readonly IMemoryCache _cache;

    public DatabaseAuthStatusProvider(
        IUserRepository userRepository,
        IMemoryCache cache)
    {
        _userRepository = userRepository;
        _cache = cache;
    }

    public async Task<AuthStatusViewModel> GetAuthStatusAsync(ClaimsPrincipal user)
    {
        var isAuthenticated = user.Identity?.IsAuthenticated ?? false;
        
        if (!isAuthenticated)
        {
            return new AuthStatusViewModel
            {
                IsAuthenticated = false,
                LoginUrl = "/Account/Login"
            };
        }

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return new AuthStatusViewModel { IsAuthenticated = false };
        }

        // Cache user data for 5 minutes
        var cacheKey = $"user_status_{userId}";
        var userStatus = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await _userRepository.GetUserStatusAsync(userId);
        });

        return new AuthStatusViewModel
        {
            IsAuthenticated = true,
            UserName = userStatus.DisplayName,
            ProfileImageUrl = userStatus.ProfileImageUrl,
            Email = userStatus.Email,
            Roles = userStatus.Roles,
            NotificationCount = userStatus.UnreadNotifications,
            IsOnline = userStatus.IsOnline,
            LastSeen = userStatus.LastSeen
        };
    }
}

public class UserStatus
{
    public string UserId { get; set; }
    public string DisplayName { get; set; }
    public string Email { get; set; }
    public string ProfileImageUrl { get; set; }
    public List<string> Roles { get; set; } = new();
    public int UnreadNotifications { get; set; }
    public bool IsOnline { get; set; }
    public DateTime LastSeen { get; set; }
    public Dictionary<string, object> CustomProperties { get; set; } = new();
}
```

## Authorization Providers

Custom authorization providers enable sophisticated permission systems.

### Permission-Based Authorization

```csharp
public class AuthorizationRequirementFactory : IAuthorizationRequirementFactory
{
    public IAuthorizationRequirement ForOperation(string resource, string operation)
    {
        return new PermissionRequirement(resource, operation);
    }

    public IAuthorizationRequirement ForRoles(params string[] roles)
    {
        return new RolesAuthorizationRequirement(roles);
    }
}

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Resource { get; }
    public string Operation { get; }

    public PermissionRequirement(string resource, string operation)
    {
        Resource = resource;
        Operation = operation;
    }
}

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService _permissionService;
    private readonly IMemoryCache _cache;

    public PermissionAuthorizationHandler(
        IPermissionService permissionService,
        IMemoryCache cache)
    {
        _permissionService = permissionService;
        _cache = cache;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            context.Fail();
            return;
        }

        var cacheKey = $"permission_{userId}_{requirement.Resource}_{requirement.Operation}";
        var hasPermission = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            return await _permissionService.UserHasPermissionAsync(
                userId, requirement.Resource, requirement.Operation);
        });

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}

public interface IPermissionService
{
    Task<bool> UserHasPermissionAsync(string userId, string resource, string operation);
    Task GrantPermissionAsync(string userId, string resource, string operation);
    Task RevokePermissionAsync(string userId, string resource, string operation);
    Task<List<Permission>> GetUserPermissionsAsync(string userId);
}

public class Permission
{
    public string Resource { get; set; }
    public string Operation { get; set; }
    public DateTime GrantedAt { get; set; }
    public string GrantedBy { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
```

### Hierarchical Resource Authorization

```csharp
public class HierarchicalResourceRegistry : IResourceOperationRegistry
{
    private readonly IResourceHierarchyService _hierarchyService;
    private readonly Dictionary<string, HashSet<string>> _registeredOperations;

    public HierarchicalResourceRegistry(IResourceHierarchyService hierarchyService)
    {
        _hierarchyService = hierarchyService;
        _registeredOperations = new Dictionary<string, HashSet<string>>();
    }

    public async Task Register(string resource, string operation)
    {
        if (!_registeredOperations.ContainsKey(resource))
        {
            _registeredOperations[resource] = new HashSet<string>();
        }

        _registeredOperations[resource].Add(operation);

        // Register with hierarchy service
        await _hierarchyService.RegisterResourceAsync(resource, operation);

        // Auto-register inherited permissions
        var parentResources = await _hierarchyService.GetParentResourcesAsync(resource);
        foreach (var parentResource in parentResources)
        {
            if (!_registeredOperations.ContainsKey(parentResource))
            {
                _registeredOperations[parentResource] = new HashSet<string>();
            }
            _registeredOperations[parentResource].Add(operation);
        }
    }

    public Task<bool> IsRegisteredAsync(string resource, string operation)
    {
        return Task.FromResult(
            _registeredOperations.TryGetValue(resource, out var operations) &&
            operations.Contains(operation));
    }
}

public interface IResourceHierarchyService
{
    Task RegisterResourceAsync(string resource, string operation);
    Task<List<string>> GetParentResourcesAsync(string resource);
    Task<List<string>> GetChildResourcesAsync(string resource);
    Task SetResourceParentAsync(string resource, string parentResource);
}

// Example: Projects -> ProjectGroups -> Organization
// If user has "Read" on Organization, they inherit "Read" on all ProjectGroups and Projects
```

## Table Providers

Custom table providers enable specialized data handling scenarios.

### Cached Table Provider

```csharp
public class CachedTableProvider : ITableProvider
{
    private readonly ITableProvider _baseProvider;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedTableProvider> _logger;

    public CachedTableProvider(
        ITableProvider baseProvider,
        IMemoryCache cache,
        ILogger<CachedTableProvider> logger)
    {
        _baseProvider = baseProvider;
        _cache = cache;
        _logger = logger;
    }

    public async Task FetchPageAsync<T, TKey>(
        TableModel<T, TKey> tableModel,
        IQueryable<T> query,
        TableState tableState) where T : class
    {
        var cacheKey = GenerateCacheKey(tableModel.TypeId, tableState);
        
        var cachedResult = _cache.Get<CachedTableResult<T, TKey>>(cacheKey);
        if (cachedResult != null && IsValidCache(cachedResult, tableState))
        {
            _logger.LogDebug("Using cached table data for {TypeId}", tableModel.TypeId);
            ApplyCachedResult(tableModel, cachedResult);
            return;
        }

        // Fetch fresh data
        await _baseProvider.FetchPageAsync(tableModel, query, tableState);

        // Cache the result
        var resultToCache = new CachedTableResult<T, TKey>
        {
            Rows = tableModel.Rows.ToList(),
            PageCount = tableModel.PageCount,
            State = tableState,
            CachedAt = DateTime.UtcNow
        };

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = GetCacheExpiration(tableModel.TypeId),
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(cacheKey, resultToCache, cacheOptions);
        _logger.LogDebug("Cached table data for {TypeId}", tableModel.TypeId);
    }

    private string GenerateCacheKey(string typeId, TableState state)
    {
        var stateHash = JsonSerializer.Serialize(state).GetHashCode();
        return $"table_{typeId}_{stateHash}";
    }

    private bool IsValidCache<T, TKey>(CachedTableResult<T, TKey> cached, TableState current)
        where T : class
    {
        // Check if cache is still valid based on state comparison
        return cached.State.Page == current.Page &&
               cached.State.PageSize == current.PageSize &&
               cached.State.SortColumn == current.SortColumn &&
               cached.State.SortDirection == current.SortDirection &&
               FiltersMatch(cached.State.Filters, current.Filters);
    }

    private bool FiltersMatch(
        Dictionary<string, string> cached, 
        Dictionary<string, string> current)
    {
        if (cached.Count != current.Count) return false;
        
        return cached.All(kvp => 
            current.TryGetValue(kvp.Key, out var value) && 
            value == kvp.Value);
    }

    private void ApplyCachedResult<T, TKey>(
        TableModel<T, TKey> tableModel, 
        CachedTableResult<T, TKey> cached) where T : class
    {
        tableModel.Rows = cached.Rows;
        tableModel.PageCount = cached.PageCount;
        tableModel.State = cached.State;
    }

    private TimeSpan GetCacheExpiration(string typeId)
    {
        // Different cache durations based on data type
        return typeId switch
        {
            "User" => TimeSpan.FromMinutes(5),   // User data changes frequently
            "Product" => TimeSpan.FromMinutes(15), // Products change less often
            "Category" => TimeSpan.FromHours(1),   // Categories rarely change
            _ => TimeSpan.FromMinutes(10)          // Default cache duration
        };
    }
}

public class CachedTableResult<T, TKey> where T : class
{
    public List<TableRowContext<T, TKey>> Rows { get; set; } = new();
    public int PageCount { get; set; }
    public TableState State { get; set; } = new();
    public DateTime CachedAt { get; set; }
}
```

### Multi-Source Table Provider

```csharp
public class MultiSourceTableProvider : ITableProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, ITableProvider> _providers;

    public MultiSourceTableProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _providers = new Dictionary<string, ITableProvider>
        {
            ["database"] = new DatabaseTableProvider(),
            ["api"] = new ApiTableProvider(serviceProvider.GetService<IHttpClientFactory>()),
            ["cache"] = new CachedTableProvider(
                new DatabaseTableProvider(), 
                serviceProvider.GetService<IMemoryCache>(),
                serviceProvider.GetService<ILogger<CachedTableProvider>>())
        };
    }

    public async Task FetchPageAsync<T, TKey>(
        TableModel<T, TKey> tableModel,
        IQueryable<T> query,
        TableState tableState) where T : class
    {
        var dataSource = GetDataSource(tableModel.TypeId);
        var provider = _providers[dataSource];
        
        await provider.FetchPageAsync(tableModel, query, tableState);
    }

    private string GetDataSource(string typeId)
    {
        // Route different entity types to different data sources
        return typeId switch
        {
            "ExternalUser" => "api",
            "CachedReport" => "cache",
            _ => "database"
        };
    }
}

public class ApiTableProvider : ITableProvider
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ApiTableProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task FetchPageAsync<T, TKey>(
        TableModel<T, TKey> tableModel,
        IQueryable<T> query,
        TableState tableState) where T : class
    {
        var httpClient = _httpClientFactory.CreateClient("ExternalApi");
        
        var request = new ApiTableRequest
        {
            EntityType = tableModel.TypeId,
            Page = tableState.Page,
            PageSize = tableState.PageSize,
            SortColumn = tableState.SortColumn,
            SortDirection = tableState.SortDirection,
            Filters = tableState.Filters
        };

        var response = await httpClient.PostAsJsonAsync("/api/table-data", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ApiTableResponse<T>>();
        
        tableModel.PageCount = result.PageCount;
        tableModel.State = tableState;
        
        var keySelector = tableModel.KeySelector?.CompileFast();
        tableModel.Rows = result.Data.Select((item, index) =>
        {
            var key = keySelector != null ? keySelector(item) : default;
            return new TableRowContext<T, TKey>
            {
                Item = item,
                ModelHandler = tableModel.ModelHandler,
                PageIndex = index,
                Key = key
            };
        }).ToList();
    }
}
```

## Model Registry Providers

Custom model registries enable dynamic model registration and configuration.

### Attribute-Scanning Registry

```csharp
public class AttributeScanningModelRegistry : ModelRegistry
{
    public AttributeScanningModelRegistry(
        ViewPaths viewPaths, 
        IServiceProvider serviceProvider,
        IResourceOperationRegistry resourceOperationRegistry) 
        : base(viewPaths, serviceProvider, resourceOperationRegistry)
    {
        ScanAndRegisterModels();
    }

    private void ScanAndRegisterModels()
    {
        var modelTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetCustomAttribute<AutoRegisterModelAttribute>() != null)
            .ToList();

        foreach (var modelType in modelTypes)
        {
            var attribute = modelType.GetCustomAttribute<AutoRegisterModelAttribute>();
            RegisterModelType(modelType, attribute);
        }
    }

    private void RegisterModelType(Type modelType, AutoRegisterModelAttribute attribute)
    {
        var keyType = attribute.KeyType ?? typeof(int);
        var typeId = attribute.TypeId ?? modelType.Name;

        // Use reflection to call the generic Register method
        var registerMethod = typeof(IModelRegistry)
            .GetMethod("Register")
            ?.MakeGenericMethod(modelType, keyType);

        var configAction = CreateConfigurationAction(modelType, keyType, attribute);
        
        registerMethod?.Invoke(this, new object[] { typeId, configAction });
    }

    private object CreateConfigurationAction(Type modelType, Type keyType, AutoRegisterModelAttribute attribute)
    {
        // Create a configuration action that sets up basic CRUD operations
        var configMethod = typeof(AttributeScanningModelRegistry)
            .GetMethod(nameof(ConfigureModel), BindingFlags.NonPublic | BindingFlags.Instance)
            ?.MakeGenericMethod(modelType, keyType);

        return configMethod?.Invoke(this, new object[] { attribute });
    }

    private Action<IServiceProvider, ModelHandlerBuilder<T, TKey>> ConfigureModel<T, TKey>(
        AutoRegisterModelAttribute attribute) where T : class, new()
    {
        return (serviceProvider, builder) =>
        {
            // Auto-configure based on attribute properties
            if (attribute.EnableCrud)
            {
                var dbContext = serviceProvider.GetRequiredService<DbContext>();
                builder.WithQueryable(() => dbContext.Set<T>());

                if (attribute.EnableCreate)
                {
                    builder.WithCreate(async entity =>
                    {
                        dbContext.Add(entity);
                        await dbContext.SaveChangesAsync();
                        return Result.Value(entity);
                    });
                }

                if (attribute.EnableUpdate)
                {
                    builder.WithUpdate(async entity =>
                    {
                        dbContext.Update(entity);
                        await dbContext.SaveChangesAsync();
                        return Result.Value(entity);
                    });
                }

                if (attribute.EnableDelete)
                {
                    builder.WithDelete(async key =>
                    {
                        var entity = await dbContext.Set<T>().FindAsync(key);
                        if (entity != null)
                        {
                            dbContext.Remove(entity);
                            await dbContext.SaveChangesAsync();
                        }
                        return Result.Ok();
                    });
                }
            }

            // Auto-configure table columns based on properties
            builder.WithTable(table =>
            {
                var properties = typeof(T).GetProperties()
                    .Where(p => p.GetCustomAttribute<TableColumnAttribute>() != null);

                foreach (var property in properties)
                {
                    var columnAttr = property.GetCustomAttribute<TableColumnAttribute>();
                    AddTableColumn(table, property, columnAttr);
                }
            });
        };
    }

    private void AddTableColumn<T, TKey>(
        TableModelBuilder<T, TKey> table, 
        PropertyInfo property, 
        TableColumnAttribute columnAttr) where T : class
    {
        // Use expression trees to create property selectors
        var parameter = Expression.Parameter(typeof(T), "x");
        var propertyAccess = Expression.Property(parameter, property);
        var lambda = Expression.Lambda<Func<T, object>>(
            Expression.Convert(propertyAccess, typeof(object)), parameter);

        table.AddSelectorColumn(lambda, column =>
        {
            if (!string.IsNullOrEmpty(columnAttr.Header))
                column.WithHeader(columnAttr.Header);

            if (columnAttr.Sortable)
                column.WithSortable(true);

            if (columnAttr.Filterable)
                column.WithFilter((query, value) => 
                    query.Where($"{property.Name}.Contains(@0)", value));

            if (columnAttr.Editable)
                column.WithEditable(true);
        });
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class AutoRegisterModelAttribute : Attribute
{
    public string TypeId { get; set; }
    public Type KeyType { get; set; }
    public bool EnableCrud { get; set; } = true;
    public bool EnableCreate { get; set; } = true;
    public bool EnableUpdate { get; set; } = true;
    public bool EnableDelete { get; set; } = true;
}

[AttributeUsage(AttributeTargets.Property)]
public class TableColumnAttribute : Attribute
{
    public string Header { get; set; }
    public bool Sortable { get; set; } = true;
    public bool Filterable { get; set; } = false;
    public bool Editable { get; set; } = false;
    public int Order { get; set; } = 0;
}

// Usage example:
[AutoRegisterModel(TypeId = "User", EnableCrud = true)]
public class User
{
    [TableColumn(Header = "ID", Sortable = true, Filterable = false)]
    public int Id { get; set; }

    [TableColumn(Header = "Name", Sortable = true, Filterable = true, Editable = true)]
    public string Name { get; set; }

    [TableColumn(Header = "Email", Sortable = true, Filterable = true, Editable = true)]
    public string Email { get; set; }

    [TableColumn(Header = "Created", Sortable = true, Filterable = false)]
    public DateTime CreatedAt { get; set; }
}
```

This comprehensive guide covers the major provider extension points in the Htmx.Components framework. Each provider type offers different levels of customization to meet specific application requirements while maintaining integration with the core framework functionality.