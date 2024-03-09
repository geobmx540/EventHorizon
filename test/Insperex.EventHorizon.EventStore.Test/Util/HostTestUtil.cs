﻿using System.Globalization;
using Destructurama;
using Insperex.EventHorizon.Abstractions.Extensions;
using Insperex.EventHorizon.Abstractions.Testing;
using Insperex.EventHorizon.EventStore.ElasticSearch.Extensions;
using Insperex.EventHorizon.EventStore.InMemory.Extensions;
using Insperex.EventHorizon.EventStore.MongoDb.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Xunit.Abstractions;

namespace Insperex.EventHorizon.EventStore.Test.Util;

public static class HostTestUtil
{
    public static IHost GetElasticHost(ITestOutputHelper output)
    {
        return GetHostBase(output)
            .ConfigureServices((context, services) =>
            {
                services.AddEventHorizon(x =>
                {
                    x.AddElasticSnapshotStore(context.Configuration.GetSection("ElasticSearch").Bind)
                        .AddElasticViewStore(context.Configuration.GetSection("ElasticSearch").Bind);
                });
            })
            .Build();
    }

    public static IHost GetInMemoryHost(ITestOutputHelper output)
    {
        return GetHostBase(output)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddEventHorizon(x =>
                {
                    x.AddInMemorySnapshotStore()
                        .AddInMemoryViewStore();
                });
            })
            .Build();
    }

    public static IHost GetMongoDbHost(ITestOutputHelper output)
    {
        return GetHostBase(output)
            .ConfigureServices((context, services) =>
            {
                services.AddEventHorizon(x =>
                {
                    x.AddMongoDbSnapshotStore(context.Configuration.GetSection("MongoDb").Bind)
                        .AddMongoDbViewStore(context.Configuration.GetSection("MongoDb").Bind);
                });
            })
            .Build();
    }

    private static IHostBuilder GetHostBase(ITestOutputHelper output)
    {
        return Host.CreateDefaultBuilder(System.Array.Empty<string>())
            .UseSerilog((_, config) =>
            {
                config.WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
                    .Destructure.UsingAttributes();

                if(output != null)
                    config.WriteTo.TestOutput(output, LogEventLevel.Information, formatProvider: CultureInfo.InvariantCulture);
            })
            .AddTestEventHorizonTesting()
            .UseEnvironment("test");
    }
}
