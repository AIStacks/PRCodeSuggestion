using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace WebApi.Models;

public record GitChange(
    Guid RepositoryId,
    string FilePath,
    string LastMergeSourceCommitId = "",
    string LastMergeTargetCommitId = "",
    VersionControlChangeType ChangeType = VersionControlChangeType.None)
{
    public string ContentChanges { get; set; } = "";

    public string SourceContent { get; set; } = "";

    public string TargetContent { get; set; } = "";
}
