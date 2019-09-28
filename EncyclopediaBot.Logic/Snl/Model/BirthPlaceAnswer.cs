namespace EncyclopediaBot.Logic.Snl
{
    public class BirthPlaceAnswer
    {
        public static readonly BirthPlaceAnswer Empty = new BirthPlaceAnswer();
        public string BirthPlace { get; set; }
        public string Source { get; set; }
    }
}