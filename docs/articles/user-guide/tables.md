# Tables

Htmx.Components provides powerful table functionality with sorting, filtering, pagination, and inline editing capabilities.

## Basic Table Setup

Tables in Htmx.Components are configured using model handlers with the `[ModelConfig]` attribute. Here's a product-management example:

### Controller with Model Configuration

```csharp
[Route("Catalog")]
[NavActionGroup(DisplayName = "Catalog", Icon = "fas fa-boxes-stacked", Order = 2)]
public class CatalogController : Controller
{
    private readonly AppDbContext _dbContext;
    private readonly IModelHandlerFactoryGeneric _modelHandlerFactory;

    public CatalogController(AppDbContext dbContext, IModelHandlerFactoryGeneric modelHandlerFactory)
    {
        _dbContext = dbContext;
        _modelHandlerFactory = modelHandlerFactory;
    }

    [HttpGet("Products")]
    [NavAction(DisplayName = "Products", Icon = "fas fa-box", Order = 0, PushUrl = true, ViewName = "_Products")]
    public async Task<IActionResult> Products()
    {
        var modelHandler = await _modelHandlerFactory.Get<Product, int>(nameof(Product), ModelUI.Table);
        var tableModel = await modelHandler.BuildTableModelAndFetchPageAsync();
        tableModel.ComponentId = TableComponentIdentity.Ensure("catalog-products");
        return Ok(tableModel);
    }

    [ModelConfig(nameof(Product))]
    private void ConfigureProduct(ModelHandlerBuilder<Product, int> builder)
    {
        builder
            .WithKeySelector(product => product.Id)
            .WithQueryable(() => _dbContext.Products)
            .WithCreate(async product =>
            {
                _dbContext.Products.Add(product);
                await _dbContext.SaveChangesAsync();
                return Result.Value(product);
            })
            .WithUpdate(async product =>
            {
                _dbContext.Products.Update(product);
                await _dbContext.SaveChangesAsync();
                return Result.Value(product);
            })
            .WithDelete(async id =>
            {
                var product = await _dbContext.Products.FindAsync(id);
                if (product != null)
                {
                    _dbContext.Products.Remove(product);
                    await _dbContext.SaveChangesAsync();
                }
                return Result.Success();
            })
            .WithTable(table => table
                .WithCrudActions()
                .AddSelectorColumn(x => x.Name, config => config.WithEditable())
                .AddSelectorColumn(x => x.Sku, config => config.WithEditable())
                .AddSelectorColumn(x => x.Price, config => config.WithEditable())
                .AddCrudDisplayColumn());
    }
}
```

### View File

Create a view file to render the table (e.g., `Views/Catalog/_Products.cshtml`):

```html
@using Htmx.Components.Table.Models
@model ITableModel

<div id="@Model.ComponentId">
    <h2>Product Management</h2>
    @await Component.InvokeAsync("Table", Model)
</div>
```

The `Table` ViewComponent renders an `htmx-table` root and a nested `htmx-request-scope`. Set a stable `ComponentId` when the same table can be revisited, or when more than one table can appear on the page, so generated DOM ids, out-of-band swap targets, and table/form state partitions stay scoped to the intended table instance.

## JavaScript Requirements

Tables with inline editing require the `table-inline-editing` JavaScript behavior:

```html
<!-- Include all behaviors (includes table-inline-editing) -->
<htmx-runtime></htmx-runtime>

<!-- Include only table-inline-editing -->
<htmx-runtime include-behaviors="table-inline-editing"></htmx-runtime>
```

The `table-inline-editing` behavior provides:
- **Visual editing states**: Highlights rows being edited
- **Inline editing coordination**: Manages edit mode transitions

Tables also use the default `request-lifecycle` and `error-handling` behaviors:
- Pending requests disable controls inside the table scope, dim stale table content, and show the table's request indicator
- HTMX errors render into the table's local `htmx-error-region` before falling back to a global error region
- Abort, network error, timeout, and response-error events restore pending UI state

## Table Configuration Options

### Basic Column Types

#### Selector Columns
Display data from model properties:

```csharp
.AddSelectorColumn(x => x.Name, config => config.WithEditable())
.AddSelectorColumn(x => x.Description, config => config.WithEditable())
.AddSelectorColumn(x => x.CreatedDate)  // Read-only column
```

#### CRUD Display Column
Adds edit/delete action buttons:

```csharp
.AddCrudDisplayColumn()
```

### CRUD Operations

Enable create, update, and delete operations:

```csharp
.WithTable(table => table
    .WithCrudActions()  // Enables CRUD functionality
    .AddSelectorColumn(x => x.Name, config => config.WithEditable())
    .AddCrudDisplayColumn())  // Adds action buttons
```

## Custom Projection Example

Use a projected model when a table should combine data from several entities or expose only a subset of fields:

