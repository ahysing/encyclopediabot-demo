using System.Collections.Generic;


namespace EncyclopediaBot.Web.Model.Search
{
    public class SearchState
    {
        public string Query { get; set; }
        public uint? Offset { get; set; }
        public uint? Limit { get; set; }

        public List<Answer> LastResult { get; set; }

        public HashSet<ArticleId> DiscardedArticles { get; set; }
        public ArticleId ArticleInFocus { get; set; }
    }
}
