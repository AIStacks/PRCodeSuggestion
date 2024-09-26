using FluentResults;
using WebApi.Models;

namespace WebApi.Core.CodeSuggestion;

public class FileValidator
{
    private readonly HashSet<string> _extensions = new HashSet<string>(Constants.CSharpFiles.Union(Constants.TypescriptFiles).Union(Constants.SqlscriptFiles));

    public Result CheckFileType(GitChange change)
    {
        var extension = Path.GetExtension(change.FilePath);
        return Result.FailIf(!_extensions.Contains(extension), $"file type ({extension}) is not supported");
    }

    public Result CheckFileSize(GitChange change)
    {
        var lines = change.SourceContent.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        return Result.FailIf(lines.Length > 1500, $"This file is too large ({lines.Length} lines)");
    }
}
