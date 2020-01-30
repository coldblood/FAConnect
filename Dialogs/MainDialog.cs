﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreBot;
using Luis;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Newtonsoft.Json;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly FAConnectIntentRecognizer _luisRecognizer;
        private readonly IHostingEnvironment _hostingEnvironment;
        
        protected readonly ILogger Logger;

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(FAConnectIntentRecognizer luisRecognizer, BookingDialog bookingDialog,  IHostingEnvironment hostingEnvironment, ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {
            _luisRecognizer = luisRecognizer;
            Logger = logger;
            _hostingEnvironment = hostingEnvironment;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            // AddDialog(bookingDialog);
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                HandleFAConnect
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
            var messageText = stepContext.Options?.ToString() ?? "What can I help you with today?\nSay something like \"Book a flight from Paris to Berlin on March 22, 2020\"";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async void SendPlanDetails(IEnumerable<Plan> plans, WaterfallStepContext context, CancellationToken cancellationToken) {
            var response = "";
            foreach(var plan in plans)
            {
                response = $"{plan.name} is {plan.status}";
                await context.Context.SendActivityAsync(response, response, "", cancellationToken);                        
            }
        }

        private async void SendAlertDetails(IEnumerable<Alert> alerts, IEnumerable<Client> clients, WaterfallStepContext context, CancellationToken cancellationToken) {
            var response = "";
            foreach(var alert in alerts)
            {
                var client = clients.FirstOrDefault(x => x.id == alert.clientId);
                response = $"For {client.firstName}, {alert.message}.";
                await context.Context.SendActivityAsync(response, response, "", cancellationToken);                        
            }
        }
        private async Task<DialogTurnResult> HandleFAConnect(WaterfallStepContext context, CancellationToken cancellationToken) {
            var fa = JsonConvert.DeserializeObject<FA>(File.ReadAllText(_hostingEnvironment.ContentRootPath + @"\FA.json"));
            // Call LUIS and gather any potential booking details. (Note the TurnContext has the response to the prompt.)
            var luisResult = await _luisRecognizer.RecognizeAsync<FAConnect>(context.Context, cancellationToken);
            string response = "";
            switch (luisResult.TopIntent().intent)
            {
                case FAConnect.Intent.None:
                    response = $"Sorry I didn't get that";
                    await context.Context.SendActivityAsync(response, response, "", cancellationToken);
                    break;
                case FAConnect.Intent.Summary:
                    response = $"The average portfolio value of the {fa.clients.Count()} clients you are managing is {fa.summary.avgPortfolio}.";
                    await context.Context.SendActivityAsync(response, response, "", cancellationToken);
                    response = $"{fa.summary.planOnTrack} plans are on track. {fa.summary.planOffTrack} plans are off track.";
                    await context.Context.SendActivityAsync(response, response, "", cancellationToken);
                    response = $"You have {fa.summary.criticalAlerts} critical alerts.";
                    await context.Context.SendActivityAsync(response, response, "", cancellationToken);
                    break;
                case FAConnect.Intent.Details:
                    var clients = fa.clients;
                    Client client = null;
                    if(luisResult.Entities.personName != null && luisResult.Entities.personName[0] != null) {
                        client = clients.FirstOrDefault(x => x.firstName.ToLower() == luisResult.Entities.personName[0].ToLower());
                    }
                    if(luisResult.Entities != null & luisResult.Entities.criticalalerts != null) {
                        SendAlertDetails(fa.alerts, clients, context, cancellationToken);
                    }
                    if(clients != null && client != null && clients.Length >= 0)
                    {
                        if(luisResult.Entities.status != null) {
                            var status = luisResult.Entities.status;
                            var plans = client.plans.Where(x => x.status.ToLower() == status[0][0]);
                            SendPlanDetails(plans, context, cancellationToken);
                        } else {
                            response = $"{client.firstName} has {client.plans.Count()} plans.";
                            await context.Context.SendActivityAsync(response, response, "", cancellationToken);
                            SendPlanDetails(client.plans, context, cancellationToken);
                        }
                    } else if(client == null && luisResult.Entities.personName != null){
                        response = $"Sorry you don't have any clients with the name {luisResult.Entities.personName[0]}";
                        await context.Context.SendActivityAsync(response, response, "", cancellationToken);
                    }
                    break;
            }
            return await context.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_luisRecognizer.IsConfigured)
            {
                // LUIS is not configured, we just run the BookingDialog path with an empty BookingDetailsInstance.
                return await stepContext.BeginDialogAsync(nameof(BookingDialog), new BookingDetails(), cancellationToken);
            }

            // Call LUIS and gather any potential booking details. (Note the TurnContext has the response to the prompt.)
            var luisResult = await _luisRecognizer.RecognizeAsync<FlightBooking>(stepContext.Context, cancellationToken);
            switch (luisResult.TopIntent().intent)
            {
                case FlightBooking.Intent.BookFlight:
                    await ShowWarningForUnsupportedCities(stepContext.Context, luisResult, cancellationToken);

                    // Initialize BookingDetails with any entities we may have found in the response.
                    var bookingDetails = new BookingDetails()
                    {
                        // Get destination and origin from the composite entities arrays.
                        Destination = luisResult.ToEntities.Airport,
                        Origin = luisResult.FromEntities.Airport,
                        TravelDate = luisResult.TravelDate,
                    };

                    // Run the BookingDialog giving it whatever details we have from the LUIS call, it will fill out the remainder.
                    return await stepContext.BeginDialogAsync(nameof(BookingDialog), bookingDetails, cancellationToken);

                case FlightBooking.Intent.GetWeather:
                    // We haven't implemented the GetWeatherDialog so we just display a TODO message.
                    var getWeatherMessageText = "TODO: get weather flow here";
                    var getWeatherMessage = MessageFactory.Text(getWeatherMessageText, getWeatherMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(getWeatherMessage, cancellationToken);
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

        // Shows a warning if the requested From or To cities are recognized as entities but they are not in the Airport entity list.
        // In some cases LUIS will recognize the From and To composite entities as a valid cities but the From and To Airport values
        // will be empty if those entity values can't be mapped to a canonical item in the Airport.
        private static async Task ShowWarningForUnsupportedCities(ITurnContext context, FlightBooking luisResult, CancellationToken cancellationToken)
        {
            var unsupportedCities = new List<string>();

            var fromEntities = luisResult.FromEntities;
            if (!string.IsNullOrEmpty(fromEntities.From) && string.IsNullOrEmpty(fromEntities.Airport))
            {
                unsupportedCities.Add(fromEntities.From);
            }

            var toEntities = luisResult.ToEntities;
            if (!string.IsNullOrEmpty(toEntities.To) && string.IsNullOrEmpty(toEntities.Airport))
            {
                unsupportedCities.Add(toEntities.To);
            }

            if (unsupportedCities.Any())
            {
                var messageText = $"Sorry but the following airports are not supported: {string.Join(',', unsupportedCities)}";
                var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                await context.SendActivityAsync(message, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // If the child dialog ("BookingDialog") was cancelled, the user failed to confirm or if the intent wasn't BookFlight
            // the Result here will be null.
            if (stepContext.Result is BookingDetails result)
            {
                // Now we have all the booking details call the booking service.

                // If the call to the booking service was successful tell the user.

                var timeProperty = new TimexProperty(result.TravelDate);
                var travelDateMsg = timeProperty.ToNaturalLanguage(DateTime.Now);
                var messageText = $"I have you booked to {result.Destination} from {result.Origin} on {travelDateMsg}";
                var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(message, cancellationToken);
            }

            // Restart the main dialog with a different message the second time around
            var promptMessage = "What else can I do for you?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }
    }
}
