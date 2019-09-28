using Newtonsoft.Json.Serialization;

namespace EncyclopediaBot.Logic
{
    internal class UnderscorePropertyNamesContractResolver : DefaultContractResolver
    {
        public UnderscorePropertyNamesContractResolver()
        {
            NamingStrategy = new SnakeCaseNamingStrategy();
        }
    }
}