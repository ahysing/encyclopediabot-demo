// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

using Microsoft.Bot.Builder.AI.Luis;
using System.Collections.Generic;
using EncyclopediaBot.Web.Dialogs.Search;
using System;

namespace EncyclopediaBot.Web
{
    /// <summary>
    /// Provides additional, `static` (Shared in Visual Basic) methods for <see cref="Dialog"/> and
    /// derived classes.
    /// </summary>
    public static class LUISExtensions
    {
        /// <summary>
        /// Creates a dialog stack and starts a dialog, pushing it onto the stack.
        /// </summary>
        /// <param name="dialog">The dialog to start.</param>
        /// <param name="turnContext">The context for the current turn of the conversation.</param>
        /// <param name="accessor">The <see cref="IStatePropertyAccessor{DialogState}"/> accessor
        /// with which to manage the state of the dialog stack.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task RunAsyncWithLUISDispatcher(this Dialog dialog, ITurnContext turnContext, IStatePropertyAccessor<DialogState> accessor, CancellationToken cancellationToken, params Dialog[] otherDialogsInConversation)
        {
            var dialogSet = new DialogSet(accessor);
            dialogSet.TelemetryClient = dialog.TelemetryClient;
            dialogSet.Add(dialog);
            foreach (var otherDialog in otherDialogsInConversation)
            {
                dialogSet.Add(otherDialog);
            }

            var dialogContext = await dialogSet.CreateContextAsync(turnContext, cancellationToken).ConfigureAwait(false);
            var results = await dialogContext.ContinueDialogAsync(cancellationToken).ConfigureAwait(false);
            if (results.Status == DialogTurnStatus.Empty)
            {
                await dialogContext.BeginDialogAsync(dialog.Id, null, cancellationToken).ConfigureAwait(false);
            }
        }
    }



    // This IBot implementation can run any type of Dialog. The use of type parameterization is to allows multiple different bots
    // to be run at different endpoints within the same project. This can be achieved by defining distinct Controller types
    // each with dependency on distinct IBot types, this way ASP Dependency Injection can glue everything together without ambiguity.
    // The ConversationState is used by the Dialog system. The UserState isn't, however, it might have been used in a Dialog implementation,
    // and the requirement is that all BotState objects are saved at the end of a turn.
    public class DialogBot<T> : ActivityHandler where T : Dialog
    {
        protected readonly Dialog Dialog;
        private readonly SearchDialog _searchDialog;
        protected readonly BotState ConversationState;
        protected readonly BotState UserState;
        protected readonly ILogger Logger;

        private LuisRecognizer _recognizer;
        private LuisSetup _luisSetup;

        public DialogBot(ConversationState conversationState, UserState userState, T dialog, ILogger<DialogBot<T>> logger, SearchDialog searchDialog, LuisSetup luisSetup)
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

            string describeGreeting = "Jeg kan svare på spørsmål om kjente personer, steder eller historiske hendelser.";
            turnContext.SendActivityAsync(MessageFactory.Text(describeGreeting, describeGreeting, InputHints.AcceptingInput), cancellationToken);
            string exGreeting = "For eksempel";
            turnContext.SendActivityAsync(MessageFactory.Text(exGreeting, exGreeting, InputHints.AcceptingInput), cancellationToken);
            string whoIsGreeting = "Definer algebra.";
            turnContext.SendActivityAsync(MessageFactory.Text(whoIsGreeting, whoIsGreeting, InputHints.AcceptingInput), cancellationToken);
            string whatPlaceGreeting = "Fortell meg om berlinmuren.";
            turnContext.SendActivityAsync(MessageFactory.Text(whatPlaceGreeting, whatPlaceGreeting, InputHints.AcceptingInput), cancellationToken);
            string whoWasGreeting = "Hvem var Edvard Munch?";
            turnContext.SendActivityAsync(MessageFactory.Text(whoWasGreeting, whatPlaceGreeting, InputHints.AcceptingInput), cancellationToken);

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
            Guid requestId = Guid.NewGuid();
            Logger.LogInformation($"RequestId: {requestId}, Running dialog with Message Activity.");

            if (!IsConfigured) { SetupLUIS(); }

            var dialogState = ConversationState.CreateProperty<DialogState>(nameof(DialogState));
            var resultTask = _recognizer.RecognizeAsync(turnContext, cancellationToken);
            var recognized = await resultTask;
            if (recognized != null)
            {
                var (intent, _) = recognized.GetTopScoringIntent();
                switch (intent)
                {
                    case "Define":
                        {
                            string query = new BotHelper().GetQuery(recognized);
                            if (!string.IsNullOrWhiteSpace(query))
                            {
                                Logger.LogInformation($"RequestId: {requestId}, Query: {query}");
                                _searchDialog.Query = query;
                                _searchDialog.RequestId = requestId;
                                await _searchDialog.RunAsyncWithLUISDispatcher(turnContext, dialogState, cancellationToken, Dialog);
                            }
                            else
                            {
                                await turnContext.SendActivityAsync(MessageFactory.Text("Jeg fant ikke tema du nakker om. kan du formulere deg på en annen måte?"), cancellationToken);
                            }
                        }

                        break;
                    default:
                        // Run the Dialog with the new message Activity.
                        await Dialog.RunAsyncWithLUISDispatcher(turnContext, dialogState, cancellationToken, _searchDialog);
                        break;
                }
            }
        }
    }
}
