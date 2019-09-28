using System.Collections.Generic;

namespace EncyclopediaBot.Logic.Snl
{
    public class Taxonomy
    {
        public string title { get; set; }
        public SubTaxonomy PrimaryArticle { get; set; } 
        public List<SubTaxonomy> articles { get; set; }
        public List<SubTaxonomy> ancestors { get; set; }
    }
}
