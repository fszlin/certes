using Newtonsoft.Json;
using System;

namespace Certes.Acme
{
    /// <summary>
    /// Represents the ACME directory.
    /// </summary>
    [Obsolete("Use Resource.Directory instead.")]
    public class AcmeDirectory : Resource.Directory
    {
        /// <summary>
        /// Gets or sets the metadata.
        /// </summary>
        /// <value>
        /// The metadata.
        /// </value>
        [JsonProperty("meta")]
        [Obsolete("Use Resource.DirectoryMeta instead.")]
        public new AcmeDirectoryMeta Meta
        {
            get
            {
                if (base.Meta == null)
                {
                    return null;
                }

                return new AcmeDirectoryMeta
                {
                    CaaIdentities = base.Meta.CaaIdentities,
                    TermsOfService = base.Meta.TermsOfService,
                    Website = base.Meta.Website
                };
            }
            set
            {
                if (value == null)
                {
                    base.Meta = null;
                }
                else
                {
                    var meta = base.Meta ?? (base.Meta = new Resource.DirectoryMeta());
                    meta.Website = value.Website;
                    meta.CaaIdentities = value.CaaIdentities;
                    meta.TermsOfService = value.TermsOfService;
                }
            }
        }

        /// <summary>
        /// Represents the metadata for ACME directory.
        /// </summary>
        [Obsolete("Use Resource.DirectoryMeta instead.")]
        public class AcmeDirectoryMeta : Resource.DirectoryMeta
        {
        }
    }
}
