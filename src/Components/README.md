# Self-Contained ViewComponents

This folder demonstrates a self-contained ViewComponent pattern where each component keeps its related files together in a single directory structure.

## Structure

```
Components/
├── AuthStatus/
│   ├── AuthStatusViewComponent.cs
│   ├── IAuthStatusProvider.cs
│   ├── DefaultAuthStatusProvider.cs
│   ├── AuthStatusUpdateAttribute.cs
│   ├── Internal/
│   │   └── AuthStatusUpdateFilter.cs    ← Internal infrastructure
│   ├── Models/
│   │   └── AuthStatusViewModel.cs
│   ├── Views/
│   │   └── Default.cshtml
│   └── README.md (optional component documentation)
├── Table/
│   ├── TableViewComponent.cs
│   ├── TableProvider.cs
│   ├── Internal/
│   │   ├── TableActionAttributes.cs     ← Internal action attributes
│   │   ├── TableOobEditFilter.cs        ← Internal infrastructure
│   │   └── TableOobRefreshFilter.cs     ← Internal infrastructure
│   ├── Models/
│   │   ├── TableModel.cs
│   │   ├── TableColumnModel.cs
│   │   └── TableState.cs
│   ├── Views/
│   │   ├── _Table.cshtml
│   │   ├── _TableBody.cshtml
│   │   └── ... (other table partials)
│   └── README.md
├── Modal/
│   ├── ModalViewComponent.cs
│   ├── Models/
│   │   ├── ModalModel.cs
│   │   └── ModalSize.cs
│   └── Views/
│       └── Default.cshtml
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
builder.Services.AddHtmxComponents(options =>
{
    options.WithAuthorizationRequirementFactory<PermissionRequirementFactory>();
    options.WithResourceOperationRegistry<ResourceOperationRegistry>();
});
```

No additional view configuration is needed - the `ComponentViewLocationExpander` is automatically registered.

## Notes

- This pattern maintains full compatibility with daisyUI and Tailwind CSS
- JavaScript behaviors are delivered through the `htmx-runtime` TagHelper system
- Views are discovered automatically through the custom view location expander
- Components remain fully testable and reusable
- **Component-specific attributes now use component namespaces:**
  - `AuthStatusUpdateAttribute` → `Htmx.Components.AuthStatus`
  - `NavActionAttribute`, `NavActionGroupAttribute` → `Htmx.Components.NavBar`
  - Internal table action attributes and filters → `Htmx.Components.Table.Internal`
  - General attributes like `ModelConfigAttribute` remain in `Htmx.Components.Attributes`
