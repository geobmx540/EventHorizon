using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.EventStore.Models;

namespace Insperex.EventHorizon.EventStore.Interfaces.Stores
{
    public interface ISnapshotStore<T> : ICrudStore<Snapshot<T>> where T : IState
    {

    }
}
