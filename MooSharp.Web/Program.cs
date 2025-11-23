using System.Reflection;
using System.Threading.Channels;
using Microsoft.SemanticKernel;
using MooSharp;
using MooSharp.Agents;
using MooSharp.Persistence;
using MooSharp.Web;
using MooSharp.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddSingleton<World>();
builder.Services.AddSingleton<CommandParser>();
builder.Services.AddSingleton<CommandExecutor>();
builder.Services.AddSingleton<AgentService>();
builder.Services.AddSingleton<IPlayerStore, JsonPlayerStore>();

builder
    .Services
    .AddKernel()
    .AddOpenAIChatCompletion(builder.Configuration["Agents:OpenAIModelId"], builder.Configuration["Agents:OpenAIApiKey"]);

builder.Services.AddHostedService<GameEngine>();
builder.Services.AddHostedService<AgentBackgroundService>();

var channel = Channel.CreateUnbounded<GameInput>();

builder.Services.AddSingleton(channel.Writer);
builder.Services.AddSingleton(channel.Reader);

builder.RegisterCommandDefinitions();
builder.RegisterCommandHandlers();

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

