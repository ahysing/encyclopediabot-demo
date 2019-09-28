namespace EncyclopediaBot.Web.Model
{
    public class Answer
    {
        public string Id { get; set; }
        public string Response { get; set; }
        public string Source { get; set; }
        public uint TopicId { get; set; }
    }
}