```csharp
[ModelConfig(nameof(TeamMemberRow))]
private void ConfigureTeamMember(ModelHandlerBuilder<TeamMemberRow, int> builder)
{
    builder
        .WithKeySelector(member => member.Id)
        .WithQueryable(() => _dbContext.TeamMembers
            .Where(member => member.IsActive)
            .Select(member => new TeamMemberRow
            {
                Id = member.Id,
                Name = member.FirstName + " " + member.LastName,
                Email = member.Email,
                Department = member.Department.Name,
                CanApproveOrders = member.Roles.Any(role => role.Code == "Approver")
            }))
        .WithInput(member => member.Email, config => config
            .WithLabel("Email")
            .WithPlaceholder("Email address")
            .WithCssClass("form-control"))
        .WithInput(member => member.Department, config => config
            .WithLabel("Department")
            .WithPlaceholder("Department")
            .WithCssClass("form-control"))
        .WithInput(member => member.CanApproveOrders, config => config
            .WithLabel("Can Approve Orders")
            .WithCssClass("form-check"))
        .WithTable(table => table
            .WithCrudActions()
            .AddSelectorColumn(x => x.Name)
            .AddSelectorColumn(x => x.Email, config => config.WithEditable())
            .AddSelectorColumn(x => x.Department, config => config.WithEditable())
            .AddSelectorColumn(x => x.CanApproveOrders, config => config.WithEditable())
            .AddCrudDisplayColumn());
}
```

## Key Features

### Automatic Table Rendering
The `Table` ViewComponent automatically renders:
- Column headers
- Data rows
- Sorting controls
- Pagination controls
- Filter inputs (when enabled)
- CRUD action buttons (when enabled)

### Built-in Functionality
- **Sorting**: Click column headers to sort
- **Filtering**: Built-in text filters for columns
- **Pagination**: Automatic pagination for large datasets
- **Inline Editing**: Edit data directly in the table
- **CRUD Operations**: Create, update, delete records
- **Scoped Instances**: Multiple tables can render on one page without sharing DOM targets or table state

### Integration with Entity Framework
Tables work seamlessly with Entity Framework Core through the `WithQueryable()` method, providing efficient database queries with proper pagination and filtering.

## Next Steps

- **[Navigation](navigation.md)**: Learn about NavAction attributes and navigation setup
- **[Authentication](authentication.md)**: Configure authentication and AuthStatus components  
- **[Authorization](authorization.md)**: Set up authorization policies
- **[Architecture Guide](../developer-guide/architecture.md)**: Understand the underlying patterns and design

## Filtering

### Built-in Filters

Easily filter tables with built-in text filters:

```csharp
table.AddSelectorColumn(p => p.Name, col => col
    .WithFilter());
```

### Custom Filters

Create specialized filters for complex scenarios:

```csharp
table.AddSelectorColumn(p => p.Status, col => col
    .WithFilter((query, value) =>
    {
        if (Enum.TryParse<ProductStatus>(value, out var status))
            return query.Where(p => p.Status == status);
        return query;
    }));
```

### Range Filters

Useful for dates and numeric values:

```csharp
table.AddSelectorColumn(p => p.CreatedDate, col => col
    .WithRangeFilter((query, fromDate, toDate) =>
    {
        var from = DateTime.Parse(fromDate);
        var to = DateTime.Parse(toDate);
        return query.Where(p => p.CreatedDate >= from && p.CreatedDate <= to);
    }));
```

## Sorting

### Automatic Sorting

Enabled by default for selector columns:

```csharp
table.AddSelectorColumn(p => p.Name); // Automatically sortable
```

## Pagination

Pagination is automatically handled by the table provider. Configure page size:

```csharp
// In your action
tableModel.ComponentId = TableComponentIdentity.Ensure("catalog-products");
var tableState = pageState.GetOrCreate<TableState>(
    TableComponentIdentity.TableStatePartition(tableModel.ComponentId),
    TableStateKeys.TableState,
    () => new TableState
{
    PageSize = 25 // Default page size
});
```

Users can change page size using the built-in pagination controls.

## CRUD Operations

### Enable CRUD

Configure create, read, update, and delete operations:

```csharp
builder.WithCreate(CreateProduct)
       .WithUpdate(UpdateProduct)
       .WithDelete(DeleteProduct)
       .WithTable(table =>
       {
           table.AddCrudDisplayColumn(); // Adds Edit/Delete buttons
           table.WithCrudActions();      // Adds Create button
       });
```

### CRUD Implementation

Implement the CRUD operations:

```csharp
private async Task<Result<Product>> CreateProduct(Product product)
{
    try
    {
        if (string.IsNullOrEmpty(product.Name))
            return Result.Error("Product name is required");
            
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return Result.Value(product);
    }
    catch (Exception ex)
    {
        return Result.Error("Failed to create product: {Error}", ex.Message);
    }
}

private async Task<Result<Product>> UpdateProduct(Product product)
{
    try
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
        return Result.Value(product);
    }
    catch (Exception ex)
    {
        return Result.Error("Failed to update product: {Error}", ex.Message);
    }
}

private async Task<Result> DeleteProduct(int productId)
{
    try
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
            return Result.Error("Product not found");
            
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return Result.Ok("Product deleted successfully");
    }
    catch (Exception ex)
    {
        return Result.Error("Failed to delete product: {Error}", ex.Message);
    }
}
```

