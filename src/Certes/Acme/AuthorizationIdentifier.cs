namespace Certes.Acme
{
    public class AuthorizationIdentifier
    {
        public string Type { get; set; }
        public string Value { get; set; }

        public override int GetHashCode()
        {
            return (Type ?? "").GetHashCode() ^ (Value ?? "").GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = obj as AuthorizationIdentifier;
            return other?.Type == this.Type && other?.Value == this.Value;
        }
    }
}
