using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using Certes.Crypto;
using NLog;
using NLog.Config;
using NLog.Targets;
using Xunit;

namespace Certes
{
    public static partial class Helper
    {
        private static readonly KeyAlgorithmProvider signatureAlgorithmProvider = new KeyAlgorithmProvider();

        public static IList<string> Logs
        {
            get
            {
                var target = (MemoryTarget)LogManager.Configuration.FindTargetByName("logger");
                return target.Logs;
            }
        }


        public static void SaveKey(string keyPath)
        {
            if (!Directory.Exists(Path.GetDirectoryName(keyPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(keyPath));
            }

            File.WriteAllText(keyPath, GetTestKey(KeyAlgorithm.ES256));
        }

        public static void ConfigureLogger()
        {
            if (LogManager.Configuration == null)
            {
                var config = new LoggingConfiguration();
                var memoryTarget = new MemoryTarget()
                {
                    Layout = @"${message}${onexception:${exception:format=tostring}}"
                };

                config.AddTarget("logger", memoryTarget);

                var consoleRule = new LoggingRule("*", LogLevel.Debug, memoryTarget);
                config.LoggingRules.Add(consoleRule);
                LogManager.Configuration = config;
            }
            else
            {
                Logs.Clear();
            }
        }

        public static void VerifyGetterSetter<TSource, TProperty>(
            this TSource source,
            Expression<Func<TSource, TProperty>> propertyLambda,
            TProperty value)
        {
            var member = propertyLambda.Body as MemberExpression;
            var propInfo = member.Member as PropertyInfo;

            propInfo.SetValue(source, value);
            var actualValue = propInfo.GetValue(source);

            Assert.Equal(value, (TProperty)actualValue);
        }

        public static string GetTestKey(this KeyAlgorithm algo)
        {
            switch (algo)
            {
                case KeyAlgorithm.ES256:
                    return Keys.ES256Key;
                case KeyAlgorithm.ES384:
                    return Keys.ES384Key;
                case KeyAlgorithm.ES512:
                    return Keys.ES512Key;
                default:
                    return Keys.RS256Key;
            }
        }

        public static void SetHomePath(string path, bool forWin = true)
        {
            path = Path.GetFullPath(path);
            if (forWin)
            {
                var drive = Path.GetPathRoot(path);
                Environment.SetEnvironmentVariable("HOME", "");
                Environment.SetEnvironmentVariable("HOMEDRIVE", drive);
                Environment.SetEnvironmentVariable("HOMEPATH", path.Substring(drive.Length));
            }
            else
            {
                Environment.SetEnvironmentVariable("HOMEDRIVE", "");
                Environment.SetEnvironmentVariable("HOMEPATH", "");
                Environment.SetEnvironmentVariable("HOME", path);
            }

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
