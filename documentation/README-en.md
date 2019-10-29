# Encyclopedia Bot

*THIS ARTICLE WAS ORIGINALLY WRITTEN IN NORWEGIAN. FOR THE NORWEGIAN VERSION PLEASE VISIT [ahysing / encyclopediabot-demo](http://github.com/ahysing/encyclopediabot-demo)*

This software is a chatbot. It enables the user to search and browse the norwegian encyclopedia [Store Norske Leksikon](https://snl.no/). The program is made for educational purposes. By following this tutorial you will learn to use [Microsoft LUIS](https://www.luis.ai/home) and [Microsoft Bot Framework](https://dev.botframework.com/)
The tutorial shoud tabe between one and two hours.
When you finish this tutorial you will have made your first chatbot and have testet it in the Microsoft Chat Bot Emulator.

## Prerequisites

Le's start developing. But some things must be in place before we can start. Please install the software listed below

- [.NET Core SDK](https://dotnet.microsoft.com/download) version 2.1

Test the install with the following command in the terminal (command prompt)

  ```bash
  # bestem dotnet versjon
  dotnet --version
  ```

- [git](https://www.git-scm.com/)
- [Microsoft Visual Studio 2019](https://visualstudio.microsoft.com/vs/)

## Steps

Before we start developing install [Bot Framework Emulator 4.5.2](https://github.com/microsoft/BotFramework-Emulator/releases/tag/v4.5.2). Follow the recepie on [Getting Started](https://github.com/Microsoft/BotFramework-Emulator/wiki/Getting-Started). Microsoft are refering to **ngrok** during the installation process. For our purpose we don't need this featur. Therfore it is not recommended to intall ngrok.
After the install please set up the Bot Emulator - Settings as listed in the table below.

| Option                            | Value     |
|---------------------------------------|-----------|
| Path to ngrok                         |           |
| Bypass ngrok for local addresses      | YES       |
| Run ngrok when the emulator starts up | NO        |
| localhost override                    | localhost |
| Locale                                | nb-NO     |

![Bot framework settings](/documentation/bot%20framework%20settings.png)

### Import the LUIS-model

Download the LUIS model from  [EncyclopediaBot.json](https://github.com/vippsas/encyclopediabot-demo/tree/master/LUIS/EncyclopediaBot.json). Upload this model on [eu.luis.ai](https://eu.luis.ai): Log in with your microsoft account and uplodad the model from a JSON-file. Use the Import New button in the menus. (see picture below)
![LUIS Import App](/documentation/import-LUIS-model.png

- In the terminal git clone [Microsoft BotBuilder Samples](https://github.com/microsoft/BotBuilder-Samples)

    ```bash
    git clone https://github.com/Microsoft/botbuilder-samples.git
    ```

- From the new folder git created Navigate to `samples/csharp_dotnetcore/05.multi-turn-prompt`
- Copy tthe files from this folder to a new folder. Rename this new folder as `EncyclopediaBot`.
- Navigate to this new folder EncyclopediaBot. Run the bot from the terminal or from inside Visual Studio. Choose alternative A or B

  A) From the terminal

  ```bash
  # run the bot
  dotnet run
  ```

  B) Or from Visual Studio

  - Start Visual Studio
  - File -> Open -> Project/Solution
  - Navigate to folder  `EncyclopediaBot`
  - Choose the  `MultiTurnPromptBot.csproj`-file
  - Press `F5`  to run the project
  
### Test the project in Bot Framework Emulator

- Start Bot Framework Emulator
- File -> Open Bot
- Input connection URL `http://localhost:3978/api/messages`

Si "hei" to the bot, and at will start asking you questions about transport.
![startprosjektet i simulatoren](/documentation/v0.png)
Close the emulator when you are done.

## Oppdater Prosjektet

1. If `MultiTurnPromptBot.sln` allready exists skip past this step.Open MultiTurnPromptBot.csproj i Visual Studio og and run the project (by pressing F5 on the keyboard). A brand new Visual Studio Solution will be created right next to MultiTurnPromptBot.csproj with name `MultiTurnPromptBot.sln`. Close Visual Studio.
2. Change name of the Visual Studio Solution-file from  `MultiTurnPromptBot.sln` to `EncyclopediaBot.sln`.
3. Move all files and folders into a new folder `EncyclopediaBot.Web` that you create. Change the name of file MultiTurnPromptBot.csproj til `EncyclopediaBot.Web.csproj`.
4. Open `EncyclopediaBot.sln` igjen i Visual Studio. Visual Studio did not notice that the project file changed name to EncyclopediaBot.Web.csproj. so we have to import the project file again. Delete the existing project in the Solution Explorer Pane by right-clicking on it's name and choosing **delete**.
5. In the Solution Explorer Pane right click the Solution `EncyclopediaBot` og choose Add > Add Existing Project... Velg `EncyclopediaBot.Web.csproj` in the dialog which appears.
6. Search and replace (by hitting Ctrl+Shift+F) **namespace Microsoft.BotBuilderSamples** with **namespace EncyclopediaBot.Web**
7. In the Solution Explorer Pane right click the Solution `EncyclopediaBot` and choose Add > Add New Project... In the setup wizard which follows choose Class Library .NET Core og .NET Runtime 2.1. Provide the name `EncyclopediaBot.Logic` when prompted for the new project name.
8.  In the Solution Explorer Pane , under Project  `EncyclopediaBot.Web` node right click *Dependencies*, and add a refrence to `EncyclopediaBot.Logic`.
9. As we did with *BotBuilder-samples* Git clone (https://github.com/vippsas/encyclopediabot-demo) . In the new folder you get open folder EncyclopediaBot.Logic. The files should be the same as [EncyclopediaBot.Logic](https://github.com/vippsas/encyclopediabot-demo/tree/master/EncyclopediaBot.Logic). Copy and paste all the content in this folder onto your project with the same name in the Solution Pane.
10. Right click Project `EncyclopediaBot.Logic` and choose Manage Nuget Packages... This brings us to the Nuget package explorer. In this explorer there is a search box in the right hand corner. In the search box Searech for Newtonsoft.Json version **12.0.1**, Microsoft.Bot.Builder.AI.Luis version XXX, System.Reflection versjon **4.3.0**.
11. Back the solution explorer find project EncyclopediaBot.Web. Right click and og add a new folder `Dialogs`. Copy and paste the foles from [github Dialogs/](https://github.com/vippsas/encyclopediabot-demo/tree/master/EncyclopediaBot.Web/Dialogs) into your new folder (These files are all in subfolders of the code you just cloned in step 9.
12. Back the solution explorer find Project EncyclopediaBot.Web and add a new folder `Model` and copy and paste the files from [github Model/](https://github.com/vippsas/encyclopediabot-demo/tree/master/EncyclopediaBot.Web/Model) into your new folder.
13. Open the class **DialogBot.cs** and insert the code below right under the constructor (at [line 32](https://github.com/microsoft/BotBuilder-Samples/blob/master/samples/csharp_dotnetcore/05.multi-turn-prompt/Bots/DialogBot.cs#L32))

```csharp

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
            string whatPlaceGreeting = "Fortell meg om en viktig mur i Tyskland.";
            turnContext.SendActivityAsync(MessageFactory.Text(whatPlaceGreeting, whatPlaceGreeting, InputHints.AcceptingInput), cancellationToken);

            return base.OnMembersAddedAsync(membersAdded, turnContext, cancellationToken);
        }

        #endregion
```

This code presents our users with nice welcome message as he or she logs in to the channel; Hei. Jeg er leksikonbot. Jeg kan svare på spørsmål om kjente personer, steder eller historiske hendelser....
14. In class **DialogBot.cs** replace the method **OnMessageActivityAsync** with.

```csharp
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
```

This allows LUIS to recognise all messages from the user as an intent with, or without keywords.

15. Open **Startup.cs**. Add a new constructor.

```csharp
#region custom
        private readonly IConfiguration Configuration;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
#endregion
```

This way we can read configuration values right out of **settings.json** upon start up.

In the bottom of method **ConfigureServices** put

```csharp
            services.AddSingleton<Dialogs.Search.SearchDialog>();
            // configuration for LUIS
            string APIkey = Configuration["LuisAPIKey"];
            string appId = Configuration["LuisAppId"];
            string hostname = Configuration["LuisAPIHostname"];
            services.AddSingleton((serviceProvider) => {
                return new LuisSetup
                {
                    APIKey = APIkey,
                    AppId = appId,
                    Hostname = hostname
                };
            });
            services.AddTransient<DefinitionManager>();
            services.AddSingleton<Logic.ILogger, WebLogger>((serviceProvider) =>
            {
                var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Startup>>();
                var webLogger = new WebLogger(logger);
                return webLogger;
            });
```

Luis ha a concept of a Subscription keys. The snippet above reads those from settings.json when the application starts.

16. Open **settings.json**. Add three new values. These values are secret, and must be fetched from inside your bot att [https://eu.luis.ai/](https://eu.luis.ai/) .
17. Copy [BotHelper.cs](https://github.com/vippsas/encyclopediabot-demo/blob/master/EncyclopediaBot.Web/Bots/BotHelper.cs) into folder Bots i Encyclopedia.Web.
18. Start the project in VIsual Studio by pressing `F5`. [Add a Break Point or two](https://docs.microsoft.com/en-us/visualstudio/debugger/using-breakpoints?view=vs-2019) to the snippets you just added, and take your bot for a test run in the emulator.

Did you notice what happened? Every time you enter a conversation with your bot (via a class SearchDialog.cs), and a question appears from class [ChoicePrompt](https://docs.microsoft.com/en-us/javascript/api/botbuilder-dialogs/choiceprompt?view=botbuilder-ts-latest) the conversation comes to a halt. You are brought back to the beginning. This is because all messages pass through [OnMessageActivityAsync](https://docs.microsoft.com/en-us/dotnet/api/microsoft.bot.builder.activityhandler.onmessageactivityasync?view=botbuilder-dotnet-stable). The new implementeation, with LUS, we added at step 14 decides what conversation we are running. The result is that all conversations "terminates".

The solution is to put all dialogs in a  [DialogSet](https://docs.microsoft.com/en-us/dotnet/api/microsoft.bot.builder.dialogs.dialogset?view=botbuilder-dotnet-stable). Then from OnMessageAcitivityAsync all dialoges must be continued before we can create a new one. See method [RunAsyncWithLUISDispatcher](https://github.com/vippsas/encyclopediabot-demo/blob/dc4e75018f009885d85a566107d1ee5ca54a75a9/EncyclopediaBot.Web/Bots/DialogBot.cs#L33), and it will show you

```csharp
            var results = await dialogContext.ContinueDialogAsync(cancellationToken).ConfigureAwait(false);
            if (results.Status == DialogTurnStatus.Empty)
            {
                await dialogContext.BeginDialogAsync(dialog.Id, null, cancellationToken).ConfigureAwait(false);
            }
```

19. Now we fix this error. Replace DialogBot.cs with [github DialogBot.cs](https://github.com/vippsas/encyclopediabot-demo/blob/master/EncyclopediaBot.Web/Bots/DialogBot.cs). **TODO**

Your program is now finished. Give it a test run in the emulator and ask it questions in norwegian about all the articles.

Examples are

* Definer Algebra.
* Hvem er Bill Gates?

![A finished bot](/documentation/final.png)
