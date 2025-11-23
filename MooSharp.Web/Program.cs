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
builder.Services.AddSingleton<AgentSpawner>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IPlayerStore, JsonPlayerStore>();

builder.Services.AddHostedService<GameEngine>();
builder.Services.AddHostedService<AgentBackgroundService>();

var channel = Channel.CreateUnbounded<GameInput>();

builder.Services.AddSingleton(channel.Writer);
builder.Services.AddSingleton(channel.Reader);

builder.RegisterCommandDefinitions();
builder.RegisterCommandHandlers();
builder.RegisterPresenters();

builder.Services.AddSignalR();

builder
    .Services
    .AddOptionsWithValidateOnStart<AppOptions>()
    .BindConfiguration(nameof(AppOptions))
    .ValidateDataAnnotations();

builder
    .Services
    .AddOptionsWithValidateOnStart<AgentOptions>()
    .BindConfiguration(AgentOptions.SectionName)
    .ValidateDataAnnotations();

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

