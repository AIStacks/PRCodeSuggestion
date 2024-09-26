using System.Text.RegularExpressions;

namespace WebApi.Utils
{
    public static class StringUtils
    {
        public static string ExtractCodeBlock(this string message, string codeBlockPrefix, string codeBlockSuffix)
        {
            return message.ExtractCodeBlocks(codeBlockPrefix, codeBlockSuffix).FirstOrDefault() ?? string.Empty;
        }

        public static IEnumerable<string> ExtractCodeBlocks(this string message, string codeBlockPrefix, string codeBlockSuffix)
        {
            string text = message ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                yield break;
            }

            foreach (Match item in Regex.Matches(text, codeBlockPrefix + "([\\s\\S]*?)" + codeBlockSuffix))
            {
                yield return item.Groups[1].Value.Trim();
            }
        }
    }
}
