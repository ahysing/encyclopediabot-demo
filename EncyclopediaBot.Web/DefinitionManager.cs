using System;
using System.Collections.Generic;
using EncyclopediaBot.Logic;
using EncyclopediaBot.Logic.Snl;
using EncyclopediaBot.Web.Model;
using EncyclopediaBot.Web.Model.Search;

namespace EncyclopediaBot.Web
{
    public class DefinitionManager
    {
        private readonly DefinitionAnswerer _definitionAnswerer;
        private readonly TopicProvider _topicProvider;

        public DefinitionManager(ILogger logger)
        {
            _definitionAnswerer = new DefinitionAnswerer(logger);
            _topicProvider = new TopicProvider(logger);
        }

        public IEnumerable<Answer> GetAnswer(string keyword, uint? offset = null, uint? limit = null, Guid? requestId = null)
        {
            _definitionAnswerer.RequestId = requestId;
            var definitions = _definitionAnswerer.GetAnswer(keyword, offset, limit);
            if (definitions != null)
            {
                foreach (var definition in definitions)
                {
                    var response = definition.Response;
                    var answer = new Answer
                    {
                        Response = response,
                        Id = definition.Id,
                        Source = definition.Source
                    };

                    yield return answer;
                }
            }

            yield break;
        }

        public IEnumerable<Topic> GetTopics(IEnumerable<uint> topicIds, Guid? requestId = null)
        {
            foreach (var topicId in topicIds)
            {
                var subTaxonomy = new List<Topic>();
                Logic.Snl.TaxonomyResult topic = _topicProvider.GetTaxonomy(topicId, requestId);
                foreach (var ancestor in topic.taxonomy.ancestors)
                {
                    var t = new Topic
                    {
                        Name = topic.taxonomy.title,
                        Id = ExtractId(ancestor.url),
                        SubTopics = new List<Topic>(0)
                    };

                    subTaxonomy.Add(t);
                }

                yield return new Topic
                {
                    Name = topic.taxonomy.title,
                    Id = topicId,
                    SubTopics = subTaxonomy
                };
            }
        }

        private uint ExtractId(string url)
        {
            uint topicId;
            var idx = url.LastIndexOf('/');
            if (idx != -1 && url.Length > idx + 1)
            {
                string taxIdRaw = url.Substring(idx + 1);
                taxIdRaw = taxIdRaw.TrimEnd(new char[] { '.', 'j', 's', 'o', 'n' });
                uint.TryParse(taxIdRaw, out topicId);
            } else
            {
                topicId = 0;
            }

            return topicId;
        }
    }
}