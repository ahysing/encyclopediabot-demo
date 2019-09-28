using System.Collections.Generic;

namespace EncyclopediaBot.Logic.Snl
{
    internal class SortByRank : Comparer<Doc>
    {
        public override int Compare(Doc x, Doc y)
        {
            double yRankAsdouble;
            double xRankAsdouble;
            if (double.TryParse(x.Rank, out xRankAsdouble)
             && double.TryParse(y.Rank, out yRankAsdouble))
            {
                return xRankAsdouble.CompareTo(yRankAsdouble);
            }

            return -1;
        }
    }
}