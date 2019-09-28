namespace EncyclopediaBot.Logic
{
    internal class Response
    {
        public static Response NotFound = new Response();
        public static Response Failed = new Response();
        public static Response Unauthorized = new Response();
        public string ErrorMessage { get; set; }
        public string Text { get; set; }

        public Response()
        { }

        public Response(string text)
        {
            Text = text;
        }
    }
}
