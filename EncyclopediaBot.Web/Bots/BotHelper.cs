using Microsoft.Bot.Builder;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EncyclopediaBot.Web
{
    public class BotHelper
    {
        public bool PutNameTokens(out string name, List<Tuple<bool, JToken>> nameParts)
        {
            StringBuilder nameTokens = new StringBuilder();
            if (nameParts.Any(namePart => namePart.Item1))
            {
                bool isSecond = false;
                foreach (var namePart in nameParts)
                {
                    if (namePart.Item1)
                    {
                        Queue<JToken> parts = new Queue<JToken>();
                        parts.Enqueue(namePart.Item2);
                        while (parts.Any())
                        {
                            JToken jToken = parts.Dequeue();
                            switch (jToken.Type)
                            {
                                case JTokenType.Array:
                                    var subs = jToken.ToObject<JArray>();
                                    foreach (var sub in subs)
                                    {
                                        parts.Enqueue(sub);
                                    }

                                    break;
                                case JTokenType.String:
                                    if (isSecond)
                                    {
                                        nameTokens.Append(" ");
                                    }
                                    string value = jToken.ToObject<string>();
                                    nameTokens.Append(value);
                                    isSecond = true;
                                    break;
                                case JTokenType.Object:
                                    foreach (var property in (jToken as JObject).Properties())
                                    {
                                        if ("FirstName" == property.Name
                                         || "LastName" == property.Name
                                         || "PersonName.Nick" == property.Name
                                         || "PersonName.Prefix" == property.Name
                                        )
                                        {
                                            parts.Enqueue(property.Value);
                                        }
                                    }

                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }

                nameTokens = StripTrailingNonLetterOrDigit(nameTokens);
                name = nameTokens.ToString();

                return true;
            }

            name = string.Empty;
            return false;
        }

        public StringBuilder PutConceptTokens(out StringBuilder concept, JToken conceptToken)
        {
            concept = new StringBuilder();
            foreach (string subConcept in conceptToken.ToObject<string[]>())
            {
                concept.Append(" ");
                concept.Append(subConcept);
            }

            concept = StripTrailingNonLetterOrDigit(concept);
            return concept;
        }

        public StringBuilder StripTrailingNonLetterOrDigit(StringBuilder concept)
        {
            while (concept.Length > 0 && char.IsLetterOrDigit(concept[concept.Length - 1]) == false)
            {
                concept.Length--;
            }

            return concept;
        }

        internal string GetQuery(RecognizerResult recognized)
        {
            string personName = null;
            JToken conceptToken = null;
            bool foundConcept = recognized.Entities.TryGetValue("Concept", out conceptToken);

            JToken nameToken = null;
            bool foundName = recognized.Entities.TryGetValue("PersonName", out nameToken);

            JToken nickToken = null;
            bool foundNick = recognized.Entities.TryGetValue("PersonName.Nick", out nickToken);

            JToken firstNameToken = null;
            bool foundFirstName = recognized.Entities.TryGetValue("FirstName", out firstNameToken);
            
            JToken lastNameToken = null;
            bool foundLastName = recognized.Entities.TryGetValue("LastName", out lastNameToken);

            JToken namePrefixToken = null;
            bool foundNamePrefix = recognized.Entities.TryGetValue("PersonName.Prefix", out namePrefixToken);

            var nameParts = new List<Tuple<bool, JToken>>()
                {
                    Tuple.Create(foundName, nameToken),
                    Tuple.Create(foundConcept, conceptToken),
                    Tuple.Create(foundNamePrefix, namePrefixToken),
                    Tuple.Create(foundFirstName, firstNameToken),
                    Tuple.Create(foundNick, nickToken),
                    Tuple.Create(foundLastName, lastNameToken)
                };

            string query = null;
            if (PutNameTokens(out personName, nameParts))
            {
                query = personName;
            }
            else if (foundConcept)
            {
                StringBuilder concept = new StringBuilder();
                PutConceptTokens(out concept, conceptToken);
                query = concept.ToString();
            }

            return query;
        }
    }
}
