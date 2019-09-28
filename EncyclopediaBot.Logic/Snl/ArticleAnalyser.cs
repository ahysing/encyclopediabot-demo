using System;
using System.Globalization;
using System.Linq;

namespace EncyclopediaBot.Logic.Snl
{
    public class ArticleAnalyser
    {
        private readonly ILogger _logger;
        private static readonly string[] _dateFormats = { "yyyy.M.d", "dd.M.yyyy", "yyyy'.0.0'", "yyyy'.0'.d" };

        public ArticleAnalyser(ILogger logger)
        {
            _logger = logger;
        }
        
        public Gender GetGender(Article article)
        {
            switch (article.Metadata.Gender)
            {
                case "m":
                    return Gender.Male;
                case "k":
                    return Gender.Female;
                default:
                    return Gender.None;
            }
        }

        public DeathdateAnswer GetDeathDate(Article article)
        {
            DateTime deathDate;
            DateTime birthDate;
            if (string.IsNullOrEmpty(article.Metadata.BirthDate))
            {
                return new DeathdateAnswer()
                {
                    State = DeathdateState.NotRelevant
                };
            } else if (DateTime.TryParseExact(article.Metadata.DeathDate, _dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out deathDate))
            {
                return new DeathdateAnswer()
                {
                    State = DeathdateState.Found,
                    Deathdate = deathDate
                };
            } else if (DateTime.TryParseExact(article.Metadata.BirthDate, _dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out birthDate))
            {
                return new DeathdateAnswer
                {
                    State = DeathdateState.StillAlive,
                };
            }
            
            return new DeathdateAnswer()
            {
                State = DeathdateState.Unknown
            };
        }

        public bool IsBiography(Article article)
        {
            return "biography" == article.Metadata.ArticleType // most people
                || "nbl_biography" == article.Metadata.ArticleType // all people from Store Biografiske Leksikon
                || "Norsk kunstnerleksikon" == article.SubjectTitle // Alle people from Norsk Kunstnerleksikon
                || null == article.Metadata.ArticleType; // Special Case for Adolf Hitler
        }

        public BirthdateAnswer GetBirthDate(Article article)
        {
            if (string.IsNullOrEmpty(article.Metadata.BirthDate))
            {
                return new BirthdateAnswer()
                {
                    State = BirthdateState.NotRelevant
                };
            }

            DateTime birthDate;
            if (DateTime.TryParseExact(article.Metadata.BirthDate, _dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out birthDate))
            {
                return new BirthdateAnswer
                {
                    State = BirthdateState.Found,
                    Birthdate = birthDate
                };
            }

            return new BirthdateAnswer
            {
                State = BirthdateState.Unknown
            };
        }

        internal uint CountWordInText(string text, params string[] words)
        {
            uint number = 0;
            int textLength = text.Length;
            int idx = int.MaxValue;
            foreach (var word in words)
            {
                int wordIdx = text.IndexOf(word, StringComparison.CurrentCultureIgnoreCase);
                if (wordIdx != -1 && wordIdx < idx)
                {
                    idx = wordIdx;
                }
            }

            while (idx != -1 && idx != int.MaxValue && idx < textLength)
            {
                number ++;

                int nextIdx = int.MaxValue;
                foreach (var word in words)
                {
                    int wordIdx = text.IndexOf(word, idx + 1, StringComparison.CurrentCultureIgnoreCase);
                    if (wordIdx != -1 && wordIdx < nextIdx)
                    {
                        nextIdx = wordIdx;
                    }
                }

                idx = nextIdx;
            }

            return number;
        }

        public bool SubjectStillExists(Article article, string bodyPlainText)
        {
            var dd = GetDeathDate(article);
            if (dd.State == DeathdateState.StillAlive)
            {
                return true;
            }
            else if (dd.State == DeathdateState.Found)
            {
                return false;
            }
            else
            {
                var numberOfEr = CountWordInText(bodyPlainText, " er ", " er,");
                var numberOfVar = CountWordInText(bodyPlainText, " var ", " var,");
                if (_logger != null)
                {
                    _logger.Debug(string.Format("Detecting if article subject still exists. Found {0} er and {1} var", numberOfEr, numberOfVar));
                }

                return numberOfEr >= numberOfVar;
            }
        }

        public Tuple<string, string> ParseUrl(string url)
        {
            string[] parts = url.Split("snl.no/");
            string articleId = parts.Last();
            string source = parts.First()
                .Replace("https", string.Empty)
                .Replace("http", string.Empty)
                .Replace("://", string.Empty)
                .TrimEnd('.');
            if (string.IsNullOrEmpty(source))
            {
                source = "snl";
            }

            return Tuple.Create<string, string>(source, articleId);
        }

    }
}