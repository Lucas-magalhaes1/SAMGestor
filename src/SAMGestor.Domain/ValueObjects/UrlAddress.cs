using SAMGestor.Domain.Commom;

namespace SAMGestor.Domain.ValueObjects;
public sealed class UrlAddress : ValueObject
{
    public string Value { get; }
    private UrlAddress() { }
    public UrlAddress(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            throw new ArgumentException(nameof(value));
        Value = uri.ToString();
    }
    protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }
    public static implicit operator string(UrlAddress url) => url.Value;
}