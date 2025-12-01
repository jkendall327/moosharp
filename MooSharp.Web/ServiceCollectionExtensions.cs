using System.Reflection;
using System.Threading.Channels;
using MooSharp.Agents;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Searching;
using MooSharp.Game;
using MooSharp.Infrastructure;
using MooSharp.Messaging;
using MooSharp.Persistence;
using MooSharp.Web.Game;
using MooSharp.World;

namespace MooSharp.Web;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public void AddMooSharpWebServices()
        {
            services.AddScoped<IClientStorageService, ClientStorageService>();
            services.AddScoped<IGameHistoryService, ClientStorageGameHistoryService>();
            services.AddScoped<IGameConnectionService, SignalRGameConnectionService>();
            services.AddScoped<GameClientViewModel>();
        }

        public void AddMooSharpServices()
        {
            // World
            services.AddSingleton<IWorldSeeder, WorldSeeder>();
            services.AddSingleton<WorldInitializer>();
            services.AddSingleton<World.World>();

            // Commands
            services.AddSingleton<CommandParser>();
            services.AddSingleton<CommandExecutor>();
            services.AddSingleton<CommandReference>();
            services.AddSingleton<TargetResolver>();

            // Agents
            services.AddSingleton<IAgentPromptProvider, AgentPromptProvider>();
            services.AddSingleton<AgentSpawner>();
            services.AddSingleton<AgentFactory>();
            services.AddSingleton<IAgentResponseProvider, AgentResponseProvider>();

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

        public void AddMooSharpMessaging()
        {
            var channel = Channel.CreateUnbounded<GameInput>();

            services.AddSingleton(channel.Writer);
            services.AddSingleton(channel.Reader);
        }

        public void AddMooSharpHostedServices()
        {
            services.AddHostedService<GameEngineBackgroundService>();
            services.AddHostedService<AgentBackgroundService>();
            services.AddHostedService<WorldClockService>();
            services.AddHostedService<TreasureSpawnerService>();
        }

        public void AddMooSharpOptions()
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

            services
                .AddOptionsWithValidateOnStart<TreasureSpawnerOptions>()
                .BindConfiguration(TreasureSpawnerOptions.SectionName)
                .ValidateDataAnnotations();

            services.Configure<ServiceProviderOptions>(s =>
            {
                s.ValidateOnBuild = true;
                s.ValidateScopes = true;
            });
        }
    }

    extension(WebApplicationBuilder webApplicationBuilder)
    {
        /// <summary>
        /// Register all ICommandHandlers via reflection.
        /// </summary>
        public void RegisterCommandHandlers()
        {
            var assemblies = new List<Assembly>([Assembly.GetExecutingAssembly(), typeof(CommandExecutor).Assembly]);

            var handlerInterfaceType = typeof(IHandler<>);

            foreach (var assembly in assemblies)
            {
                var handlerTypes = assembly.GetTypes()
                    .Where(t => t is { IsAbstract: false, IsInterface: false })
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
        public void RegisterCommandDefinitions()
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
                        t is { IsAbstract: false, IsInterface: false } &&
                        definitionType.IsAssignableFrom(t));

                foreach (var type in types)
                {
                    webApplicationBuilder.Services.AddTransient(definitionType, type);
                }
            }
        }

        /// <summary>
        /// Register all IGameEventFormatter implementations and the presenter pipeline.
        /// </summary>
        public void RegisterPresenters()
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
                    webApplicationBuilder.Services.AddSingleton(typeof(IGameEventFormatter), typePair.Implementation);
                }
            }

            webApplicationBuilder.Services.AddSingleton<IGameMessagePresenter, GameMessagePresenter>();
        }
    }
}