## Inline Editing

### Enable Inline Editing

Configure columns for inline editing:

```csharp
table.AddSelectorColumn(p => p.Name, col => col
    .WithEditable());

table.AddSelectorColumn(p => p.Category, col => col
    .WithEditable());
```

### Input Configuration

Define how fields are edited:

```csharp
builder.WithInput(p => p.Name, input => input
    .WithLabel("Product Name")
    .WithKind(InputKind.Text)
    .WithPlaceholder("Enter product name"));

builder.WithInput(p => p.Category, input => input
    .WithLabel("Category")
    .WithKind(InputKind.Select)
    .WithOptions(GetCategoryOptions()));

private List<KeyValuePair<string, string>> GetCategoryOptions()
{
    return new List<KeyValuePair<string, string>>
    {
        new("electronics", "Electronics"),
        new("clothing", "Clothing"),
        new("books", "Books")
    };
}
```

## Custom Table Views

### Override Table Templates

Customize table rendering by overriding view paths:

```csharp
builder.Services.AddHtmxComponents(options =>
{
    options.WithViewOverrides(views =>
    {
        views.Table.Table = "CustomTable";
        views.Table.Row = "CustomTableRow";
        views.Table.Cell = "CustomTableCell";
    });
});
```

### Custom Cell Rendering

Create custom cell templates:

```csharp
table.AddSelectorColumn(p => p.Status, col => col
    .WithCellPartial("_StatusCell"));
```

Create `Views/Shared/_StatusCell.cshtml`:

```html
@using Htmx.Components.Table.Models
@model TableCellPartialModel

@{
    var status = (ProductStatus)Model.Column.GetValue(Model.Row);
    var statusClass = status switch
    {
        ProductStatus.Active => "badge-success",
        ProductStatus.Inactive => "badge-error",
        _ => "badge-neutral"
    };
}

<span class="badge @statusClass">@status</span>
```

## Advanced Features

### Conditional Actions

Show different actions based on row data:

```csharp
table.AddDisplayColumn("Actions", col => col
    .WithActions((row, actions) =>
    {
        var product = (Product)row.Item;
        
        if (product.Status == ProductStatus.Active)
        {
            actions.AddAction(action => action
                .WithLabel("Deactivate")
                .WithIcon("fas fa-pause")
                .WithHxPost($"/Products/Deactivate/{row.Key}"));
        }
        else
        {
            actions.AddAction(action => action
                .WithLabel("Activate")
                .WithIcon("fas fa-play")
                .WithHxPost($"/Products/Activate/{row.Key}"));
        }
    }));
```

### Bulk Operations

Add table-level actions for bulk operations:

```csharp
table.WithActions((tableModel, actions) =>
{
    actions.AddAction(action => action
        .WithLabel("Export CSV")
        .WithIcon("fas fa-download")
        .WithHxGet($"/Products/ExportCsv"));
        
    actions.AddAction(action => action
        .WithLabel("Bulk Delete")
        .WithIcon("fas fa-trash")
        .WithClass("btn-error")
        .WithHxPost("/Products/BulkDelete"));
});
```

### Complex Filtering

Implement complex filtering scenarios:

```csharp
public async Task<IActionResult> FilterByCategory(string category)
{
    var modelHandler = await _modelRegistry.GetModelHandler<Product, int>("products", ModelUI.Table);
    var tableState = this.GetPageState().GetOrCreate<TableState>("Table", "TableState", () => new());
    
    // Apply custom filter
    tableState.Filters["Category"] = category;
    
    var tableModel = await modelHandler.BuildTableModelAndFetchPageAsync(tableState);
    return Ok(tableModel);
}
```

## Performance Optimization

### Efficient Queries

Optimize your queryables for performance:

```csharp
builder.WithQueryable(() => _context.Products
    .Include(p => p.Category)
    .AsNoTracking() // For read-only scenarios
    .AsSplitQuery()); // For complex includes
```

### Pagination Strategy

Use efficient pagination for large datasets:

```csharp
// Consider using cursor-based pagination for very large tables
private async Task<TableModel<Product, int>> GetProductsPage(int page, int pageSize)
{
    var skip = (page - 1) * pageSize;
    var products = await _context.Products
        .OrderBy(p => p.Id)
        .Skip(skip)
        .Take(pageSize)
        .ToListAsync();
        
    // Use the async builder for consistency
    var handler = await _modelRegistry.GetModelHandler<Product, int>("products", ModelUI.Table);
    var tableModel = await handler.BuildTableModelAsync();
    return tableModel;
}
```

## Troubleshooting

### Table Not Loading

1. Check that the model handler is properly configured
2. Verify the queryable returns data
3. Ensure the Table component is invoked with correct model type

### Filtering Not Working

1. Verify filter functions are properly implemented
2. Check that columns are marked as filterable
3. Ensure filter syntax is correct

### CRUD Operations Failing

1. Check error handling in CRUD operations
2. Verify authorization for operations
3. Ensure proper model validation

### Performance Issues

1. Review query efficiency and includes
2. Consider pagination for large datasets
3. Implement proper indexing on filtered/sorted columns
