using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("EncyclopediaBot.Logic.Tests")]
namespace EncyclopediaBot.Logic
{
    internal class NorwegianStemmer
    {
        // Inspired, but not copied from snowball stemmer
        // https://github.com/snowballstem/snowball/blob/master/algorithms/norwegian.sbl
        private static readonly string[] _mainSuffix = {
             "a", "e", "ede", "ande", "ende", "ane", "ene", "hetene", "en", "heten", "ar",
            "er", "heter", "as", "es", "edes", "endes", "enes", "hetenes", "ens",
            "hetens", "ers", "ets", "et", "het", "ast"
        };

        private static readonly string[] _otherSuffix = {

            "leg", "eleg", "ig", "eig", "lig", "elig", "els", "lov", "elov", "slov", "hetslov"
        };

        private static readonly string[] _consonantPair = {
            "dt", "vt"
        };

        private static readonly char _k = 'k';
        private static readonly char _s = 's';
        private static readonly char[] _sEnding = {
            'b','c', 'd', 'f','g','h','j', 'l','m','n','o','p','r','t','v','y','z'
        };

        private static readonly char[] _vocals =
        {
            'a', 'e', 'i', 'o', 'u', 'y', 'æ', 'ø', 'å'
        };

        private static readonly string[] _erteOrErt = {
            "erte", "ert"
        };

        private bool IsVocal(char letter)
        {
            return Array.IndexOf(_vocals, letter) != -1;
        }

        internal string CreateSuffix(char[] tokenAsCharacters, uint start, uint end)
        {
            if (start < end) {
                uint suffixLength = end - start + 1;

                char[] suffix = new char[suffixLength];
                for (uint i = 0; i < suffixLength; i++)
                {
                    uint suffixIdx = i + start;
                    suffix[i] = tokenAsCharacters[suffixIdx];
                }

                return new string(suffix);
            }

            return tokenAsCharacters.ToString();
        }

        private void Delete(uint deleteAt, uint length, ref bool[] copySet)
        {
            uint loopEnd = deleteAt + length;
            for (uint i = deleteAt; i < loopEnd; i++)
            {
                copySet[i] = false;
            }
            // will be supported in .net 2.1
            // Array.Fill<bool>(copySet, false, deleteAt, loopEnd);
        }

        private bool RemoveSEnding(string tomark, ref uint rightLimit, ref bool[] copySet)
        {
            char secondLastLetter = tomark[tomark.Length - 2];
            char lastLetter = tomark[tomark.Length - 1];
            if (secondLastLetter == _s && _sEnding.Contains(lastLetter))
            {
                Delete(rightLimit - 2, 2, ref copySet);
                rightLimit -= 2;
                return true;
            }

            return false;
        }

        private bool RemoveSKVocalEnding(string tomark, ref uint rightLimit, ref bool[] copySet)
        {
            char thirdLastLetter = tomark[tomark.Length - 3];
            char secondLastLetter = tomark[tomark.Length - 2];
            char lastLetter = tomark[tomark.Length - 1];
            if (thirdLastLetter == _s && secondLastLetter == _k && IsVocal(lastLetter) == false)
            {
                Delete(rightLimit - 3, 3, ref copySet);
                rightLimit -= 3;
                return true;
            }

            return false;
        }

        internal void MainSuffix(char[] tokenAsCharacters, ref uint p1, ref uint rightLimit, ref bool[] copySet)
        {
            string tomark = CreateSuffix(tokenAsCharacters, p1, rightLimit);
            var mainSuffixes = SortByLength(_mainSuffix);
            foreach (var mainSuffix in mainSuffixes)
            {
                if (tomark.EndsWith(mainSuffix, StringComparison.CurrentCultureIgnoreCase))
                {
                    uint deleteLength = (uint)mainSuffix.Length;
                    uint deleteAt = rightLimit - deleteLength + 1;
                    Delete(deleteAt, deleteLength, ref copySet);
                    rightLimit -= (uint)mainSuffix.Length;
                    return;
                }
            }

            // '' <- 's{vocal}'
            // AND
            // '' <- 'sk{vocal}'
            if (tomark.Length >= 2)
            {
                if (RemoveSEnding(tomark, ref rightLimit, ref copySet))
                {
                    return;
                }
            } else if (tomark.Length >= 3)
            {
                if (RemoveSKVocalEnding(tomark, ref rightLimit, ref copySet) == false)
                {
                    if (RemoveSEnding(tomark, ref rightLimit, ref copySet))
                    {
                        return;
                    }
                } else
                {
                    return;
                }
            }

            // 'er' <- 'erte' or 'ert'
            foreach (var erteOrErt in SortByLength(_erteOrErt))
            {
                if (tomark.EndsWith(erteOrErt, StringComparison.CurrentCultureIgnoreCase))
                {
                    uint deleteLength = (uint)erteOrErt.Length - 2;
                    uint deleteAt = rightLimit - deleteLength;
                    Delete(deleteAt, rightLimit, ref copySet);
                    rightLimit -= deleteLength;
                    return;
                }
            }
        }

