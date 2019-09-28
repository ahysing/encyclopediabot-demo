using System.Collections.Generic;

namespace EncyclopediaBot.Logic.Opplysningen1881
{
    public class Geography
    {
        public string Municipality { get; set; }
        public string County { get; set; }
        public string Region { get; set; }
        public Coordinate Coordinate { get; set; }
        public Address Address { get; set; }
        public List<ContactPoint> ContactPoints { get; set; }
    }
}
