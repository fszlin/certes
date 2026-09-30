using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Pkcs;

// Exercises a published Certes package against Let's Encrypt staging. Never use production domains here.
internal static class Program
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private static readonly string StagingUrl = Environment.GetEnvironmentVariable("LE_STAGING_URL")
        ?? "https://acme-staging-v02.api.letsencrypt.org/directory";
    private static readonly string? Domain = Environment.GetEnvironmentVariable("ACME_TEST_DOMAIN");
    private static readonly string? Token = Environment.GetEnvironmentVariable("CLOUDFLARE_API_TOKEN");
    private static readonly string? Zone = Environment.GetEnvironmentVariable("CLOUDFLARE_ZONE_ID");
    private static readonly List<string> RecordIds = new();

    private static async Task<int> Main()
    {
        if (string.IsNullOrWhiteSpace(Domain) || string.IsNullOrWhiteSpace(Token) || string.IsNullOrWhiteSpace(Zone))
        {
            Console.Error.WriteLine("Set ACME_TEST_DOMAIN, CLOUDFLARE_API_TOKEN, and CLOUDFLARE_ZONE_ID.");
            return 1;
        }

        if (!int.TryParse(Environment.GetEnvironmentVariable("VERIFICATION_TIMEOUT_SECONDS") ?? "600", out var timeout) || timeout <= 0 ||
            !int.TryParse(Environment.GetEnvironmentVariable("CHALLENGE_PROPAGATION_WAIT") ?? "60", out var wait) || wait < 0)
        {
            Console.Error.WriteLine("VERIFICATION_TIMEOUT_SECONDS must be positive and CHALLENGE_PROPAGATION_WAIT nonnegative.");
            return 1;
        }

        if (!Uri.TryCreate(StagingUrl, UriKind.Absolute, out var stagingUri) ||
            stagingUri.Scheme != Uri.UriSchemeHttps ||
            stagingUri.Host != "acme-staging-v02.api.letsencrypt.org" ||
            stagingUri.AbsolutePath != "/directory")
        {
            Console.Error.WriteLine("LE_STAGING_URL must be the Let's Encrypt staging directory.");
            return 1;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
        var success = false;
        try
        {
            var acme = new AcmeContext(stagingUri, KeyFactory.NewKey(KeyAlgorithm.ES256));
            var directory = await acme.GetDirectory(cts.Token);
            Console.Error.WriteLine($"Staging directory: {StagingUrl}");
            Console.Error.WriteLine($"Advertised profiles: {string.Join(", ", directory.Meta?.Profiles?.Keys ?? Enumerable.Empty<string>())}");
            Console.Error.WriteLine($"ARI advertised: {directory.RenewalInfo != null}");
            await acme.NewAccount(Array.Empty<string>(), true, cancellationToken: cts.Token);

            var first = await Issue(acme, null, KeyAlgorithm.RS256, wait, cts.Token);
            if (directory.RenewalInfo != null)
            {
                var id = first.GetRenewalInfoCertificateId();
                var info = await acme.GetRenewalInfo(id, cts.Token);
                if (info.SuggestedWindow == null || info.SuggestedWindow.End <= info.SuggestedWindow.Start)
                {
                    throw new InvalidOperationException("Invalid ARI suggested window.");
                }

                var replacement = await acme.NewReplacementOrder(new[] { Domain! }, id, cancellationToken: cts.Token);
                var resource = await replacement.Resource(cts.Token);
                if (resource.Replaces != id || (resource.Status != OrderStatus.Pending && resource.Status != OrderStatus.Ready))
                {
                    throw new InvalidOperationException("ARI replacement order did not contain the expected certificate ID/status.");
                }

                Console.Error.WriteLine($"ARI lookup and replacement order passed: {replacement.Location}");
            }

            if (directory.Meta?.Profiles?.ContainsKey("shortlived") == true)
            {
                await Issue(acme, "shortlived", KeyAlgorithm.ES256, wait, cts.Token);
            }

            success = true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Staging verification failed: {ex}");
        }
        finally
        {
            // Always attempt cleanup even after a timeout. Do not print credentials or DNS values.
            foreach (var id in RecordIds)
            {
                try
                {
                    using var request = CloudflareRequest(HttpMethod.Delete, $"dns_records/{id}");
                    using var response = await Http.SendAsync(request);
                    await CheckCloudflareResponse(response);
                }
                catch (Exception ex)
                {
                    success = false;
                    Console.Error.WriteLine($"DNS cleanup failed for record {id}: {ex.Message}");
                }
            }
        }

        if (success)
        {
            Console.WriteLine("Let's Encrypt staging issuance, certificate export, and advertised extension checks passed.");
        }

        return success ? 0 : 1;
    }

    private static async Task<CertificateChain> Issue(IAcmeContext acme, string? profile, KeyAlgorithm algorithm, int wait, CancellationToken token)
    {
        var order = profile == null
            ? await acme.NewOrder(new[] { Domain! }, cancellationToken: token)
            : await acme.NewOrderWithProfile(new[] { Domain! }, profile, cancellationToken: token);

        foreach (var authorization in await order.Authorizations(token))
        {
            var resource = await authorization.Resource(token);
            if (resource.Status == AuthorizationStatus.Valid)
            {
                Console.Error.WriteLine($"Reusing an already valid authorization for {Domain} ({profile ?? "default"}); DNS-01 not repeated.");
                continue;
            }

            if (resource.Status != AuthorizationStatus.Pending)
            {
                throw new InvalidOperationException($"Unexpected authorization status: {resource.Status}");
            }

            var challenge = await authorization.Dns(token) ?? throw new InvalidOperationException("DNS-01 not offered.");
            var challengeResource = await challenge.Resource(token);
            var id = await CreateRecord($"_acme-challenge.{Domain}", acme.AccountKey.DnsTxt(challengeResource.Token), token);
            RecordIds.Add(id);
            await Task.Delay(TimeSpan.FromSeconds(wait), token);
            await challenge.Validate(token);

            while (true)
            {
                resource = await authorization.Resource(token);
                if (resource.Status == AuthorizationStatus.Valid)
                {
                    break;
                }

                if (resource.Status != AuthorizationStatus.Pending)
                {
                    var failed = await challenge.Resource(token);
                    throw new InvalidOperationException($"DNS-01 authorization failed: {resource.Status}; ACME error: {failed.Error?.Type}: {failed.Error?.Detail}");
                }

                await Task.Delay(TimeSpan.FromSeconds(3), token);
            }
        }

        var key = KeyFactory.NewKey(algorithm);
        var chain = await order.Generate(new CsrInfo { CommonName = Domain! }, key, retryCount: 120, cancellationToken: token);
        using var cert = X509CertificateLoader.LoadCertificate(chain.Certificate.ToDer());
        var san = cert.Extensions.OfType<X509SubjectAlternativeNameExtension>().SingleOrDefault();
        if (san == null || !san.EnumerateDnsNames().Contains(Domain!, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Issued certificate does not contain the requested DNS SAN.");
        }

        // Export exercises certificate/key pairing and chain packaging; staging roots are not trusted by the OS.
        var pfx = chain.ToPfx(key).Build("staging-verification", "temporary-password");
        using var exported = X509CertificateLoader.LoadPkcs12(pfx, "temporary-password");
        if (!exported.HasPrivateKey || !exported.RawData.SequenceEqual(cert.RawData))
        {
            throw new InvalidOperationException("PFX round-trip did not preserve the issued certificate and key.");
        }

        Console.Error.WriteLine($"Issued {profile ?? "default"} ({algorithm}) certificate for {Domain}: {order.Location}");
        return chain;
    }

    private static HttpRequestMessage CloudflareRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, $"https://api.cloudflare.com/client/v4/zones/{Zone}/{path}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token);
        return request;
    }

    private static async Task<JsonElement> CheckCloudflareResponse(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (!document.RootElement.TryGetProperty("success", out var success) || success.ValueKind != JsonValueKind.True)
        {
            throw new InvalidOperationException("Cloudflare rejected the DNS request.");
        }

        return document.RootElement.Clone();
    }

    private static async Task<string> CreateRecord(string name, string value, CancellationToken token)
    {
        using var request = CloudflareRequest(HttpMethod.Post, "dns_records");
        request.Content = new StringContent(JsonSerializer.Serialize(new { type = "TXT", name, content = value, ttl = 300 }), Encoding.UTF8, "application/json");
        using var response = await Http.SendAsync(request, token);
        var result = await CheckCloudflareResponse(response);
        return result.GetProperty("result").GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Cloudflare returned no DNS record ID.");
    }
}
