using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace Htmx.Components.Models.Builders;

public class InputSetBuilder<T> : BuilderBase<InputSetBuilder<T>, InputSet>
    where T : class
{
    private readonly InputSetConfig _config = new();

    public InputSetBuilder(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

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

    public InputSetBuilder<T> AddInput(IInputModel inputModel)
    {
        _config.Inputs.Add(inputModel);
        return this;
    }

    public InputSetBuilder<T> AddRange(IEnumerable<IInputModel> inputModels)
    {
        _config.Inputs.AddRange(inputModels);
        return this;
    }

    public InputSetBuilder<T> WithLabel(string label)
    {
        _config.Label = label;
        return this;
    }

    protected override Task<InputSet> BuildImpl()
        => Task.FromResult(new InputSet(_config));

}
