using Certes.Acme.Resource;
using Certes.Jws;
using Certes.Pkcs;
using System;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;

namespace Certes
{
    public static class Helper
    {
        internal const string PrivateKey = "MIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQCZp5PoSxdNUcB3C6rciUCX26WNcTkKeRGmvq5LD8WT2dCyF9mooV15keRIG8houKsTZmJ0wRjL6ulLU7aP6+JMblTptRJajxqh4qhfBh/Btc1ugQD4/geAYKMPbZdpcCQEd2GDJnRQFfbP6MLHWyZ3ue5yyLiHhGEL/ZwQHXH+YTpdLlWWm8b3UdtyCaXhyXQ1mtY+0ngOEV081S1DyrcBheQ6v85IVmBEqN6xFqp+dJ0k0AGqU7+MTMDHR+Al89l7VaVFY58ZFUFWilnj83kelH/2Ww0PMJnKJ0WwfDO+mQ/pHodtLwIys1aC2RNufPbsRWoiGRh2I9Ug3ZeTCPQFAgMBAAECggEANlyiCSzL/Th/uf6AQE808aUtwMl+j1R/KLnMq1TUp7cHzYJ/qNgSXKj/lX1y3Y38RLxT+A+7KKYfTN28uNWRNk5Qr3C3IiAAIacxv5DImn2qRT7R68XgPIy0FAjHaW/Z5lSgRMjNnOnwbOViSCrZBMHc+XJHSvbMaPQci10HkCIhhz993QTJm5ltH3JphnIuyBnVlUyBP5XF7B6ry1qUiApLWGRXm9+c9paUWWuUSLDTaC0t0qtJcMdkX9hA2JyFPbaQJYhedRV6XfAncai3EHV1LdmXd0vL0Y0gvrVK/tZL1yT51LKeSaMwBF/Nhv85Hc1yMbpToJY6O5rCz7g07wKBgQD1G8lUaTXlIwjPRRB0uvf0bYZF6/sawanzI6mzdOGSxsdXXpfRgatVIGJWDRFeuUSWfosVkuFfWZQXJTia6+y6RyFxVj7rCuMejWXGL6WQJDDpfxstgGGxH+q+HUU66BFY9FsS/DpffhMYdkwQgQBmBaiEnCu8fb0Zo2oZAX3IFwKBgQCge3bHe6NAav5MVIyVOPNkaHQo0El+aL1+qzC4oA3ymWk3YRTFTHLH5n15V/GwyqVoA4I86D17rDY5QY4g2pjitU1IqZMOrJm3eXuFF/8XkwYXgjAkWt/a+deL0nTl8hGGe03zx5VH6zwLYEgStn6c5Bbe/Cn989IoKXrG3VbaQwKBgQDtEj8c4dY7FjPDJi3QebayN+0TXDe3nXFftjLBXF+Bs7nDC78T6LNq1rPGP0V5tQBd/29PIo3Rx7aw3FNvpJmHYp06Hg0lEZazSlgR5KviSt70OPh0fiP/SbumvnDjlOqSe2ZLaqKbEjouAt13aQ6VnwtrmBHFcmigj6pjHUonaQKBgQCBQY/0sbdWXha+AedNFRasS5krej+HifL+QAG44mj5eeiNyyqAksdsDFAJWPT4kO9SbGkMh31ly9nMmelQuuAi0SYTHUmtqwUQCs+a7i3uneNtMdV2op7kbxDVtEelIShOaafqbljlGSk+fGjwcX5e/TMSnIVx3lzpLieOXp3iowKBgARwnBJHzCz17cU4pbE9rZdoOuybMs2piV1BKlyBerD+qSE5zAIf2J+99ytOLriDhkkrB3qg+fORgeGjYjRDd2Q/AwBZQdppaNMBaaISuiYTjP1A1v4ieTGp4gCV0kfjNouqQRcf/rjc2MsV5DLvDhxt04MBLAaoEQlr1IkmMx9v";
        
        internal static Directory AcmeDir = new Directory
        {
            Meta = new DirectoryMeta
            {
                TermsOfService = new Uri("http://example.com/tos.pdf")
            },
            NewAuthz = new Uri("http://example.com/new-authz"),
            NewCert = new Uri("http://example.com/new-cert"),
            NewReg = new Uri("http://example.com/new-reg"),
            RevokeCert = new Uri("http://example.com/revoke-cert")
        };

        internal static AccountKey Loadkey()
        {
            return new AccountKey(new KeyInfo { PrivateKeyInfo = Convert.FromBase64String(PrivateKey) });
        }

        internal static void VerifyGetterSetter<TSource, TProperty>(
            this TSource source,
            Expression<Func<TSource, TProperty>> propertyLambda,
            TProperty value)
        {
            var member = propertyLambda.Body as MemberExpression;
            var propInfo = member.Member as PropertyInfo;

            propInfo.SetValue(source, value);
            var actualValue = propInfo.GetValue(source);

            Assert.Equal(value, (TProperty)actualValue);
        }
    }
}
