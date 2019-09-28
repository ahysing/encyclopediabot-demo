using System.Text.RegularExpressions;

namespace EncyclopediaBot.Logic.Snl
{
    internal class XhtmlMarkupTrimmer
    {
        private static readonly string[] _replacements = {
            "<a>", "</a>", "<div>", "</div>", "<p>", "</p>", "<mark>", "</mark>"
        };

        private static readonly Regex _replace = new Regex(@"(<[^>]+>|&[a-z]+;)");

        internal TrimResults Trim(string text)
        {
            bool hasTrimmed = false;
            bool nowReplaced;
            
            foreach (var replacement in _replacements)
            {
                do
                {
                    string newText = text.Replace(replacement, string.Empty);

                    nowReplaced = newText != text;
                    
                    text = newText;
                    hasTrimmed |= nowReplaced;                    
                } while (nowReplaced);
            }

            do
            {
                // trick to remove markup. It is not perfect for all HTML, but
                // it is good enough
                string newText = _replace.Replace(text, string.Empty);
                nowReplaced = newText != text;
                
                text = newText;
            } while (nowReplaced);

            return new TrimResults()
            {
                HasTrimmed = hasTrimmed,
                Text = text
            };
        }
    }
}