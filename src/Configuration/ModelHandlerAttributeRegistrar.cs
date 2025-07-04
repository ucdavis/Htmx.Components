using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;
using Htmx.Components.Attributes;
using Htmx.Components.Models;
using Htmx.Components.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Htmx.Components.Configuration;

/// <summary>
/// Automatically discovers and registers model handlers marked with <see cref="ModelConfigAttribute"/>
/// during application startup by scanning controller classes for configuration methods.
/// </summary>
/// <remarks>
/// This registrar uses reflection to find methods decorated with <see cref="ModelConfigAttribute"/>
/// and automatically invokes them during model registry initialization. The scanning is performed
/// once and cached for performance.
/// </remarks>
public static class ModelHandlerAttributeRegistrar
{
    private static readonly List<HandlerRegistration> _registrations = new();

    /// <summary>
    /// Registers all discovered model handlers with the provided registry.
    /// </summary>
    /// <param name="registry">The model registry to register handlers with.</param>
    /// <remarks>
    /// This method performs controller scanning on first call and caches results.
    /// Subsequent calls reuse the cached registrations for performance.
    /// </remarks>
    public static void RegisterAll(IModelRegistry registry)
    {
        // Ensure we only scan controllers once to avoid performance issues
        if (_registrations.Count == 0)
            ScanControllers();

        foreach (var reg in _registrations)
            reg.RegisterWithRegistry(registry);
    }

    /// <summary>
    /// Scans all loaded assemblies for controller types containing model configuration methods.
    /// </summary>
    /// <remarks>
    /// Looks for methods decorated with <see cref="ModelConfigAttribute"/> and creates
    /// registration entries for each discovered configuration method.
    /// </remarks>
    private static void ScanControllers()
    {
        var controllerTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(Microsoft.AspNetCore.Mvc.Controller).IsAssignableFrom(t) && !t.IsAbstract);

        foreach (var (controllerType, configMethod, modelConfigAttribute) in controllerTypes
            .SelectMany(t => t.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Select(m => (ControllerType: t, ConfigMethod: m, ModelConfigAttribute: m.GetCustomAttribute<ModelConfigAttribute>()))
                .Where(x => x.ConfigMethod != null && x.ModelConfigAttribute != null)))
        {
            var configParam = configMethod.GetParameters().FirstOrDefault();
            if (configParam == null || !configParam.ParameterType.IsGenericType)
                continue;

            var builderType = configParam.ParameterType;
            var modelType = builderType.GetGenericArguments()[0];
            var keyType = builderType.GetGenericArguments()[1];

            // Compile delegates using FastExpressionCompiler
            var controllerParam = Expression.Parameter(typeof(object), "controller");
            var argParam = Expression.Parameter(typeof(object), "arg");

            _registrations.Add(new HandlerRegistration
            {
                TypeId = modelConfigAttribute!.ModelTypeId,
                ModelType = modelType,
                KeyType = keyType,
                ControllerType = controllerType,
                ConfigMethod = configMethod!,
            });
        }
    }

    /// <summary>
    /// Represents a discovered model handler configuration that can be registered with a model registry.
    /// </summary>
    /// <remarks>
    /// Stores metadata about a configuration method and provides functionality to invoke it
    /// with the appropriate model handler builder when registration is requested.
    /// </remarks>
    private class HandlerRegistration
    {
        /// <summary>
        /// Gets or sets the unique identifier for the model type.
        /// </summary>
        public string TypeId { get; set; } = null!;

        /// <summary>
        /// Gets or sets the CLR type of the model being configured.
        /// </summary>
        public Type ModelType { get; set; } = null!;

        /// <summary>
        /// Gets or sets the CLR type of the model's key.
        /// </summary>
        public Type KeyType { get; set; } = null!;

        /// <summary>
        /// Gets or sets the controller type containing the configuration method.
        /// </summary>
        public Type ControllerType { get; set; } = null!;

        /// <summary>
        /// Gets or sets the method info for the configuration method.
        /// </summary>
        public MethodInfo ConfigMethod { get; set; } = null!;

        /// <summary>
        /// Registers this handler configuration with the specified model registry.
        /// </summary>
        /// <param name="registry">The registry to register with.</param>
        /// <remarks>
        /// Creates a controller instance and invokes the configuration method with
        /// an appropriate model handler builder.
        /// </remarks>
        public void RegisterWithRegistry(IModelRegistry registry)
        {
            var registerMethod = typeof(IModelRegistry).GetMethod("Register")
                ?.MakeGenericMethod(ModelType, KeyType);

            registerMethod?.Invoke(registry,
            [
                TypeId,
                (Action<IServiceProvider, object>)((sp, builder) =>
                {
                    var controller = ActivatorUtilities.CreateInstance(sp, ControllerType);

                    // Call config method
                    ConfigMethod.Invoke(controller, new object[] { builder });

                    // Use reflection to get strongly-typed builder methods
                    var builderType = builder.GetType();

                })
            ]);
        }
    }
}