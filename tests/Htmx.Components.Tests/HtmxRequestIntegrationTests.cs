using Htmx.Components;
using Htmx.Components.Authorization;
using Htmx.Components.Configuration;
using Htmx.Components.Models.Builders;
using Htmx.Components.Table.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Htmx.Components.Tests;

public class HtmxRequestIntegrationTests
{
    [Fact]
    public async Task FormController_SetPage_ProcessesRepresentativeHtmxRequest()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddDbContext<WidgetsDbContext>(options => options.UseSqlite(connection));
                    services.AddAuthentication("Test")
                        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthenticationHandler>(
                            "Test", _ => { });
                    services.AddAuthorization();
                    services.AddSingleton<IAuthorizationHandler, AlwaysAllowAuthorizationHandler>();

                    services.AddHtmxComponents(options =>
                    {
                        options.WithAuthorizationRequirementFactory<TestAuthorizationRequirementFactory>();
                        options.WithResourceOperationRegistry<RecordingResourceOperationRegistry>();
                        options.WithModelHandlerRegistry((registry, _) =>
                        {
                            registry.Register<Widget, int>("Widget", ConfigureWidget);
                        });
                    });

                    services.AddControllersWithViews()
                        .AddHtmxComponentsApplicationPart();

                    services.RemoveAll<ICompositeViewEngine>();
                    services.AddSingleton<ICompositeViewEngine, StubCompositeViewEngine>();
                })
                .Configure(app =>
                {
                    using (var scope = app.ApplicationServices.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<WidgetsDbContext>();
                        dbContext.Database.EnsureCreated();
                        dbContext.Widgets.AddRange(
                            new Widget { Name = "Alpha" },
                            new Widget { Name = "Beta" });
                        dbContext.SaveChanges();
                    }

                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseHtmxPageState();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                }))
            .StartAsync();

        var client = host.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/Form/Widget/SetPage")
        {
            Content = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("page", "1"),
                new KeyValuePair<string, string>("componentId", "hc-table-widget")
            ])
        };
        request.Headers.Add("HX-Request", "true");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("hx-swap-oob=\"outerHTML\"", body);
        Assert.Contains("id=\"hc-table-widget-body\"", body);
        Assert.Contains("id=\"page_state\"", body);
    }

    private static void ConfigureWidget(IServiceProvider serviceProvider, ModelHandlerBuilder<Widget, int> builder)
    {
        builder
            .WithKeySelector(widget => widget.Id)
            .WithQueryable(() => serviceProvider.GetRequiredService<WidgetsDbContext>().Widgets)
            .WithTable(table => table.AddSelectorColumn(widget => widget.Name));
    }

    private sealed class WidgetsDbContext : DbContext
    {
        public WidgetsDbContext(DbContextOptions<WidgetsDbContext> options) : base(options)
        {
        }

        public DbSet<Widget> Widgets => Set<Widget>();
    }
}
