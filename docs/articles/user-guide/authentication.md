# Authentication

Htmx.Components provides seamless integration with ASP.NET Core authentication and includes special handling for HTMX requests.

## Basic Authentication Setup

Htmx.Components works with any ASP.NET Core authentication scheme. Here's a real example from CruSibyl.Web using OpenID Connect:

```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie()
.AddOpenIdConnect(oidc =>
{
    oidc.ClientId = builder.Configuration["Authentication:ClientId"];
    oidc.ClientSecret = builder.Configuration["Authentication:ClientSecret"];
    oidc.Authority = builder.Configuration["Authentication:Authority"];
    oidc.ResponseType = OpenIdConnectResponseType.Code;
    oidc.Scope.Add("openid");
    oidc.Scope.Add("profile");
    oidc.Scope.Add("email");
    oidc.Scope.Add("eduPerson");
    oidc.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
    };
    
    // Configure HTMX-specific authentication handling
    oidc.ConfigureHtmxAuthPopup("/auth/popup-login");
    oidc.AddIamFallback();
});

// Configure Htmx.Components with authentication
builder.Services.AddHtmxComponents(htmxOptions =>
{
    htmxOptions.WithUserIdClaimType("your-user-id-claim-type");
    // ... other options
});

// Configure middleware
app.UseHtmxPageState();  // Important: Before authentication
app.UseAuthentication();
app.UseAuthorization();
```

## HTMX Authentication Popup

Htmx.Components includes special handling for authentication in HTMX requests. When an unauthenticated HTMX request is made, instead of redirecting the entire page, it can show a popup for authentication.

### Popup Login Controller

```csharp
[Authorize]
public class AuthController : Controller
{
    [Authorize]
    [AuthStatusUpdate]  // This attribute updates the AuthStatus component after login
    [HttpGet("/auth/login")]
    public IActionResult Login()
    {
        // If this executes, the user is already authenticated
        return Ok();
    }

    [Authorize]
    [HttpGet("/auth/popup-login")]
    public IActionResult PopupLogin()
    {
        // Return a view that posts a message to the parent window and closes itself
        return View();
    }
}
```

### Popup Login View

Create `Views/Auth/PopupLogin.cshtml`:

```html
<!DOCTYPE html>
<html>
<head>
    <title>Login Success</title>
</head>
<body>
    <script>
        // Notify the opener window and close the popup
        window.opener?.postMessage('login-success', '*');
        window.close();
    </script>
</body>
</html>
```

## AuthStatus Component

The `AuthStatus` component displays the current user's authentication status and provides login/logout functionality.

### Basic Usage

Include the component in your layout:

```html
<div class="navbar-end">
    @await Component.InvokeAsync("NavBar")
    @await Component.InvokeAsync("AuthStatus")
</div>
```

### AuthStatusUpdate Attribute

Use the `[AuthStatusUpdate]` attribute on actions that change authentication state to automatically refresh the AuthStatus component:

```csharp
[Authorize]
[AuthStatusUpdate]
[HttpGet("/auth/login")]
public IActionResult Login()
{
    return Ok();
}

[AuthStatusUpdate]
[HttpPost("/auth/logout")]
public IActionResult Logout()
{
    return SignOut(CookieAuthenticationDefaults.AuthenticationScheme, 
                   OpenIdConnectDefaults.AuthenticationScheme);
}
```

## Authorization with Navigation

Navigation items are automatically filtered based on user permissions:

```csharp
[Route("Admin")]
[NavActionGroup(DisplayName = "Admin", Icon = "fas fa-cogs", Order = 2)]
public class AdminController : Controller
{
    [HttpGet("AdminUsers")]
    [Authorize(Policy = "SystemAccess")]
    [NavAction(DisplayName = "Admin Users", Icon = "fas fa-users-cog", Order = 1)]
    public async Task<IActionResult> AdminUsers()
    {
        // Only users with SystemAccess policy can see and access this
        return Ok(tableModel);
    }
}
```

## User Claims Configuration

Configure which claim type contains the user ID:

```csharp
builder.Services.AddHtmxComponents(htmxOptions =>
{
    htmxOptions.WithUserIdClaimType("sub");  // or whatever your user ID claim is
});
```

