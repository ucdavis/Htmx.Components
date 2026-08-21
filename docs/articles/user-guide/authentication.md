# Authentication

Htmx.Components provides seamless integration with ASP.NET Core authentication and includes special handling for HTMX requests.

## Basic Authentication Setup

Htmx.Components works with any ASP.NET Core authentication scheme. Here's an OpenID Connect example:

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
[Route("Users")]
[NavActionGroup(DisplayName = "Users", Icon = "fas fa-users", Order = 2)]
public class UsersController : Controller
{
    [HttpGet("Manage")]
    [Authorize(Policy = "CanManageUsers")]
    [NavAction(DisplayName = "Manage Users", Icon = "fas fa-users-cog", Order = 1)]
    public async Task<IActionResult> Manage()
    {
        // Only users with CanManageUsers policy can see and access this
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

1. **Claim Configuration**: Configure the correct user ID claim type for your identity provider
2. **Authorization Attributes**: Use standard ASP.NET Core authorization attributes on controllers and actions
3. **Status Updates**: Use `[AuthStatusUpdate]` on actions that change authentication state

## Security Considerations

### Session Security

Configure secure session options when your application uses session state:

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

The packaged runtime is served from your application under `/_content/Htmx.Components/`. If you use the popup-login view shown above, account for that inline script with a nonce or a narrowly scoped CSP exception.

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline';");
    await next();
});
```

## Troubleshooting

### Authentication Loops

If users get stuck in authentication loops:

1. Check that `UseHtmxPageState()` is called before authentication middleware
2. Verify authentication scheme configuration
3. Ensure proper HTMX headers are being sent

### HTMX Auth Popup Not Working

1. Verify the popup configuration is correct
2. Check that `<htmx-runtime></htmx-runtime>` includes `authentication-retry`
3. Ensure popup blockers are not interfering

## Next Steps

- **[Authorization](authorization.md)**: Learn about setting up authorization policies and permissions
- **[Navigation](navigation.md)**: Understand how navigation integrates with authentication
- **[Tables](tables.md)**: See how tables respect authorization rules
