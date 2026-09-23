using Newtonsoft.Json;

namespace Certes.Acme.Resource;

/// <summary>
/// Represents the renewal info for an ACME directory.
/// </summary>
public class RenewalInfo
{
    /// <summary>
    /// The recommended renewal period.
    /// </summary>
    [JsonProperty("suggestedWindow")]
    public SuggestedWindow SuggestedWindow { get; set; }
    
    /// <summary>
    /// Provides additional context about the renewal suggestion.
    /// </summary>
    [JsonProperty("explanationURL")]
    public string ExplanationUrl { get; set; }
}
