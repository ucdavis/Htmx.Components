# Self-Contained ViewComponents

This folder demonstrates a self-contained ViewComponent pattern where each component keeps its related files together in a single directory structure.

## Structure

```
Components/
├── AuthStatus/
│   ├── AuthStatusViewComponent.cs
│   ├── AuthStatusViewModel.cs
│   ├── IAuthStatusProvider.cs
│   ├── DefaultAuthStatusProvider.cs
│   ├── AuthStatusUpdateAttribute.cs
│   ├── Internal/
│   │   └── AuthStatusUpdateFilter.cs    ← Internal infrastructure
│   ├── Views/
│   │   └── Default.cshtml
│   └── README.md (optional component documentation)
├── Table/
│   ├── TableViewComponent.cs
│   ├── TableProvider.cs
│   ├── TableActionAttributes.cs
│   ├── Internal/
│   │   ├── TableOobEditFilter.cs        ← Internal infrastructure
│   │   └── TableOobRefreshFilter.cs     ← Internal infrastructure
│   ├── Views/
│   │   ├── _Table.cshtml
│   │   ├── _TableBody.cshtml
│   │   └── ... (other table partials)
│   └── README.md
└── NavBar/
    ├── NavBarViewComponent.cs
    ├── NavActionAttribute.cs
    ├── Internal/
    │   └── NavActionResultFilter.cs      ← Internal infrastructure
    ├── Views/
    │   └── Default.cshtml
    └── README.md
```

## Benefits

- **Cohesion**: All related files (C#, Razor, attributes, docs) are in one place
- **Discoverability**: Easy to find all aspects of a component
- **Maintainability**: Changes to a component are localized
- **Reusability**: Components can be easily copied or extracted
- **Documentation**: Each component can have its own README
- **Component-specific attributes**: Attributes are co-located with their components
- **Internal organization**: Framework infrastructure is organized in Internal subfolders

## Setup

Self-contained ViewComponents are automatically enabled when you register Htmx.Components:

```csharp
builder.Services.AddHtmxComponents();
```

No additional configuration is needed - the `ComponentViewLocationExpander` is automatically registered.

## Notes

- This pattern maintains full compatibility with daisyUI and Tailwind CSS
- JavaScript behaviors are delivered through the `htmx-scripts` TagHelper system
- Views are discovered automatically through the custom view location expander
- Components remain fully testable and reusable
- **Component-specific attributes now use component namespaces:**
  - `AuthStatusUpdateAttribute` → `Htmx.Components.AuthStatus`
  - `NavActionAttribute`, `NavActionGroupAttribute` → `Htmx.Components.NavBar`
  - `TableEditActionAttribute`, `TableRefreshActionAttribute` → `Htmx.Components.Table`
  - General attributes like `ModelConfigAttribute` remain in `Htmx.Components.Attributes`
