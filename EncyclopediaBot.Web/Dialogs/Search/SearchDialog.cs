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
    public class SearchDialog : ComponentDialog, ISearchDialog
    {
        private readonly DefinitionManager _definitionManager;
        private const string UserInfo = "userinfo";
        private const string TopicsData = "topics";

        public string Query { get; set; }
        public Guid? RequestId { get; set; }

        public SearchDialog(DefinitionManager definitionManager, UserState userState) : base(nameof(SearchDialog))
        {
            _definitionManager = definitionManager;

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            // The initial child Dialog to run.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                SearchStepAsync,
                AskUserSearchFinishedAsync,
                AskFollowUpTopicAsync,
                PaginateStepAsync,
                FinalGreetStepAsync
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        #region logic
        private uint CountAnswers(IEnumerable<Answer> answers)
        {
            uint Limit = 0;
            foreach (var _ in answers) { Limit++; }
            return Limit;
        }

        private ArticleId MapAnswerToArticleId(Answer answer)
        {
            return new ArticleId { Id = answer.Id, Source = answer.Source };
        }


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

        private List<Topic> FetchTopics(List<Answer> answers, Guid? requestId)
        {
            IEnumerable<uint> topicIds = GetUniqueTopicIDs(answers);
            var topics = new List<Topic>();
            foreach (var snlTopic in _definitionManager.GetTopics(topicIds, requestId))
            {
                topics.Add(new Topic
                {
                    Id = snlTopic.Id,
                    Name = snlTopic.Name,
                    SubTopics = snlTopic.SubTopics.Select(st => new Topic { Name = st.Name, SubTopics = new List<Topic>(0) }).ToList()
                });
            }
            return topics;
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

        private void PutDownVote(SearchState searchQueryVotes, ArticleId articleInFocus)
        {
            var downVotes = searchQueryVotes.DownVotes;
            if (downVotes != null)
            { 
                var vote = Vote.Down;
                PutNewVote(downVotes, vote, articleInFocus);
            }
        }

        private void PutUpVote(SearchState searchQueryVotes, ArticleId articleInFocus)
        {
            var upVotes = searchQueryVotes.UpVotes;
            if (upVotes != null)
            {
                var vote = Vote.Up;
                PutNewVote(upVotes, vote, articleInFocus);
            }
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
                uint deltas = ((uint)searchState.DiscardedArticles.Count - offset);
                if (limit > 0)
                {
                    deltas /= limit;
                    offset += (deltas * limit);
                } else
                {
                    offset = deltas;
                }

                newValue = true;
            }

            if (newValue)
            {
                return offset;
            }

            return searchState.Offset;
        }
        #endregion

        private async Task<DialogTurnResult> SearchStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var requestId = RequestId ?? Guid.NewGuid();
            Answer answer = null;
            SearchState searchState = null;
            IEnumerable<Answer> answers;
            if (stepContext.Options is SearchState)
            {
                searchState = stepContext.Options as SearchState;
                if (searchState.LastResult.Any())
                {
                    answers = searchState.LastResult;
                }
                else
                {
                    if (searchState.Limit.HasValue)
                    {

                        searchState.Offset += searchState.Limit.Value;
                        if (searchState.Limit <= 100)
                            searchState.Limit++;
                    }
                    else
                    {
                        searchState.Offset++;
                    }

                    answers = _definitionManager.GetAnswer(Query, searchState.Offset, searchState.Limit, requestId: requestId);
                    searchState.LastResult = new List<Answer>(answers);
                }

                answer = searchState.LastResult.FirstOrDefault();
                searchState.ArticleInFocus = new ArticleId { Id = answer.Id, Source = answer.Source };
            }
            else
            {
                uint limit = 7;
                answers = _definitionManager.GetAnswer(Query, limit: limit, requestId: requestId);
                var lastResult = new List<Answer>(answers);
                if (lastResult.Any())
                {
                    answer = lastResult.FirstOrDefault();
                    // Save the query before continuing
                    searchState = new SearchState
                    {
                        Query = Query,
                        DiscardedArticles = new HashSet<ArticleId>(),
                        UpVotes = new List<QueryVote>(),
                        DownVotes = new List<QueryVote>(),
                        LastResult = new List<Answer>(answers),
                        ArticleInFocus = MapAnswerToArticleId(answer),
                        Limit = CountAnswers(answers)
                    };
                }
            }

            if (answers.Any())
            {
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

        private static async Task<DialogTurnResult> AskUserSearchFinishedAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var promptOptionsChoicePrompt = new PromptOptions
            {
                Prompt = MessageFactory.Text("Fant jeg det du lette etter?"),
                Choices = ChoiceFactory.ToChoices(new List<string> { "ja", "nei" }),
            };

            // Prompt the user for a choice.)
            return await stepContext.PromptAsync(nameof(ChoicePrompt), promptOptionsChoicePrompt, cancellationToken);
        }
        private string AnswerUndecided = "Jeg vet ikke";
        private async Task<DialogTurnResult> AskFollowUpTopicAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            Guid? requestId = Guid.NewGuid();
            var searchState = stepContext.Values[UserInfo] as SearchState;

            var userAnswered = ((FoundChoice)stepContext.Result).Value ?? string.Empty;
            if ("nei".Equals(userAnswered, StringComparison.CurrentCulture))
            {
                var articleInFocus = searchState.ArticleInFocus;
                PutDownVote(searchState, articleInFocus);

                searchState.DiscardedArticles.Add(articleInFocus);
                searchState.LastResult.RemoveAll(a => a.Id == searchState.ArticleInFocus.Id && a.Source == searchState.ArticleInFocus.Source);

                stepContext.Values[UserInfo] = searchState;
                if (searchState.LastResult.Any())
                {
                    var topics = FetchTopics(searchState.LastResult, requestId);
                    stepContext.Values[TopicsData] = topics;
                    if (topics.Count > 1)
                    {
                        var choices = topics.Select(t => t.Name).ToList();
                        choices.Add(AnswerUndecided);
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text("Ok. Jeg trenger litt fler detaljer for å kunne besvare det spørsmålet."), cancellationToken);
                        var choicePromptOptions = new PromptOptions
                        {
                            Prompt = MessageFactory.Text("Hvilket emne snakker vi om?"),
                            RetryPrompt = MessageFactory.Text("Vi er nødt til å holde oss til temaet. Vennligst velg det relevante emnet for det du leter etter."),
                            Choices = ChoiceFactory.ToChoices(choices)
                        };

                        return await stepContext.PromptAsync(nameof(ChoicePrompt), choicePromptOptions, cancellationToken);
                    }
                    else
                    {
                        stepContext.Values[TopicsData] = new List<Topic>(0);
                        return await stepContext.NextAsync(null, cancellationToken);
                    }
                } else
                {
                    stepContext.Values[TopicsData] = new List<Topic>(0);
                    return await stepContext.NextAsync(null, cancellationToken);
                }
            } else if ("ja".Equals(userAnswered, StringComparison.CurrentCulture))
            {
                var articleInFocus = searchState.ArticleInFocus;
        
                PutUpVote(searchState, articleInFocus);
                stepContext.Values[UserInfo] = searchState;
                return await stepContext.NextAsync(new Nullable<bool>(true), cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Vi mistet tråden her. La oss snakke om noe annet"), cancellationToken);
                return await stepContext.EndDialogAsync(searchState, cancellationToken);
            }
        }

        private bool UserKnowsTopic(FoundChoice userChoice)
        {
            return AnswerUndecided.Equals(userChoice.Value) == false;
        }

        private async Task<DialogTurnResult> PaginateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            Guid requestId = RequestId ?? Guid.NewGuid();
            if (stepContext.Result is bool? && (stepContext.Result as bool?).Value == true)
            {
                return await stepContext.NextAsync(new Nullable<bool>(true), cancellationToken);
            }

            var searchState = stepContext.Values[UserInfo] as SearchState;
            ArticleId articleInFocus = searchState.ArticleInFocus;

            searchState.Offset = CalculateOffset(searchState);
            
            IEnumerable<Answer> answersInTopic = null;
            var userChoice = stepContext.Result as FoundChoice;
            
            if (UserKnowsTopic(userChoice)
                && stepContext.Values.ContainsKey(TopicsData)
                && stepContext.Values[TopicsData] is List<Topic>)
            {
                Topic topic = (stepContext.Values[TopicsData] as List<Topic>).FirstOrDefault(topic => topic.Name == userChoice.Value);
                if (topic != null)
                { 
                    var answers = searchState.LastResult;
                    answersInTopic = FilterByTopic(answers, topic);
                    searchState.LastResult = new List<Answer>(answersInTopic);
                }
                else
                {
                    answersInTopic = searchState.LastResult;
                }
            }
            else
            {
                answersInTopic = searchState.LastResult;
            }

            
            stepContext.Values[UserInfo] = searchState;
            return await stepContext.ReplaceDialogAsync(nameof(SearchDialog), searchState, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalGreetStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            bool? result = stepContext.Result as bool?;
            if (result.HasValue && result.Value)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Det er alltid hyggelig å kunne hjelpe."), cancellationToken);
            }
            else if (result.HasValue && result.Value == false)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Leit at jeg ikke fant ut av det. Dette falt utenfor mitt domene."), cancellationToken);
            } else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("La oss snakke om noe annet."), cancellationToken);

            }

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog, here it is the end.
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
