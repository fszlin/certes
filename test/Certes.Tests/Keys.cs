namespace Certes
{
    public static class Keys
    {
        #region account keys
        internal const string RS256Key = @"-----BEGIN RSA PRIVATE KEY-----
MIIEpAIBAAKCAQEAmaeT6EsXTVHAdwuq3IlAl9uljXE5CnkRpr6uSw/Fk9nQshfZ
qKFdeZHkSBvIaLirE2ZidMEYy+rpS1O2j+viTG5U6bUSWo8aoeKoXwYfwbXNboEA
+P4HgGCjD22XaXAkBHdhgyZ0UBX2z+jCx1smd7nucsi4h4RhC/2cEB1x/mE6XS5V
lpvG91Hbcgml4cl0NZrWPtJ4DhFdPNUtQ8q3AYXkOr/OSFZgRKjesRaqfnSdJNAB
qlO/jEzAx0fgJfPZe1WlRWOfGRVBVopZ4/N5HpR/9lsNDzCZyidFsHwzvpkP6R6H
bS8CMrNWgtkTbnz27EVqIhkYdiPVIN2Xkwj0BQIDAQABAoIBADZcogksy/04f7n+
gEBPNPGlLcDJfo9Ufyi5zKtU1Ke3B82Cf6jYElyo/5V9ct2N/ES8U/gPuyimH0zd
vLjVkTZOUK9wtyIgACGnMb+QyJp9qkU+0evF4DyMtBQIx2lv2eZUoETIzZzp8Gzl
Ykgq2QTB3PlyR0r2zGj0HItdB5AiIYc/fd0EyZuZbR9yaYZyLsgZ1ZVMgT+Vxewe
q8talIgKS1hkV5vfnPaWlFlrlEiw02gtLdKrSXDHZF/YQNichT22kCWIXnUVel3w
J3GotxB1dS3Zl3dLy9GNIL61Sv7WS9ck+dSynkmjMARfzYb/OR3NcjG6U6CWOjua
ws+4NO8CgYEA9RvJVGk15SMIz0UQdLr39G2GRev7GsGp8yOps3ThksbHV16X0YGr
VSBiVg0RXrlEln6LFZLhX1mUFyU4muvsukchcVY+6wrjHo1lxi+lkCQw6X8bLYBh
sR/qvh1FOugRWPRbEvw6X34TGHZMEIEAZgWohJwrvH29GaNqGQF9yBcCgYEAoHt2
x3ujQGr+TFSMlTjzZGh0KNBJfmi9fqswuKAN8plpN2EUxUxyx+Z9eVfxsMqlaAOC
POg9e6w2OUGOINqY4rVNSKmTDqyZt3l7hRf/F5MGF4IwJFrf2vnXi9J05fIRhntN
88eVR+s8C2BIErZ+nOQW3vwp/fPSKCl6xt1W2kMCgYEA7RI/HOHWOxYzwyYt0Hm2
sjftE1w3t51xX7YywVxfgbO5wwu/E+izatazxj9FebUAXf9vTyKN0ce2sNxTb6SZ
h2KdOh4NJRGWs0pYEeSr4kre9Dj4dH4j/0m7pr5w45TqkntmS2qimxI6LgLdd2kO
lZ8La5gRxXJooI+qYx1KJ2kCgYEAgUGP9LG3Vl4WvgHnTRUWrEuZK3o/h4ny/kAB
uOJo+XnojcsqgJLHbAxQCVj0+JDvUmxpDId9ZcvZzJnpULrgItEmEx1JrasFEArP
mu4t7p3jbTHVdqKe5G8Q1bRHpSEoTmmn6m5Y5RkpPnxo8HF+Xv0zEpyFcd5c6S4n
jl6d4qMCgYAEcJwSR8ws9e3FOKWxPa2XaDrsmzLNqYldQSpcgXqw/qkhOcwCH9if
vfcrTi64g4ZJKwd6oPnzkYHho2I0Q3dkPwMAWUHaaWjTAWmiEromE4z9QNb+Inkx
qeIAldJH4zaLqkEXH/643NjLFeQy7w4cbdODASwGqBEJa9SJJjMfbw==
-----END RSA PRIVATE KEY-----";

        internal const string ES256Key = @"-----BEGIN EC PRIVATE KEY-----
MDECAQEEIJTKjLb/7vi68uMaaktLL8A8uKeM5r2ibBJm1KPG/xhVoAoGCCqGSM49
AwEH
-----END EC PRIVATE KEY-----";

        // Special case: X coord of EC point is a 31 bytes instead of 32 and requires padding before encoding
        internal const string ES256Key_Alt1 = @"-----BEGIN EC PRIVATE KEY-----
MHcCAQEEIDWajU0PyhYKeulfy/luNtkAve7DkwQ01bXJ97zbxB66oAoGCCqGSM49
AwEHoUQDQgAEAJz0yAAXAwEmOhTRkjXxwgedbWO6gobYM3lWszrS68G8QSzhXR6A
mQ3IzZDimnTTXO7XhVylDT8SLzE44/Epmw==
-----END EC PRIVATE KEY-----";

        internal const string ES384Key = @"-----BEGIN EC PRIVATE KEY-----
MD4CAQEEMIipuyxYYQecexCUNx5U9bo+OWhHm7ZfdxRY3juNkNmNTD7r87KHZkNr
LXMS57GJOqAHBgUrgQQAIg==
-----END EC PRIVATE KEY-----";

        internal const string ES512Key = @"-----BEGIN EC PRIVATE KEY-----
MFACAQEEQgHjbZs0LAlpMoTgyDNQ5JJajX8TTk0jlm+SPOmP0nJcMb2fK5oqY4z8
Aq8YDXfnAZVZ2xQJkiysQjyDoJrNpz5kBKAHBgUrgQQAIw==
-----END EC PRIVATE KEY-----";
        #endregion
    }
}
