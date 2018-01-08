using System;

namespace Certes.Acme
{
    /// <summary>
    /// Represents the identifier for ACME Authorization.
    /// </summary>
    public class AuthorizationIdentifier : IEquatable<AuthorizationIdentifier>
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        /// <seealso cref="AuthorizationIdentifierTypes"/>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public string Value { get; set; }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return (Type ?? "").GetHashCode() ^ (Value ?? "").GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as AuthorizationIdentifier);
        }

        /// <summary>
        /// Determines whether the specified <see cref="AuthorizationIdentifier" />, is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="AuthorizationIdentifier" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="AuthorizationIdentifier" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(AuthorizationIdentifier other)
        {
            return other?.Type == this.Type && other?.Value == this.Value;
        }
    }
}
