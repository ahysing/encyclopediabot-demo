using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace EncyclopediaBot.Logic.Snl
{
    public class NblProvider : IArticleProvider
    {
        private ILogger _logger { get; set; }
        private readonly SearchUrlBuilder _searchUrlBuilder;
        public int MaxRetries { get; set; }
        public Guid? RequestId { get; set; }

        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            ContractResolver = new UnderscorePropertyNamesContractResolver()
        };

        public NblProvider(ILogger logger)
        {
            _logger = logger;
            MaxRetries = 3;
            _searchUrlBuilder = new SearchUrlBuilder("https://nbl.snl.no/api/v1/search");
        }

        public Task<Article> GetArticleAsync(ArticleRequest articleRequest)
        {
            return Task.Factory.StartNew((object para) =>
            {
                var articleReq = (ArticleRequest)para;
                return GetArticle(articleReq);
            }, articleRequest);
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

            return null;
        }
        
        public async Task<SearchResult> SearchAsync(SearchRequest searchRequest)
        {
            string url = _searchUrlBuilder.Build(searchRequest);
            _logger.Debug("Requesting " + url, RequestId);
            var responseTask = RequestWithRetiresAsync(url);
            return await Task.Run(() =>
            {
                responseTask.Wait();
                var response = responseTask.Result;
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
                        _logger.Error($"Failed parsing search results from {url}", RequestId, e);
                    }
                }

                return new SearchResult
                {
                    Results = new List<Doc>(0)
                };
            });
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
                var docs = JsonConvert.DeserializeObject<List<Doc>>(response.Text, _settings);
                return new SearchResult
                {
                    Results = docs
                };
            }
            return new SearchResult
            {
                Results = new List<Doc>(0)
            };
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

                    var webResponse = await httpWebRequest.GetResponseAsync();
                    var httpWebResponse = (HttpWebResponse)webResponse;
                    using (var sr = new StreamReader(httpWebResponse.GetResponseStream()))
                    {
                        return new Response(sr.ReadToEnd());
                    }
                } catch (WebException e)
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