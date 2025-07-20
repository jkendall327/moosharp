using System.Reflection;
using MooSharp;
using MooSharp.Web;
using MooSharp.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddSingleton<World>();
builder.Services.AddSingleton<CommandParser>();
builder.Services.AddSingleton<PlayerMultiplexer>();
builder.Services.AddSingleton<CommandParser>();
builder.Services.AddSingleton<CommandExecutor>();
builder.Services.AddSingleton<StringProvider>();
//builder.Services.AddSingleton<LoginManager>();
builder.Services.AddSingleton<PlayerGameLoopManager>();

RegisterCommandHandlers(builder);

builder.Services.AddHostedService<TelnetServer>();
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

app.Run();

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