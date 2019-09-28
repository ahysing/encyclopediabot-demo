namespace EncyclopediaBot.Logic.Opplysningen1881
{
    internal class UrlBuilder
    {
        private readonly string _endpoint;

        public UrlBuilder(string endpoint)
        {
            _endpoint = endpoint;
        }

        public UrlBuilder() : this("https://services.api1881.no/lookup/phonenumber/")
        {
        }

        public string Build(string phoneNumber)
        {
            string phoneNumberEncoded = URLEncode(phoneNumber);
            return $"{_endpoint}{phoneNumberEncoded}";
        }

        private string URLEncode(string query)
        {
            query = query ?? string.Empty;
            return System.Uri.EscapeDataString(query);
        }
    }
}