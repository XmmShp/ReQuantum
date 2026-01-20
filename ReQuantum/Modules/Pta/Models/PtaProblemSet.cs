using System;
using System.Text.Json.Serialization;

namespace ReQuantum.Modules.Pta.Models;

/// <summary>
/// PTA 习题集/作业
/// </summary>
public class PtaProblemSet
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("timeType")]
    public string TimeType { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("organizationName")]
    public string OrganizationName { get; set; } = string.Empty;

    [JsonPropertyName("ownerNickname")]
    public string OwnerNickname { get; set; } = string.Empty;

    [JsonPropertyName("manageable")]
    public bool Manageable { get; set; }

    [JsonPropertyName("createAt")]
    public DateTime CreateAt { get; set; }

    [JsonPropertyName("updateAt")]
    public DateTime UpdateAt { get; set; }

    [JsonPropertyName("scoringRule")]
    public string ScoringRule { get; set; } = string.Empty;

    [JsonPropertyName("organizationType")]
    public string OrganizationType { get; set; } = string.Empty;

    [JsonPropertyName("ownerId")]
    public string OwnerId { get; set; } = string.Empty;

    [JsonPropertyName("startAt")]
    public DateTime StartAt { get; set; }

    [JsonPropertyName("endAt")]
    public DateTime EndAt { get; set; }

    [JsonPropertyName("duration")]
    public int Duration { get; set; }

    [JsonPropertyName("ownerOrganizationId")]
    public string OwnerOrganizationId { get; set; } = string.Empty;

    [JsonPropertyName("stage")]
    public string Stage { get; set; } = string.Empty;

    [JsonPropertyName("feature")]
    public string Feature { get; set; } = string.Empty;

    [JsonPropertyName("sourceProblemSetId")]
    public string SourceProblemSetId { get; set; } = string.Empty;
}
