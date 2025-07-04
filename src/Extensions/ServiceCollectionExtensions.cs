using System.Security.Claims;
using Htmx.Components.Authorization;
using Htmx.Components.AuthStatus;
using Htmx.Components.AuthStatus.Internal;
using Htmx.Components.Configuration;
using Htmx.Components.Filters;
using Htmx.Components.Models;
using Htmx.Components.Models.Builders;
using Htmx.Components.Table.Models;
using Htmx.Components.NavBar;
using Htmx.Components.NavBar.Internal;
using Htmx.Components.Services;
using Htmx.Components.State;
using Htmx.Components.Table;
using Htmx.Components.Table.Internal;
using Htmx.Components.ViewResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Htmx.Components;

/// <summary>
/// Extension methods for registering Htmx Components services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all required Htmx Components services and configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for HtmxComponentOptions.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddHtmxComponents(this IServiceCollection services, Action<HtmxComponentOptions>? configure = null)
    {
        services.AddSafeActionContextAccessor(nameof(AddHtmxComponents));
        services.AddHttpContextAccessor();
        services.Configure<RazorViewEngineOptions>(options =>
        {
            // Add self-contained ViewComponent view location expander
            // This handles both ViewComponent views and partial views automatically
            options.ViewLocationExpanders.Add(new ComponentViewLocationExpander());
            
            // Keep existing view location formats for backwards compatibility
            options.ViewLocationFormats.Insert(0, "/src/Views/Shared/Components/Table/{0}.cshtml");
            options.ViewLocationFormats.Insert(1, "/src/Views/{1}/{0}.cshtml");
            options.ViewLocationFormats.Insert(2, "/src/Views/Shared/{0}.cshtml");
            options.ViewLocationFormats.Insert(3, "/src/Views/Shared/Components/{1}/{0}.cshtml");
        });

        var options = new HtmxComponentOptions();
        configure?.Invoke(options);

        RegisterCoreServices(services, options);
        RegisterFilters(services);
        RegisterAuthorization(services, options);

        return services;
    }

    /// <summary>
    /// Adds the Htmx Components application part to MVC.
    /// </summary>
    public static IMvcBuilder AddHtmxComponentsApplicationPart(this IMvcBuilder builder)
    {
        builder.Services.AddSingleton<HtmxComponentsApplicationPartMarker>();
        builder.AddApplicationPart(typeof(ServiceCollectionExtensions).Assembly);
        return builder;
    }

    /// <summary>
    /// Adds the Htmx Components application part to MVC Core.
    /// </summary>
    public static IMvcCoreBuilder AddHtmxComponentsApplicationPart(this IMvcCoreBuilder builder)
    {
        builder.Services.AddSingleton<HtmxComponentsApplicationPartMarker>();
        builder.AddApplicationPart(typeof(ServiceCollectionExtensions).Assembly);
        return builder;
    }

    /// <summary>
    /// Registers IActionContextAccessor safely before MVC infrastructure.
    /// </summary>
    public static IServiceCollection AddSafeActionContextAccessor(this IServiceCollection services,
        string extensionMethodName = nameof(AddSafeActionContextAccessor))
    {
        bool mvcAlreadyRegistered = services.Any(sd =>
            sd.ServiceType.FullName?.StartsWith("Microsoft.AspNetCore.Mvc.Infrastructure") == true ||
            sd.ServiceType == typeof(Microsoft.AspNetCore.Mvc.Infrastructure.IActionInvokerFactory) ||
            sd.ImplementationType?.FullName?.StartsWith("Microsoft.AspNetCore.Mvc") == true
        );

        if (mvcAlreadyRegistered)
        {
            throw new InvalidOperationException(
                $"IActionContextAccessor must be registered before MVC. " +
                $"Call {extensionMethodName}() before AddControllers() or AddMvc()."
            );
        }

        services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();
        return services;
    }

    /// <summary>
    /// Internal marker class used by the framework to ensure that AddHtmxComponentsApplicationPart() is called
    /// during service registration. This class should not be used directly in user code.
    /// </summary>
    /// <remarks>
    /// This marker class is registered as a singleton to detect whether the HTMX Components
    /// application part has been properly added to the MVC pipeline.
    /// </remarks>
    internal class HtmxComponentsApplicationPartMarker { }

    private static void RegisterCoreServices(IServiceCollection services, HtmxComponentOptions options)
    {
        services.AddSingleton(options.ViewPaths);
        services.AddScoped<ITableProvider, TableProvider>();
        services.AddMemoryCache();

        if (options.NavProviderFactory is not null)
            services.AddScoped<INavProvider, BuilderBasedNavProvider>(options.NavProviderFactory);
        else
            services.AddScoped<INavProvider, AttributeNavProvider>();

        if (options.ModelRegistryFactory is not null)
        {
            services.AddScoped<IModelRegistry>(options.ModelRegistryFactory);
            services.AddScoped<IModelHandlerFactory, ModelHandlerFactory>();
            services.AddScoped<IModelHandlerFactoryGeneric, ModelHandlerFactory>();
        }

        services.AddScoped<IPageState, PageState>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddDataProtection();
        services.AddScoped<IAuthStatusProvider>(sp =>
            options.AuthStatusProviderFactory?.Invoke(sp) ?? new DefaultAuthStatusProvider());
    }

    private static void RegisterFilters(IServiceCollection services)
    {
        services.AddScoped<TableOobRefreshFilter>();
        services.AddScoped<PageStateOobInjectorFilter>();
        services.AddScoped<TableOobEditFilter>();
        services.AddScoped<NavActionResultFilter>();
        services.AddScoped<AuthStatusUpdateFilter>();

        services.PostConfigure<MvcOptions>(options =>
        {
            // Be sure to place filters that convert models to MultiSwapViewResults before the filters that inject OOB content.
            options.Filters.AddService<TableOobRefreshFilter>();
            options.Filters.AddService<TableOobEditFilter>();
            options.Filters.AddService<AuthStatusUpdateFilter>();
            options.Filters.AddService<NavActionResultFilter>();
            options.Filters.AddService<PageStateOobInjectorFilter>();
        });
    }

    private static void RegisterAuthorization(IServiceCollection services, HtmxComponentOptions options)
    {
        services.Configure<AuthorizationMetadataSettings>(settings =>
        {
            settings.UserIdClaimType = options.UserIdClaimType;
        });
        services.AddScoped<IAuthorizationMetadataService, AuthorizationMetadataService>();

        if (options.RegisterAuthorizationRequirementFactory == null
            && !services.Any(sd => sd.ServiceType == typeof(IAuthorizationRequirementFactory)))
        {
            throw new InvalidOperationException(
                $"{nameof(HtmxComponentOptions)}.{nameof(HtmxComponentOptions.WithAuthorizationRequirementFactory)}() must be called to register a permission requirement factory.");
        }
        options.RegisterAuthorizationRequirementFactory!(services);

        if (options.RegisterResourceOperationRegistry == null
            && !services.Any(sd => sd.ServiceType == typeof(IResourceOperationRegistry)))
        {
            throw new InvalidOperationException(
                $"{nameof(HtmxComponentOptions)}.{nameof(HtmxComponentOptions.WithResourceOperationRegistry)}() must be called to register a resource operation registry.");
        }
        options.RegisterResourceOperationRegistry!(services);

        // Optional: Register role service if provided
        options.RegisterRoleService?.Invoke(services);
    }
}

