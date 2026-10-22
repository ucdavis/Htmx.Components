# AuthStatus ViewComponent

A self-contained ViewComponent that displays authentication status and user information.

## Structure

```
AuthStatus/
├── AuthStatusViewComponent.cs      # Main ViewComponent class
├── IAuthStatusProvider.cs          # Interface for auth status providers
├── DefaultAuthStatusProvider.cs    # Default implementation
├── Internal/
│   └── AuthStatusUpdateFilter.cs   # OOB refresh filter
├── Models/
│   └── AuthStatusViewModel.cs      # View model for authentication data
├── Views/
│   └── Default.cshtml             # Main component view
└── README.md                      # This file
```

## Features

- Displays user profile information when authenticated
- Shows login prompt when not authenticated
- Supports profile images with fallback to default icon
- Includes loading states and error handling
- Styled with the package's Tailwind/DaisyUI component classes
- Enhanced through the packaged browser runtime

## Usage

```csharp
@await Component.InvokeAsync("AuthStatus")
```

## Configuration

The component uses the registered `IAuthStatusProvider` to determine authentication status and user information. You can provide a custom implementation to customize the behavior.

## JavaScript

The component includes JavaScript enhancements delivered through the `htmx-runtime` TagHelper:

```html
<!-- Include all behaviors (includes authentication-retry) -->
<htmx-runtime></htmx-runtime>

<!-- Include only authentication-retry behavior -->
<htmx-runtime include-behaviors="authentication-retry"></htmx-runtime>
```

The `authentication-retry` behavior provides seamless authentication retry functionality with popup windows.
