# Documentation Setup

This directory contains the documentation generation system for Htmx.Components.

## Documentation URL

The documentation is automatically published to GitHub Pages at:
**https://ucdavis.github.io/Htmx.Components/**

## How It Works

The documentation system uses DocFX to generate API documentation from C# code comments and combines it with manually written articles.

### Key Components:

- **`docfx.json`**: DocFX configuration file
- **`generate-docs.sh`**: Cross-platform script that:
  - Cleans previous build artifacts
  - Generates documentation using DocFX
- **`articles/`**: Manual documentation articles
- **`api/`**: Auto-generated API documentation (generated at build time)
- **`_site/`**: Final generated documentation site (generated at build time)

### GitHub Actions Workflow

The `.github/workflows/docs.yml` workflow:
1. Runs on every push to main and on pull requests
2. Sets up .NET and DocFX
3. Cleans previous build artifacts
4. Runs DocFX directly with `--warningsAsErrors` to ensure high quality documentation
5. Deploys the generated site to GitHub Pages (main branch only)

## Local Development

### Prerequisites

- .NET 8.0 SDK
- DocFX (installed via `dotnet tool install -g docfx`)

### Generate Documentation Locally

You can generate documentation using either the provided script or DocFX directly:

**Using the script (recommended for local development):**
```bash
# Navigate to the docs directory
cd docs

# Make the script executable (if needed)
chmod +x generate-docs.sh

# Generate documentation
./generate-docs.sh

# Generate and serve documentation locally
./generate-docs.sh --serve

# Generate with verbose output
./generate-docs.sh --verbose

# Generate only API metadata
./generate-docs.sh --metadata-only
```

**Using DocFX directly (same as CI/CD):**
```bash
# Navigate to the docs directory
cd docs

# Clean output directories
rm -rf _site
find api -type f \( \( -name '*.yml' -a ! -name 'toc.yml' \) -o -name '.manifest' \) -delete 2>/dev/null || true

# Generate documentation with warnings as errors
docfx docfx.json --logLevel Verbose --warningsAsErrors

# Or generate and serve locally
docfx docfx.json --serve
```

### Cross-Platform Compatibility

The `generate-docs.sh` script is designed to work on:
- **macOS**: Native bash environment
- **Linux/Ubuntu**: GitHub Actions environment
- **Windows**: Git Bash environment

The script includes cross-platform compatibility features:
- Cross-platform process cleanup for server mode

## Adding Documentation

### API Documentation
API documentation is automatically generated from XML comments in the C# source code. To improve API docs:
1. Add XML documentation comments to your C# classes, methods, and properties
2. Use `<summary>`, `<param>`, `<returns>`, and `<example>` tags
3. The documentation will be automatically updated on the next build

### Articles
To add new articles:
1. Create markdown files in the `articles/` directory
2. Update `articles/toc.yml` to include your new articles in the table of contents
3. Reference C# types using DocFX cross-references (see [Type Link References](#type-link-references) below)

### Type Link References
To reference C# types in your documentation, use DocFX cross-references instead of direct HTML links:

**✅ Correct (using xref):**
```markdown
- Use <xref:Htmx.Components.Models.ModelHandler`2> for model configuration
- The <xref:Htmx.Components.Table.ITableProvider> interface
- Configure with <xref:Htmx.Components.ViewResults.MultiSwapViewResult>
```

**❌ Incorrect (direct HTML links):**
```markdown
- Use [`ModelHandler<T, TKey>`](../../api/Htmx.Components.Models.ModelHandler-2.html)
- The [`ITableProvider`](../../api/Htmx.Components.Table.ITableProvider.html) interface
```

**Cross-Reference Syntax:**
- **Full type name**: `<xref:Htmx.Components.Models.ModelHandler`2>` (use backtick + number for generics)
- **Inline with custom text**: `[Custom Text](xref:Htmx.Components.Models.ModelHandler`2)`
- **Auto-generated text**: `<xref:Htmx.Components.Models.ModelHandler`2>` (uses type name)

**Benefits of xref:**
- ✅ No broken links during build process
- ✅ Automatically resolves to correct URLs
- ✅ DocFX validates that the referenced types exist
- ✅ Works with generic types properly
- ✅ CI/CD builds fail if any cross-references are invalid (using `--warningsAsErrors`)

## Troubleshooting

### Common Issues

1. **"DocFX not found"**: Install DocFX with `dotnet tool install -g docfx`
2. **Permission denied on script**: Run `chmod +x generate-docs.sh`

### Windows/Git Bash Issues

If running on Windows with Git Bash:
- Ensure Git Bash is running as administrator if you encounter permission issues

### GitHub Pages Not Updating

1. Check that the GitHub Actions workflow completed successfully
2. Ensure GitHub Pages is enabled in repository settings
3. Verify that the workflow has the correct permissions (pages: write, id-token: write)
4. Make sure you're pushing to the main branch

## Configuration

### DocFX Configuration (`docfx.json`)

Key configuration options:
- **metadata.src**: Points to the C# source files (currently `../` from docs directory)
- **metadata.filter**: Uses `filterConfig.yml` to control which APIs are documented
- **build.content**: Includes both generated API docs and manual articles
- **build.globalMetadata**: Sets site-wide metadata like repository URL

### Filter Configuration (`filterConfig.yml`)

Controls which APIs appear in the documentation. You can:
- Exclude internal classes
- Include/exclude specific namespaces
- Control visibility of private members

For more information on DocFX configuration, see the [DocFX documentation](https://dotnet.github.io/docfx/).
