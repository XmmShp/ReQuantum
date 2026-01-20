using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReQuantum.Modules.Pta.Models;

/// <summary>
/// PTA 习题集列表响应
/// </summary>
public class PtaProblemSetsResponse
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("problemSets")]
    public List<PtaProblemSet> ProblemSets { get; set; } = [];
}
