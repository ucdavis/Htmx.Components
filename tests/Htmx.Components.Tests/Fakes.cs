using System.Security.Claims;
using System.Text.Encodings.Web;
using Htmx.Components.Authorization;
using Htmx.Components.Models;
using Htmx.Components.State;
using Htmx.Components.Table.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Htmx.Components.Tests;

internal sealed class RecordingResourceOperationRegistry : IResourceOperationRegistry
{
    public List<(string Resource, string Operation)> Registrations { get; } = [];

    public Task Register(string resource, string operation)
    {
        Registrations.Add((resource, operation));
        return Task.CompletedTask;
    }
}

internal sealed class TestAuthorizationRequirementFactory : IAuthorizationRequirementFactory
{
    public IAuthorizationRequirement ForOperation(string resource, string operation)
        => new TestAuthorizationRequirement(resource, operation);

    public IAuthorizationRequirement ForRoles(params string[] roles)
        => new TestAuthorizationRequirement(string.Join(",", roles), "roles");
}

internal sealed record TestAuthorizationRequirement(string Resource, string Operation) : IAuthorizationRequirement;

internal sealed class AlwaysAllowAuthorizationHandler : AuthorizationHandler<TestAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TestAuthorizationRequirement requirement)
    {
        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

internal sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "test-user")],
            Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

internal sealed class StubCompositeViewEngine : ICompositeViewEngine
{
    public ViewEngineResult FindView(ActionContext context, string viewName, bool isMainPage)
        => ViewEngineResult.Found(viewName, new StubView(viewName));

    public ViewEngineResult GetView(string? executingFilePath, string viewPath, bool isMainPage)
        => ViewEngineResult.Found(viewPath, new StubView(viewPath));

    public IEnumerable<string> ViewLocationFormats => [];
    public IEnumerable<string> AreaViewLocationFormats => [];
    public IEnumerable<string> PageViewLocationFormats => [];
    public IEnumerable<string> AreaPageViewLocationFormats => [];
    public IReadOnlyList<IViewEngine> ViewEngines => [];
}

internal sealed class StubView : IView
{
    public StubView(string path)
    {
        Path = path;
    }

    public string Path { get; }

    public Task RenderAsync(ViewContext context)
    {
        context.Writer.Write(Render(Path, context.ViewData.Model));
        return Task.CompletedTask;
    }

    private static string Render(string viewName, object? model)
    {
        if (viewName.Contains("PageStateHiddenInput", StringComparison.OrdinalIgnoreCase))
        {
            return """<input id="page_state" name="page_state" value="stub-state" />""";
        }

        if (viewName.Contains("TableBody", StringComparison.OrdinalIgnoreCase))
        {
            return """<tbody id="table-body"><tr><td>row</td></tr></tbody>""";
        }

        if (viewName.Contains("TablePagination", StringComparison.OrdinalIgnoreCase))
        {
            return """<div id="table-pagination">pagination</div>""";
        }

        if (viewName.Contains("TableHeader", StringComparison.OrdinalIgnoreCase))
        {
            return """<thead id="table-header"><tr><th>Name</th></tr></thead>""";
        }

        if (viewName.Contains("TableActionList", StringComparison.OrdinalIgnoreCase))
        {
            return """<div id="table-actions">actions</div>""";
        }

        if (viewName.Contains("TableEditClassToggle", StringComparison.OrdinalIgnoreCase))
        {
            return """<div id="table-container">edit</div>""";
        }

        if (viewName.Contains("Row", StringComparison.OrdinalIgnoreCase))
        {
            return """<tr id="row_1"><td>row</td></tr>""";
        }

        return model is ITableModel tableModel
            ? $"""<div id="{HtmlEncoder.Default.Encode(tableModel.TypeId)}">{HtmlEncoder.Default.Encode(viewName)}</div>"""
            : $"""<section id="content">{HtmlEncoder.Default.Encode(viewName)}</section>""";
    }
}

internal sealed class Widget
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

internal static class TestServices
{
    public static ServiceProvider CreateHtmxServices(RecordingResourceOperationRegistry? registry = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddScoped<IPageState, PageState>();
        services.AddScoped<Table.ITableProvider, Table.TableProvider>();
        services.AddSingleton<IResourceOperationRegistry>(registry ?? new RecordingResourceOperationRegistry());
        return services.BuildServiceProvider();
    }
}
