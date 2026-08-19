# JavaScript Architecture

## Overview

Htmx.Components uses a packaged static web asset runtime delivered through a unified TagHelper system. The TagHelper emits versioned script references plus a small JSON configuration block so pages can choose which runtime behaviors are active without generating inline JavaScript.

## Structure

### Source Location
The maintainable runtime source is split into TypeScript modules under:
```text
/tools/runtime/src
```

The bundled static web asset is generated at:
```text
/wwwroot/js/htmx-components.js
```

`wwwroot/js/htmx-components.js` remains checked in and is the only browser asset referenced by the TagHelper.

### Available Behaviors

- **page-state-headers**: Automatically adds page state to HTMX request headers
- **table-inline-editing**: Defines the `tableinline` extension and manages table editing mode visual states
- **blur-save-coordination**: Prevents race conditions between blur events and save/submit operations
- **request-lifecycle**: Manages default pending UI state for HTMX requests
- **error-handling**: Routes expected HTMX failures to scoped or global error regions
- **authentication-retry**: Handles popup-based authentication retry for 401 errors
- **modal**: Opens HTMX-loaded modal dialogs, handles close triggers, resets modal body content, and restores focus

### Custom Elements

The runtime registers lightweight custom elements that let server-rendered markup opt into scoped behavior without page-local scripts:

- `htmx-table`: table component root; carries the stable table component id and dispatches table lifecycle events
- `htmx-request-scope`: nearest request boundary for pending state, indicators, stale regions, and disabled controls
- `htmx-error-region`: local or global destination for safe error messages

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
- `request-lifecycle`
- `error-handling`
- `authentication-retry`
- `modal`

## Benefits

1. **Packaged Delivery**: Behaviors are served from a versioned static web asset
2. **Unified Management**: Single TagHelper manages all JavaScript inclusion
3. **Flexible Inclusion**: Choose which scripts to include per page/section
4. **Maintainability**: Runtime behavior lives in focused TypeScript modules that build to one browser-tested JavaScript asset
5. **Performance**: Static assets can be cached independently of server-rendered pages

## Adding New Behaviors

To add a new JavaScript behavior:

1. Add the behavior installer to `/tools/runtime/src/behaviors`
2. Add the script name to the `allScripts` array in `HtmxScriptsTagHelper.GetScriptsToInclude()`
3. Add the mapping in `HtmxScriptsTagHelper.MapScriptName()`
4. Register the installer in `/tools/runtime/src/index.ts`
5. Run `npm run build:js` and commit the updated `/wwwroot/js/htmx-components.js`
6. Add unit or browser-smoke coverage for the behavior
7. Document the new behavior in this file

## Architecture Benefits

This system enables:
- Versioned static web asset delivery
- Minimal server-side configuration through JSON
- Conditional script inclusion based on features/permissions
- Centralized management of JavaScript behaviors
- Browser-level testing of the shared runtime

## Implementation Details

### HtmxScriptsTagHelper

The <xref:Htmx.Components.TagHelpers.HtmxScriptsTagHelper> is the central component that manages JavaScript inclusion:

```csharp
[HtmlTargetElement("htmx-scripts")]
public class HtmxScriptsTagHelper : TagHelper
{
    // Manages script inclusion logic
    // Supports include/exclude attributes
    // Emits runtime configuration and a versioned static script reference
}
```

### Script Mapping

Each behavior is installed from the packaged runtime:

| Behavior | Purpose |
|-------------|---------|
| `page-state-headers` | Page state management |
| `table-inline-editing` | Table interactions |
| `blur-save-coordination` | Form coordination |
| `request-lifecycle` | Pending UI behavior |
| `error-handling` | Scoped/global HTMX error display |
| `authentication-retry` | Authentication handling |
| `modal` | HTMX-loaded modal dialog behavior |

### Request Lifecycle Contract

`request-lifecycle` watches HTMX request events and resolves the nearest `htmx-request-scope` from the trigger or target. Within that scope it can:

- Disable the trigger or the configured selector through `data-hc-pending-disable`
- Show a request indicator selected by `data-hc-request-indicator-selector`
- Mark stale regions selected by `data-hc-stale-region-selector`
- Restore all pending state after success, response error, send error, abort, or timeout

Elements can opt out of scope-level disabling with `data-hc-no-pending-disable`.

### Error Handling Contract

`error-handling` clears a scope's error region before a new request and renders user-safe messages for `htmx:responseError`, `htmx:sendError`, `htmx:timeout`, and `htmx:swapError`. It prefers the nearest scoped `htmx-error-region` and falls back to a global region marked with `data-hc-global-error-region`.
