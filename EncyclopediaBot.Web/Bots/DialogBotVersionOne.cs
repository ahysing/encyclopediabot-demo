using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

using Microsoft.Bot.Builder.AI.Luis;
using System.Collections.Generic;
using EncyclopediaBot.Web.Dialogs.Search;

namespace EncyclopediaBot.Web
{
    // This IBot implementation can run any type of Dialog. The use of type parameterization is to allows multiple different bots
    // to be run at different endpoints within the same project. This can be achieved by defining distinct Controller types
    // each with dependency on distinct IBot types, this way ASP Dependency Injection can glue everything together without ambiguity.
    // The ConversationState is used by the Dialog system. The UserState isn't, however, it might have been used in a Dialog implementation,
    // and the requirement is that all BotState objects are saved at the end of a turn.
    public class DialogBotVersionOne<T> : ActivityHandler where T : Dialog
    {
        protected readonly Dialog Dialog;
        private readonly SearchDialog _searchDialog;
        protected readonly BotState ConversationState;
        protected readonly BotState UserState;
        protected readonly ILogger Logger;

        private LuisRecognizer _recognizer;
        private LuisSetup _luisSetup;
        private IStatePropertyAccessor<DialogState> _searchState;

        public DialogBotVersionOne(ConversationState conversationState, UserState userState, T dialog, ILogger<DialogBot<T>> logger, SearchDialog searchDialog, LuisSetup luisSetup)
        {
            ConversationState = conversationState;
            UserState = userState;
            Dialog = dialog;
            Logger = logger;
            _searchDialog = searchDialog;
            _luisSetup = luisSetup;
        }

        #region custom
        public bool IsConfigured
        {
            get
            {
                return _recognizer != null;
            }
        }

        internal void SetupLUIS()
        {
            string hostname = _luisSetup.Hostname;
            var luisApplication = new LuisApplication(_luisSetup.AppId,
                                                        _luisSetup.APIKey,
                                                        hostname);
            _recognizer = new LuisRecognizer(luisApplication);
        }

        protected override Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            string greeting = "Hei. Jeg er leksikonbot.";
            var promptMessage = MessageFactory.Text(greeting, greeting, InputHints.IgnoringInput);
            turnContext.SendActivityAsync(promptMessage, cancellationToken);

            string describeGreeting = "Jeg kan vare på spørsmål om kjente personer, steder eller historiske hendelser.";
            turnContext.SendActivityAsync(MessageFactory.Text(describeGreeting, describeGreeting, InputHints.AcceptingInput), cancellationToken);
            string exGreeting = "For eksempel:";
            turnContext.SendActivityAsync(MessageFactory.Text(exGreeting, exGreeting, InputHints.AcceptingInput), cancellationToken);
            string whoIsGreeting = "Fortell meg om en kvinne innen vitenskap fra Norge.";
            turnContext.SendActivityAsync(MessageFactory.Text(whoIsGreeting, whoIsGreeting, InputHints.AcceptingInput), cancellationToken);
            string whatPlaceGreeting = "Fortell meg om en viktig mur i Tyskland.";
            turnContext.SendActivityAsync(MessageFactory.Text(whatPlaceGreeting, whatPlaceGreeting, InputHints.AcceptingInput), cancellationToken);

            return base.OnMembersAddedAsync(membersAdded, turnContext, cancellationToken);
        }

        #endregion
        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occured during the turn.
            await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Running dialog with Message Activity.");

            if (!IsConfigured) { SetupLUIS(); }

            var resultTask = _recognizer.RecognizeAsync(turnContext, cancellationToken);
            var recognized = await resultTask;
            if (recognized != null)
            {
                var (intent, _) = recognized.GetTopScoringIntent();
                switch (intent)
                {
                    case "Define":
                        {
                            _searchState = ConversationState.CreateProperty<DialogState>("define");
                            string query = new BotHelper().GetQuery(recognized);
                            if (!string.IsNullOrWhiteSpace(query))
                            {
                                _searchDialog.Query = query;
                                await _searchDialog.RunAsync(turnContext, _searchState, cancellationToken);
                            }
                            else
                            {
                                await turnContext.SendActivityAsync(MessageFactory.Text("Jeg fant ikke tema du nakker om. kan du formulere deg på en annen måte?"), cancellationToken);
                            }
                        }

                        break;
                    default:
                        // Run the Dialog with the new message Activity.
                        await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
                        break;
                }
            }
        }
    }
}
