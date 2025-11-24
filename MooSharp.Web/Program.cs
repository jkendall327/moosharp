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
builder.Services.AddSignalR();

builder.Services.AddMooSharpServices(builder.Configuration);
builder.Services.AddMooSharpOptions(builder.Configuration);
builder.Services.AddMooSharpHostedServices(builder.Configuration);
builder.Services.AddMooSharpMessaging(builder.Configuration);

builder.RegisterCommandDefinitions();
builder.RegisterCommandHandlers();
builder.RegisterPresenters();

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

await app.RunAsync();

