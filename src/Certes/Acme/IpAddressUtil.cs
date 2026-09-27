using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace Certes.Acme
{
    /// <summary>
    /// Strict IP address parsing for ACME IP identifiers (RFC 8738).
    /// </summary>
    internal static class IpAddressUtil
    {
        /// <summary>
        /// Parses an IPv4 dotted-quad or IPv6 address and returns its canonical text form
        /// (RFC 5952 for IPv6). Shorthand IPv4 forms, leading zeros, and IPv6 zone IDs are rejected.
        /// </summary>
        public static bool TryNormalize(string value, out string normalized)
        {
            normalized = null;
            if (!TryParse(value, out var address))
            {
                return false;
            }

            normalized = address.ToString();
            return true;
        }

        /// <summary>
        /// Parses an IP address using the same strict rules as <see cref="TryNormalize"/>.
        /// </summary>
        public static bool TryParse(string value, out IPAddress address)
        {
            address = null;
            if (string.IsNullOrEmpty(value) || value.Trim() != value)
            {
                return false;
            }

            if (value.IndexOf(':') >= 0)
            {
                // IPAddress.TryParse also accepts endpoint forms such as "[::1]" and "[::1]:443"
                // and zone IDs ("%eth0"); allow only address literal characters.
                foreach (var c in value)
                {
                    if (!(c == ':' || c == '.' || (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                    {
                        return false;
                    }
                }

                if (!IPAddress.TryParse(value, out var v6) ||
                    v6.AddressFamily != AddressFamily.InterNetworkV6)
                {
                    return false;
                }

                address = v6;
                return true;
            }

            var parts = value.Split('.');
            if (parts.Length != 4)
            {
                return false;
            }

            var bytes = new byte[4];
            for (var i = 0; i < 4; i++)
            {
                var part = parts[i];
                if (part.Length == 0 || part.Length > 3 || (part.Length > 1 && part[0] == '0'))
                {
                    return false;
                }

                foreach (var c in part)
                {
                    if (c < '0' || c > '9')
                    {
                        return false;
                    }
                }

                var octet = int.Parse(part, NumberStyles.None, CultureInfo.InvariantCulture);
                if (octet > 255)
                {
                    return false;
                }

                bytes[i] = (byte)octet;
            }

            address = new IPAddress(bytes);
            return true;
        }
    }
}
