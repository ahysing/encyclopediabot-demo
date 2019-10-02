# Leksikon Bot

Dette programmet er en chatbot. Programmet lar brukeren søke og bla i [Store Norske Leksikon](https://snl.no/). Programmet er laget for opplæring og undervisning. Ved å følge oppskriften under lærer du [Microsoft LUIS](https://www.luis.ai/home) og [Microsoft Bot Framework](https://dev.botframework.com/)
Oppskriften er ment å ta fra 1 til 2 timer.
Etter oppskriften har du laget din første chatbot og testet den i botsimulatoren.

## Forutsetninger

Noen ting må være på plass din PC eller Mac før vi kan begynne

- [.NET Core SDK](https://dotnet.microsoft.com/download) version 2.1

  ```bash
  # bestem dotnet versjon
  dotnet --version
  ```

- [git](https://www.git-scm.com/)
- [Microsoft Visual Studio 2019](https://visualstudio.microsoft.com/vs/)

## Oppskrift

Før vi starter installer [Bot Framework Emulator 4.5.2](https://github.com/microsoft/BotFramework-Emulator/releases/tag/v4.5.2) etter oppskriften [Getting Started](https://github.com/Microsoft/BotFramework-Emulator/wiki/Getting-Started).
For å komme i gang med bot framework emulator trenger du programmet **ngrok**. Installasjon av ngrok varierer på forskjellige systemer.
For MacOS gjør som i skriptet under. Installasjonen krever [homebrew](https://brew.sh)

```bash
username$ brew cask install ngrok
==> Satisfying dependencies
==> Downloading https://bin.equinox.io/c/4VmDzA7iaHb/ngrok-stable-darwin-amd64.zip
######################################################################## 100.0%
==> No SHA-256 checksum defined for Cask 'ngrok', skipping verification.
==> Installing Cask ngrok
==> Linking Binary 'ngrok' to '/usr/local/bin/ngrok'.
🍺  ngrok was successfully installed!
username$ which ngrok
/usr/local/bin/ngrok
```

- I terminalen git klon [Microsoft BotBuilder Samples](https://github.com/microsoft/BotBuilder-Samples)

    ```bash
    git clone https://github.com/Microsoft/botbuilder-samples.git
    ```

- naviger til `samples/csharp_dotnetcore/05.multi-turn-prompt`
- kopier filene i denne mappen til en ny mappe. Kall denne mappen `EncyclopediaBot`.
- Naviger til denne nye mappen EncyclopediaBot. Kjør boten fra terminalen eller fra Visual Studio. Velg alternativ A eller B.

  A) From terminalen

  ```bash
  # run the bot
  dotnet run
  ```

  B) Eller fra Visual Studio

  - Kjør Visual Studio
  - File -> Open -> Project/Solution
  - Naviger til `EncyclopediaBot` mappen
  - Velg `MultiTurnPromptBot.csproj` filen
  - Trykk `F5` for å kjøre prosjektet

## Test prosjektet i Bot Framework Emulator

- Åpne Bot Framework Emulator
- File -> Open Bot
- Oppgi Bot URL `http://localhost:3978/api/messages`

Si "hei" til boten, og den vil begynne å stille deg spørsmål om transport.
![startprosjektet i simulatoren](/documentation/v0.png)
Lukk simulatoren når du er ferdig.

## Oppdater Prosjektet

1. Hvis `MultiTurnPromptBot.sln` finnes i allerede hopp over dette steget. Åpne MultiTurnPromptBot.csproj i Visual Studio og kjør prosjektet. En ny Visual Studio Solution dukker opp i mappen med davn.
2. Bytt navn på Visual Studio Solution-filen fra `MultiTurnPromptBot.sln` til `EncyclopediaBot.sln`.
3. Flytt alle andre filer og mapper til til en ny mappe `EncyclopediaBot.Web` som du lager. Bytt navn på MultiTurnPromptBot.csproj til `EncyclopediaBot.Web.sln`.
4. Åpne `EncyclopediaBot.sln` igjen i Visual Studio. Slett det ekstistrende prosjektet i solution explorer ved å høyreklikke på navnet og velge **delete**.
5. Høyreklikk på Solution `EncyclopediaBot` og velg Add > Add Existing Project... Velg `EncyclopediaBot.Web.sln` dialogen som dukker opp.
6. Søk og erstatt **namespace Microsoft.BotBuilderSamples** med **namespace EncyclopediaBot.Web**
7. Høyreklikk på Solution `EncyclopediaBot` og velg Add > Add New Project... Velg prosjekt type Class Library .NET Core og .NET Runtime 2.1. Oppgi navnet `EncyclopediaBot.Logic`.
8. Høyreklikk Prosjektet `EncyclopediaBot.Web` node Dependencies og legg til en referanse til `EncyclopediaBot.Logic`.
9. Åpne EncyclopediaBot.Logic. Slipp inn filene i [EncyclopediaBot.Logic](https://github.com/vippsas/encyclopediabot-demo/tree/master/EncyclopediaBot.Logic) inn i prosjektet.
10. Høyreklikk Prosjektet `EncyclopediaBot.Logic` og velg Manage Nuget Packages... Søk opp pakken Newtonsoft.Json versjon **12.0.1**, Microsoft.Bot.Builder.AI.Luis versjon XXX, System.Reflection versjon **4.3.0**.
11. Åpne EncyclopediaBot.Web og legg til en ny mappe `Dialogs` og slipp filene koden fra [github Dialogs/](https://github.com/vippsas/encyclopediabot-demo/tree/master/EncyclopediaBot.Web/Dialogs) inn i din nye mappe.
12. Åpne EncyclopediaBot.Web og legg til en ny mappe `Model` og slipp filene koden fra [github Model/](https://github.com/vippsas/encyclopediabot-demo/tree/master/EncyclopediaBot.Web/Model) inn i din nye mappe.
13. Åpne klassen **DialogBot.cs** og putt inn snutten rett under konstruktøren.

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
```

14. I klassen **DialogBot.cs** og bytt ut innholdet i metoden **OnMessageActivityAsync** med.

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

15. Åpne filen **Startup.cs**. Legg til konstruktøren under

```csharp
#region custom
        private readonly IConfiguration Configuration;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
#endregion
```

I bunn av metoden **ConfigureServices** putt inn

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

16. Åpne filen **settings.json** og tre nye verdier. Disse er sensitiv informasjon, må hentes fra [https://eu.luis.ai/](https://eu.luis.ai/) .
17. Kopier filen [BotHelper.cs](https://github.com/vippsas/encyclopediabot-demo/blob/master/EncyclopediaBot.Web/Bots/BotHelper.cs) inn i mappen Bots i Encyclopedia.Web.
18. Start prosjektet i Visual Studio med `F5`. Debug prosjektet i visual studio og observer hva som skjer.

Så du det? Hver gang du kommer til en Dialog, og spørsmål [ChoicePrompt](https://docs.microsoft.com/en-us/javascript/api/botbuilder-dialogs/choiceprompt?view=botbuilder-ts-latest) i **UserProfileDialog.cs** eller **SearchDialog.cs** dukker opp, så slutter samtalen. Årsaken er at alle meldinger går gjennom [OnMessageActivityAsync](https://docs.microsoft.com/en-us/dotnet/api/microsoft.bot.builder.activityhandler.onmessageactivityasync?view=botbuilder-dotnet-stable). Den nye LUIS-implementasjonen vi laget i steg 14 bestemmer hvilken dialog som styrer samtalen. Oppførselen føles som samtalen "faller ut".

Løsningen er å putte alle dialoger i et [DialogSet](https://docs.microsoft.com/en-us/dotnet/api/microsoft.bot.builder.dialogs.dialogset?view=botbuilder-dotnet-stable). Etterpå må alltid dialogen fortsettes fra OnMessageAcitivityAsync før en ny startes. Se metoden [RunAsyncWithLUISDispatcher](https://github.com/vippsas/encyclopediabot-demo/blob/dc4e75018f009885d85a566107d1ee5ca54a75a9/EncyclopediaBot.Web/Bots/DialogBot.cs#L33), og du vil se

```csharp
            var results = await dialogContext.ContinueDialogAsync(cancellationToken).ConfigureAwait(false);
            if (results.Status == DialogTurnStatus.Empty)
            {
                await dialogContext.BeginDialogAsync(dialog.Id, null, cancellationToken).ConfigureAwait(false);
            }
```

19. Nå retter vi feilen. Bytt ut din DialogBot.cs med [github DialogBot.cs](https://github.com/vippsas/encyclopediabot-demo/blob/master/EncyclopediaBot.Web/Bots/DialogBot.cs).

Programmet er nå ferdig. Kjører du det i simulatoren en gang til kan du stille spørsmål om alle artiklene.

![En ferdig bot](/documentation/final.png)
