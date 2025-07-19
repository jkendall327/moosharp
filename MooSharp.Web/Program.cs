using MooSharp;
using MooSharp.Web;
using MooSharp.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddSingleton<World>();
builder.Services.AddSingleton<CommandParser>();
builder.Services.AddSingleton<PlayerMultiplexer>();

builder.Services.AddHostedService<TelnetServer>();

builder.Services.Configure<AppOptions>(
    builder.Configuration.GetSection(nameof(AppOptions)));

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

app.Run();