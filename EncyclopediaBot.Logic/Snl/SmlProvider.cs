using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;


namespace EncyclopediaBot.Logic.Snl
{
    public class SmlProvider : IArticleProvider
    {
        public int MaxRetries { get; set; }
        private ILogger _logger { get; set; }
        public Guid? RequestId { get; set; }


        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            ContractResolver = new UnderscorePropertyNamesContractResolver()
        };

        private readonly SearchUrlBuilder _searchUrlBuilder;

        public SmlProvider(ILogger logger)
        {
            _logger = logger;
            MaxRetries = 3;
            _searchUrlBuilder = new SearchUrlBuilder("https://sml.snl.no/api/v1/search");
        }

        public async Task<Article> GetArticleAsync(ArticleRequest articleRequest)
        {
            string url = articleRequest.ArticleUrlJson;
            _logger.Debug("Requesting " + url, RequestId);
            var responseTask = RequestWithRetiresAsync(url);
            var response = await responseTask;
            if (response != Response.NotFound && response != Response.Failed)
            {
                try {
                    return JsonConvert.DeserializeObject<Article>(response.Text, _settings);
                }
                catch (JsonException e)
                {
                    _logger.Error($"Failed parsing search results from {url}", RequestId, e);
                }
            }

            return null;
        }
        
        public Article GetArticle(ArticleRequest articleRequest)
        {
            string url = articleRequest.ArticleUrlJson;
            _logger.Debug("Requesting " + url, RequestId);
            var responseTask = RequestWithRetiresAsync(url);
            responseTask.Wait();
            var response = responseTask.Result;
            if (response != Response.NotFound && response != Response.Failed)
            {
                try
                {
                    return JsonConvert.DeserializeObject<Article>(response.Text, _settings);
                }
                catch (JsonException e)
                {
                    _logger.Error($"Failed parsing search results from {url}", RequestId, e);
                }

                return new Article();
            }
            return null;
        }
        
        public async Task<SearchResult> SearchAsync(SearchRequest searchRequest)
        {
            string url = _searchUrlBuilder.Build(searchRequest);
            _logger.Debug("Requesting " + url, RequestId);

            var responseTask = RequestWithRetiresAsync(url);

            var response = await responseTask;
            if (response != Response.NotFound && response != Response.Failed)
            {
                try
                {
                    var docs = JsonConvert.DeserializeObject<List<Doc>>(response.Text, _settings);
                    return new SearchResult
                    {
                        Results = docs
                    };
                }
                catch (JsonException e)
                {
                    _logger.Error($"Failed parsing search results from {url}", RequestId, e);
                }

                return new SearchResult()
                {
                    Results = new List<Doc>(0)
                };
            }
            return new SearchResult
            {
                Results = new List<Doc>(0)
            };
        }

        public SearchResult Search(SearchRequest searchRequest)
        {
            string url = _searchUrlBuilder.Build(searchRequest);
            _logger.Debug("Requesting " + url, RequestId);
            var responseTask = RequestWithRetiresAsync(url);
            responseTask.Wait();
            var response = responseTask.Result;
           

            if (response != Response.NotFound && response != Response.Failed)
            {
                try {
                    var docs = JsonConvert.DeserializeObject<List<Doc>>(response.Text, _settings);
                    return new SearchResult()
                    {
                        Results = docs
                    };
                }
                catch (JsonException e)
                {
                    _logger.Error($"Failed parsing search results from {url}", RequestId, e);
                }

                return new SearchResult()
                {
                    Results = new List<Doc>(0)
                };
            }
            else
            {
                return new SearchResult()
                {
                    Results = new List<Doc>(0)
                };
            }
        }

        private async Task<Response> RequestWithRetiresAsync(string requestUrl)
        {
            int tries = 0;
            do {
                try {
                    tries ++;

                    HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(requestUrl);
                    httpWebRequest.Method = "GET";
                    httpWebRequest.Accept = "application/json";

                    WebResponse webResponse = await httpWebRequest.GetResponseAsync();
                    var httpWebResponse = (HttpWebResponse)webResponse;
                    using (var sr = new StreamReader(httpWebResponse.GetResponseStream()))
                    {
                        return new Response(sr.ReadToEnd());
                    }
                }
                catch (WebException e)
                {
                    var response = e.Response as HttpWebResponse;
                    if (response != null)
                    {
                        switch (response.StatusCode)
                        {
                            case HttpStatusCode.NotFound:
                                return Response.NotFound;
                            default:
                                Task.Delay(500).Wait();
                                break;
                        }
                    }
                }
            } while (tries < MaxRetries);


            return Response.Failed;
        }
    }
}