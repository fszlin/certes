using Certes.Acme;
using Certes.Jws;
using Certes.Pkcs;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Certes
{
    public class AcmeClient : IDisposable
    {
        private readonly AcmeHttpHandler handler;
        private IAccountKey key;

        public AcmeClient(Uri serverUri)
            : this(new AcmeHttpHandler(serverUri))
        {
        }

        public AcmeClient(AcmeHttpHandler handler)
        {
            this.handler = handler;
        }

        public void Use(KeyInfo keyData)
        {
            this.key = new AccountKey(keyData);
        }

        public async Task<AcmeAccount> NewRegistraton(params string[] contact)
        {
            if (this.key == null)
            {
                this.key = new AccountKey();
            }
            
            var registration = new Registration
            {
                Contact = contact,
                Resource = ResourceTypes.NewRegistration
            };

            var uri = await this.handler.GetResourceUri(registration.Resource);
            var result = await this.handler.Post(uri, registration, key);
            if (result.Error != null)
            {
                throw new Exception($"{ result.Error.Type}: {result.Error.Detail} ({result.Error.Status})");
            }

            var account = new AcmeAccount
            {
                Links = result.Links,
                Data = result.Data,
                Location = result.Location,
                Key = key.Export(),
                ContentType = result.ContentType
            };

            return account;
        }

        public async Task<AcmeAccount> UpdateRegistration(AcmeAccount account)
        {
            if (this.key == null)
            {
                throw new InvalidOperationException();
            }

            var registration = account.Data;

            var result = await this.handler.Post(account.Location, registration, key);
            if (result.Error != null)
            {
                throw new Exception($"{result.Error.Type}: {result.Error.Detail} ({result.Error.Status})");
            }

            account.Data = result.Data;
            return account;
        }

        public async Task<AcmeResult<Authorization>> NewAuthorization(AuthorizationIdentifier identifier)
        {
            if (this.key == null)
            {
                throw new InvalidOperationException();
            }

            var auth = new Authorization
            {
                Identifier = identifier,
                Resource = ResourceTypes.NewAuthorization
            };

            var uri = await this.handler.GetResourceUri(ResourceTypes.NewAuthorization);
            var result = await this.handler.Post(uri, auth, key);
            if (result.Error != null)
            {
                throw new Exception($"{result.Error.Type}: {result.Error.Detail} ({result.Error.Status})");
            }

            if (result.HttpStatus == HttpStatusCode.SeeOther) // An authentication with the same identifier exists.
            {
                result = await this.handler.Get<Authorization>(result.Location);
                if (result.Error != null)
                {
                    throw new Exception($"{result.Error.Type}: {result.Error.Detail} ({result.Error.Status})");
                }
            }

            return new AcmeResult<Authorization>
            {
                Data = result.Data,
                Links = result.Links,
                Location = result.Location,
                ContentType = result.ContentType
            };
        }
        
        public async Task<AcmeResult<Authorization>> RefreshAuthorization(Uri location)
        {
            var result = await this.handler.Get<Authorization>(location);
            if (result.Error != null)
            {
                throw new Exception($"{ result.Error.Type}: {result.Error.Detail} ({result.Error.Status})");
            }

            return new AcmeResult<Authorization>
            {
                Data = result.Data,
                Links = result.Links,
                Location = result.Location ?? location,
                ContentType = result.ContentType
            };
        }
        
        public string ComputeKeyAuthorization(Challenge challenge)
        {
            var jwkThumbprint = this.key.GenerateThumbprint();
            var jwkThumbprintEncoded = JwsConvert.ToBase64String(jwkThumbprint);
            var token = challenge.Token;
            return $"{token}.{jwkThumbprintEncoded}";

        }

        public async Task<AcmeResult<Challenge>> CompleteChallenge(Challenge authChallenge)
        {
            if (this.key == null)
            {
                throw new InvalidOperationException();
            }

            var challenge = new Challenge
            {
                KeyAuthorization = ComputeKeyAuthorization(authChallenge),
                Type = authChallenge.Type
            };

            var result = await this.handler.Post(authChallenge.Uri, challenge, key);
            if (result.Error != null)
            {
                throw new Exception($"{ result.Error.Type}: {result.Error.Detail} ({result.Error.Status})");
            }

            return new AcmeResult<Challenge>
            {
                Data = result.Data,
                Links = result.Links,
                Location = result.Location,
                ContentType = result.ContentType
            };
        }

        public async Task<AcmeCertificate> NewCertificate(ICertificationRequestBuilder csrProvider)
        {
            var csrBytes = csrProvider.Generate();

            var payload = new Certificate
            {
                Csr = JwsConvert.ToBase64String(csrBytes),
                Resource = ResourceTypes.NewCertificate
            };

            var uri = await this.handler.GetResourceUri(ResourceTypes.NewCertificate);
            var result = await this.handler.Post(uri, payload, key);
            if (result.Error != null)
            {
                throw new Exception($"{ result.Error.Type}: {result.Error.Detail} ({result.Error.Status})");
            }

            byte[] pem;
            using (var buffer = new MemoryStream())
            {
                pem = buffer.ToArray();
            }

            var cert = new AcmeCertificate
            {
                Raw = result.Raw,
                Key = csrProvider.Export(),
                Links = result.Links,
                Location = result.Location,
                ContentType = result.ContentType
            };

            var currentCert = cert;
            while (true)
            {
                var upLink = currentCert.Links?.Where(l => l.Rel == "up").FirstOrDefault();
                if (upLink == null)
                {
                    break;
                }
                else
                {
                    var issuerResult = await this.handler.Get<AcmeCertificate>(upLink.Uri);
                    currentCert.Issuer = new AcmeCertificate
                    {
                        Raw = issuerResult.Raw,
                        Key = csrProvider.Export(),
                        Links = issuerResult.Links,
                        Location = issuerResult.Location,
                        ContentType = issuerResult.ContentType
                    };

                    currentCert = currentCert.Issuer;
                }
            }

            return cert;
        }

        public async Task<AcmeCertificate> RevokeCertificate(AcmeCertificate certificate)
        {
            var payload = new RevokeCertificate
            {
                Certificate = JwsConvert.ToBase64String(certificate.Raw),
                Resource = ResourceTypes.RevokeCertificate
            };

            var uri = await this.handler.GetResourceUri(ResourceTypes.RevokeCertificate);
            var result = await this.handler.Post(uri, payload, key);
            if (result.Error != null)
            {
                throw new Exception($"{ result.Error.Type}: {result.Error.Detail} ({result.Error.Status})");
            }

            certificate.Revoked = true;
            return certificate;
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.handler?.Dispose();
                    (key as IDisposable)?.Dispose();
                }
                
                disposedValue = true;
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
