using System.Text.Json.Serialization;

namespace ReQuantum.Application.Models.Pta;

public class PtaProblemSetsResponse
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("problemSets")]
    public List<PtaProblemSet> ProblemSets { get; set; } = [];
}
