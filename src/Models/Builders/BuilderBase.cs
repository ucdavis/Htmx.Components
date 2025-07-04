using Htmx.Components.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Htmx.Components.Models.Builders;

public abstract class BuilderBase<TBuilder, TModel>
    where TBuilder : BuilderBase<TBuilder, TModel>
    where TModel : class
{
    public IServiceProvider ServiceProvider { get; }
    private readonly List<Func<Task>> _buildTasks;

    protected BuilderBase(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        _buildTasks = [];
    }

    public ActionContext ActionContext
    {
        get
        {
            var actionContextAccessor = ServiceProvider.GetRequiredService<IActionContextAccessor>();
            return actionContextAccessor.GetValidActionContext();
        }
    }

    protected void AddBuildTask(Task task)
    {
        _buildTasks.Add(() => task);
    }

    protected void AddBuildTask(Func<Task> taskFunc)
    {
        _buildTasks.Add(taskFunc);
    }

    protected void AddBuildTask(Action action)
    {
        _buildTasks.Add(() => Task.Run(action));
    }

    protected abstract Task<TModel> BuildImpl();

    internal async Task<TModel> BuildAsync()
    {
        var tasks = _buildTasks.Select(f => f());
        await Task.WhenAll(tasks);
        return await BuildImpl();
    }
}
