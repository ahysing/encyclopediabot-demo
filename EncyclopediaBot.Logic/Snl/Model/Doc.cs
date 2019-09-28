namespace EncyclopediaBot.Logic.Snl
{
    public class Doc
    {
        public string ArticleId { get; set; }
        public string ArticleTypeId { get; set; }
        public string Clarification { get; set; }
        public uint? EncyclopediaId { get; set; }
        public string Headword { get; set; }
        public string Permalink { get; set; }
        public string Rank { get; set; }
        public string Snippet { get; set; }
        public string TaxonomyId { get; set; }
        public string TaxonomyTitle { get; set; }
        public string ArticleUrl { get; set; }
        public string ArticleUrlJson { get; set; }
        public string Title { get; set; }
        public string License { get; set; }
        public string FirstImageUrl { get; set; }
        public string FirstImageLicense { get; set; }
        public string FirstTwoSentences { get; set; }
    }
}