using LiteDB;
using WebApi.Models;
using WebApi.Repositories;

namespace WebApi.Core.CodeSuggestion
{
    public class Cache
    {
        private readonly ILiteCollection<PRCodeImprovement> _collection;

        public Cache(LiteDbContext dbContext)
        {
            _collection = dbContext.Database.GetCollection<PRCodeImprovement>(nameof(PRCodeImprovement));
            _collection.EnsureIndex("PRCodeImprovement_Key", x => new { x.RepositoryId, x.RelevantFile, x.SourceCommitId, x.TargetCommitId }, true);
        }

        public PRCodeImprovement Get(GitChange change)
        {
            var result = _collection.FindOne(x => x.RepositoryId == change.RepositoryId && x.RelevantFile == change.FilePath && x.SourceCommitId == change.LastMergeSourceCommitId && x.TargetCommitId == change.LastMergeTargetCommitId);

            return result;
        }

        public void Save(PRCodeImprovement record)
        {
            _collection.Insert(record);
        }
    }
}
