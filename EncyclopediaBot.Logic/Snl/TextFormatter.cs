using System;
using System.Text;

namespace EncyclopediaBot.Logic.Snl
{
    public static class TextFormatter
    {

        public static string TrimSpaces(this string input)
        {
            StringBuilder stringBuilder = new StringBuilder(input.Length);
            bool previousIsSpace = true;
            foreach (char c in input)
            {
                bool currentIsWhitespace = char.IsWhiteSpace(c);
                if (!(previousIsSpace && currentIsWhitespace))
                {
                    stringBuilder.Append(c);
                }

                previousIsSpace = currentIsWhitespace;
            }

            while (stringBuilder.Length > 0 && char.IsWhiteSpace(stringBuilder[stringBuilder.Length - 1]))
            {
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
            }

            while (stringBuilder.Length > 0 && char.IsWhiteSpace(stringBuilder[0]))
            {
                stringBuilder.Remove(0, 1);
            }

            return stringBuilder.ToString();
        }
    }
}
