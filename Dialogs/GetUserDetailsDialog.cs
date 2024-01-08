using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace NewDialogBot.Dialogs
{
    public class GetUserDetailsDialog : ComponentDialog
    {
        // Define value names for values tracked inside the dialogs.
        private const string UserInfo = "value-userInfo";

        public class EmailPrompt : TextPrompt
        {
            public EmailPrompt(string dialogId, PromptValidator<string> validator = null)
                : base(dialogId, validator)
            {
            }
        }

        public class PhNoPrompt : NumberPrompt<long>
        {
            public PhNoPrompt(string dialogId, PromptValidator<long> validator = null)
                : base(dialogId, validator)
            {
            }
        }

        public GetUserDetailsDialog() : base(nameof(GetUserDetailsDialog))
        {
            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                NameStepAsync,
                PhoneStepAsync,
                EmailStepAsync,
                ExitStepAsync
                //SummaryStepAsync,
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new EmailPrompt(nameof(EmailPrompt), EmailPromptValidatorAsync));
            AddDialog(new PhNoPrompt(nameof(PhNoPrompt), PhNoPromptValidatorAsync));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values[UserInfo] = new UserProfile();
            return await stepContext.PromptAsync(
                nameof(TextPrompt),
                new PromptOptions { Prompt = MessageFactory.Text("Sure, let's get started. Please provide your name.") },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PhoneStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = (UserProfile)stepContext.Values[UserInfo];
            userProfile.Name = (string)stepContext.Result;
            return await stepContext.PromptAsync(
                nameof(PhNoPrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Great! Now, please provide your phone number."),
                    RetryPrompt = MessageFactory.Text("The phone number must be a valid Indian phone number. Try again."),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> EmailStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = (UserProfile)stepContext.Values[UserInfo];
            userProfile.Phone = (long)stepContext.Result;

            return await stepContext.PromptAsync(
                nameof(EmailPrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Excellent! Finally, please provide your email."),
                    RetryPrompt = MessageFactory.Text("The email you entered is not valid. Try again."),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> ExitStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = (UserProfile)stepContext.Values[UserInfo];
            userProfile.Email = (string)stepContext.Result;
            return await stepContext.EndDialogAsync(stepContext.Values[UserInfo], cancellationToken);
        }
        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = (UserProfile)stepContext.Values[UserInfo];
            userProfile.Email = (string)stepContext.Result;

            // Print summary
            var summaryMessage = $"Thank you! Here is the summary:\n\nName: {userProfile.Name}\n\nEmail: {userProfile.Email}\n\nPhone: {userProfile.Phone}";
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(summaryMessage), cancellationToken);

            // Notify about future connection
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Someone from our side will connect with you soon."), cancellationToken);

            // Exit the dialog, returning the collected user information.
            return await stepContext.EndDialogAsync(stepContext.Values[UserInfo], cancellationToken);
        }

        private Task<bool> PhNoPromptValidatorAsync(PromptValidatorContext<long> promptContext, CancellationToken cancellationToken)
        {
            // Validate if the entered number is a valid Indian phone number
            return Task.FromResult(promptContext.Recognized.Succeeded && promptContext.Recognized.Value > 6000000000 && promptContext.Recognized.Value < 9999999999);
        }

        private Task<bool> EmailPromptValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            //return Task.FromResult(true);
            string emailPattern = @"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$";
            bool isValidEmail = Regex.IsMatch(promptContext.Recognized.Value, emailPattern);

            return Task.FromResult(isValidEmail);
        }
    }
}