/// <summary>
/// Configuration options for the HTMX Components library.
/// This class provides a fluent API for configuring various aspects of the component system
/// including navigation providers, authorization, view paths, and model handlers.
/// </summary>
public class HtmxComponentOptions
{
    internal Func<IServiceProvider, BuilderBasedNavProvider>? NavProviderFactory { get; private set; }
    internal ViewPaths ViewPaths { get; private set; } = new ViewPaths();
    internal Func<IServiceProvider, ModelRegistry>? ModelRegistryFactory { get; private set; }
    internal Action<IServiceCollection>? RegisterAuthorizationRequirementFactory { get; private set; }
    internal Action<IServiceCollection>? RegisterResourceOperationRegistry { get; private set; }
    internal Action<IServiceCollection>? RegisterRoleService { get; set; }
    internal string UserIdClaimType { get; set; } = ClaimTypes.NameIdentifier;
    internal Func<IServiceProvider, IAuthStatusProvider> AuthStatusProviderFactory { get; set; } = sp =>
        new DefaultAuthStatusProvider();
    
    /// <summary>
    /// Configures a custom authentication status provider for the application.
    /// </summary>
    /// <param name="factory">A factory function that creates the authentication status provider.</param>
    /// <returns>The current options instance for method chaining.</returns>
    public HtmxComponentOptions WithAuthStatusProvider(Func<IServiceProvider, IAuthStatusProvider> factory)
    {
        AuthStatusProviderFactory = factory;
        return this;
    }

