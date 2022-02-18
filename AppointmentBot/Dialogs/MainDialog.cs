// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.15.0

using AppointmentBot.CognitiveModels;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AppointmentBot.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly AppointmentBookingRecognizer _luisRecognizer;
        protected readonly ILogger Logger;

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(AppointmentBookingRecognizer luisRecognizer, AppointmentBookingDialog appointmentDialog, ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {
            _luisRecognizer = luisRecognizer;
            Logger = logger;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(appointmentDialog);
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_luisRecognizer.IsConfigured)
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("NOTE: LUIS is not configured. To enable all capabilities, add 'LuisAppId', 'LuisAPIKey' and 'LuisAPIHostName' to the appsettings.json file.", inputHint: InputHints.IgnoringInput), cancellationToken);

                return await stepContext.NextAsync(null, cancellationToken);
            }

            // Use the text provided in FinalStepAsync or the default if it is the first time.
            var messageText = stepContext.Options?.ToString() ?? "How can I help you with today?";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_luisRecognizer.IsConfigured)
            {
                // LUIS is not configured, we just run the BookingDialog path with an empty BookingDetailsInstance.
                return await stepContext.BeginDialogAsync(nameof(AppointmentBookingDialog), new AppointmentDetails(), cancellationToken);
            }

            // Call LUIS and gather any potential booking details. (Note the TurnContext has the response to the prompt.)
            var luisResult = await _luisRecognizer.RecognizeAsync<DoctorBooking>(stepContext.Context, cancellationToken);
            switch (luisResult.TopIntent().intent)
            {
                case DoctorBooking.Intent.BookAppointment:
                    var validDoctor = await ValidateDoctors(stepContext.Context, luisResult, cancellationToken);
                    if (!validDoctor)
                    {
                        return await stepContext.ReplaceDialogAsync(InitialDialogId, "Doctor Peter, Susan and Kathy are available?", cancellationToken);
                    }

                    // Initialize BookingDetails with any entities we may have found in the response.
                    var bookingDetails = new AppointmentDetails()
                    {
                        // Get destination and origin from the composite entities arrays.
                        Doctor = luisResult.Doctor,
                        AppointmenDate = luisResult.AppointmentDate,
                    };

                    // Run the AppointmentBookingDialog giving it whatever details we have from the LUIS call, it will fill out the remainder.
                    return await stepContext.BeginDialogAsync(nameof(AppointmentBookingDialog), bookingDetails, cancellationToken);

                case DoctorBooking.Intent.GetAvailableDoctors:
                    // We haven't implemented the GetAvailableDoctorsDialog so we just display a mock message.
                    var getAvailableDoctorsMessageText = "Doctor Kathy, Doctor Peter are available today";
                    var getAvailableDoctorsMessage = MessageFactory.Text(getAvailableDoctorsMessageText, getAvailableDoctorsMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(getAvailableDoctorsMessage, cancellationToken);
                    break;

                default:
                    // Catch all for unhandled intents
                    var didntUnderstandMessageText = $"Sorry, I didn't get that. Please try asking in a different way (intent was {luisResult.TopIntent().intent})";
                    var didntUnderstandMessage = MessageFactory.Text(didntUnderstandMessageText, didntUnderstandMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);
                    break;
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        // Shows a warning if the doctor is not specified or doctor entity values can't be mapped to a canonical item in the Airport.
        private static async Task<Boolean> ValidateDoctors(ITurnContext context, DoctorBooking luisResult, CancellationToken cancellationToken)
        {
            var doctorChoosen = luisResult.Doctor;
            var noDoctor = string.IsNullOrEmpty(doctorChoosen);

            if (noDoctor)
            {
                var messageText = "Please choose a doctor";
                var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                await context.SendActivityAsync(message, cancellationToken);
            }
            return !noDoctor;
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // If the child dialog ("AppointmentBookingDialog") was cancelled, the user failed to confirm or if the intent wasn't appointment Booking
            // the Result here will be null.
            if (stepContext.Result is AppointmentDetails result)
            {
                // Now we have all the booking details call the booking service.

                // If the call to the booking service was successful tell the user.

                var timeProperty = new TimexProperty(result.AppointmenDate);
                var appointmentDateMsg = timeProperty.ToNaturalLanguage(DateTime.Now);
                var messageText = $"I have you booked to Doctor {result.Doctor} on {appointmentDateMsg}";
                var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(message, cancellationToken);
            }

            // Restart the main dialog with a different message the second time around
            var promptMessage = "What else can I do for you?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }
    }
}
