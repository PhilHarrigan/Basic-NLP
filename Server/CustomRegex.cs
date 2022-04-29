using System.Text.RegularExpressions;

namespace NLP_API.Server
{
    public class CustomRegex
    {
        public static Regex headingWord = new Regex("(?<=h1 class=\"hword\">)[a-zA-Z]+(?=</h1>)");
        public static Regex classRGX = new Regex("<a class=\"important-blue-link\"");
        public static Regex type = new Regex("(?<=>)[a-z ()0-9,]+(?=</a>)");
        public static Regex replacePattern = new Regex("[()\\/\n\"\\\"\\\\“”]");
        public static Regex sentenceStart = new Regex("(?<=[a-z]+[.])\\s+(?=[\"A-Z])");
        public static Regex findSentence = new Regex("(?<![A-Z])[.]");
        public static Regex otherWords = new Regex("(?<=<span class=\"ure\">)[a-zA-Z]+(?=</span>)");
        public static Regex otherTypes = new Regex("(?<=<span class=\"fl\">)[a-z]+(?=</span>)");
    }
}
