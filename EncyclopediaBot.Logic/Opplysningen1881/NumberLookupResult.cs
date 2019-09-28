using System.Collections.Generic;

namespace EncyclopediaBot.Logic.Opplysningen1881
{
    public class NumberLookupResult
    {
        public int? Count { get; set; }
        public List<Contact> Contacts { get; set; }
        public string Error { get; set; }
        public bool IsError { get; set; }
    }
}
