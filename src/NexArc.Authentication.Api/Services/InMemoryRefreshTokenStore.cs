using System.Collections.Concurrent;
using NexArc.Authentication.Api.Models;

namespace NexArc.Authentication.Api.Services;

public sealed class InMemoryRefreshTokenStore : IRefreshTokenStore
{
    private readonly ConcurrentDictionary<string, RefreshTokenRecord> _store = new();

    public Task StoreAsync(RefreshTokenRecord record, CancellationToken ct)
    {
        _store[record.TokenHash] = record;
        return Task.CompletedTask;
    }

    public Task<RefreshTokenRecord?> FindAsync(string tokenHash, CancellationToken ct)
    {
        _store.TryGetValue(tokenHash, out var record);
        return Task.FromResult(record);
    }

    public Task RevokeAsync(string tokenHash, CancellationToken ct)
    {
        _store.TryRemove(tokenHash, out _);
        return Task.CompletedTask;
    }
}
