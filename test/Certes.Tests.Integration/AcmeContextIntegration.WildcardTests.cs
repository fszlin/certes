﻿using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

using static Certes.Helper;
using static Certes.IntegrationHelper;

namespace Certes
{
    public partial class AcmeContextIntegration
    {
        public class WildcardTests : AcmeContextIntegration
        {
            public WildcardTests(ITestOutputHelper output)
                : base(output)
            {
            }

            [Fact]
            public async Task CanGenerateWildcard()
            {
                var dirUri = await GetAcmeUriV2();
                var hosts = new[] { $"wildcard-{DomainSuffix}.es256.certes-ci.dymetis.com" };
                var ctx = new AcmeContext(dirUri, GetKeyV2(), http: GetAcmeHttpClient(dirUri));

                var orderCtx = await AuthzDns(ctx, hosts);
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
                var pem = await orderCtx.Download();
            }
        }
    }
}
