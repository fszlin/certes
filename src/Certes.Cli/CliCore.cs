using System;
using System.CommandLine;
using System.Threading.Tasks;
using Certes.Cli.Options;
using Certes.Cli.Processors;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NLog;

namespace Certes.Cli
{
    public class CliCore
    {
        private readonly ILogger consoleLogger = LogManager.GetLogger(nameof(CliCore));

        private JsonSerializerSettings jsonSettings;
        private AccountOptions accountOptions;
        private OrderOptions orderOptions;
        private AzureOptions azureOptions;

        public async Task<bool> Process(string[] args)
        {
            try
            {
                ArgumentSyntax.Parse(args, syntax =>
                {
                    syntax.ApplicationName = "certes";
                    syntax.HandleErrors = false;

                    accountOptions = AccountCommand.TryParse(syntax);
                    orderOptions = OrderCommand.TryParse(syntax);
                    azureOptions = AzureCommand.TryParse(syntax);

                });
            }
            catch (ArgumentSyntaxException ex)
            {
                consoleLogger.Error(ex, ex.Message);
                return false;
            }

            jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            };

            jsonSettings.Converters.Add(new StringEnumConverter());

            try
            {
                if (accountOptions != null)
                {
                    var cmd = new AccountCommand(accountOptions);
                    var result = await cmd.Process();
                    consoleLogger.Info(JsonConvert.SerializeObject(result, Formatting.Indented, jsonSettings));
                    return true;
                }

                if (orderOptions != null)
                {
                    var cmd = new OrderCommand(orderOptions);
                    var result = await cmd.Process();
                    consoleLogger.Info(JsonConvert.SerializeObject(result, Formatting.Indented, jsonSettings));
                    return true;
                }

                if (azureOptions != null)
                {
                    var cmd = new AzureCommand(azureOptions);
                    var result = await cmd.Process();
                    consoleLogger.Info(JsonConvert.SerializeObject(result, Formatting.Indented, jsonSettings));
                    return true;
                }

                return false;
            }
            catch (AggregateException ex)
            {
                foreach (var err in ex.InnerExceptions)
                {
                    consoleLogger.Error(err, err.Message);
                }
            }
            catch (Exception ex)
            {
                consoleLogger.Error(ex, ex.Message);
            }

            return false;
        }
    }
}
