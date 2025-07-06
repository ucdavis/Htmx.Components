# JavaScript Architecture

## Overview

Htmx.Components uses dynamically generated JavaScript behaviors delivered through Razor partial views and a unified TagHelper system. This approach enables server-side configuration injection and flexible script inclusion.

## Structure

### Partial Views Location
All JavaScript behaviors are located in:
```
/src/Views/Shared/Scripts/
├── _PageStateHeaders.cshtml
├── _TableInlineEditing.cshtml  
├── _BlurSaveCoordination.cshtml
└── _AuthenticationRetry.cshtml
```

### Available Behaviors

- **PageStateHeaders**: Automatically adds page state to HTMX request headers
- **TableInlineEditing**: Defines 'tableinline' extension and manages table editing mode visual states
- **BlurSaveCoordination**: Prevents race conditions between blur events and save/submit operations
- **AuthenticationRetry**: Handles popup-based authentication retry for 401 errors

## Usage

### Include All Scripts (Default)
```html
<htmx-scripts></htmx-scripts>
```

### Include Specific Scripts Only
```html
<htmx-scripts include="page-state-headers,table-inline-editing"></htmx-scripts>
```

### Exclude Specific Scripts
```html
<htmx-scripts exclude="authentication-retry"></htmx-scripts>
```

### Valid Script Names
- `page-state-headers`
- `table-inline-editing` 
- `blur-save-coordination`
- `authentication-retry`

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

Each behavior is delivered through a Razor partial view:

| Partial View | Purpose |
|-------------|---------|
| `_PageStateHeaders.cshtml` | Page state management |
| `_TableInlineEditing.cshtml` | Table interactions |
| `_BlurSaveCoordination.cshtml` | Form coordination |
| `_AuthenticationRetry.cshtml` | Authentication handling |

