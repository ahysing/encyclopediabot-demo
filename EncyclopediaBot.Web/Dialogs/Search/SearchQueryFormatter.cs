namespace EncyclopediaBot.Web.Dialogs.Search
{
    public class SearchQueryFormatter
    {
        private static readonly char[] _normalSymbols = { '.', ':', ';', '?', '!', ',' };
        public string FormatQuery(string query)
        {
            string searchQueryFormatted = query;
            int idx;
            while ((idx = searchQueryFormatted.IndexOfAny(_normalSymbols)) != -1)
            {
                searchQueryFormatted = searchQueryFormatted.Remove(idx);
            }

            searchQueryFormatted = searchQueryFormatted.Replace('\t', ' ');
            searchQueryFormatted = searchQueryFormatted.Replace('\n', ' ');
            searchQueryFormatted = searchQueryFormatted.ToLowerInvariant().Trim();
            return searchQueryFormatted;
        }
    }
}
