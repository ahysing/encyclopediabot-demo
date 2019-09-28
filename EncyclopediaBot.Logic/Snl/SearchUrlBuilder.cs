using System.Text;

namespace EncyclopediaBot.Logic.Snl
{
    internal class SearchUrlBuilder
    {
        internal string UrlTemplate {get; set; }

        internal SearchUrlBuilder(string urlTemplate)
        {
            UrlTemplate = urlTemplate;
        }
        
        public string Build(SearchRequest searchRequest)
        {
            StringBuilder stringBuilder = new StringBuilder(UrlTemplate);
            stringBuilder.Append('?');
            stringBuilder.Append("query");
            stringBuilder.Append('=');
            stringBuilder.Append(URLEncode(searchRequest.Query));
            
            if (searchRequest.Offset.HasValue)
            {
                stringBuilder.Append('&');
                stringBuilder.Append("offset");
                stringBuilder.Append('=');
                stringBuilder.Append(searchRequest.Offset.Value);
            }

            if (searchRequest.Limit.HasValue)
            {
                stringBuilder.Append('&');
                stringBuilder.Append("limit");
                stringBuilder.Append('=');
                stringBuilder.Append(searchRequest.Limit.Value);
            }

            return stringBuilder.ToString();
        }

        private string URLEncode(string query)
        {
            query = query ?? string.Empty;
            return System.Uri.EscapeDataString(query);
        }
    }
}