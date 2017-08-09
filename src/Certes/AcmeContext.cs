using Certes.Acme;
using Certes.Jws;
using Certes.Pkcs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Certes
{
    /// <summary>
    /// Presents the context for ACME operations.
    /// </summary>
    /// <seealso cref="Certes.IAcmeContext" />
    public class AcmeContext : IAcmeContext
    {
        /// <summary>
        /// The directory URI.
        /// </summary>
        private readonly Uri directoryUri;

        private JwsSigner jws;

        /// <summary>
        /// Initializes a new instance of the <see cref="AcmeContext"/> class.
        /// </summary>
        public AcmeContext() : this(WellKnownServers.LetsEncrypt)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AcmeContext"/> class.
        /// </summary>
        /// <param name="directoryUri">The directory URI.</param>
        public AcmeContext(Uri directoryUri)
        {
            this.directoryUri = directoryUri;
        }

        /// <summary>
        /// Fetches an ACME account by key.
        /// </summary>
        /// <param name="key">The account key.</param>
        /// <returns>
        /// The account fetched from ACME server.
        /// </returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<IAccountContext> Account(KeyInfo key)
        {
            this.jws = new JwsSigner(new AccountKey(key));
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates an ACME account.
        /// </summary>
        /// <param name="contact">The contact.</param>
        /// <param name="key">The account key to use with the account, optional.</param>
        /// <returns>
        /// The account created from ACME server.
        /// </returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<IAccountContext> Account(IList<string> contact, KeyInfo key = null)
        {
            throw new NotImplementedException();
        }
    }
}
