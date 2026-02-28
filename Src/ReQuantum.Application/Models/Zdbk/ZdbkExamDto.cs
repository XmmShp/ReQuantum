using System.Text.Json.Serialization;

namespace ReQuantum.Application.Models.Zdbk;

public class ZdbkExamDto
{
    [JsonPropertyName("xkkh")]
    public string CourseId { get; set; } = string.Empty;

    [JsonPropertyName("kcmc")]
    public string CourseName { get; set; } = string.Empty;

    [JsonPropertyName("xf")]
    public string Credit { get; set; } = string.Empty;

    [JsonPropertyName("kssj")]
    public string? FinalExamTime { get; set; }

    [JsonPropertyName("jsmc")]
    public string? FinalExamLocation { get; set; }

    [JsonPropertyName("zwxh")]
    public string? FinalExamSeat { get; set; }

    [JsonPropertyName("qzkssj")]
    public string? MidTermExamTime { get; set; }

    [JsonPropertyName("qzjsmc")]
    public string? MidTermExamLocation { get; set; }

    [JsonPropertyName("qzzwxh")]
    public string? MidTermExamSeat { get; set; }
}

public class ZdbkExamResponse
{
    [JsonPropertyName("items")]
    public List<ZdbkExamDto> Items { get; set; } = [];
}
