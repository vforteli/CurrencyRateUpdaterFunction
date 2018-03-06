using Flexinets.Common;
using Flexinets.Core.Database.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace CurrencyRateUpdaterFunction
{
    public static class Function1
    {
        [FunctionName("UpdateCurrencyRates")]
        public async static Task Run([TimerTrigger("0 0 12 * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"Updating currency rates at: {DateTime.UtcNow}");

            var currencyRateProvider = new CurrencyRateProvider(new FlexinetsContextFactory(Environment.GetEnvironmentVariable("FlexinetsContext")));

            log.Info("Getting currency rates");
            var rates = await currencyRateProvider.GetCurrencyRates();

            //log.Info("Updating FRP currency rates");
            //await currencyRateProvider.UpdateCurrencyRatesAsync();

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Access-Token", Environment.GetEnvironmentVariable("Fortnox:accesstoken"));
            client.DefaultRequestHeaders.Add("Client-Secret", Environment.GetEnvironmentVariable("Fortnox:clientsecret"));

            foreach (var currency in rates)
            {
                var url = $"https://api.fortnox.se/3/currencies/{currency.Key.ToUpperInvariant()}";
                var json = new
                {
                    Currency = new
                    {
                        Code = currency.Key.ToUpperInvariant(),
                        BuyRate = 1 / currency.Value,
                        SellRate = 1 / currency.Value
                    }
                };

                log.Info($"Setting {currency.Key} rate to {1 / currency.Value}");
                var response = await client.PutAsJsonAsync(url, json);
                log.Info($"Response from {currency.Key.ToUpperInvariant()} update: {response.StatusCode}");
            }
        }
    }
}
