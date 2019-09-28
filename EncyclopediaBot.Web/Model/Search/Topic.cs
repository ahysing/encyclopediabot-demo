using System.Collections.Generic;

namespace EncyclopediaBot.Web.Model.Search
{
    public class Topic
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public List<Topic> SubTopics { get; set; }
    }
}
