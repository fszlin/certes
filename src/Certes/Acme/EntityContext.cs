﻿using System;
using System.Threading.Tasks;
using Certes.Properties;

namespace Certes.Acme
{
    /// <summary>
    /// Represents the context of ACME entity.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    internal class EntityContext<T>
    {
        /// <summary>
        /// Gets the context.
        /// </summary>
        /// <value>
        /// The context.
        /// </value>
        protected IAcmeContext Context { get; }

        /// <summary>
        /// Gets the entity location.
        /// </summary>
        /// <value>
        /// The entity location.
        /// </value>
        public Uri Location { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityContext{T}"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="location">The location.</param>
        public EntityContext(
            IAcmeContext context,
            Uri location)
        {
            Context = context;
            Location = location;
        }

        /// <summary>
        /// Gets the resource entity data.
        /// </summary>
        /// <returns>The resource entity data.</returns>
        public virtual async Task<T> Resource()
        {
            var payload = await Context.Sign(null, Location);
            var resp = await Context.HttpClient.Post<T>(Location, payload, true);
            return resp.Resource;
        }
    }

}
