﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.Choice;
using static Microsoft.Recognizers.Text.Culture;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder;

namespace EncyclopediaBot.Web.Dialogs.Search
{
    /// <summary>
    /// Prompts a user to confirm something with a yes/no response.
    /// </summary>
    public class ConfirmPromptNo : Prompt<bool>
    {
        private const string NbNO = "nb-NO";
        private static readonly Dictionary<string, (Choice, Choice, ChoiceFactoryOptions)> ChoiceDefaults = new Dictionary<string, (Choice, Choice, ChoiceFactoryOptions)>()
        {
            { NbNO, (new Choice("Ja"), new Choice("Nei"), new ChoiceFactoryOptions(", ", " eller ", ", eller ", true)) },
            { English, (new Choice("Yes"), new Choice("No"), new ChoiceFactoryOptions(", ", " or ", ", or ", true)) },
            };

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfirmPrompt"/> class.
        /// </summary>
        /// <param name="dialogId">The ID to assign to this prompt.</param>
        /// <param name="validator">Optional, a <see cref="PromptValidator{FoundChoice}"/> that contains additional,
        /// custom validation for this prompt.</param>
        /// <param name="defaultLocale">Optional, the default locale used to determine language-specific behavior of the prompt.
        /// The locale is a 2, 3, or 4 character ISO 639 code that represents a language or language family.</param>
        /// <remarks>The value of <paramref name="dialogId"/> must be unique within the
        /// <see cref="DialogSet"/> or <see cref="ComponentDialog"/> to which the prompt is added.
        /// <para>If the <see cref="Activity.Locale"/>
        /// of the <see cref="DialogContext"/>.<see cref="DialogContext.Context"/>.<see cref="ITurnContext.Activity"/>
        /// is specified, then that local is used to determine language specific behavior; otherwise
        /// the <paramref name="defaultLocale"/> is used. US-English is the used if no language or
        /// default locale is available, or if the language or locale is not otherwise supported.</para></remarks>
        public ConfirmPromptNo(string dialogId, PromptValidator<bool> validator = null, string defaultLocale = null)
            : base(dialogId, validator)
        {
            Style = ListStyle.Auto;
            DefaultLocale = defaultLocale;
        }

        /// <summary>
        /// Gets or sets the style of the yes/no choices rendered to the user when prompting.
        /// </summary>
        /// <value>
        /// The style of the yes/no choices rendered to the user when prompting.
        /// </value>
        public ListStyle Style { get; set; }

        /// <summary>
        /// Gets or sets the default locale used to determine language-specific behavior of the prompt.
        /// </summary>
        /// <value>The default locale used to determine language-specific behavior of the prompt.</value>
        public string DefaultLocale { get; set; }

        /// <summary>
        /// Gets or sets additional options passed to the <seealso cref="ChoiceFactory"/>
        /// and used to tweak the style of choices rendered to the user.
        /// </summary>
        /// <value>Additional options for presenting the set of choices.</value>
        public ChoiceFactoryOptions ChoiceOptions { get; set; }

        /// <summary>
        /// Gets or sets the yes and no <see cref="Choice"/> for the prompt.
        /// </summary>
        /// <value>The yes and no <see cref="Choice"/> for the prompt.</value>
        public Tuple<Choice, Choice> ConfirmChoices { get; set; }

        /// <summary>
        /// Prompts the user for input.
        /// </summary>
        /// <param name="turnContext">Context for the current turn of conversation with the user.</param>
        /// <param name="state">Contains state for the current instance of the prompt on the dialog stack.</param>
        /// <param name="options">A prompt options object constructed from the options initially provided
        /// in the call to <see cref="DialogContext.PromptAsync(string, PromptOptions, CancellationToken)"/>.</param>
        /// <param name="isRetry">true if this is the first time this prompt dialog instance
        /// on the stack is prompting the user for input; otherwise, false.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected override async Task OnPromptAsync(ITurnContext turnContext, IDictionary<string, object> state, PromptOptions options, bool isRetry, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            // Format prompt to send
            IMessageActivity prompt;
            var channelId = turnContext.Activity.ChannelId;
            var culture = DetermineCulture(turnContext.Activity);
            var choiceOptionsDefaults = ChoiceDefaults[culture];
            ChoiceFactoryOptions choiceOptions = ChoiceOptions ?? choiceOptionsDefaults.Item3;
            var confirmChoices = ConfirmChoices ?? Tuple.Create(choiceOptionsDefaults.Item1, choiceOptionsDefaults.Item2);
            var choices = new List<Choice> { confirmChoices.Item1, confirmChoices.Item2 };
            if (isRetry && options.RetryPrompt != null)
            {
                prompt = AppendChoices(options.RetryPrompt, channelId, choices, Style, choiceOptions);
            }
            else
            {
                prompt = AppendChoices(options.Prompt, channelId, choices, Style, choiceOptions);
            }

            // Send prompt
            await turnContext.SendActivityAsync(prompt, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Attempts to recognize the user's input.
        /// </summary>
        /// <param name="turnContext">Context for the current turn of conversation with the user.</param>
        /// <param name="state">Contains state for the current instance of the prompt on the dialog stack.</param>
        /// <param name="options">A prompt options object constructed from the options initially provided
        /// in the call to <see cref="DialogContext.PromptAsync(string, PromptOptions, CancellationToken)"/>.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>If the task is successful, the result describes the result of the recognition attempt.</remarks>
        protected override Task<PromptRecognizerResult<bool>> OnRecognizeAsync(ITurnContext turnContext, IDictionary<string, object> state, PromptOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            var result = new PromptRecognizerResult<bool>();
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                // Recognize utterance
                var message = turnContext.Activity.AsMessageActivity();
                var culture = DetermineCulture(turnContext.Activity);
                if (culture == NbNO)
                {
                    switch (message.Text)
                    {
                        case "Ja":
                            result.Succeeded = true;
                            result.Value = true;
                            break;
                        case "Nei":
                            result.Succeeded = true;
                            result.Value = false;
                            break;
                        default:
                            result.Succeeded = false;
                            break;
                    }
                }
                else
                {
                    var results = ChoiceRecognizer.RecognizeBoolean(message.Text, culture, Microsoft.Recognizers.Text.Choice.ChoiceOptions.None);
                    if (results.Count > 0)
                    {
                        var first = results[0];
                        if (bool.TryParse(first.Resolution["value"].ToString(), out var value))
                        {
                            result.Succeeded = true;
                            result.Value = value;
                        }
                    }
                }
            }

            return Task.FromResult(result);
        }

        private string DetermineCulture(Activity activity)
        {
            var culture = activity.Locale ?? DefaultLocale;
            if (string.IsNullOrEmpty(culture) || !ChoiceDefaults.ContainsKey(culture))
            {
                culture = English;
            }

            return culture;
        }
    }
}
