using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;


namespace EncyclopediaBot.Logic.Snl
{
    public class SnlProvider : IArticleProvider
    {     
        private ILogger _logger { get; set; }
        private readonly SearchUrlBuilder _searchUrlBuilder;
        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings()
        {
            ContractResolver = new UnderscorePropertyNamesContractResolver()
        };

        public int MaxRetries { get; set; }
        public Guid? RequestId { get; set; }


        public SnlProvider(ILogger logger)
        {
            _logger = logger;
            MaxRetries = 3;
            _searchUrlBuilder = new SearchUrlBuilder("https://snl.no/api/v1/search");
        }

        public async Task<Article> GetArticleAsync(ArticleRequest articleRequest)
        {
            string url = articleRequest.ArticleUrlJson;
            _logger.Debug("Requesting " + url, RequestId);
            var responseTask = RequestWithRetiresAsync(url);
            return await Task.Run(() =>
            {
                responseTask.Wait();
                var response = responseTask.Result;
                if (response != Response.NotFound && response != Response.Failed)
                {
                    return JsonConvert.DeserializeObject<Article>(response.Text, _settings);
                }
                else
                {
                    return null;
                }
            });
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
                return JsonConvert.DeserializeObject<Article>(response.Text, _settings);
            }
            else {
                return null;
            }
        }
        
        public Task<SearchResult> SearchAsync(SearchRequest searchRequest)
        {
            return Task.Factory.StartNew((object para) =>
            {
                var sr = (SearchRequest)para;
                return Search(sr);
            }, searchRequest);
        }

        public SearchResult Search(SearchRequest searchRequest)
        {
            string url = _searchUrlBuilder.Build(searchRequest);
            _logger.Debug("Requesting " + url, RequestId);
            var responseAsync = RequestWithRetiresAsync(url);
            responseAsync.Wait();
            var response = responseAsync.Result;
            if (response != Response.NotFound && response != Response.Failed)
            {
                try
                {
                    var docs = JsonConvert.DeserializeObject<List<Doc>>(response.Text, _settings);
                    return new SearchResult
                    {
                        Results = docs
                    };
                } catch (JsonException e)
                {
                    _logger.Error($"Failed parsing search results from {url}", RequestId, exception: e);
                }
            }

            return new SearchResult()
            {
                Results = new List<Doc>(0)
            };
        }

        private async Task<Response> RequestWithRetiresAsync(string requestUrl)
        {
            Exception lastException = null;
            int tries = 0;
            do {
                try {
                    tries ++;

                    HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(requestUrl);
                    httpWebRequest.Method = "GET";
                    httpWebRequest.Accept = "application/json";

                    var webResponse = await httpWebRequest.GetResponseAsync();
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

                    lastException = e;
                }
            } while (tries < MaxRetries);

            if (tries >= MaxRetries)
            {
                _logger.Error("Failed with error.", RequestId, exception:lastException);
            }

            return Response.Failed;
        }
    }
}