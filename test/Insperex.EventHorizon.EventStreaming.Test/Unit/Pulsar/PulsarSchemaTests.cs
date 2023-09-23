﻿using Insperex.EventHorizon.EventStreaming.Pulsar.AdvancedFailure;
using Pulsar.Client.Api;
using Xunit;

namespace Insperex.EventHorizon.EventStreaming.Test.Unit.Pulsar;

[Trait("Category", "Unit")]
public class PulsarSchemaTests
{
    [Fact]
    public void CreateTopicStreamStateSchema()
    {
        var schema = Schema.JSON<TopicStreamState>();
        Assert.NotNull(schema);
    }
}
