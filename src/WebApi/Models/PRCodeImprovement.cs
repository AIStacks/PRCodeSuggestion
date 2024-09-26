using System.Text.Json.Serialization;

namespace WebApi.Models;

public record PRCodeSuggestion
{
    [JsonPropertyName("suggestion_content")]
    public string SuggestionContent { get; set; } = "";

    [JsonPropertyName("existing_code")]
    public string ExistingCode { get; set; } = "";

    [JsonPropertyName("improved_code")]
    public string ImprovedCode { get; set; } = "";

    [JsonPropertyName("one_sentence_summary")]
    public string OneSentenceSummary { get; set; } = "";

    [JsonPropertyName("relevant_lines_start")]
    public int RelevantLinesStart { get; set; }

    [JsonPropertyName("relevant_lines_end")]
    public int RelevantLinesEnd { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; } = "";
}

public record PRCodeImprovement
{
    [JsonPropertyName("relevant_file")]
    public string RelevantFile { get; set; } = "";

    [JsonPropertyName("code_suggestions")]
    public List<PRCodeSuggestion> CodeSuggestions { get; set; } = new List<PRCodeSuggestion>();

    [JsonIgnore]
    public Guid RepositoryId { get; set; }

    [JsonIgnore]
    public string SourceCommitId { get; set; } = "";

    [JsonIgnore]
    public string TargetCommitId { get; set; } = "";
}
