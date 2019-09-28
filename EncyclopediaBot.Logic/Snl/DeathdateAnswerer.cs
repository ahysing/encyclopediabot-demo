using System;

namespace EncyclopediaBot.Logic.Snl
{
    public class DeathdateAnswerer
    {
        private readonly SnlProvider _provider;
        private readonly ArticleAnalyser _articleAnalyser;
        private readonly ILogger _logger;
        public Guid? RequestId { get; set; }

        public DeathdateAnswerer(ILogger logger)
        {
            _logger = logger;
            _provider = new SnlProvider(_logger);
            _articleAnalyser = new ArticleAnalyser(_logger);
        }
        
        public DeathdateAnswer GetDeathdate(string personName)
        {
            personName = personName.TrimSpaces();
            string personNameLowerCase = personName.ToLower();
            SearchRequest searchRequest = new SearchRequest() 
            {
                Query = personName,
                Limit = 3
            };
            Article bestArticle = null;
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
                    lookupResult.Wait();
                    var article = lookupResult.Result;
                    if (article != null)
                    {
                        if (_articleAnalyser.IsBiography(article))
                        {
                            return _articleAnalyser.GetDeathDate(article);
                        }
                        bestArticle = bestArticle ?? article;
                    }
                } else if (doc.Snippet.IndexOf(personName, StringComparison.CurrentCultureIgnoreCase) != -1
                        || doc.FirstTwoSentences.IndexOf(personName, StringComparison.CurrentCultureIgnoreCase) != -1)
                {
                    // picks up nicknames
                    var lookupResult = _provider.GetArticleAsync(articleRequest);
                    lookupResult.Wait();
                    var article = lookupResult.Result;
                    bool isBiography = article != null && _articleAnalyser.IsBiography(article);
                    if (article != null
                        && !string.IsNullOrEmpty(article.Metadata.AlternativeForm)
                        && isBiography)
                    {
                        var alternativeNames = new XhtmlMarkupTrimmer().Trim(article.Metadata.AlternativeForm).Text.Split(',');
                        foreach (var alternativeName in alternativeNames)
                        {
                            if (LevensteinDistance.Calculate(alternativeName, personName) <= 2)
                            {
                                return _articleAnalyser.GetDeathDate(article);   
                            }
                        }
                    }
                }
            }

            if (bestArticle != null)
            {
                return new DeathdateAnswer()
                {
                    State = DeathdateState.NotRelevant
                };
            }

            return new DeathdateAnswer()
            {
                State = DeathdateState.Unknown
            };
        }
    }
}