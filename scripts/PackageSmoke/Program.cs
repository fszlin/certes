using System;
using System.Runtime.Versioning;
using Certes;
using Certes.Pkcs;

// Exercise the packed public API and dependencies without contacting a CA.
var assembly = typeof(AcmeContext).Assembly.GetName();
var framework = (TargetFrameworkAttribute)Attribute.GetCustomAttribute(
    typeof(AcmeContext).Assembly, typeof(TargetFrameworkAttribute));
#if NET10_0_OR_GREATER
const string expectedFramework = ".NETCoreApp,Version=v10.0";
#elif NET8_0_OR_GREATER
const string expectedFramework = ".NETCoreApp,Version=v8.0";
#else
const string expectedFramework = ".NETStandard,Version=v2.0";
#endif
if (framework?.FrameworkName != expectedFramework)
{
    throw new InvalidOperationException($"Expected library asset {expectedFramework}, got {framework?.FrameworkName ?? "no target framework attribute"}.");
}
if (assembly.GetPublicKeyToken()?.Length != 8)
{
    throw new InvalidOperationException("The package must preserve strong-name identity.");
}

foreach (var algorithm in new[] { KeyAlgorithm.RS256, KeyAlgorithm.ES256 })
{
    var key = KeyFactory.NewKey(algorithm);
    var restored = KeyFactory.FromPem(key.ToPem());
    if (restored.Algorithm != algorithm || restored.Thumbprint() != key.Thumbprint())
    {
        throw new InvalidOperationException($"Key round-trip failed for {algorithm}.");
    }

    var csr = new CertificationRequestBuilder(restored);
    csr.AddName("CN", "package-smoke.example");
    csr.SubjectAlternativeNames.Add("package-smoke.example");
    if (csr.Generate().Length == 0)
    {
        throw new InvalidOperationException($"CSR generation failed for {algorithm}.");
    }
}

Console.WriteLine("Package consumption passed: assembly identity, RSA/ECDSA key round-trips, and CSR generation.");