    /// <summary>
    /// Registers a role service implementation for role-based authorization.
    /// </summary>
    /// <typeparam name="T">The type of the role service implementation.</typeparam>
    /// <returns>The current options instance for method chaining.</returns>
    public HtmxComponentOptions WithRoleService<T>()
            where T : class, IRoleService
    {
        RegisterRoleService = services =>
        {
            services.AddScoped<IRoleService, T>();
        };
        return this;
    }

    /// <summary>
    /// Configures the claim type used to identify users in authorization caching.
    /// </summary>
    /// <param name="claimType">The claim type to use for user identification. Defaults to NameIdentifier.</param>
    /// <returns>The current options instance for method chaining.</returns>
    public HtmxComponentOptions WithUserIdClaimType(string claimType)
    {
        UserIdClaimType = claimType;
        return this;
    }

    /// <summary>
    /// Configures navigation using an asynchronous builder pattern.
    /// </summary>
    /// <param name="builderFactory">A function that configures the navigation structure asynchronously.</param>
    /// <returns>The current options instance for method chaining.</returns>
    public HtmxComponentOptions WithNavBuilder(Func<ActionSetBuilder, Task> builderFactory)
    {
        NavProviderFactory = serviceProvider =>
        {
            var actionSetBuilder = new ActionSetBuilder(serviceProvider);
            return new BuilderBasedNavProvider(serviceProvider, builderFactory);
        };
        return this;
    }

    /// <summary>
    /// Configures navigation using a synchronous builder pattern.
    /// </summary>
    /// <param name="builderFactory">An action that configures the navigation structure synchronously.</param>
    /// <returns>The current options instance for method chaining.</returns>
    public HtmxComponentOptions WithNavBuilder(Action<ActionSetBuilder> builderFactory)
    {
        NavProviderFactory = serviceProvider =>
        {
            var funcBuilderFactory = new Func<ActionSetBuilder, Task>(builder =>
            {
                builderFactory(builder);
                return Task.CompletedTask;
            });
            return new BuilderBasedNavProvider(serviceProvider, funcBuilderFactory);
        };
        return this;
    }

    /// <summary>
    /// Configures custom view paths for component views.
    /// </summary>
    /// <param name="configure">An action that configures the view paths.</param>
    /// <returns>The current options instance for method chaining.</returns>
    public HtmxComponentOptions WithViewOverrides(Action<ViewPaths> configure)
    {
        configure(ViewPaths);
        return this;
    }

    /// <summary>
    /// Configures the model handler registry with custom model configurations.
    /// </summary>
    /// <param name="configure">An action that configures the model registry with custom handlers.</param>
    /// <returns>The current options instance for method chaining.</returns>
    public HtmxComponentOptions WithModelHandlerRegistry(Action<IModelRegistry, IServiceProvider> configure)
    {
        ModelRegistryFactory = serviceProvider =>
        {
            var resourceOperationRegistry = serviceProvider.GetRequiredService<IResourceOperationRegistry>();
            var modelRegistry = new ModelRegistry(serviceProvider, resourceOperationRegistry);
            configure(modelRegistry, serviceProvider);
            return modelRegistry;
        };
        return this;
    }

    /// <summary>
    /// Registers an authorization requirement factory implementation.
    /// </summary>
    /// <typeparam name="T">The type of the authorization requirement factory implementation.</typeparam>
    /// <remarks>This method is required and must be called to register authorization support.</remarks>
    public void WithAuthorizationRequirementFactory<T>()
        where T : class, IAuthorizationRequirementFactory
    {
        RegisterAuthorizationRequirementFactory = services =>
        {
            services.AddSingleton<IAuthorizationRequirementFactory, T>();
        };
    }

    /// <summary>
    /// Registers a resource operation registry implementation.
    /// </summary>
    /// <typeparam name="T">The type of the resource operation registry implementation.</typeparam>
    /// <remarks>This method is required and must be called to register authorization support.</remarks>
    public void WithResourceOperationRegistry<T>()
        where T : class, IResourceOperationRegistry
    {
        RegisterResourceOperationRegistry = services =>
        {
            services.AddScoped<IResourceOperationRegistry, T>();
        };
    }
}