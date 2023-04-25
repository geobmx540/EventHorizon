﻿using System;
using System.Net.Http;
using Insperex.EventHorizon.Abstractions.Util;
using Insperex.EventHorizon.EventStreaming.Admins;
using Insperex.EventHorizon.EventStreaming.Interfaces.Streaming;
using Insperex.EventHorizon.EventStreaming.Publishers;
using Insperex.EventHorizon.EventStreaming.Pulsar.Models;
using Insperex.EventHorizon.EventStreaming.Subscriptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pulsar.Client.Api;
using SharpPulsar.Admin.v2;

namespace Insperex.EventHorizon.EventStreaming.Pulsar.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPulsarEventStream(this IServiceCollection collection,
        IConfiguration configuration)
    {
        var section = configuration.GetSection("Pulsar");
        var config = section.Get<PulsarConfig>();
        if (config == null)
            throw new Exception("Pulsar Config is Missing");

        // Add Pulsar Client
        collection.AddSingleton(x => new PulsarClientBuilder()
            .ServiceUrl(config.ServiceUrl)
            .EnableTransaction(true)
            .BuildAsync()
            .Result);

        // Add Pulsar Admin
        collection.AddSingleton<IPulsarAdminRESTAPIClient>(x =>
            new PulsarAdminRESTAPIClient(new HttpClient { BaseAddress = new Uri($"{config.AdminUrl}/admin/v2/") }));

        // Add Admin
        collection.AddSingleton<ITopicAdmin, PulsarTopicAdmin>();

        // Add Factory
        collection.Configure<PulsarConfig>(section);
        collection.AddSingleton(typeof(IStreamFactory), typeof(PulsarStreamFactory));
        collection.AddSingleton(typeof(StreamingClient));
        collection.AddSingleton(typeof(PublisherBuilder<>));
        collection.AddSingleton(typeof(Readers.ReaderBuilder<>));
        collection.AddSingleton(typeof(SubscriptionBuilder<>));
        collection.AddSingleton(typeof(Admin<>));
        collection.AddSingleton<AttributeUtil>();

        return collection;
    }
}
