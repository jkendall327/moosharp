using System.Reflection;
using System.Threading.Channels;
using Microsoft.SemanticKernel;
using MooSharp;
using MooSharp.Persistence;
using MooSharp.Web;
using MooSharp.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddSingleton<World>();
builder.Services.AddSingleton<CommandParser>();
builder.Services.AddSingleton<CommandParser>();
builder.Services.AddSingleton<CommandExecutor>();
builder.Services.AddSingleton<IPlayerStore, JsonPlayerStore>();

builder
    .Services
    .AddKernel()
    .AddOpenAIChatCompletion(builder.Configuration["Agents:OpenAIModelId"], builder.Configuration["Agents:OpenAIModelName"]);

builder.Services.AddHostedService<GameEngine>();
builder.Services.AddHostedService<AgentBackgroundService>();

var channel = Channel.CreateUnbounded<GameInput>();

builder.Services.AddSingleton(channel.Writer);
builder.Services.AddSingleton(channel.Reader);

RegisterCommandHandlers(builder);

builder.Services.AddSignalR();

builder.Services.Configure<AppOptions>(builder.Configuration.GetSection(nameof(AppOptions)));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);

    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.MapHub<MooHub>("/moohub");

var world = app.Services.GetRequiredService<World>();

await world.InitializeAsync();

await app.RunAsync();

void RegisterCommandHandlers(WebApplicationBuilder webApplicationBuilder)
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