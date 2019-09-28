using System;

namespace EncyclopediaBot.Web.Model.Search
{
    public class QueryVote
    {
        public Vote Vote { get; set; }
        public ArticleId ArticleId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
