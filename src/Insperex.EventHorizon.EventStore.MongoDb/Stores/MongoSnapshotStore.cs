using System;
using Insperex.EventHorizon.Abstractions.Attributes;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Util;
using Insperex.EventHorizon.EventStore.Interfaces.Stores;
using Insperex.EventHorizon.EventStore.Models;
using Insperex.EventHorizon.EventStore.MongoDb.Attributes;
using MongoDB.Driver;

namespace Insperex.EventHorizon.EventStore.MongoDb.Stores
{
    public class MongoSnapshotStore<T> : AbstractMongoCrudStore<Snapshot<T>>, ISnapshotStore<T> where T : IState
    {
        private static readonly Type Type = typeof(T);

        public MongoSnapshotStore(MongoClientResolver clientResolver, AttributeUtil attributeUtil)
            : base(clientResolver.GetClient(), attributeUtil.GetOne<MongoCollectionAttribute>(Type), attributeUtil.GetOne<SnapshotStoreAttribute>(Type).Database)
        {
        }
    }
}
