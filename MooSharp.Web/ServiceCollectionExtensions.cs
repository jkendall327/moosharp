using System.Reflection;
using System.Threading.Channels;
using MooSharp.Agents;
using MooSharp.Infrastructure;
using MooSharp.Messaging;
using MooSharp.Persistence;

namespace MooSharp.Web;

public static class ServiceCollectionExtensions
{
    public static void AddMooSharpServices(this IServiceCollection services, IConfiguration config)
    {
        // World
        services.AddSingleton<IWorldSeeder, WorldSeeder>();
        services.AddSingleton<WorldInitializer>();
        services.AddSingleton<World>();
        
        // Commands
        services.AddSingleton<CommandParser>();
        services.AddSingleton<CommandExecutor>();
        services.AddSingleton<CommandReference>();
        
        // Agents
        services.AddSingleton<IAgentPromptProvider, AgentPromptProvider>();
        services.AddSingleton<AgentSpawner>();
        services.AddSingleton<AgentFactory>();
        
        // Persistence
        services.AddSingleton<IPlayerStore, SqlitePlayerStore>();
        services.AddSingleton<IWorldStore, SqliteWorldStore>();

        // Connections and message-sending
        services.AddSingleton<IPlayerConnectionFactory, SignalRPlayerConnectionFactory>();
        services.AddSingleton<IRawMessageSender, SignalRRawMessageSender>();
        services.AddSingleton<PlayerSessionManager>();
        
        // Generic infrastructure
        services.AddSingleton<SlugCreator>();
        services.AddSingleton(TimeProvider.System);
        
        // Systems
        services.AddSingleton<IWorldClock, WorldClock>();
        services.AddSingleton<GameEngine>();
    }
    
    public static void AddMooSharpMessaging(this IServiceCollection services, IConfiguration config)
    {
        var channel = Channel.CreateUnbounded<GameInput>();

        services.AddSingleton(channel.Writer);
        services.AddSingleton(channel.Reader);
    }
    
    public static void AddMooSharpHostedServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddHostedService<GameEngineBackgroundService>();
        services.AddHostedService<AgentBackgroundService>();
        services.AddHostedService<WorldClockService>();
    }
    
    public static void AddMooSharpOptions(this IServiceCollection services, IConfiguration config)
    {
        services
            .AddOptionsWithValidateOnStart<AppOptions>()
            .BindConfiguration(nameof(AppOptions))
            .ValidateDataAnnotations();

        services
            .AddOptionsWithValidateOnStart<AgentOptions>()
            .BindConfiguration(AgentOptions.SectionName)
            .ValidateDataAnnotations();

        services
            .AddOptionsWithValidateOnStart<WorldClockOptions>()
            .BindConfiguration(WorldClockOptions.SectionName)
            .ValidateDataAnnotations();

        services.Configure<ServiceProviderOptions>(s =>
        {
            s.ValidateOnBuild = true;
            s.ValidateScopes = true;
        });
    }

    
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
                builder.Services.AddSingleton(typeof(IGameEventFormatter), typePair.Implementation);
            }
        }

        builder.Services.AddSingleton<IGameMessagePresenter, GameMessagePresenter>();
    }

}