## Cookie Authentication Example

For simpler scenarios, you can use cookie authentication:

```csharp
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
    });
```

## How It Works

1. **Automatic Detection**: Htmx.Components automatically detects when HTMX requests need authentication
2. **Popup Authentication**: Instead of redirecting the entire page, authentication can happen in a popup
3. **Status Updates**: The `AuthStatus` component automatically updates when authentication state changes
4. **Navigation Filtering**: Navigation items are filtered based on user authorization
5. **Seamless Integration**: Works with any ASP.NET Core authentication provider

## Best Practices

1. **Middleware Order**: Always place `UseHtmxPageState()` before `UseAuthentication()`
2. **Claim Configuration**: Configure the correct user ID claim type for your identity provider
3. **Authorization Attributes**: Use standard ASP.NET Core authorization attributes on controllers and actions
4. **Status Updates**: Use `[AuthStatusUpdate]` on actions that change authentication state
5. **Popup Handling**: Implement proper popup authentication for better user experience

## Next Steps

- **[Authorization](authorization.md)**: Learn about setting up authorization policies and permissions
- **[Navigation](navigation.md)**: Understand how navigation integrates with authentication
- **[Tables](tables.md)**: See how tables respect authorization rules
    foreach (var role in roles)
    {
        identity.AddClaim(new Claim(ClaimTypes.Role, role));
    }
    
    return identity;
}
```

### User Service Integration

Create a user service for profile management:

```csharp
public interface IUserService
{
    Task<UserProfile> GetUserProfileAsync(string userId);
    Task<bool> UpdateProfileAsync(string userId, UserProfile profile);
}

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;

    public UserService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserProfile> GetUserProfileAsync(string userId)
    {
        var user = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => new UserProfile
            {
                Id = u.Id,
                DisplayName = u.DisplayName,
                Email = u.Email,
                AvatarUrl = u.AvatarUrl,
                LastLoginAt = u.LastLoginAt
            })
            .FirstOrDefaultAsync();
            
        return user ?? new UserProfile();
    }
}
```

## Multi-Tenant Authentication

### Tenant Resolution

Implement tenant resolution for multi-tenant applications:

```csharp
public class TenantAuthStatusProvider : IAuthStatusProvider
{
    private readonly ITenantService _tenantService;

    public async Task<AuthStatusViewModel> GetAuthStatusAsync(ClaimsPrincipal user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return new AuthStatusViewModel { IsAuthenticated = false };

        var tenantId = user.FindFirst("TenantId")?.Value;
        var tenant = await _tenantService.GetTenantAsync(tenantId);

        return new AuthStatusViewModel
        {
            IsAuthenticated = true,
            UserName = $"{user.Identity.Name} ({tenant?.Name})",
            ProfileImageUrl = tenant?.LogoUrl
        };
    }
}
```

## Security Considerations

### CSRF Protection

HTMX Components automatically handles CSRF tokens:

```html
<!-- CSRF tokens are automatically included in HTMX requests -->
<button hx-post="/api/secure-action">Secure Action</button>
```

### Session Security

Configure secure session options:

```csharp
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
```

### Content Security Policy

Configure CSP for HTMX compatibility:

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Content-Security-Policy", 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://unpkg.com; " +
        "style-src 'self' 'unsafe-inline';");
    await next();
});
```

## Best Practices

1. **Use HTTPS**: Always use HTTPS in production
2. **Secure Cookies**: Configure secure cookie settings
3. **Token Validation**: Validate authentication tokens properly
4. **Session Management**: Implement proper session timeout
5. **Audit Logging**: Log authentication events
6. **Rate Limiting**: Implement rate limiting for login attempts

## Troubleshooting

### Authentication Loops

If users get stuck in authentication loops:

1. Check that `UseHtmxPageState()` is called before authentication middleware
2. Verify authentication scheme configuration
3. Ensure proper HTMX headers are being sent

### HTMX Auth Popup Not Working

1. Verify the popup configuration is correct
2. Check that JavaScript event handlers are registered
3. Ensure popup blockers aren't interfering

### Claims Not Available

1. Check that claims are properly added during authentication
2. Verify claim transformations are working
3. Ensure user service is correctly resolving user data