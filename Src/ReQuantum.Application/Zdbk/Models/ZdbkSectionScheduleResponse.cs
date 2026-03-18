using System.Text.Json.Serialization;

namespace ReQuantum.Application.Zdbk.Models;

public class ZdbkSectionScheduleResponse
{
    [JsonPropertyName("kbList")]
    public List<ZdbkSectionDto> SectionList { get; set; } = [];

    [JsonPropertyName("xnm")]
    public string? AcademicYear { get; set; }

    [JsonPropertyName("xqm")]
    public string? Semester { get; set; }

    [JsonIgnore]
    public string[]? RelatedSemesters { get; set; }

    [JsonPropertyName("xh")]
    public string? StudentId { get; set; }

    [JsonPropertyName("xm")]
    public string? StudentName { get; set; }

    [JsonPropertyName("xzb")]
    public string? AdministrativeClass { get; set; }

    [JsonPropertyName("xy")]
    public string? College { get; set; }

    [JsonPropertyName("zy")]
    public string? Major { get; set; }
}
