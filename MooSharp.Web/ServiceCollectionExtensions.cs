using System.Reflection;
using System.Text;
using System.Threading.Channels;
using Microsoft.IdentityModel.Tokens;
using MooSharp.Actors;
using MooSharp.Agents;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Searching;
using MooSharp.Game;
using MooSharp.Infrastructure;
using MooSharp.Messaging;
using MooSharp.Web.Endpoints;
using MooSharp.Web.Game;
using MooSharp.Web.Services;
using MooSharp.World;

namespace MooSharp.Web;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public void AddMooSharpWebServices()
        {
            services.AddSingleton<ISessionGateway, SignalRSessionGateway>();
            services.AddScoped<IClientStorageService, ClientStorageService>();
            services.AddScoped<IGameHistoryService, ClientStorageGameHistoryService>();
            services.AddScoped<IGameConnectionService, SignalRGameConnectionService>();
            services.AddScoped<GameClientViewModel>();
            services.AddSingleton<JwtTokenService>();
            services.AddSingleton<ActorIdentityResolver>();

            services.AddHttpContextAccessor();

            services.AddHttpClient(nameof(AuthEndpoints),
                (sp, client) =>
                {
                    var accessor = sp.GetRequiredService<IHttpContextAccessor>();
                    var http = accessor.HttpContext!;

                    // Builds: "https://yoursite.com"
                    var baseUri = new Uri($"{http.Request.Scheme}://{http.Request.Host}");

                    client.BaseAddress = baseUri;
                });
        }

        public void AddMooSharpServices()
        {
            // World
            services.AddSingleton<IWorldSeeder, WorldSeeder>();
            services.AddSingleton<WorldInitializer>();
            services.AddSingleton<World.World>();
            services.AddSingleton<IGameEngine, GameEngine>();

            // Players
            services.AddSingleton<PlayerHydrator>();
            services.AddSingleton<PlayerMessageProvider>();

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

            // Connections and message-sending
            services.AddSingleton<IGameMessageEmitter, SessionGatewayMessageEmitter>();

            // Generic infrastructure
            services.AddSingleton<SlugCreator>();
            services.AddSingleton(TimeProvider.System);

            // Systems
            services.AddSingleton<IWorldClock, WorldClock>();
            services.AddSingleton<GameInputProcessor>();
        }

        public void AddMooSharpMessaging()
        {
            var channel = Channel.CreateUnbounded<InputCommand>();

            services.AddSingleton(channel.Writer);
            services.AddSingleton(channel.Reader);
        }

        public void AddMooSharpHostedServices()
        {
            services.AddHostedService<GameEngineBackgroundService>();
            services.AddHostedService<AgentBackgroundService>();
            services.AddHostedService<WorldClockService>();
            services.AddHostedService<TreasureSpawnerService>();
            services.AddHostedService<PlayerLoginService>();
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

        public void AddMooSharpAuth(IConfiguration config)
        {
            services
                .AddAuthentication("Bearer")
                .AddJwtBearer("Bearer",
                    options =>
                    {
                        // Without this it renames the 'sub', 'name' claims etc. into some stupid SOAP format.
                        options.MapInboundClaims = false;

                        var jwtSettings = config.GetSection("Jwt");

                        var keyString = jwtSettings["Key"] ??
                                        throw new InvalidOperationException("JWT key is not configured.");

                        options.TokenValidationParameters = new()
                        {
                            ValidateIssuer = true,
                            ValidIssuer = jwtSettings["Issuer"],
                            ValidateAudience = true,
                            ValidAudience = jwtSettings["Audience"],
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString)),
                            ValidateLifetime = true
                        };

                        // For SignalR, read the token from the query string.
                        // SignalR browsers cannot send headers in WebSockets, so they send it in ?access_token=...
                        options.Events = new()
                        {
                            OnMessageReceived = context =>
                            {
                                var accessToken = context.Request.Query["access_token"];
                                var path = context.HttpContext.Request.Path;

                                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments(MooHub.HubName))
                                {
                                    context.Token = accessToken;
                                }

                                return Task.CompletedTask;
                            }
                        };
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
                var handlerTypes = assembly
                    .GetTypes()
                    .Where(t => t is {IsAbstract: false, IsInterface: false})
                    .SelectMany(t => t
                        .GetInterfaces()
                        .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType)
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
                Assembly.GetExecutingAssembly(), typeof(CommandParser).Assembly
            };

            var definitionType = typeof(ICommandDefinition);

            foreach (var assembly in assemblies)
            {
                var types = assembly
                    .GetTypes()
                    .Where(t => t is {IsAbstract: false, IsInterface: false} && definitionType.IsAssignableFrom(t));

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
            var assemblies = new[]
            {
                Assembly.GetExecutingAssembly(), typeof(CommandExecutor).Assembly
            };

            var formatterInterfaceType = typeof(IGameEventFormatter<>);

            foreach (var assembly in assemblies)
            {
                var formatterTypes = assembly
                    .GetTypes()
                    .Where(t => t is {IsAbstract: false, IsInterface: false})
                    .SelectMany(t => t
                        .GetInterfaces()
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