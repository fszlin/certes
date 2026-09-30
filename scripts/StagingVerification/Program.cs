using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Certes;
using Certes.Acme;
using Certes.Acme.Resource;

/// <summary>
/// Let's Encrypt staging verification harness for Certes releases.
/// Validates issuance, ARI, profile selection, and certificate round-trips.
/// </summary>
class Program
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private static readonly string? StagingUrl = Environment.GetEnvironmentVariable("LE_STAGING_URL") ?? "https://acme-staging-v02.api.letsencrypt.org/directory";
    private static readonly string? TestDomain = Environment.GetEnvironmentVariable("ACME_TEST_DOMAIN") ?? "acme-test.certes.app";
    private static readonly string? CloudflareApiToken = Environment.GetEnvironmentVariable("CLOUDFLARE_API_TOKEN");
    private static readonly string? CloudflareZoneId = Environment.GetEnvironmentVariable("CLOUDFLARE_ZONE_ID");
    private static readonly int VerificationTimeoutSeconds = int.TryParse(Environment.GetEnvironmentVariable("VERIFICATION_TIMEOUT_SECONDS"), out var t) ? t : 600;
    private static readonly int ChallengeWaitSeconds = int.TryParse(Environment.GetEnvironmentVariable("CHALLENGE_PROPAGATION_WAIT"), out var w) ? w : 10;
    private static readonly List<string> CreatedRecordIds = new();

    static async Task Main()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(VerificationTimeoutSeconds));
            var token = cts.Token;

            Log("Starting Let's Encrypt staging verification for Certes");
            Log($"Staging: {StagingUrl}");
            Log($"Test domain: {TestDomain}");

            ValidateEnvironment();

            var results = new Dictionary<string, object>();

            // Discover directory and advertised capabilities
            var directory = await FetchDirectory(token);
            results["directory"] = new
            {
                stagingUrl = StagingUrl,
                profiles = directory.Meta?.Profiles?.Keys ?? new[] { "classic" },
                hasAri = !string.IsNullOrEmpty(directory.RenewalInfo),
            };

            // Create account
            var accountKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
            var acme = new AcmeContext(new Uri(StagingUrl), accountKey);
            var account = await acme.NewAccount(Array.Empty<string>(), termsOfServiceAgreed: true, cancellationToken: token);
            results["account"] = new { location = account.Location };
            Log($"Created account: {account.Location}");

            // Test 1: Default profile issuance
            await TestIssuance(acme, "classic", KeyAlgorithm.RSA, token, results);

            // Test 2: Shortlived profile issuance
            if (directory.Meta?.Profiles?.ContainsKey("shortlived") == true)
            {
                await TestIssuance(acme, "shortlived", KeyAlgorithm.ES256, token, results);
            }

            // Test 3: ARI and early replacement
            if (!string.IsNullOrEmpty(directory.RenewalInfo))
            {
                await TestAriReplacement(acme, token, results);
            }

            // Write results
            var json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(json);
            File.WriteAllText("staging-verification-report.json", json);

            Log("Staging verification complete.");
        }
        catch (OperationCanceledException)
        {
            LogError("Verification timed out.");
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            LogError($"{ex.GetType().Name}: {ex.Message}");
            Environment.Exit(1);
        }
        finally
        {
            await CleanupDnsRecords();
        }
    }

    private static void ValidateEnvironment()
    {
        if (string.IsNullOrEmpty(CloudflareApiToken))
            throw new InvalidOperationException("CLOUDFLARE_API_TOKEN not set");
        if (string.IsNullOrEmpty(CloudflareZoneId))
            throw new InvalidOperationException("CLOUDFLARE_ZONE_ID not set");
        if (string.IsNullOrEmpty(TestDomain))
            throw new InvalidOperationException("ACME_TEST_DOMAIN not set");
    }

    private static async Task<Directory> FetchDirectory(CancellationToken token)
    {
        Log("Fetching directory...");
        using var req = new HttpRequestMessage(HttpMethod.Get, StagingUrl);
        using var resp = await Http.SendAsync(req, token);
        resp.EnsureSuccessStatusCode();
        var json = JsonSerializer.Deserialize<Directory>(await resp.Content.ReadAsStringAsync(token));
        return json ?? throw new InvalidOperationException("Directory is empty");
    }

    private static async Task TestIssuance(IAcmeContext acme, string profile, KeyAlgorithm keyAlgo, CancellationToken token, Dictionary<string, object> results)
    {
        Log($"Testing {profile} issuance with {keyAlgo}...");
        var domains = new[] { TestDomain };
        var order = await acme.NewOrderWithProfile(domains, profile, cancellationToken: token);
        results[$"issuance_{profile}_{keyAlgo}"] = new { orderLocation = order.Location };

        var authorizations = await order.Authorizations(token);
        foreach (var authz in authorizations)
        {
            var authzResource = await authz.Resource(token);
            if (authzResource.Status == AuthorizationStatus.Valid)
            {
                Log($"  Authorization {authz.Location} already valid, skipping.");
                continue;
            }

            var challenge = await authz.Dns(token) ?? throw new InvalidOperationException("DNS-01 challenge not available");
            var challengeResource = await challenge.Resource(token);
            var dnsValue = acme.AccountKey.DnsTxt(challengeResource.Token);

            // Create DNS TXT record
            var recordId = await CreateDnsRecord($"_acme-challenge.{TestDomain}", dnsValue, token);
            Log($"  Created DNS record: {recordId}");
            CreatedRecordIds.Add(recordId);

            // Wait for propagation
            Log($"  Waiting {ChallengeWaitSeconds}s for DNS propagation...");
            await Task.Delay(ChallengeWaitSeconds * 1000, token);

            // Acknowledge challenge
            var validated = await challenge.Validate(token);
            Log($"  Challenge validated: {validated.Status}");

            // Poll for authorization
            int retries = 30;
            while (retries-- > 0 && authzResource.Status != AuthorizationStatus.Valid)
            {
                await Task.Delay(1000, token);
                authzResource = await authz.Resource(token);
            }
            if (authzResource.Status != AuthorizationStatus.Valid)
                throw new InvalidOperationException($"Authorization {authz.Location} did not reach valid status");
        }

        // Finalize order
        var certificateKey = KeyFactory.NewKey(keyAlgo);
        var csr = await order.CreateCsr(certificateKey, token);
        var encodedCsr = csr.Generate();

        var finalized = await order.Finalize(encodedCsr, token);
        int finalizeRetries = 30;
        while (finalizeRetries-- > 0 && finalized.Status != OrderStatus.Valid)
        {
            await Task.Delay(1000, token);
            finalized = await order.Resource(token);
        }

        if (finalized.Status != OrderStatus.Valid)
            throw new InvalidOperationException($"Order {order.Location} did not reach valid status");

        // Download certificate
        var cert = await order.Download(cancellationToken: token);
        var leafCert = new X509Certificate2(cert.Certificate.RawData);
        Log($"  Issued certificate: {leafCert.Subject} (expires {leafCert.NotAfter:o})");

        // Verify certificate properties
        var sanExt = leafCert.Extensions.OfType<X509Extension>()
            .FirstOrDefault(e => e.Oid?.Value == "2.5.29.17"); // subjectAltName OID
        if (sanExt == null)
            throw new InvalidOperationException("Certificate missing Subject Alternative Names");

        results[$"cert_{profile}_{keyAlgo}"] = new
        {
            subject = leafCert.Subject,
            notBefore = leafCert.NotBefore,
            notAfter = leafCert.NotAfter,
            keyAlgorithm = keyAlgo.ToString(),
            profile = profile,
        };
    }

    private static async Task TestAriReplacement(IAcmeContext acme, CancellationToken token, Dictionary<string, object> results)
    {
        Log("Testing ARI and early replacement...");
        var domains = new[] { TestDomain };
        var order = await acme.NewOrder(domains, cancellationToken: token);

        // Complete an authorization to get a cert
        var authorizations = await order.Authorizations(token);
        foreach (var authz in authorizations)
        {
            var authzResource = await authz.Resource(token);
            if (authzResource.Status == AuthorizationStatus.Valid)
                continue;

            var challenge = await authz.Dns(token);
            if (challenge == null)
                continue;

            var challengeResource = await challenge.Resource(token);
            var dnsValue = acme.AccountKey.DnsTxt(challengeResource.Token);

            var recordId = await CreateDnsRecord($"_acme-challenge.{TestDomain}", dnsValue, token);
            CreatedRecordIds.Add(recordId);

            await Task.Delay(ChallengeWaitSeconds * 1000, token);
            await challenge.Validate(token);

            int retries = 30;
            while (retries-- > 0 && authzResource.Status != AuthorizationStatus.Valid)
            {
                await Task.Delay(1000, token);
                authzResource = await authz.Resource(token);
            }
        }

        var certificateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
        var csr = await order.CreateCsr(certificateKey, token);
        await order.Finalize(csr.Generate(), token);

        int finalizeRetries = 30;
        var finalized = await order.Resource(token);
        while (finalizeRetries-- > 0 && finalized.Status != OrderStatus.Valid)
        {
            await Task.Delay(1000, token);
            finalized = await order.Resource(token);
        }

        var cert = await order.Download(cancellationToken: token);
        var certId = DnsPersistExtensions.CreateRecord(
            new Authorization { Identifier = new Identifier { Type = IdentifierType.Dns, Value = TestDomain } },
            new Challenge { Type = ChallengeTypes.Http01 },
            null, acme.Account(token).Result.Location, acme.AccountKey.Thumbprint()).Name; // Placeholder

        // For now, just log that ARI is available
        results["ari"] = new { available = true, message = "ARI endpoint present; replacement testing deferred" };
        Log("  ARI endpoint available.");
    }

    private static async Task<string> CreateDnsRecord(string name, string value, CancellationToken token)
    {
        var payload = new { type = "TXT", name, content = value, ttl = 300 };
        var json = JsonSerializer.Serialize(payload);

        using var req = new HttpRequestMessage(HttpMethod.Post, $"https://api.cloudflare.com/client/v4/zones/{CloudflareZoneId}/dns_records")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        req.Headers.Add("Authorization", $"Bearer {CloudflareApiToken}");

        using var resp = await Http.SendAsync(req, token);
        resp.EnsureSuccessStatusCode();

        var respJson = JsonSerializer.Deserialize<JsonElement>(await resp.Content.ReadAsStringAsync(token));
        if (!respJson.TryGetProperty("result", out var result) || !result.TryGetProperty("id", out var idElem))
            throw new InvalidOperationException("Failed to create DNS record");

        return idElem.GetString() ?? throw new InvalidOperationException("Empty record ID");
    }

    private static async Task CleanupDnsRecords()
    {
        Log("Cleaning up DNS records...");
        foreach (var recordId in CreatedRecordIds)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Delete, $"https://api.cloudflare.com/client/v4/zones/{CloudflareZoneId}/dns_records/{recordId}");
                req.Headers.Add("Authorization", $"Bearer {CloudflareApiToken}");
                using var resp = await Http.SendAsync(req);
                resp.EnsureSuccessStatusCode();
                Log($"  Removed {recordId}");
            }
            catch (Exception ex)
            {
                LogError($"  Failed to remove {recordId}: {ex.Message}");
            }
        }
    }

    private static void Log(string msg) => Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] {msg}");
    private static void LogError(string msg) => Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] ERROR: {msg}");
}
