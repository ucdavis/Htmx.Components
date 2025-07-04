# JavaScript Architecture

## Overview

Htmx.Components uses dynamically generated JavaScript behaviors delivered through Razor partial views and a unified TagHelper system. This approach enables server-side configuration injection and flexible script inclusion.

## Structure

### Partial Views Location
All JavaScript behaviors are located in:
```
/src/Views/Shared/Scripts/
├── _PageStateBehavior.cshtml
├── _TableBehavior.cshtml  
├── _BlurSaveCoordination.cshtml
└── _HtmxAuthRetry.cshtml
```

### Available Behaviors

- **PageStateBehavior**: Automatically adds page state to HTMX request headers
- **TableBehavior**: Defines 'tableinline' extension and manages table editing mode visual states
- **BlurSaveCoordination**: Prevents race conditions between blur events and save/submit operations
- **HtmxAuthRetry**: Handles popup-based authentication retry for 401 errors

## Usage

### Include All Scripts (Default)
```html
<htmx-scripts></htmx-scripts>
```

### Include Specific Scripts Only
```html
<htmx-scripts include="page-state,table-behavior"></htmx-scripts>
```

### Exclude Specific Scripts
```html
<htmx-scripts exclude="auth-retry"></htmx-scripts>
```

### Valid Script Names
- `page-state`
- `table-behavior` 
- `blur-save-coordination`
- `auth-retry`

## Benefits

1. **Dynamic Generation**: Scripts can include server-side generated content (URLs, configuration, etc.)
2. **Unified Management**: Single TagHelper manages all JavaScript inclusion
3. **Flexible Inclusion**: Choose which scripts to include per page/section
4. **Maintainability**: Scripts are organized as partial views alongside other Razor content
5. **Performance**: Inline scripts eliminate additional HTTP requests

## Adding New Behaviors

To add a new JavaScript behavior:

1. Create a new partial view in `/src/Views/Shared/Scripts/_YourBehavior.cshtml`
2. Add the mapping in `HtmxScriptsTagHelper.MapScriptName()` method
3. Add the script name to the `allScripts` array in `GetScriptsToInclude()` method
4. Document the new behavior in this file

## Architecture Benefits

This system enables:
- Server-side route generation for JavaScript
- Dynamic configuration injection based on application state
- Conditional script inclusion based on features/permissions
- Centralized management of JavaScript behaviors
- Easy testing and maintenance of individual behaviors

## Implementation Details

### HtmxScriptsTagHelper

The [`HtmxScriptsTagHelper`](../../api/Htmx.Components.TagHelpers.HtmxScriptsTagHelper.html) is the central component that manages JavaScript inclusion:

```csharp
[HtmlTargetElement("htmx-scripts")]
public class HtmxScriptsTagHelper : TagHelper
{
    // Manages script inclusion logic
    // Supports include/exclude attributes
    // Renders partial views dynamically
}
```

### Script Mapping

Each behavior is mapped to a specific partial view:

| Script Name | Partial View | Purpose |
|-------------|-------------|---------|
| `page-state` | `_PageStateBehavior.cshtml` | Page state management |
| `table-behavior` | `_TableBehavior.cshtml` | Table interactions |
| `blur-save-coordination` | `_BlurSaveCoordination.cshtml` | Form coordination |
| `auth-retry` | `_HtmxAuthRetry.cshtml` | Authentication handling |

### Best Practices

1. **Keep behaviors focused**: Each script should handle a single concern
2. **Use server-side data**: Leverage Razor syntax for dynamic configuration
3. **Test individually**: Each behavior can be tested in isolation
4. **Document thoroughly**: Update this documentation when adding new behaviors
5. **Consider performance**: Inline scripts reduce HTTP requests but increase page size
