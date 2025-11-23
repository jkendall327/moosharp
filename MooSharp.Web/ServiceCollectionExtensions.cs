using System.Reflection;
using MooSharp.Messaging;

namespace MooSharp.Web;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register all ICommandHandler<T>s via reflection.
    /// </summary>
    public static void RegisterCommandHandlers(this WebApplicationBuilder webApplicationBuilder)
    {
        var assemblies = new List<Assembly>([Assembly.GetExecutingAssembly(), typeof(CommandExecutor).Assembly]);

        var handlerInterfaceType = typeof(IHandler<>);

        foreach (var assembly in assemblies)
        {
            var handlerTypes = assembly.GetTypes()
                .Where(t => t is {IsAbstract: false, IsInterface: false})
                .SelectMany(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() ==
                        handlerInterfaceType)
                    .Select(i => new
                    {
                        Implementation = t,
                        Service = i
                    }));

            foreach (var typePair in handlerTypes)
            {
                webApplicationBuilder.Services.AddTransient(typePair.Service, typePair.Implementation);
            }
        }
    }

    /// <summary>
    /// Register all ICommandDefinitions via reflection.
    /// </summary>
    public static void RegisterCommandDefinitions(this WebApplicationBuilder builder)
    {
        var assemblies = new[]
        {
            Assembly.GetExecutingAssembly(),
            typeof(CommandParser).Assembly
        };

        var definitionType = typeof(ICommandDefinition);

        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes()
                .Where(t =>
                    t is {IsAbstract: false, IsInterface: false} &&
                    definitionType.IsAssignableFrom(t));

            foreach (var type in types)
            {
                builder.Services.AddTransient(definitionType, type);
            }
        }
    }

    /// <summary>
    /// Register all IGameEventFormatter implementations and the presenter pipeline.
    /// </summary>
    public static void RegisterPresenters(this WebApplicationBuilder builder)
    {
        var assemblies = new[] { Assembly.GetExecutingAssembly(), typeof(CommandExecutor).Assembly };
        var formatterInterfaceType = typeof(IGameEventFormatter<>);

        foreach (var assembly in assemblies)
        {
            var formatterTypes = assembly.GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false })
                .SelectMany(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == formatterInterfaceType)
                    .Select(i => new
                    {
                        Implementation = t,
                        Service = i
                    }));

            foreach (var typePair in formatterTypes)
            {
                builder.Services.AddSingleton(typePair.Service, typePair.Implementation);
            }
        }

        builder.Services.AddSingleton<IGameMessagePresenter, GameMessagePresenter>();
    }

}