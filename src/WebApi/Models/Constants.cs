namespace WebApi.Models
{
    public class Constants
    {
        public static readonly IEnumerable<string> CSharpFiles = new List<string>
        {
            ".cs", ".cshtml", ".razor",
        };

        public static readonly IEnumerable<string> TypescriptFiles = new List<string>
        {
            ".tsx", ".ts", "scss"
        };

        public static readonly IEnumerable<string> SqlscriptFiles = new List<string>
        {
            ".sql",
        };
    }
}
