using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using NewDialogBot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NewDialogBot.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly IStatePropertyAccessor<UserProfile> _userState;
        private readonly JObject responses;

        public MainDialog(UserState userState)
            : base(nameof(MainDialog))
        {
            _userState = userState.CreateProperty<UserProfile>("UserProfile");

            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                WelcomeStepAsync,
                //HelpStartAsync,
                FinalStepAsync
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            AddDialog(new GetUserDetailsDialog());

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> WelcomeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var intentsJson = File.ReadAllText("Resources/intents.json");
            var intents = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(intentsJson);

            // Process user input
            var userInput = stepContext.Context.Activity.Text.ToLower();

            switch (userInput)
            {
                case var hiHello when intents["greetings"].Contains(hiHello):
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Hello! If you need help, please type 'help'."), cancellationToken);
                    break;

                case var whoAreYou when intents["whoAreYou"].Contains(whoAreYou):
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I am a bot. Please type 'help'."), cancellationToken);
                    break;

                case var helpIntent when intents["help"].Contains(helpIntent):
                    // Begin the GetUserDetailsDialog
                    return await stepContext.BeginDialogAsync(nameof(GetUserDetailsDialog), null, cancellationToken);

                default:
                    // Display a general message
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"If you need help, please type 'help'."), cancellationToken);
                    break;
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }


/*        private async Task<DialogTurnResult> HelpStartAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.BeginDialogAsync(nameof(GetUserDetailsDialog), null, cancellationToken);
        }*/

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userInfo = (UserProfile)stepContext.Result;
            if (userInfo != null)
            {
                var summaryMessage = $"TThank you! Here is the summary:\n\nName: {userInfo.Name}\n\nEmail: {userInfo.Email}\n\nPhone: {userInfo.Phone}";
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(summaryMessage), cancellationToken);

                // Notify about future connection
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Someone from our side will connect with you soon."), cancellationToken);
            }

            //return await stepContext.ReplaceDialogAsync(nameof(MainDialog), null, cancellationToken);

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }


    }
}
