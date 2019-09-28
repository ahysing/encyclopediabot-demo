using EncyclopediaBot.Web.Model;
using EncyclopediaBot.Web.Model.Search;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EncyclopediaBot.Web.Dialogs.Search
{
    public class SearchDialogBackup : ComponentDialog, ISearchDialog
    {
        private readonly DefinitionManager _definitionManager;
        private string UserInfo = "userinfo";
        private string QueryInfo = "queryinfo";

        public string Query { get; set; }
        public Guid? RequestId { get; set; }

        public SearchDialogBackup(DefinitionManager definitionManager) : base(nameof(SearchDialog))
        {
            _definitionManager = definitionManager;

            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                SearchStepAsync,
                AskUserSearchFinishedAsync,
                AskFollowUpTopicAsync,
                PaginateStepAsync,
                FinalGreetStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }
  
        #region Helpers
        private uint CountAnswers(IEnumerable<Answer> answers)
        {
            uint Limit = default;
            foreach (var _ in answers) { Limit++; }
            return Limit;
        }

        private ArticleId MapAnswerToArticleId(Answer answer)
        {
            return new ArticleId { Id = answer.Id, Source = answer.Source };
        }
        #endregion

        private async Task<DialogTurnResult> SearchStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            Guid requestId = RequestId ?? Guid.NewGuid();

            if (string.IsNullOrWhiteSpace(Query))
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Jeg mistet spørsmålet ditt på veien. huff"), cancellationToken);
                // WaterfallStep always finishes with the end of the Waterfall or with another dialog, here it is the end.
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }

            // await _userStateAccessor.GetAsync(stepContext.Context, () => new SearchState());
            var answers = _definitionManager.GetAnswer(Query, requestId: requestId);
            var lastResult = new List<Answer>(answers);
            if (lastResult != null && lastResult.Any())
            {
                var answer = lastResult.FirstOrDefault();
                // Save the query before continuing
                var searchState = new SearchState
                {
                    Query = Query,
                    DiscardedArticles = new HashSet<ArticleId>()
                };

                searchState.ArticleInFocus = MapAnswerToArticleId(answer);
                searchState.LastResult = lastResult;
                var answerCount = CountAnswers(answers);
                searchState.Limit = answerCount;  
                searchState.DiscardedArticles = new HashSet<ArticleId>((int)answerCount);
                stepContext.Values[UserInfo] = searchState;

                // pass the answer to the user
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(answer.Response, answer.Response), cancellationToken);
                
                // Pass the answer to the next dialog for follow up questions
                return await stepContext.NextAsync(searchState, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Jeg har ikke svar på spørsmålet ditt."), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }

        private async Task<DialogTurnResult> AskUserSearchFinishedAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var promptOptionsChoicePrompt = new PromptOptions
            {
                Prompt = MessageFactory.Text("Fant jeg det du lette etter?"),
                Choices = ChoiceFactory.ToChoices(new List<string> { "ja", "nei" }),
            };

            // Prompt the user for a choice.)
            return await stepContext.PromptAsync(nameof(ChoicePrompt), promptOptionsChoicePrompt, cancellationToken);
        }

        private async Task<DialogTurnResult> AskFollowUpTopicAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            Guid? requestId = Guid.NewGuid();
            var searchState = stepContext.Values[UserInfo] as SearchState;

            var userAnswered = stepContext.Result as bool?;
            if (userAnswered.Value == false
                && searchState != null
                && searchState.LastResult != null
                && searchState.LastResult.Any())
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Ok. Jeg trenger litt fler detaljer for å kunne besvare det spørsmålet."), cancellationToken);
                var topics = FetchTopics(searchState.LastResult, requestId);
                var choicePromptOptions = new PromptOptions
                {
                    Prompt = MessageFactory.Text("Hvilket emne snakker vi om?"),
                    RetryPrompt = MessageFactory.Text("Vi er nødt til å holde oss til temaet. Vennligst velg det relevante emnet for det du leter etter."),
                    Choices = ChoiceFactory.ToChoices(topics.Select(t => t.Name).ToList())
                };

                return await stepContext.PromptAsync(nameof(ChoicePrompt), choicePromptOptions, cancellationToken);
            }

            if (userAnswered.Value == true)
            {
                //var conversationStateAccessor = _conversationState.CreateProperty<SearchQueryVotes>(nameof(SearchQueryVotes));
                // ArticleId articleInFocus = searchState.ArticleInFocus;
                // var searchQueryVotes = await conversationStateAccessor.GetAsync(stepContext.Context, () => new SearchQueryVotes(FormatQueryOrNull(searchState)));
                // PutUpVote(searchQueryVotes, articleInFocus);

                var searchQueryVotes = stepContext.Values[QueryInfo] as SearchQueryVotes;
                var articleInFocus = searchState.ArticleInFocus;
                PutUpVote(searchQueryVotes, articleInFocus);
                stepContext.Values[QueryInfo] = searchQueryVotes;

                return await stepContext.NextAsync(null, cancellationToken);
            }
            else
            {
                return await stepContext.EndDialogAsync(searchState, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> PaginateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            Guid requestId = Guid.NewGuid();

            var searchState = stepContext.Values[UserInfo] as SearchState;
            ArticleId articleInFocus = searchState.ArticleInFocus;

            var searchQueryVotes = stepContext.Values[QueryInfo] as SearchQueryVotes;
            PutDownVote(searchQueryVotes, articleInFocus);

            var userAnsweredTopic = stepContext.Result as string;
            searchState.DiscardedArticles.Add(articleInFocus);
            searchState.Offset = CalculateOffset(searchState);
            bool hasNextQuestion = false;

            List<Answer> answers = searchState.LastResult;
            if (answers == null || answers.Count == 0)
            {
                if (string.IsNullOrEmpty(searchState.Query))
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Jeg mistet spørsmålet ditt på veien. huff"), cancellationToken);
                    // WaterfallStep always finishes with the end of the Waterfall or with another dialog, here it is the end.
                    return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                }

                answers = new List<Answer>(_definitionManager.GetAnswer(searchState.Query, searchState.Offset, searchState.Limit, requestId));
            }

            Topic topic = null;
            IEnumerable<Answer> answersInTopic = FilterByTopic(answers, topic);
            foreach (var answer in answersInTopic)
            {
                var articleId = new ArticleId { Id = answer.Id, Source = answer.Source };
                if (searchState.DiscardedArticles.Contains(articleId) == false)
                {
                    searchState.ArticleInFocus = articleId;
                    searchState.LastResult = new List<Answer>(answersInTopic);

                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(answer.Response), cancellationToken);

                    hasNextQuestion = true;
                    break;
                }
            }

            stepContext.Values[UserInfo] = searchState;
            if (hasNextQuestion)
            {
                return await stepContext.ReplaceDialogAsync(nameof(SearchDialog), searchState, cancellationToken);

            }

            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Dette vet jeg ikke svaret på."), cancellationToken);
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> FinalGreetStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            bool? result = stepContext.Result as bool?;
            if (result.HasValue && result.Value)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Det er alltid hyggelig å kunne hjelpe."), cancellationToken);
            } else if (result.HasValue && result.Value == false)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Leit at jeg ikke fant ut av det. Dette falt utenfor mitt domene."), cancellationToken);
            }

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog, here it is the end.
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }



        #region logic

        internal IEnumerable<Answer> FilterByTopic(IEnumerable<Answer> answers, Topic topic)
        {
            foreach (var answer in answers)
            {
                if (answer.TopicId == topic.Id || topic.SubTopics.Any(t => t.Id == answer.TopicId))
                {
                    yield return answer;
                }
            }
        }

        private IEnumerable<Topic> FetchTopics(List<Answer> answers, Guid? requestId)
        {
            IEnumerable<uint> topicIds = GetUniqueTopicIDs(answers);
            foreach (var snlTopic in _definitionManager.GetTopics(topicIds, requestId))
            {
                yield return new Topic
                {
                    Id = snlTopic.Id,
                    Name = snlTopic.Name,
                    SubTopics = snlTopic.SubTopics.Select(st => new Topic { Name = st.Name, SubTopics = new List<Topic>(0) }).ToList()
                };
            }
        }

        internal IEnumerable<uint> GetUniqueTopicIDs(List<Answer> answers)
        {
            HashSet<uint> topicIds = new HashSet<uint>();
            foreach (var answer in answers)
            {
                topicIds.Add(answer.TopicId);
            }

            return topicIds;
        }

        private void PutDownVote(SearchQueryVotes searchQueryVotes, ArticleId articleInFocus)
        {
            var downVotes = searchQueryVotes.DownVotes;
            var vote = Vote.Down;
            PutNewVote(downVotes, vote, articleInFocus);
        }

        private void PutUpVote(SearchQueryVotes searchQueryVotes, ArticleId articleInFocus)
        {
            var downVotes = searchQueryVotes.UpVotes;
            var vote = Vote.Up;
            PutNewVote(downVotes, vote, articleInFocus);
        }

        private void PutNewVote(List<QueryVote> votes, Vote vote, ArticleId articleInFocus)
        {
            votes.Add(new QueryVote
            {
                ArticleId = articleInFocus,
                Vote = vote,
                CreatedAt = DateTime.Now
            });
        }

        internal uint? CalculateOffset(SearchState searchState)
        {
            uint offset = 0;
            if (searchState.Offset.HasValue)
            {
                offset = searchState.Offset.Value;
            }

            uint limit = 0;
            if (searchState.Limit.HasValue)
            {
                limit = Math.Max(searchState.Limit.Value, 1);
            }

            bool newValue = false;
            if (searchState.DiscardedArticles.Count > 0)
            {
                uint deltas = ((uint)searchState.DiscardedArticles.Count - offset) / limit;
                offset += (deltas * limit);
                newValue = true;
            }

            if (newValue)
            {
                return offset;
            }

            return searchState.Offset;
        }

        #endregion

        private static Task<bool> YesNoValidatorFunc(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            bool result = default;
            var responses = (Dictionary<string, object>)promptContext.State;
            foreach (var response in responses.Values)
            {
                switch (response)
                {
                    case "ja":
                    case "nei":
                        result = true;
                        break;
                    default:
                        result = false;
                        break;
                }
            }

            return Task.FromResult(result);
        }
    }
}
