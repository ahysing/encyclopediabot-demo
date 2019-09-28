using System;

namespace EncyclopediaBot.Logic.Snl
{
    public class SearchRequest
    {
        public string Query { get; set; }
        public uint? Limit { get; set; }
        public uint? Offset { get; set; }
    }
}