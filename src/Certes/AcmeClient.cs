﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Jws;
using Certes.Pkcs;
using Certes.Properties;

namespace Certes
{
    /// <summary>
    /// Represents a ACME client.
    /// </summary>
    public class AcmeClient : IAcmeClient, IDisposable
    {
        private readonly IAcmeHttpHandler handler;
        private IAccountKey key;
        private readonly bool shouldDisposeHander;

        /// <summary>
        /// Gets the HTTP handler.
        /// </summary>
        /// <value>
        /// The HTTP handler.
        /// </value>
        public IAcmeHttpHandler HttpHandler
        {
            get
            {
                return handler;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AcmeClient"/> class.
        /// </summary>
        /// <param name="serverUri">The ACME server URI.</param>
        public AcmeClient(Uri serverUri)
            : this(new AcmeHttpHandler(serverUri))
        {
            shouldDisposeHander = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AcmeClient"/> class.
        /// </summary>
        /// <param name="handler">The ACME handler.</param>
        public AcmeClient(IAcmeHttpHandler handler)
        {
            this.handler = handler;
        }

        /// <summary>
        /// Uses the specified account key data.
        /// </summary>
        /// <param name="keyData">The account key data.</param>
        public void Use(KeyInfo keyData)
        {
            this.key = new AccountKey(keyData);
        }

        /// <summary>
        /// Creates a new registraton.
        /// </summary>
        /// <param name="contact">The contact method, e.g. <c>mailto:admin@example.com</c>.</param>
        /// <returns>The ACME account created.</returns>
        public async Task<AcmeAccount> NewRegistraton(params string[] contact)
        {
            if (this.key == null)
            {
                this.key = new AccountKey();
            }

            var registration = new RegistrationEntity
            {
                Contact = contact,
                Resource = ResourceTypes.NewRegistration
            };

            var uri = await this.handler.GetResourceUri(registration.Resource);
            var result = await this.handler.Post(uri, registration, key);
            ThrowIfError(result, uri);

            var account = new AcmeAccount
            {
                Links = result.Links,
                Data = result.Data,
                Json = result.Json,
                Raw = result.Raw,
                Location = result.Location,
                Key = key.Export(),
                ContentType = result.ContentType
            };

            return account;
        }

        /// <summary>
        /// Updates the registration.
        /// </summary>
        /// <param name="account">The account to update.</param>
        /// <returns>The updated ACME account.</returns>
        /// <exception cref="InvalidOperationException">If the account key is missing.</exception>
        public async Task<AcmeAccount> UpdateRegistration(AcmeAccount account)
        {
            if (this.key == null)
            {
                throw new InvalidOperationException();
            }

            var registration = account.Data;

            var result = await this.handler.Post(account.Location, registration, key);
            ThrowIfError(result, account.Location);

            account.Data = result.Data;
            return account;
        }

        /// <summary>
        /// Changes the registration key.
        /// </summary>
        /// <param name="account">The account.</param>
        /// <param name="newKey">The new registration key.</param>
        /// <returns>The awaitable.</returns>
        public async Task ChangeKey(AcmeAccount account, KeyInfo newKey)
        {
            var keyPair = new AccountKey(newKey);
            
            var body = new
            {
                account = account.Location,
                newKey = keyPair.JsonWebKey,
            };
            
            var jws = new JwsSigner(keyPair.SignatureKey);
            var payload = jws.Sign(body);
            var payloadWithResourceType = new
            {
                payload.Payload,
                payload.Protected,
                payload.Signature,
                Resource = ResourceTypes.KeyChange
            };

            var uri = await this.handler.GetResourceUri(ResourceTypes.KeyChange);
            var result = await this.handler.Post(uri, payloadWithResourceType, key);
            ThrowIfError(result, uri);

            this.key = keyPair;
        }

        /// <summary>
        /// Deletes the registration.
        /// </summary>
        /// <returns>The awaitable.</returns>
        public async Task DeleteRegistration(AcmeAccount account)
        {
            if (this.key == null)
            {
                throw new InvalidOperationException();
            }

            await this.handler.Post(
                account.Location,
                new RegistrationEntity
                {
                    Delete = true
                }, key);
        }

        /// <summary>
        /// Create a new authorization.
        /// </summary>
        /// <param name="identifier">The identifier to be authorized.</param>
        /// <returns>The authorization created.</returns>
        /// <exception cref="InvalidOperationException">If the account key is missing.</exception>
        public async Task<AcmeResult<AuthorizationEntity>> NewAuthorization(AuthorizationIdentifier identifier)
        {
            if (this.key == null)
            {
                throw new InvalidOperationException();
            }

            var auth = new AuthorizationEntity
            {
                Identifier = identifier,
                Resource = ResourceTypes.NewAuthorization
            };

            var uri = await handler.GetResourceUri(ResourceTypes.NewAuthorization);
            var result = await handler.Post(uri, auth, key);
            ThrowIfError(result, uri);

            if (result.HttpStatus == HttpStatusCode.SeeOther) // An authentication with the same identifier exists.
            {
                result = await handler.Get<AuthorizationEntity>(result.Location);
                ThrowIfError(result, result.Location);
            }

            return new AcmeResult<AuthorizationEntity>
            {
                Data = result.Data,
                Json = result.Json,
                Raw = result.Raw,
                Links = result.Links,
                Location = result.Location,
                ContentType = result.ContentType
            };
        }

        /// <summary>
        /// Gets the authorization from <paramref name="location"/>.
        /// </summary>
        /// <param name="location">The authorization location URI.</param>
        /// <returns>The authorization retrieved.</returns>
        public async Task<AcmeResult<AuthorizationEntity>> GetAuthorization(Uri location)
        {
            var result = await this.handler.Get<AuthorizationEntity>(location);
            ThrowIfError(result, location);

            return new AcmeResult<AuthorizationEntity>
            {
                Data = result.Data,
                Json = result.Json,
                Raw = result.Raw,
                Links = result.Links,
                Location = result.Location ?? location,
                ContentType = result.ContentType
            };
        }

        /// <summary>
        /// Computes the key authorization string for <paramref name="challenge"/>.
        /// </summary>
        /// <param name="challenge">The challenge.</param>
        /// <returns>The key authorization string.</returns>
        public string ComputeKeyAuthorization(ChallengeEntity challenge)
        {
            return challenge.ComputeKeyAuthorization(this.key);
        }

        /// <summary>
        /// Computes the DNS value for the <paramref name="challenge"/>.
        /// </summary>
        /// <param name="challenge">The challenge.</param>
        /// <returns>The value for the text DNS record.</returns>
        /// <exception cref="System.InvalidOperationException">If the provided challenge is not a DNS challenge.</exception>
        public string ComputeDnsValue(ChallengeEntity challenge)
        {
            if (challenge?.Type != ChallengeTypes.Dns01)
            {
                throw new InvalidOperationException("The provided challenge is not a DNS challenge.");
            }

            return challenge.ComputeDnsValue(this.key);
        }

        /// <summary>
        /// Submits the challenge for the ACME server to validate.
        /// </summary>
        /// <param name="authChallenge">The authentication challenge.</param>
        /// <returns>The challenge updated.</returns>
        /// <exception cref="InvalidOperationException">If the account key is missing.</exception>
        public async Task<AcmeResult<ChallengeEntity>> CompleteChallenge(ChallengeEntity authChallenge)
        {
            if (this.key == null)
            {
                throw new InvalidOperationException();
            }

            var challenge = new ChallengeEntity
            {
                KeyAuthorization = ComputeKeyAuthorization(authChallenge),
                Type = authChallenge.Type
            };

            var result = await this.handler.Post(authChallenge.Uri, challenge, key);
            ThrowIfError(result, authChallenge.Uri);

            return new AcmeResult<ChallengeEntity>
            {
                Data = result.Data,
                Json = result.Json,
                Raw = result.Raw,
                Links = result.Links,
                Location = result.Location,
                ContentType = result.ContentType
            };
        }

        /// <summary>
        /// Creates a new certificate.
        /// </summary>
        /// <param name="csrBytes">The certificate signing request data.</param>
        /// <returns>The certificate issued.</returns>
        public async Task<AcmeCertificate> NewCertificate(byte[] csrBytes)
        {
            var payload = new CertificateEntity
            {
                Csr = JwsConvert.ToBase64String(csrBytes),
                Resource = ResourceTypes.NewCertificate
            };

            var uri = await this.handler.GetResourceUri(ResourceTypes.NewCertificate);
            var result = await this.handler.Post(uri, payload, key);
            ThrowIfError(result, uri);

            byte[] pem;
            using (var buffer = new MemoryStream())
            {
                pem = buffer.ToArray();
            }

            var cert = new AcmeCertificate
            {
                Raw = result.Raw,
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
                        Links = issuerResult.Links,
                        Location = issuerResult.Location,
                        ContentType = issuerResult.ContentType
                    };

                    currentCert = currentCert.Issuer;
                }
            }

            return cert;
        }

        /// <summary>
        /// Creates a new certificate.
        /// </summary>
        /// <param name="csrProvider">The certificate signing request (CSR) provider.</param>
        /// <returns>The certificate issued.</returns>
        public async Task<AcmeCertificate> NewCertificate(ICertificationRequestBuilder csrProvider)
        {
            var csrBytes = csrProvider.Generate();
            var cert = await NewCertificate(csrBytes);
            cert.Key = csrProvider.Export();
            return cert;
        }

        /// <summary>
        /// Revokes the certificate.
        /// </summary>
        /// <param name="certificate">The certificate.</param>
        /// <returns>The certificate revoked.</returns>
        public async Task<AcmeCertificate> RevokeCertificate(AcmeCertificate certificate)
        {
            var payload = new RevokeCertificateEntity
            {
                Certificate = JwsConvert.ToBase64String(certificate.Raw),
                Resource = ResourceTypes.RevokeCertificate
            };

            var uri = await this.handler.GetResourceUri(ResourceTypes.RevokeCertificate);
            var result = await this.handler.Post(uri, payload, key);
            ThrowIfError(result, uri);

            certificate.Revoked = true;
            return certificate;
        }

        private void ThrowIfError<T>(AcmeResponse<T> response, Uri uri)
        {
            if (response.Error != null)
            {
                throw new AcmeRequestException(
                    string.Format(Strings.ErrorFetchResource, uri),
                    response.Error);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false;

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (shouldDisposeHander)
                    {
                        (handler as IDisposable)?.Dispose();
                    }

                    (key as IDisposable)?.Dispose();
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
