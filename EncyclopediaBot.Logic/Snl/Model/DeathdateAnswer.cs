using System;

namespace EncyclopediaBot.Logic.Snl
{
    public enum DeathdateState
    {

        Unknown,
        Found,
        StillAlive,
        NotRelevant
    }

    public class DeathdateAnswer
    {
        public DeathdateState State { get; set; }
        public DateTime? Deathdate { get; set; }
    }
}