using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace EncyclopediaBot.Logic.Opplysningen1881
{
    public class NumberProvider : INumberProvider
    {        
        private readonly string _APIKey;
        private ILogger _logger { get; set; }
        private readonly UrlBuilder _builder;
        private readonly NumberDeserializerFactory _numberDeserializerFactory;

        public int MaxRetries { get; set; }

        public Guid? RequestId { get; set; }

        public NumberProvider()
        { }

        public NumberProvider(string APIKey, ILogger logger) : this(APIKey, new NumberDeserializerFactory(), logger)
        {
        }

        internal NumberProvider(string APIKey, NumberDeserializerFactory numberDeserializerFactory, ILogger logger)
        {
            _APIKey = APIKey;
            _logger = logger;
            MaxRetries = 3;
            _builder = new UrlBuilder();
            _numberDeserializerFactory = numberDeserializerFactory;
        }

        public virtual async Task<NumberLookupResult> LookupAsync(string phoneNumber)
        {
            if (phoneNumber.Any(x => char.IsDigit(x) == false))
            {
                phoneNumber = phoneNumber.Replace("+47", string.Empty);
                char[] numberArray = phoneNumber.ToCharArray().Where(ch => char.IsDigit(ch)).ToArray();
                phoneNumber = new string(numberArray);
            }

            string url = _builder.Build(phoneNumber);
            if (_logger != null)
            {
                _logger.Debug("Requesting phone number " + phoneNumber, RequestId);
                _logger.Debug("Requesting url " + url, RequestId);
            }

            var responseTask = RequestWithRetiresAsync(url);
            return await Task.Run(() =>
            {
                responseTask.Wait();
                var response = responseTask.Result;
                const int count = 0;
                var numberLookupResult = new NumberLookupResult
                {
                    Contacts = new List<Contact>(count),
                    Count = count,
                };

                if (response != Response.NotFound && response != Response.Failed && response != Response.Unauthorized)
                {
                    string errorText;
                    if (response == Response.Unauthorized) {
                        errorText = "Du har ikke tilgang";
                    } else if (response == Response.NotFound) {
                        errorText = "Ressurs finnes ikke";
                    } else {
                        errorText = null;
                    }

                    var numberDeserializer = _numberDeserializerFactory.Create(url, RequestId, _logger);
                    numberLookupResult = numberDeserializer.Deserialize(response.Text);
                    numberLookupResult.Error = errorText;
                    numberLookupResult.IsError = response == Response.Unauthorized;
                }

                return numberLookupResult;
            });
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
                    httpWebRequest.Headers["Ocp-Apim-Subscription-Key"] = _APIKey;

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
                            case HttpStatusCode.Unauthorized:
                                return Response.Unauthorized;
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
