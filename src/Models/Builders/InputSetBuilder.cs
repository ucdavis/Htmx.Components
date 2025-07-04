using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace Htmx.Components.Models.Builders;

/// <summary>
/// Builder for creating InputSet instances with multiple input fields.
/// Provides fluent configuration for groups of related input controls.
/// </summary>
/// <typeparam name="T">The model type that the input set is for</typeparam>
public class InputSetBuilder<T> : BuilderBase<InputSetBuilder<T>, InputSet>
    where T : class
{
    private readonly InputSetConfig _config = new();

    /// <summary>
    /// Initializes a new instance of the InputSetBuilder with the specified service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection</param>
    public InputSetBuilder(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    /// <summary>
    /// Adds an input field for the specified property with fluent configuration.
    /// Creates a strongly-typed input model that binds to the specified property.
    /// </summary>
    /// <typeparam name="TProp">The type of the property being configured</typeparam>
    /// <param name="propSelector">Expression that selects the property to create an input for</param>
    /// <param name="configure">Delegate to configure the input model builder</param>
    /// <returns>The current builder instance for method chaining</returns>
    public InputSetBuilder<T> AddInput<TProp>(Expression<Func<T, TProp>> propSelector,
        Action<InputModelBuilder<T, TProp>> configure)
    {
        AddBuildTask(async () =>
        {
            var builder = new InputModelBuilder<T, TProp>(ServiceProvider, propSelector);
            configure(builder);
            var inputModel = await builder.BuildAsync();
            _config.Inputs.Add(inputModel);
        });
        return this;
    }

    /// <summary>
    /// Adds a pre-configured input model to the set.
    /// Useful when you have an existing IInputModel instance to add.
    /// </summary>
    /// <param name="inputModel">The input model to add to the set</param>
    /// <returns>The current builder instance for method chaining</returns>
    public InputSetBuilder<T> AddInput(IInputModel inputModel)
    {
        _config.Inputs.Add(inputModel);
        return this;
    }

    /// <summary>
    /// Adds multiple pre-configured input models to the set.
    /// Allows for bulk addition of input models from another source.
    /// </summary>
    /// <param name="inputModels">The collection of input models to add</param>
    /// <returns>The current builder instance for method chaining</returns>
    public InputSetBuilder<T> AddRange(IEnumerable<IInputModel> inputModels)
    {
        _config.Inputs.AddRange(inputModels);
        return this;
    }

    /// <summary>
    /// Sets the label for the input set.
    /// The label is typically displayed as a group header or section title.
    /// </summary>
    /// <param name="label">The label text to display for the input set</param>
    /// <returns>The current builder instance for method chaining</returns>
    public InputSetBuilder<T> WithLabel(string label)
    {
        _config.Label = label;
        return this;
    }

    /// <summary>
    /// Builds the final InputSet instance with all configured input fields.
    /// This method is called internally to create the final result.
    /// </summary>
    /// <returns>A task that represents the asynchronous build operation, containing the built InputSet</returns>
    protected override Task<InputSet> BuildImpl()
        => Task.FromResult(new InputSet(_config));

}
