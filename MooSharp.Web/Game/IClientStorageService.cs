namespace MooSharp.Web;

using Microsoft.JSInterop;

public interface IClientStorageService
{
    Task<string?> GetItemAsync(string key);
    Task SetItemAsync(string key, string value);
    Task RemoveItemAsync(string key);
}

public class ClientStorageService(IJSRuntime js) : IClientStorageService
{
    public async Task<string?> GetItemAsync(string key) => 
        await js.InvokeAsync<string?>("localStorage.getItem", key);

    public async Task SetItemAsync(string key, string value) => 
        await js.InvokeVoidAsync("localStorage.setItem", key, value);

    public async Task RemoveItemAsync(string key) => 
        await js.InvokeVoidAsync("localStorage.removeItem", key);
}