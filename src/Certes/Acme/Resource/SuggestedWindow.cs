using System;
using Newtonsoft.Json;

namespace Certes.Acme.Resource;

/// <summary>
/// Reflects the recommended renewal window for a certificate.
/// </summary>
public sealed class SuggestedWindow
{
    /// <summary>
    /// The start of the recommended renewal period.
    /// </summary>
    [JsonProperty("start")]
    public DateTimeOffset Start { get; set; }
    
    /// <summary>
    /// The end of the recommended renewal period.
    /// </summary>
    [JsonProperty("end")]
    public DateTimeOffset End { get; set; }
}
