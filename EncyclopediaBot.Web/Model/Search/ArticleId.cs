using System;

namespace EncyclopediaBot.Web.Model.Search
{
    public class ArticleId
    {
        public string Source { get; set; }
        public string Id { get; set; }

        public override bool Equals(object obj)
        {
            return obj is ArticleId && Source == (obj as ArticleId).Source && Id == (obj as ArticleId).Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Source, Id);
        }
    }
}
