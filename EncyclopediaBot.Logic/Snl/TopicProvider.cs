using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace EncyclopediaBot.Logic.Snl
{
    public class TopicProvider
    {
        private readonly ILogger _logger;
        private readonly string _urlTemplate;
        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            ContractResolver = new UnderscorePropertyNamesContractResolver()
        };
        public int MaxRetries { get; }

        public TopicProvider(ILogger logger)
        {
            _logger = logger;

            MaxRetries = 3;

            _urlTemplate = "https://snl.no/.taxonomy/{0}.json";
        }

        public TaxonomyResult GetTaxonomy(uint id, Guid? requestId)
        {
            string url = CreateUrl(id);
            _logger.Debug("Requesting " + url, requestId);
            var responseTask = RequestWithRetiresAsync(url);
            responseTask.Wait();
            var response = responseTask.Result;
            if (response != Response.NotFound && response != Response.Failed)
            {
                var docs = JsonConvert.DeserializeObject<TaxonomyResult>(response.Text, _settings);
                return docs;
            }

            return new TaxonomyResult();
        }

        private string CreateUrl(uint id)
        {
            return string.Format(_urlTemplate, id);
        }

        private async Task<Response> RequestWithRetiresAsync(string requestUrl)
        {
            int tries = 0;
            do
            {
                try
                {
                    tries++;

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
                }
            } while (tries < MaxRetries);

            return Response.Failed;
        }
    }
}
