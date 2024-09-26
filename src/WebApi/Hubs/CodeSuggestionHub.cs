using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using WebApi.Core;
using WebApi.Models;

namespace WebApi.Hubs;

public class CodeSuggestionHub : Hub
{
    private readonly CodeSuggestionWorkFlow _workFlow;

    public CodeSuggestionHub(CodeSuggestionWorkFlow workFlow)
    {
        _workFlow = workFlow;
    }

    public ChannelReader<APIResult<PRCodeImprovement>> StreamUpdatesToClient(string connectionId, string url, CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<APIResult<PRCodeImprovement>>();

        _ = WriteResultAsync(channel.Writer, connectionId, url, cancellationToken);

        return channel.Reader;
    }

    private async Task WriteResultAsync(ChannelWriter<APIResult<PRCodeImprovement>> writer, string connectionId, string url, CancellationToken cancellationToken)
    {
        // Write the update if the connection ID matches
        if (Context.ConnectionId == connectionId)
        {
            await foreach (var codeSuggestion in _workFlow.GetResultsAsync(url, cancellationToken))
            {
                var result = codeSuggestion.ToDto();
                await writer.WriteAsync(result, cancellationToken).ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        writer.TryComplete();
    }
}
