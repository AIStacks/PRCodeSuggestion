using FluentResults;
using WebApi.Core.CodeSuggestion;
using WebApi.Models;

namespace WebApi.Core;

public class CodeSuggestionWorkFlow
{

    private readonly Cache _cache;
    private readonly GitProvider _gitProvider;
    private readonly FileValidator _fileValidator;
    private readonly Agent _agent;
    private readonly DiffProvider _diff;
    private readonly ILogger<CodeSuggestionWorkFlow> _logger;

    public CodeSuggestionWorkFlow(IServiceProvider serviceProvider)
    {
        _cache = serviceProvider.GetRequiredService<Cache>();
        _gitProvider = serviceProvider.GetRequiredService<GitProvider>();
        _fileValidator = serviceProvider.GetRequiredService<FileValidator>();
        _agent = serviceProvider.GetRequiredService<Agent>();
        _diff = serviceProvider.GetRequiredService<DiffProvider>();

        _logger = serviceProvider.GetRequiredService<ILogger<CodeSuggestionWorkFlow>>();
    }

    public async IAsyncEnumerable<Result<PRCodeImprovement>> GetResultsAsync(string url, CancellationToken cancellationToken)
    {
        var gitChangesResult = await _gitProvider.GetGitChangesAsync(url, cancellationToken).ConfigureAwait(false);
        if (gitChangesResult.IsFailed)
        {
            yield return Result.Fail(gitChangesResult.Errors);
            yield break;
        }

        foreach (var change in gitChangesResult.Value)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            var improvement = new PRCodeImprovement
            {
                RelevantFile = change.FilePath
            };

            var fileTypeValidation = _fileValidator.CheckFileType(change);
            if (fileTypeValidation.IsFailed)
            {
                improvement.CodeSuggestions.Add(new PRCodeSuggestion { Label = "error", OneSentenceSummary = fileTypeValidation.Errors[0].Message });
                yield return Result.Ok(improvement);
                continue;
            }

            var cacheItem = _cache.Get(change);
            if (cacheItem != null)
            {
                yield return Result.Ok(cacheItem);
                continue;
            }

            await _gitProvider.GetFileContentAsync(change, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(change.SourceContent))
            {
                _logger.LogInformation($"File not found, path `{change.FilePath}`");
                continue;
            }

            var fileSizeValidation = _fileValidator.CheckFileSize(change);
            if (fileSizeValidation.IsFailed)
            {
                improvement.CodeSuggestions.Add(new PRCodeSuggestion { Label = "error", OneSentenceSummary = fileSizeValidation.Errors[0].Message });
                yield return Result.Ok(improvement);
                continue;
            }

            _diff.GetResult(change);

            var response = await _agent.GetResponseAsync(change, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccess)
            {
                _cache.Save(response.Value);
            }

            yield return response;
        }
    }

}
