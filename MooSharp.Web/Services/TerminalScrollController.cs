using System.Threading;
using Microsoft.JSInterop;

namespace MooSharp.Web.Services;

public static class TerminalScrollSelectors
{
    public const string TerminalElementId = "world-terminal-output";
}

public interface ITerminalScrollController
{
    Task ScrollTerminalToBottomAsync(CancellationToken ct = default);
    Task ScrollTerminalToTopAsync(CancellationToken ct = default);
}

public class TerminalScrollController(IJSRuntime jsRuntime) : ITerminalScrollController
{
    private const string TerminalElementId = TerminalScrollSelectors.TerminalElementId;

    public Task ScrollTerminalToBottomAsync(CancellationToken ct = default)
    {
        return jsRuntime.InvokeVoidAsync("terminalScroll.scrollToBottom", ct, TerminalElementId).AsTask();
    }

    public Task ScrollTerminalToTopAsync(CancellationToken ct = default)
    {
        return jsRuntime.InvokeVoidAsync("terminalScroll.scrollToTop", ct, TerminalElementId).AsTask();
    }
}
