using Microsoft.AspNetCore.Hosting;
using MooSharp.Agents;
using MooSharp.Infrastructure;
using MooSharp.Tests.TestDoubles;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MooSharp.Tests;

public class MooSharpTestApp : WebApplicationFactory<Program>
{
    public string DbName { get; } = $"test_{Guid.NewGuid()}.db";
    public TestConnectionFactory ConnectionFactory { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IPlayerConnectionFactory>();
            services.AddSingleton<IPlayerConnectionFactory>(ConnectionFactory);

            services.Configure<AppOptions>(opts =>
            {
                opts.DatabaseFilepath = DbName;
                opts.WorldDataFilepath = "world.json";
            });

            services.Configure<AgentOptions>(opts => { opts.Enabled = false; });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (File.Exists(DbName))
        {
            File.Delete(DbName);
        }
    }
}