using System.Text.Json.Serialization;

namespace ReQuantum.Modules.Pta.Models;

[JsonSerializable(typeof(PtaUserInfoResponse))]
internal partial class PtaJsonContext : JsonSerializerContext
{
}

public class PtaUserInfoResponse
{
    [JsonPropertyName("user")]
    public PtaUser? User { get; set; }
}

public class PtaUser
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;
}
