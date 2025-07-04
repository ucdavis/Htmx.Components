# AuthStatus ViewComponent

A self-contained ViewComponent that displays authentication status and user information.

## Structure

```
AuthStatus/
├── AuthStatusViewComponent.cs      # Main ViewComponent class
├── AuthStatusViewModel.cs          # View model for authentication data
├── IAuthStatusProvider.cs          # Interface for auth status providers
├── DefaultAuthStatusProvider.cs    # Default implementation
├── Views/
│   └── Default.cshtml             # Main component view
└── README.md                      # This file
```

## Features

- Displays user profile information when authenticated
- Shows login prompt when not authenticated
- Supports profile images with fallback to default icon
- Includes loading states and error handling
- Fully styled with component-specific CSS
- Enhanced with JavaScript for better UX

## Usage

```csharp
@await Component.InvokeAsync("AuthStatus")
```

## Configuration

The component uses the registered `IAuthStatusProvider` to determine authentication status and user information. You can provide a custom implementation to customize the behavior.

## Styling

The component includes its own CSS file that should be included in your layout:

```html
<link rel="stylesheet" href="~/css/auth-status.css" />
```

## JavaScript

The component includes JavaScript enhancements delivered through the `htmx-scripts` TagHelper:

```html
<!-- Include all behaviors (includes auth-retry) -->
<htmx-scripts></htmx-scripts>

<!-- Include only auth-retry behavior -->
<htmx-scripts include="auth-retry"></htmx-scripts>
```

The `auth-retry` behavior provides seamless authentication retry functionality with popup windows.
