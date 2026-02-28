using System.Text.Json.Serialization;

namespace ReQuantum.Application.Models.CoursesZju;

public class CoursesZjuTodosResponse
{
    [JsonPropertyName("todo_list")]
    public required List<CoursesZjuTodoDto> TodoList { get; set; }
}
