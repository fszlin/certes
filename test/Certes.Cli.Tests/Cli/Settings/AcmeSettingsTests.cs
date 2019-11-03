using System;
using System.Text;
using Xunit;

namespace Certes.Cli.Settings
{
    public class AcmeSettingsTests
    {
        [Fact]
        public void CanGetSetProperties()
        {
            var entity = new AcmeSettings();
            entity.VerifyGetterSetter(a => a.Key, Encoding.UTF8.GetBytes("certes"));
            entity.VerifyGetterSetter(a => a.ServerUri, new Uri("http://certes.is.working"));
        }
    }
}
