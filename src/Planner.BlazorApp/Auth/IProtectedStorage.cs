using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Planner.BlazorApp.Auth;

public interface IProtectedStorage
{
    Task<ProtectedBrowserStorageResult<T>> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value);
    Task DeleteAsync(string key);
}

public sealed class ProtectedStorageWrapper : IProtectedStorage
{
    private readonly ProtectedLocalStorage _protectedLocalStorage;

    public ProtectedStorageWrapper(ProtectedLocalStorage protectedLocalStorage)
    {
        _protectedLocalStorage = protectedLocalStorage;
    }

    public Task<ProtectedBrowserStorageResult<T>> GetAsync<T>(string key) =>
        _protectedLocalStorage.GetAsync<T>(key).AsTask();

    public Task SetAsync<T>(string key, T value) =>
        _protectedLocalStorage.SetAsync(key, value!).AsTask();

    public Task DeleteAsync(string key) =>
        _protectedLocalStorage.DeleteAsync(key).AsTask();
}
