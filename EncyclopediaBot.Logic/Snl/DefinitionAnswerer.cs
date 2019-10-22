using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EncyclopediaBot.Logic.Snl
{
    public class DefinitionAnswerer
    {
        private readonly IArticleProvider _snlProvider;
        private readonly IArticleProvider _smlProvider;
        private readonly IArticleProvider _nklProvider;
        private readonly IArticleProvider _nblProvider;
        private readonly ILogger _logger;
        private readonly ArticleAnalyser _articleAnalyser;
        private readonly NorwegianStemmer _norwegianStemmer;
        private readonly XhtmlMarkupTrimmer _markupTrimmer;

        public Guid? RequestId { get; set; }
        public uint? Limit { get; set; }
        public uint? Offset { get; set; }

        public DefinitionAnswerer(ILogger logger)
        {
            _logger = logger;

            _snlProvider = new SnlProvider(_logger);
            _smlProvider = new SmlProvider(_logger);
            _nklProvider = new NklProvider(_logger);
            _nblProvider = new NblProvider(_logger);

            _articleAnalyser = new ArticleAnalyser(_logger);

            _norwegianStemmer = new NorwegianStemmer();
            _markupTrimmer = new XhtmlMarkupTrimmer();
        }

        internal bool HasValidLicense(Article article)
        {
            string licenseName = article.LicenseName;
            return !"begrenset".Equals(licenseName, StringComparison.CurrentCultureIgnoreCase);
        }

        internal Defintion FormatArticle(Article article)
        {
            string xhtmlBody = article.XhtmlBody;
            var endParagraphIdx = Math.Min(xhtmlBody.IndexOf("</p>", StringComparison.CurrentCultureIgnoreCase), xhtmlBody.IndexOf("</div>", StringComparison.CurrentCultureIgnoreCase));
            bool hasParagraph = endParagraphIdx != -1;
            if (hasParagraph)
            {
                xhtmlBody = xhtmlBody.Substring(0, endParagraphIdx);
            }

            Tuple<string, string> parsedUrl = _articleAnalyser.ParseUrl(article.Url);
            string source = parsedUrl.Item1;
            string articleId = parsedUrl.Item2;
            return new Defintion
            {
                Response = FormatText(xhtmlBody, article),
                Id = articleId,
                Url = article.Url,
                SubjectTitle = article.SubjectTitle,
                Source = source,
                TopicId = ExtractTopicId(article)
            };
        }

        const string _prefix = "http://snl.no/.taxonomy/";

        const string _prefix2 = "https://snl.no/.taxonomy/";
        private uint ExtractTopicId(Article article)
        {
            if (article != null && article.SubjectUrl != null)
            {
                uint topicId;

                if (article.SubjectUrl.IndexOf(_prefix, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    string rawId = article.SubjectUrl.Substring(_prefix.Length);
                    if (uint.TryParse(rawId, out topicId))
                    {
                        return topicId;
                    }
                } else if (article.SubjectUrl.IndexOf(_prefix2, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    string rawId = article.SubjectUrl.Substring(_prefix2.Length);
                    if (uint.TryParse(rawId, out topicId))
                    {
                        return topicId;
                    }
                }
            }
            return 0;
        }

        internal string TrimStartSpaceAndComma(string endText)
        {
            endText = endText.TrimStart();
            endText = endText.TrimStart(',');
            endText = endText.TrimStart();
            return endText;
        }

        internal string TrimStartVerbSpaceAndComma(string endText, string verb)
        {
            endText = TrimStartSpaceAndComma(endText);
            if (endText.IndexOf(verb + ' ', StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                endText = endText.Remove(0, verb.Length);
                endText = TrimStartSpaceAndComma(endText);
            }

            return endText;
        }

        internal string Uncapitalize(string endText)
        {
            if (char.IsUpper(endText[0]))
            {
                endText = char.ToLower(endText[0]) + endText.Substring(1);
            }

            return endText;
        }

        internal string DetectVerbInFocus(string headword, string bodyPlainText)
        {
            if (bodyPlainText.IndexOf(headword, StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                char letterAfterHeadword = bodyPlainText[headword.Length];
                if (letterAfterHeadword == ',')
                {
                    return DetectVerbAfterComma(bodyPlainText);
                }
            }

            char lastLetter = '\0';
            int i;
            for (i = 0; i < bodyPlainText.Length; i++)
            {
                lastLetter = bodyPlainText[i];
                if (char.IsLetterOrDigit(lastLetter) == false)
                {
                    break;
                }
            }

            bool formatIsWordComma = lastLetter == ',';
            if (formatIsWordComma)
            {
                return DetectVerbAfterComma(bodyPlainText);
            }
            else
            {
                return bodyPlainText.Substring(0, i);
            }
        }

        internal string DetectVerbAfterComma(string bodyPlainText)
        {
            int commatAt = bodyPlainText.IndexOf(',');
            if (commatAt != -1)
            {
                int wordStart = 0;
                int numberOfLetters = 0;
                for (int i = commatAt + 1; i < bodyPlainText.Length; i++)
                {
                    if (char.IsLetterOrDigit(bodyPlainText[i]))
                    {
                        wordStart = i;
                        break;
                    }
                }

                for (int i = wordStart; i < bodyPlainText.Length; i++)
                {
                    if (char.IsLetterOrDigit(bodyPlainText[i]) == false)
                    {
                        break;
                    }

                    numberOfLetters++;

                }

                return bodyPlainText.Substring(wordStart, numberOfLetters);
            }

            return null;
        }

        public string GetDeterminerWord(string headword, string bodyPlainText)
        {
            string verbInFocus = DetectVerbInFocus(headword, bodyPlainText);
            if (IsProbablyVerb(verbInFocus) == false)
            {
                return string.Empty;
            }

            string verbInFocusStemmed = _norwegianStemmer.StemToken(verbInFocus);
            Dictionary<string, uint> numberOfSuffixesInFocus = new Dictionary<string, uint>();

            int wordAt = bodyPlainText.IndexOf(verbInFocusStemmed, StringComparison.CurrentCultureIgnoreCase);
            char[] tokenEnds = { ',', ' ', '\n', '.', ':', ';', '?' };
            int lastIndex = bodyPlainText.Length - 1;
            while (wordAt >= 0 && wordAt <= lastIndex)
            {
                int nextSpace = bodyPlainText.IndexOfAny(tokenEnds, wordAt);
                if (nextSpace != -1 && nextSpace != wordAt + 1)
                {
                    int suffixAt = wordAt + verbInFocusStemmed.Length;
                    int suffixLength = nextSpace - wordAt - verbInFocusStemmed.Length;
                    if (suffixLength > 0)
                    {
                        string suffix = bodyPlainText.Substring(suffixAt, suffixLength).ToLower();
                        if (numberOfSuffixesInFocus.ContainsKey(suffix))
                        {
                            numberOfSuffixesInFocus[suffix]++;
                        }
                        else
                        {
                            numberOfSuffixesInFocus[suffix] = 1;
                        }
                    }
                }

                if (wordAt + 1 < lastIndex)
                {
                    wordAt = bodyPlainText.IndexOf(verbInFocusStemmed, wordAt + 1, StringComparison.CurrentCultureIgnoreCase);
                }
                else
                {
                    wordAt = -1;
                }
            }

            if (_logger != null)
            {
                string message = Summerise(verbInFocus, numberOfSuffixesInFocus);
                _logger.Debug(message, RequestId);
            }

            while (numberOfSuffixesInFocus.Count > 0)
            {
                string suffix = numberOfSuffixesInFocus.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
                switch (suffix)
                {
                    case "en":
                        return "en";
                    case "a":
                        return "ei";
                    case "et":
                        return "et";
                    default:
                        numberOfSuffixesInFocus.Remove(suffix);
                        break;
                }
            }

            return string.Empty;
        }

        private string Summerise(string verbInFocus, Dictionary<string, uint> numberOfSuffixesInFocus)
        {
            StringBuilder summaryBuilder = new StringBuilder();
            summaryBuilder.Append("Found head word \"");
            summaryBuilder.Append(verbInFocus);
            summaryBuilder.Append("\" suffix to determine the determiner:\n");
            foreach (var item in numberOfSuffixesInFocus.Where(item => item.Value > 1))
            {
                summaryBuilder.Append(item.Key);
                summaryBuilder.Append(':');
                summaryBuilder.Append(' ');
                summaryBuilder.Append(item.Value);
                summaryBuilder.Append(Environment.NewLine);
            }

            summaryBuilder.Append(Environment.NewLine);
            string message = summaryBuilder.ToString();
            return message;
        }

        private bool IsProbablyVerb(string verbInFocus)
        {
            if (string.IsNullOrEmpty(verbInFocus))
            {
                return false;
            }

            foreach (var letter in verbInFocus)
            {
                if (char.IsLetter(letter) == false)
                {
                    return false;
                }
            }
            return true;
        }

        internal string ConvertToAnswer(string ingress, Article article, string bodyPlainText)
        {
            string headWordOrTitle = article.Metadata.Headword;
            // True for article kupper'n, Knut Johannesen
            if (string.IsNullOrEmpty(headWordOrTitle))
            {
                headWordOrTitle = article.Title;
            }

            bool subjectStillExists = _articleAnalyser.SubjectStillExists(article, bodyPlainText);
            string verb = (subjectStillExists ? "er" : "var");

            // article are words used together with nouns. In Norwegian the articles are "en", "ei", "et"
            string articleWord = GetDeterminerWord(headWordOrTitle, bodyPlainText);

            int firstSentanceEndIdx = ingress.IndexOfAny(new char[] { '.', '!', '?' });
            int headWordAt = ingress.IndexOf(headWordOrTitle, 0, firstSentanceEndIdx, StringComparison.CurrentCultureIgnoreCase);
            bool foundHeadword = headWordAt != -1;
            if (foundHeadword)
            {
                int headWordEndAt = headWordAt + headWordOrTitle.Length;
                string startText = ingress.Substring(0, headWordEndAt);
                string endText = ingress.Substring(headWordEndAt);
                endText = TrimStartVerbSpaceAndComma(endText, verb);
                endText = Uncapitalize(endText);
                if (!string.IsNullOrWhiteSpace(articleWord))
                {
                    return string.Join(" ", startText, verb, articleWord, endText);
                }

                return string.Join(" ", startText, verb, endText);
            }
            var textInfo = CultureInfo.CurrentCulture.TextInfo;
            string headWordTitle = ToTitleCase(headWordOrTitle, textInfo);
            if (!string.IsNullOrEmpty(articleWord))
            {
                string ingressPlainTextTrimmed = TrimStartVerbSpaceAndComma(TrimStartVerbSpaceAndComma(ingress, verb), articleWord);
                return string.Join(" ", headWordTitle, verb, articleWord, Uncapitalize(ingressPlainTextTrimmed
                    ));
            }

            return string.Join(" ", headWordTitle, verb, Uncapitalize(TrimStartVerbSpaceAndComma(ingress, verb)));
        }

        internal string ToTitleCase(string headWordOrTitle, TextInfo textInfo)
        {
            char upperCapitalLetter = textInfo.ToUpper(headWordOrTitle[0]);
            string restOfWord = headWordOrTitle.Substring(1);
            return string.Concat(upperCapitalLetter, restOfWord);
        }

        internal string FormatText(string articleText
        , Article article)
        {
            var trimResults = _markupTrimmer.Trim(articleText);
            string trimmedText = trimResults.Text;
            string ingress = trimmedText;

            string plainTextBody = _markupTrimmer.Trim(article.XhtmlBody).Text;
            int idxfirstFirstSubSentance = trimmedText.IndexOfAny(new char[] { '.', '!', '?' });
            if (idxfirstFirstSubSentance != -1)
            {
                string firstFirstSubSentance = trimmedText.Substring(0, idxfirstFirstSubSentance);
                if (!(firstFirstSubSentance.IndexOf(" er ", StringComparison.CurrentCultureIgnoreCase) != -1)
                    || firstFirstSubSentance.IndexOf(" var ", StringComparison.CurrentCultureIgnoreCase) != -1
                    )
                {
                    ingress = ConvertToAnswer(ingress, article, plainTextBody);
                }
                else
                {
                    ingress = TrimStartSpaceAndComma(ingress);
                }
            }

            return ingress;
        }

        public IEnumerable<Defintion> GetAnswer(string keyword, uint? offset = null, uint? limit = null)
        {
            string keywordLowerCase = keyword.Trim();
            SearchRequest searchRequest = new SearchRequest()
            {
                Query = keyword,
                Limit = limit ?? Limit,
                Offset = offset ?? Offset
            };

            _snlProvider.RequestId = RequestId;
            SearchResult snlResult = _snlProvider.Search(searchRequest);
            var aggregatedResults = QuerySubCatalogues(searchRequest, snlResult);
            var result = FilterResults(aggregatedResults, keywordLowerCase);
            return result;
        }

        private IEnumerable<Defintion> FilterResults(SearchResult searchResults, string keywordLowerCase)
        {
            foreach (Doc doc in searchResults.Results)
            {
                var articleRequest = new ArticleRequest()
                {
                    ArticleUrlJson = doc.ArticleUrlJson
                };

                int distance = LevensteinDistance.Calculate(doc.Headword.ToLower(), keywordLowerCase);
                if (distance <= 3 || doc.Headword.Length / (float)distance > 0.75)
                {
                    _snlProvider.RequestId = RequestId;
                    var articleResult = _snlProvider.GetArticleAsync(articleRequest);
                    articleResult.Wait();
                    var article = articleResult.Result;
                    if (article != null && HasValidLicense(article) == false)
                    {
                        _logger.Debug("Article titled " + doc.Title + " cut due to lisencing. Keeping to citiations only");
                    }

                    if (article != null)
                    {
                        yield return FormatArticle(article);
                    }
                }
                else if (doc.Snippet.IndexOf(keywordLowerCase, StringComparison.CurrentCultureIgnoreCase) != -1
                      || doc.FirstTwoSentences.IndexOf(keywordLowerCase, StringComparison.CurrentCultureIgnoreCase) != -1)
                {
                    // picks up nicknames
                    var articleResult = _snlProvider.GetArticleAsync(articleRequest);
                    articleResult.Wait();
                    var article = articleResult.Result;
                    if (article != null
                        && !string.IsNullOrEmpty(article.Metadata.AlternativeForm)
                        && _articleAnalyser.IsBiography(article))
                    {
                        var alternativeNames = new XhtmlMarkupTrimmer().Trim(article.Metadata.AlternativeForm).Text.Split(',');
                        foreach (var alternativeName in alternativeNames)
                        {
                            // special case: "<p>Henrik Johan Ibsen. Pseudonym: Brynjolf Bjarme</p>"
                            string alternativeNameTrimmed = alternativeName.Trim(new char[] { '«', '»', ' ', ':', '.' });
                            if (LevensteinDistance.Calculate(alternativeNameTrimmed, keywordLowerCase) <= 2)
                            {
                                if (article != null)
                                {
                                    if (HasValidLicense(article) == false)
                                        _logger.Debug("Article titled " + doc.Title + " cut due to lisencing. Keeping to citiations only");
                                    yield return FormatArticle(article);
                                }
                            }
                        }
                    }
                }
            }
        }

        private SearchResult QuerySubCatalogues(SearchRequest searchRequest, SearchResult snlResult)
        {
            _smlProvider.RequestId = RequestId;
            var smlResultTask = _smlProvider.SearchAsync(searchRequest);

            _nklProvider.RequestId = RequestId;
            var nklResultTask = _nklProvider.SearchAsync(searchRequest);

            _nblProvider.RequestId = RequestId;
            var nblResultTask = _nblProvider.SearchAsync(searchRequest);

            var allTasks = new Task<SearchResult>[] { smlResultTask, nklResultTask, nblResultTask };

            Task.WaitAll(allTasks);

            var nblResult = nblResultTask.Result;
            var nklResult = nklResultTask.Result;
            var smlResult = smlResultTask.Result;

            var aggregatedResults = new SearchResult()
            {
                Results = new List<Doc>(smlResult.Results.Count + nklResult.Results.Count + nblResult.Results.Count + snlResult.Results.Count)
            };

            aggregatedResults.Results.AddRange(snlResult.Results);
            aggregatedResults.Results.AddRange(nblResult.Results);
            aggregatedResults.Results.AddRange(nklResult.Results);
            aggregatedResults.Results.AddRange(smlResult.Results);

            var comparer = new SortByRank();
            aggregatedResults.Results.Sort(comparer);
            aggregatedResults.Results.Reverse();
            
            return aggregatedResults;
        }
    }
}