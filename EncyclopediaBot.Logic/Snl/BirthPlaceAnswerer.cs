using System;

namespace EncyclopediaBot.Logic.Snl
{
    public class BirthPlaceAnswerer
    {
        private readonly SnlProvider _provider;
        private readonly ILogger _logger;
        public Guid? RequestId { get; set; }

        public BirthPlaceAnswerer(ILogger logger)
        {
            _logger = logger;
            _provider = new SnlProvider(_logger);
        }
        
        public BirthPlaceAnswer GetBirthPlace(string personName)
        {
            personName = personName.TrimSpaces();
            string personNameLowerCase = personName.ToLower();
            SearchRequest searchRequest = new SearchRequest() 
            {
                Query = personName,
                Limit = 1
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
                    var lookupResult = _provider.GetArticleAsync(articleRequest);
                    var articleAnalyser = new ArticleAnalyser(_logger);
                    lookupResult.Wait();
                    var article = lookupResult.Result;
                    if (article != null && articleAnalyser.IsBiography(article))
                    {
                        var urlAnalysed = articleAnalyser.ParseUrl(article.Url);
                        var source = urlAnalysed.Item1;
                        return new BirthPlaceAnswer
                        {
                            BirthPlace = article.Metadata.Birthplace,
                            Source = source
                        };
                    }
                }
            }

            return BirthPlaceAnswer.Empty;
        }
    }
}