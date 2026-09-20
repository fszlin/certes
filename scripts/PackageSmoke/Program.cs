using System;
using Certes;
using Certes.Pkcs;

// Exercise the packed public API and dependencies without contacting a CA.
var assembly = typeof(AcmeContext).Assembly.GetName();
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
