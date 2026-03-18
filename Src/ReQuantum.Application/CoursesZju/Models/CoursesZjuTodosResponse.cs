using System.Text.Json.Serialization;

namespace ReQuantum.Application.CoursesZju.Models;

public class CoursesZjuTodosResponse
{
    [JsonPropertyName("todo_list")]
    public required List<CoursesZjuTodoDto> TodoList { get; set; }
}
