using Microsoft.Extensions.DependencyInjection;
using MooSharp;
using MooSharp.Web;
using MooSharp.Web.Components;
using MooSharp.Web.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSignalR();
builder.Services.AddHttpClient();

builder.Services.AddMooSharpServices(builder.Configuration);
builder.Services.AddMooSharpOptions(builder.Configuration);
builder.Services.AddMooSharpHostedServices(builder.Configuration);
builder.Services.AddMooSharpMessaging(builder.Configuration);

builder.RegisterCommandDefinitions();
builder.RegisterCommandHandlers();
builder.RegisterPresenters();

var app = builder.Build();

await InitializeWorldAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.MapPlayerCountEndpoint();

app.MapHub<MooHub>("/moohub");

await app.RunAsync();

static async Task InitializeWorldAsync(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var initializer = scope.ServiceProvider.GetRequiredService<WorldInitializer>();

    await initializer.InitializeAsync();
}

