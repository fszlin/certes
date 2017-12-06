﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Certes.Pkcs;
using Newtonsoft.Json;
using Xunit;

namespace Certes.Crypto
{
    public class SignatureKeyTests
    {
        [Theory]
        [InlineData(SignatureAlgorithm.RS256)]
        [InlineData(SignatureAlgorithm.ES256)]
        [InlineData(SignatureAlgorithm.ES384)]
        [InlineData(SignatureAlgorithm.ES512)]
        private void CanExportKey(SignatureAlgorithm signatureAlgorithm)
        {
            var provider = new SignatureAlgorithmProvider();
            var algo = provider.Get(signatureAlgorithm);
            var key = algo.GenerateKey();
            Assert.NotNull(key);

            using (var buffer = new MemoryStream())
            {
                key.Save(buffer);

                buffer.Seek(0, SeekOrigin.Begin);
                var exported = provider.GetKey(buffer);

                Assert.Equal(
                    JsonConvert.SerializeObject(key.JsonWebKey),
                    JsonConvert.SerializeObject(exported.JsonWebKey));

                buffer.Seek(0, SeekOrigin.Begin);
                exported = algo.ReadKey(buffer);

                Assert.Equal(
                    JsonConvert.SerializeObject(key.JsonWebKey),
                    JsonConvert.SerializeObject(exported.JsonWebKey));
            }
        }
    }
}