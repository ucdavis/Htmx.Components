# JavaScript Architecture

## Overview

Htmx.Components uses a packaged static web asset runtime delivered through a unified TagHelper system. The TagHelper emits one versioned runtime reference plus a small JSON configuration block so pages can choose which runtime behaviors are active without generating inline JavaScript.

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

The runtime registers lightweight custom elements that let server-rendered markup opt into scoped behavior without page-local behavior code:

- `htmx-table`: table component root; carries the stable table component id and dispatches table lifecycle events
- `htmx-request-scope`: nearest request boundary for pending state, indicators, stale regions, and disabled controls
- `htmx-error-region`: local or global destination for safe error messages

## Usage

### Include All Behaviors (Default)
```html
<htmx-runtime></htmx-runtime>
```

### Include Specific Behaviors Only
```html
<htmx-runtime include-behaviors="page-state-headers,table-inline-editing"></htmx-runtime>
```

### Exclude Specific Behaviors
```html
<htmx-runtime exclude-behaviors="authentication-retry"></htmx-runtime>
```

### Valid Behavior Names
- `page-state-headers`
- `table-inline-editing` 
- `blur-save-coordination`
- `request-lifecycle`
- `error-handling`
- `authentication-retry`
- `modal`

## Loading and failure behavior

Load HTMX before the runtime. Body-dependent behavior installation waits for DOM readiness, and automatic startup emits one `htmx-components:load` event. Explicit `HtmxComponents.init(root)` calls still synchronize the requested root and emit an event, as do subsequent HTMX content loads. Repeated bundle references do not install duplicate listeners. A missing HTMX dependency produces a warning; reverse script order is not supported.

Blur-triggered field updates finish before a deferred save is replayed. If they remain pending after roughly one second, the save stops and displays an error; the user can retry after the updates finish. Disconnected save elements are discarded.

Structured server errors use the text from `data-hc-error-title` and `data-hc-error-message`, preserving only error-kind and component-ID metadata. Rich response HTML is not inserted into the error region. Page state remains one shared encrypted field containing component partitions.

## Browser tests

Run `npm ci`, `npx playwright install chromium`, `npm run build:js`, and `npm run test:browser`. The automated suite serves local fixtures and HTMX 2.0.8 without CDN access. It covers startup, blur/save ordering, authentication, errors, pending UI, page state, and modals. CI runs the same suite with Chromium and uploads traces/screenshots on failure.

The manual request-lifecycle smoke fixture remains available at `/smoke` when running `node tests/browser/server.mjs` (port 4173).

## Benefits

1. **Packaged Delivery**: Behaviors are served from a versioned static web asset
2. **Unified Management**: Single TagHelper manages all JavaScript inclusion
3. **Flexible Inclusion**: Choose which behaviors to include per page/section
4. **Maintainability**: Runtime behavior lives in focused TypeScript modules that build to one browser-tested JavaScript asset
5. **Performance**: Static assets can be cached independently of server-rendered pages

## Adding New Behaviors

To add a new JavaScript behavior:

1. Add the behavior installer to `/tools/runtime/src/behaviors`
2. Register the behavior name and installer in `/tools/runtime/src/behaviors/registry.ts`
3. Add the behavior name to `HtmxRuntimeTagHelper`
4. Add unit or browser-smoke coverage for the behavior
5. Run `npm run build:js` and commit the updated `/wwwroot/js/htmx-components.js`
6. Document the new behavior in this file

## Architecture Benefits

This system enables:
- Versioned static web asset delivery
- Minimal server-side configuration through JSON
- Conditional script inclusion based on features/permissions
- Centralized management of JavaScript behaviors
- Browser-level testing of the shared runtime

## Implementation Details

### HtmxRuntimeTagHelper

The <xref:Htmx.Components.TagHelpers.HtmxRuntimeTagHelper> is the central component that manages runtime behavior inclusion:

```csharp
[HtmlTargetElement("htmx-runtime")]
public class HtmxRuntimeTagHelper : TagHelper
{
    // Manages behavior inclusion logic
    // Supports include-behaviors/exclude-behaviors attributes
    // Emits runtime configuration and a versioned static script reference
}
```

### Behavior Registry

Each behavior is installed from the packaged runtime registry:

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
