using System.Text.Json;
using Certes.Json;
using Xunit;

namespace Certes.Acme.Resource
{
    public class EnumSerializationTests
    {
        [Fact]
        public void AccountStatusSerializesWithEnumMemberValue()
        {
            var options = JsonUtil.CreateSettings();
            var account = new Account { Status = AccountStatus.Valid };
            var json = JsonSerializer.Serialize(account, options);
            
            // Should contain "valid" (lowercase) not "Valid" (PascalCase)
            Assert.Contains("\"valid\"", json);
            Assert.DoesNotContain("\"Valid\"", json);
        }

        [Fact]
        public void OrderStatusSerializesWithEnumMemberValue()
        {
            var options = JsonUtil.CreateSettings();
            var order = new Order { Status = OrderStatus.Processing };
            var json = JsonSerializer.Serialize(order, options);
            
            Assert.Contains("\"processing\"", json);
            Assert.DoesNotContain("\"Processing\"", json);
        }

        [Fact]
        public void IdentifierTypeSerializesWithEnumMemberValue()
        {
            var options = JsonUtil.CreateSettings();
            var identifier = new Identifier { Type = IdentifierType.Dns };
            var json = JsonSerializer.Serialize(identifier, options);
            
            Assert.Contains("\"dns\"", json);
            Assert.DoesNotContain("\"Dns\"", json);
        }

        [Fact]
        public void ChallengeStatusSerializesWithEnumMemberValue()
        {
            var options = JsonUtil.CreateSettings();
            var challenge = new Challenge { Status = ChallengeStatus.Valid };
            var json = JsonSerializer.Serialize(challenge, options);
            
            Assert.Contains("\"valid\"", json);
            Assert.DoesNotContain("\"Valid\"", json);
        }
    }
}
