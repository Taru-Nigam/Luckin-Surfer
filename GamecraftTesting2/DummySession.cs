using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class DummySession : ISession
{
    private Dictionary<string, byte[]> _sessionStorage = new Dictionary<string, byte[]>();

    public IEnumerable<string> Keys => _sessionStorage.Keys;

    public string Id => Guid.NewGuid().ToString();

    public bool IsAvailable => true;

    public void Clear() => _sessionStorage.Clear();

    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Remove(string key) => _sessionStorage.Remove(key);

    public void Set(string key, byte[] value) => _sessionStorage[key] = value;

    public bool TryGetValue(string key, out byte[] value) => _sessionStorage.TryGetValue(key, out value);
}
