using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("EncyclopediaBot.Logic.Tests")]
namespace EncyclopediaBot.Logic.Opplysningen1881
{
    internal class NumberDeserializer
    {
        private readonly string _url;
        private readonly ILogger _logger;
        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings()
        {
            ContractResolver = new UnderscorePropertyNamesContractResolver()
        };

        public Guid? RequestId { get; set; }

        public NumberDeserializer(string url, Guid? requestId, ILogger logger)
        {
            _url = url;
            _logger = logger;
            RequestId = requestId;
        }

        public NumberLookupResult Deserialize(string text)
        {
            try
            {
                return JsonConvert.DeserializeObject<NumberLookupResult>(text, _settings);
            }
            catch (JsonException e)
            {
                _logger.Error($"Failed parsing phone number lookup results from {_url}", RequestId, e);
            }

            const int count = 0;
            return new NumberLookupResult
            {
                Contacts = new List<Contact>(count),
                Count = count
            };
        }
    }
}
