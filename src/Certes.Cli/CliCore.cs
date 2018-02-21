﻿using System;
using System.CommandLine;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Certes.Cli.Commands;
using Certes.Cli.Options;
using Certes.Cli.Processors;
using Certes.Cli.Settings;
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

        private readonly UserSettings userSettings = new UserSettings();
        private readonly ICliCommand[] commands;

        public CliCore()
        {
            commands = typeof(CliCore).GetTypeInfo().Assembly
                .GetTypes()
                .Where(t => t.GetTypeInfo().IsClass)
                .Where(t => typeof(ICliCommand).IsAssignableFrom(t))
                .Select(t => Activator.CreateInstance(t, userSettings))
                .Cast<ICliCommand>()
                .ToArray();
        }

        public async Task<bool> Process(string[] args)
        {
            ICliCommand commandFound = null;
            ArgumentSyntax argSyntax = null;
            try
            {
                argSyntax = ArgumentSyntax.Parse(args, syntax =>
                {
                    syntax.ApplicationName = "certes";
                    syntax.HandleErrors = false;
                    
                    foreach (var cmd in commands)
                    {
                        var argCmd = cmd.Define(syntax);
                        if (argCmd.IsActive)
                        {
                            commandFound = cmd;
                            break;
                        }
                    }

                    if (commandFound == null)
                    {
                        accountOptions = AccountCommand.TryParse(syntax);
                        orderOptions = OrderCommand.TryParse(syntax);
                        azureOptions = AzureCommand.TryParse(syntax);
                    }

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
                if (commandFound != null)
                {
                    var result = await commandFound.Execute(argSyntax);
                    consoleLogger.Info(JsonConvert.SerializeObject(
                        result, Formatting.Indented, jsonSettings));
                    return true;
                }

                if (accountOptions != null)
                {
                    var cmd = new AccountCommand(accountOptions, userSettings);
                    var result = await cmd.Process();
                    consoleLogger.Info(JsonConvert.SerializeObject(result, Formatting.Indented, jsonSettings));
                    return true;
                }

                if (orderOptions != null)
                {
                    var cmd = new OrderCommand(orderOptions, userSettings);
                    var result = await cmd.Process();
                    consoleLogger.Info(JsonConvert.SerializeObject(result, Formatting.Indented, jsonSettings));
                    return true;
                }

                if (azureOptions != null)
                {
                    var cmd = new AzureCommand(azureOptions, userSettings);
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
