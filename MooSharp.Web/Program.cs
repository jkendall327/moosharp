using MooSharp.Data;
using MooSharp.Web;
using MooSharp.Web.Components;
using MooSharp.Web.Endpoints;
using MooSharp.Web.Game;
using MooSharp.Web.Services;
using MooSharp.World;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddSignalR(hubOptions =>
{
    // 32KB. This is the framework default, but set it explicitly to prevent it changing from under us in future .NET versions etc.
    hubOptions.MaximumReceiveMessageSize = 32 * 1024;
});

builder.Services.AddHttpClient();

builder.Services.AddMooSharpOptions();
builder.Services.AddMooSharpWebServices();

var databasePath = builder.Configuration.GetValue<string>("AppOptions:DatabaseFilepath")
                   ?? throw new InvalidOperationException("DatabaseFilepath is not configured.");

builder.Services.AddMooSharpData(databasePath);
builder.Services.AddMooSharpServices();
builder.Services.AddMooSharpHostedServices();
builder.Services.AddMooSharpMessaging();
builder.Services.AddMooSharpAuth(builder.Configuration);

builder.RegisterCommandDefinitions();
builder.RegisterCommandHandlers();
builder.RegisterPresenters();

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    await app.EnsureMooSharpDatabaseCreatedAsync();
}

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

app.MapHub<MooHub>(MooHub.HubName);

await app.RunAsync();

return;

static async Task InitializeWorldAsync(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var initializer = scope.ServiceProvider.GetRequiredService<WorldInitializer>();

    await initializer.InitializeAsync();
}