        private IEnumerable<string> SortByLength(string[] text)
        {
            return text.OrderByDescending(x => x.Length); //.ThenByDescending(x => Convert.ToInt32(x));
        }

        // '' <- 'dt'
        // '' <- 'vt'
        internal void ConsonantPair(char[] tokenAsCharacters, ref uint p1, ref uint rightLimit, ref bool[] copySet)
        {
            string tomark = CreateSuffix(tokenAsCharacters, p1, rightLimit);
            var consonantPairs = SortByLength(_consonantPair);
            foreach (var consonantPair in consonantPairs)
            {
                if (tomark.EndsWith(consonantPair, StringComparison.CurrentCultureIgnoreCase))
                {
                    Delete(rightLimit - 2, rightLimit, ref copySet);
                    p1 -= 2;
                    break;
                }
            }
        }

        internal void OtherSuffix(char[] tokenAsCharacters, ref uint p1, ref uint rightLimit, ref bool[] copySet)
        {
            string tomark = CreateSuffix(tokenAsCharacters, p1, rightLimit);
            var otherSuffixes = SortByLength(_otherSuffix);
            foreach (var otherSuffix in otherSuffixes)
            {
                if (tomark.EndsWith(otherSuffix, StringComparison.CurrentCultureIgnoreCase))
                {
                    uint suffixLength = (uint)otherSuffix.Length;
                    uint deleteAt = rightLimit - suffixLength + 1;
                    Delete(deleteAt, suffixLength, ref copySet);
                    rightLimit -= suffixLength;
                    break;
                }
            }
        }


        private string BuildTokenWithoutStem(char[] tokenAsCharacters, bool[] copySet)
        {
            int i;
            for (i = tokenAsCharacters.Length - 1; i >= 0; i--)
            {
                bool preserveLetter = copySet[i];
                if (preserveLetter)
                {
                    break;
                }
            }

            if (i > 0)
            {
                int copyLength = i + 1;
                char[] tokenTrimmedAsCharacters = new char[copyLength];
                Array.Copy(tokenAsCharacters, tokenTrimmedAsCharacters, copyLength);
                return new string(tokenTrimmedAsCharacters);
            }

            return string.Empty;

        }

        public string StemToken(string token)
        {
            var tokenAsCharacters = token.ToCharArray();
            bool[] copySet = Enumerable.Repeat(true, tokenAsCharacters.Length).ToArray();
            uint p1;
            uint x = 3;
            uint leftLimit;
            uint rightLimit = (uint)tokenAsCharacters.Length - 1;
            // Mark regions
            for (leftLimit = 0; leftLimit < tokenAsCharacters.Length
                            && IsVocal(tokenAsCharacters[leftLimit]);
                            leftLimit++)
            { }

            for (leftLimit = 0; leftLimit < tokenAsCharacters.Length
                            && IsVocal(tokenAsCharacters[leftLimit]);
                            leftLimit++)
            { }

            p1 = (uint)leftLimit;
            if (p1 < x)
            {
                p1 = x;
            }

            uint lastRightLimit;
            do
            {
                lastRightLimit = rightLimit;
                MainSuffix(tokenAsCharacters, ref p1, ref rightLimit, ref copySet);
                ConsonantPair(tokenAsCharacters, ref p1, ref rightLimit, ref copySet);
                OtherSuffix(tokenAsCharacters, ref p1, ref rightLimit, ref copySet);
            } while (lastRightLimit != rightLimit && rightLimit > p1);

            return BuildTokenWithoutStem(tokenAsCharacters, copySet);
        }
    }
}
