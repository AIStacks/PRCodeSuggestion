using LiteDB;

namespace WebApi.Repositories;

public class LiteDbContext : IDisposable
{
    private readonly LiteDatabase _database;

    // Constructor that initializes the LiteDatabase instance
    public LiteDbContext()
    {
        _database = new LiteDatabase($"Filename={Path.Combine(Directory.GetCurrentDirectory(), "results.db")};Connection=shared");
    }

    // Property to expose the LiteDatabase instance
    public LiteDatabase Database => _database;

    // Dispose pattern to ensure that the database is disposed of properly
    public void Dispose()
    {
        _database?.Dispose();
    }
}
