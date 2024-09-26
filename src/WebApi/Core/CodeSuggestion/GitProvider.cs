using FluentResults;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace WebApi.Core.CodeSuggestion;

public class GitProvider
{

    private readonly VssConnection _connection;
    private readonly bool _reviewCompletedPR = true;

    public GitProvider(IConfiguration configuration)
    {
        string orgUrl = configuration["AZURE_DEVOPS_ORG"];
        if (string.IsNullOrWhiteSpace(orgUrl))
        {
            throw new InvalidOperationException("Environment variable `AZURE_DEVOPS_ORG` not exists or value is null");
        }

        string pat = configuration["AZURE_DEVOPS_PAT"];
        if (string.IsNullOrWhiteSpace(pat))
        {
            throw new InvalidOperationException("Environment variable `AZURE_DEVOPS_PAT` not exists or value is null");
        }

        bool.TryParse(configuration["ReviewCompletedPR"], out _reviewCompletedPR);

        _connection = new VssConnection(new Uri(orgUrl), new VssBasicCredential(string.Empty, pat));
    }

    public async Task<Result> GetFileContentAsync(Models.GitChange change, CancellationToken cancellationToken)
    {
        using var client = _connection.GetClient<GitHttpClient>();

        if ((change.ChangeType & VersionControlChangeType.Edit) == VersionControlChangeType.Edit || (change.ChangeType & VersionControlChangeType.Add) == VersionControlChangeType.Add)
        {
            var sourceFile = await client.GetItemContentAsync(
                        cancellationToken: cancellationToken,
                        repositoryId: change.RepositoryId,
                        path: change.FilePath,
                        versionDescriptor: new GitVersionDescriptor
                        {
                            VersionType = GitVersionType.Commit,
                            Version = change.LastMergeSourceCommitId
                        })
                .ConfigureAwait(false);
            using var srcReader = new StreamReader(sourceFile);
            string srcContent = await srcReader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

            change.SourceContent = srcContent;

            if ((change.ChangeType & VersionControlChangeType.Edit) == VersionControlChangeType.Edit)
            {
                var targetFile = await client.GetItemContentAsync(
                        cancellationToken: cancellationToken,
                        repositoryId: change.RepositoryId,
                        path: change.FilePath,
                        versionDescriptor: new GitVersionDescriptor
                        {
                            VersionType = GitVersionType.Commit,
                            Version = change.LastMergeTargetCommitId
                        })
                    .ConfigureAwait(false);

                using var tgtReader = new StreamReader(targetFile);
                string tgtContent = await tgtReader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

                change.TargetContent = tgtContent;
            }

            return Result.Ok();
        }

        return Result.Fail($"{change.ChangeType}");
    }

    public async Task<Result<IEnumerable<Models.GitChange>>> GetGitChangesAsync(string url, CancellationToken cancellationToken)
    {
        var urlParseResult = GetUrlParseResult(url);
        if (!urlParseResult.IsSuccess)
        {
            return Result.Fail(urlParseResult.Errors);
        }

        string error = "";
        GitPullRequest pullRequest = null;
        using var client = _connection.GetClient<GitHttpClient>();
        try
        {
            pullRequest = await client.GetPullRequestByIdAsync(urlParseResult.Value, null, cancellationToken).ConfigureAwait(false);

            if (!_reviewCompletedPR && pullRequest.Status == PullRequestStatus.Completed && pullRequest.ClosedDate < DateTime.Today)
            {
                error = "This pull request has completed.";
            }
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }

        if (!string.IsNullOrEmpty(error) || pullRequest == null)
        {
            return Result.Fail(error);
        }

        var commitDiffs = await client.GetCommitDiffsAsync(
                cancellationToken: cancellationToken,
                repositoryId: pullRequest.Repository.Id,
                diffCommonCommit: true,
                baseVersionDescriptor: new GitBaseVersionDescriptor
                {
                    VersionType = GitVersionType.Commit,
                    Version = pullRequest.LastMergeTargetCommit.CommitId
                },
                targetVersionDescriptor: new GitTargetVersionDescriptor
                {
                    VersionType = GitVersionType.Commit,
                    Version = pullRequest.LastMergeSourceCommit.CommitId
                })
            .ConfigureAwait(false);

        var changes = commitDiffs.Changes
                        .Where(c => !c.Item.IsFolder)
                        .Select(c => new Models.GitChange(
                                pullRequest.Repository.Id,
                                c.Item.Path,
                                pullRequest.LastMergeSourceCommit.CommitId,
                                pullRequest.LastMergeTargetCommit.CommitId,
                                ChangeType: c.ChangeType));

        return Result.Ok(changes);
    }

    private Result<int> GetUrlParseResult(string url)
    {
        var uri = new Uri(url);

        string pullRequestSegment = "/pullrequest/";
        int index = uri.AbsolutePath.IndexOf(pullRequestSegment, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return Result.Fail("Invalid pull request url");
        }

        string idPart = uri.AbsolutePath.Substring(index + pullRequestSegment.Length);
        string[] segments = idPart.Split('/');

        if (segments.Length == 0 || !int.TryParse(segments[0], out int pullRequestId))
        {
            return Result.Fail("Invalid pull request ID");
        }

        return Result.Ok(pullRequestId);
    }
}
