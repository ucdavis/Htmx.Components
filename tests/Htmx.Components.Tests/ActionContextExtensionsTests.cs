using Htmx.Components.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;

namespace Htmx.Components.Tests;

public class ActionContextExtensionsTests
{
    [Fact]
    public void GetValidActionContext_UsesEndpointActionDescriptorMetadata()
    {
        var httpContext = new DefaultHttpContext();
        var routeData = new RouteData();
        routeData.Values["controller"] = "Widgets";
        routeData.Values["action"] = "Index";
        var actionDescriptor = new ActionDescriptor { DisplayName = "Widgets.Index" };

        httpContext.Features.Set<IRoutingFeature>(new RoutingFeature { RouteData = routeData });
        httpContext.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(actionDescriptor),
            "Widgets.Index"));

        var accessor = new HttpContextAccessor { HttpContext = httpContext };

        var actionContext = accessor.GetValidActionContext();

        Assert.Same(httpContext, actionContext.HttpContext);
        Assert.Same(routeData, actionContext.RouteData);
        Assert.Same(actionDescriptor, actionContext.ActionDescriptor);
    }

    [Fact]
    public void GetValidActionContext_ThrowsWhenEndpointHasNoActionDescriptor()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set<IRoutingFeature>(new RoutingFeature { RouteData = new RouteData() });
        httpContext.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            EndpointMetadataCollection.Empty,
            "Plain endpoint"));
        var accessor = new HttpContextAccessor { HttpContext = httpContext };

        var exception = Assert.Throws<InvalidOperationException>(() => accessor.GetValidActionContext());

        Assert.Equal("ActionDescriptor is not available.", exception.Message);
    }
}
