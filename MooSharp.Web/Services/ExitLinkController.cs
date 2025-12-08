using Microsoft.JSInterop;

namespace MooSharp.Web.Services;

public interface IExitLinkController
{
    Task InitializeAsync(Func<string, Task> onExitClick, CancellationToken ct = default);
}

public class ExitLinkController(IJSRuntime jsRuntime) : IExitLinkController
{
    public async Task InitializeAsync(Func<string, Task> onExitClick, CancellationToken ct = default)
    {
        var handler = new ExitLinkCallbackHandler(onExitClick);
        var dotNetRef = DotNetObjectReference.Create(handler);

        await jsRuntime.InvokeVoidAsync(
            "terminalInterop.initializeExitLinks",
            ct,
            dotNetRef,
            TerminalScrollSelectors.TerminalElementId);
    }

    public class ExitLinkCallbackHandler(Func<string, Task> callback)
    {
        [JSInvokable]
        public Task HandleExitClick(string exitName) => callback(exitName);
    }
}
