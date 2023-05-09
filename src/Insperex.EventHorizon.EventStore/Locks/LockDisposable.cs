﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Insperex.EventHorizon.EventStore.Interfaces.Stores;
using Insperex.EventHorizon.EventStore.Models;

namespace Insperex.EventHorizon.EventStore.Locks;

public class LockDisposable : IAsyncDisposable
{
    private readonly ICrudStore<Lock> _crudStore;
    private readonly string _id;
    private readonly TimeSpan _timeout;
    private bool _isReleased;
    private bool _ownsLock;

    public LockDisposable(ICrudStore<Lock> crudStore, string id, TimeSpan timeout)
    {
        _id = id;
        _timeout = timeout;
        _crudStore = crudStore;

        // Used for when process is stopped mid way
        AppDomain.CurrentDomain.ProcessExit += OnExit;
    }

    public async Task WaitForLockAsync()
    {
        do
        {
            _ownsLock = await TryLockAsync();
            if (!_ownsLock)
                await Task.Delay(200);
        } while (!_ownsLock);
    }

    public async Task<bool> TryLockAsync()
    {
        var @lock = new Lock { Id = _id, Expiration = DateTime.UtcNow.AddMilliseconds(_timeout.TotalMilliseconds) };
        var result = await _crudStore.InsertAsync(new[] { @lock }, CancellationToken.None);
        _ownsLock = result.FailedIds?.Any() != true;

        if (!_ownsLock)
        {
            var current = (await _crudStore.GetAllAsync(new[] { _id }, CancellationToken.None)).FirstOrDefault();
            if (current != null)
                return current.Expiration < DateTime.UtcNow;
        }

        SetTimeout();

        return _ownsLock;
    }

    public async Task<LockDisposable> ReleaseAsync()
    {
        if (_isReleased || _ownsLock != true)
            return this;

        _isReleased = true;
        await _crudStore.DeleteAsync(new[] { _id }, CancellationToken.None);
        return this;
    }

    private async void SetTimeout()
    {
        await Task.Delay(_timeout);
        await ReleaseAsync();
    }

    private void OnExit(object sender, EventArgs e)
    {
        if(!_isReleased)
            ReleaseAsync().Wait();
    }

    public async ValueTask DisposeAsync()
    {
        if(!_isReleased)
            await ReleaseAsync();
    }
}
