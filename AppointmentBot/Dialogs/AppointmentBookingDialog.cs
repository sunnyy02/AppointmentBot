// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.15.0

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System.Threading;
using System.Threading.Tasks;

namespace AppointmentBot.Dialogs
{
    public class AppointmentBookingDialog : CancelAndHelpDialog
    {
        private const string DoctorStepMsgText = "Who would you like to see?";

        public AppointmentBookingDialog()
            : base(nameof(AppointmentBookingDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                DoctorStepAsync,
                AppointmentDateStepAsync,
                ConfirmStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult>DoctorStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var bookingDetails = (AppointmentDetails)stepContext.Options;

            if (bookingDetails.Doctor == null)
            {
                var promptMessage = MessageFactory.Text(DoctorStepMsgText, DoctorStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(bookingDetails.Doctor, cancellationToken);
        }

        private async Task<DialogTurnResult> AppointmentDateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var bookingDetails = (AppointmentDetails)stepContext.Options;

            bookingDetails.AppointmenDate = (string)stepContext.Result;

            if (bookingDetails.AppointmenDate == null || IsAmbiguous(bookingDetails.AppointmenDate))
            {
                return await stepContext.BeginDialogAsync(nameof(DateResolverDialog), bookingDetails.AppointmenDate, cancellationToken);
            }

            return await stepContext.NextAsync(bookingDetails.AppointmenDate, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var bookingDetails = (AppointmentDetails)stepContext.Options;

            bookingDetails.AppointmenDate = (string)stepContext.Result;

            var messageText = $"Please confirm, I have you book with Doctor: {bookingDetails.Doctor} on: {bookingDetails.AppointmenDate}. Is this correct?";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                var bookingDetails = (AppointmentDetails)stepContext.Options;

                return await stepContext.EndDialogAsync(bookingDetails, cancellationToken);
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private static bool IsAmbiguous(string timex)
        {
            var timexProperty = new TimexProperty(timex);
            return !timexProperty.Types.Contains(Constants.TimexTypes.Definite);
        }
    }
}
