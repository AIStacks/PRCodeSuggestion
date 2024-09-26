using System.Reflection;
using System.Text;
using System.Text.Json;
using FluentResults;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using WebApi.Models;
using WebApi.Utils;

namespace WebApi.Core.CodeSuggestion;

public class Agent
{
    private readonly PromptExecutionSettings _settings = new PromptExecutionSettings
    {
        ExtensionData = new Dictionary<string, object>
        {
            { "temperature", 0.3d }
        }
    };
    private readonly ChatHistory _chatHistory = new ChatHistory();
    private readonly Kernel _kernel;

    public Agent(IConfiguration configuration)
    {
        string apiKey = configuration["AZURE_OPENAI_KEY"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Environment variable `AZURE_OPENAI_KEY` not exists or value is null");
        }

        string endpoint = configuration["AZURE_OPENAI_ENDPOINT"];
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new InvalidOperationException("Environment variable `AZURE_OPENAI_ENDPOINT` not exists or value is null");
        }

        string deployment = configuration["AZURE_OPENAI_DEPLOYMENT"];
        if (string.IsNullOrWhiteSpace(deployment))
        {
            throw new InvalidOperationException("Environment variable `AZURE_OPENAI_DEPLOYMENT` not exists or value is null");
        }

        var builder = Kernel.CreateBuilder();
        _kernel = builder
            .AddAzureOpenAIChatCompletion(
                deploymentName: deployment,
                apiKey: apiKey,
                endpoint: endpoint,
                httpClient: new HttpClient { Timeout = TimeSpan.FromMinutes(30) })
            .Build();

        string resourceName = "WebApi.Core.CodeSuggestion.Prompts.SystemMessage.txt";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream);
        var prompt = reader.ReadToEnd();

        _chatHistory.AddSystemMessage(prompt);
    }

    public async Task<Result<PRCodeImprovement>> GetResponseAsync(GitChange change, CancellationToken cancellationToken)
    {
        var userPrompt = new StringBuilder();
        userPrompt.AppendLine($"## file: '{change.FilePath}'");
        userPrompt.AppendLine(change.ContentChanges);

        _chatHistory.AddUserMessage(userPrompt.ToString());
        var ai = _kernel.GetRequiredService<IChatCompletionService>();
        var response = await ai.GetChatMessageContentAsync(_chatHistory, _settings, _kernel, cancellationToken).ConfigureAwait(false);

        _chatHistory.RemoveAt(1);

        var message = response.ToString().ExtractCodeBlock("```json", "```");

        var result = new Result<PRCodeImprovement>();
        try
        {
            var improvement = JsonSerializer.Deserialize<PRCodeImprovement>(message);
            improvement.RelevantFile = change.FilePath;
            improvement.RepositoryId = change.RepositoryId;
            improvement.SourceCommitId = change.LastMergeSourceCommitId;
            improvement.TargetCommitId = change.LastMergeTargetCommitId;

            result.WithValue(improvement).WithSuccess("");
        }
        catch (Exception ex)
        {
            result.WithError(ex.Message);
        }

        return result;
    }
}
