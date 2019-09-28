using System;

namespace EncyclopediaBot.Logic.Snl
{
    public class GenderAnswerer
    {
        private readonly SnlProvider _provider;
        private readonly ILogger _logger;

        public GenderAnswerer(ILogger logger)
        {
            _logger = logger;
            _provider = new SnlProvider(_logger);
        }

        public Guid RequestId { get; set; }

        public Gender GetGender(string personName)
        {
            personName = personName.TrimSpaces();
            string personNameLowerCase = personName.ToLower();
            SearchRequest searchRequest = new SearchRequest() 
            {
                Query = personName,
                Limit = 3
            };
            
            SearchResult searchResult = _provider.Search(searchRequest);
            foreach (Doc doc in searchResult.Results)
            {
                var articleRequest = new ArticleRequest()
                {
                    ArticleUrlJson = doc.ArticleUrlJson
                };

                int distance = LevensteinDistance.Calculate(doc.Headword.ToLower(), personNameLowerCase);
                if (distance <= 3 || doc.Headword.Length / (float)distance > 0.75)
                
                {
                    var articleResult = _provider.GetArticleAsync(articleRequest);
                    var articleAnalyser = new ArticleAnalyser(_logger);
                    articleResult.Wait();
                    var article = articleResult.Result;
                    if (article != null && articleAnalyser.IsBiography(article))
                    {
                        return articleAnalyser.GetGender(article);
                    }
                }
            }

            return default;
        }
    }
}