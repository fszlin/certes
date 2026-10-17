using System;
using System.Collections.Generic;
using System.Linq;

namespace Certes.Acme
{
    /// <summary>
    /// A single DNS TXT record for draft-ietf-acme-dns-persist-02.
    /// </summary>
    public sealed class DnsPersistRecord
    {
        internal DnsPersistRecord(string name, string value)
        {
            Name = name;
            Value = value;
            var chunks = new List<string>();
            for (var offset = 0; offset < value.Length; offset += 255)
            {
                chunks.Add(value.Substring(offset, Math.Min(255, value.Length - offset)));
            }

            if (value.Length + chunks.Count > ushort.MaxValue)
            {
                throw new AcmeException("The persistent DNS TXT record exceeds the DNS RDATA length limit.");
            }

            TextChunks = chunks.AsReadOnly();
        }

        /// <summary>
        /// Gets the normalized, absolute record owner name, without a trailing dot.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the unquoted TXT value for DNS APIs accepting a complete value.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Gets the ASCII character-strings (at most 255 octets each) of this one TXT record.
        /// These strings must be concatenated, not published as separate TXT records.
        /// </summary>
        public IReadOnlyList<string> TextChunks { get; }

        /// <summary>
        /// Gets the quoted and escaped RDATA suitable for a DNS zone file.
        /// </summary>
        public string ZoneFileValue => "(" + string.Join(" ", TextChunks.Select(
            text => "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"")) + ")";
    }
}
