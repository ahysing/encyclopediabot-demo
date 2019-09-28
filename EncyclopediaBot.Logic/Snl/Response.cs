using System.Text.RegularExpressions;

namespace EncyclopediaBot.Snl
{
    public class Response
    {
        public static Response NotFound = new Response(null);
        public static Response Failed = new Response(null);
        
        public string Text {get; set; }
        public Response(string text)
        {
            Text = text;
        }
    }
}
