using System.Text;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using FluentResults;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace WebApi.Core.CodeSuggestion;

public class DiffProvider
{
    public Result GetResult(Models.GitChange change)
    {
        if ((change.ChangeType & VersionControlChangeType.Add) == VersionControlChangeType.Add)
        {
            var content = new StringBuilder();
            var lines = change.SourceContent.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                content.AppendLine($"{i + 1} + {lines[i]}");
            }

            change.ContentChanges = content.ToString();
        }
        else if ((change.ChangeType & VersionControlChangeType.Edit) == VersionControlChangeType.Edit)
        {
            var diffBuilder = new SideBySideDiffBuilder(new Differ());
            var diff = diffBuilder.BuildDiffModel(change.SourceContent, change.TargetContent);

            var content = new StringBuilder();
            foreach (var line in diff.OldText.Lines)
            {
                switch (line.Type)
                {
                    case ChangeType.Deleted:
                        content.AppendLine($"{line.Position} + {line.Text}");
                        break;
                    case ChangeType.Modified:
                        content.AppendLine($"{line.Position} + {line.Text}");
                        break;
                    case ChangeType.Unchanged:
                        content.AppendLine($"{line.Position}  {line.Text}");
                        break;
                }
            }
            change.ContentChanges = content.ToString();
        }

        return Result.Ok();
    }
}
