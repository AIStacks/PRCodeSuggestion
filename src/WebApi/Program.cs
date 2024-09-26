using WebApi.Core;
using WebApi.Core.CodeSuggestion;
using WebApi.Hubs;
using WebApi.Repositories;
using Serilog;

namespace WebApi;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddEnvironmentVariables();
        builder.Configuration.AddJsonFile("privatesettings.json", true, false);

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSignalR(options =>
        {
            options.KeepAliveInterval = TimeSpan.FromSeconds(60 * 30);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(60 * 30);
        });

        builder.Services.AddSingleton<LiteDbContext>();
        builder.Services.AddScoped<GitProvider>();
        builder.Services.AddScoped<FileValidator>();
        builder.Services.AddScoped<Cache>();
        builder.Services.AddScoped<Agent>();
        builder.Services.AddScoped<DiffProvider>();
        builder.Services.AddScoped<CodeSuggestionWorkFlow>();

        builder.Services.AddSerilog(configuration =>
        {
            configuration
                .WriteTo.Console()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext();
        });

        var app = builder.Build();

        app.UseRouting();

        app.MapHub<CodeSuggestionHub>("/CodeSuggestionHub");

        app.UseStaticFiles();

        app.MapFallbackToFile("/index.html");

        app.Run();
    }
}
