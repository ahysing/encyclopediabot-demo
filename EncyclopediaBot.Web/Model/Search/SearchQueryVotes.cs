using System.Collections.Generic;

namespace EncyclopediaBot.Web.Model.Search
{
    public class SearchQueryVotes
    {
        public string QueryFormatted { get; set; }
        public List<QueryVote> UpVotes { get; set; }
        public List<QueryVote> DownVotes { get; set; }

        public SearchQueryVotes(string queryFormatted)
        {
            UpVotes = new List<QueryVote>();
            DownVotes = new List<QueryVote>();
            QueryFormatted = queryFormatted;
        }

        public SearchQueryVotes()
        {
            UpVotes = new List<QueryVote>();
            DownVotes = new List<QueryVote>();
        }
    }
}
