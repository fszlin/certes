using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NLog;
using NLog.Config;
using NLog.Targets;
using Xunit;
using static Certes.IntegrationHelper;

namespace Certes
{
    public static partial class Helper
    {
        private static string ValidCertPem = null;

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

        public static async Task<string> GetValidCert()
        {
            if (ValidCertPem != null)
            {
                return ValidCertPem;
            }

            var hosts = new[] { $"xunit-es256.certes-ci.dymetis.com" };
            var dirUri = await GetAcmeUriV2();
            var httpClient = GetAcmeHttpClient(dirUri);
            var ctx = new AcmeContext(dirUri, GetKeyV2(), http: httpClient);
            var orderCtx = await AuthorizeHttp(ctx, hosts);

            var certKey = KeyFactory.NewKey(KeyAlgorithm.RS256);
            var finalizedOrder = await orderCtx.Finalize(new CsrInfo
            {
                CountryName = "CA",
                State = "Ontario",
                Locality = "Toronto",
                Organization = "Certes",
                OrganizationUnit = "Dev",
                CommonName = hosts[0],
            }, certKey);

            var cert = await orderCtx.Download(null);

            var buffer = new StringBuilder();
            buffer.AppendLine(cert.Certificate.ToPem());
            foreach (var issuer in cert.Issuers)
            {
                buffer.AppendLine(issuer.ToPem());
            }

            var pem = buffer.ToString();
            return ValidCertPem = pem;
        }
    }
}
