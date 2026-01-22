using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReQuantum.Modules.CoursesZju.Models;

public class CoursesZjuTodosResponse
{
    [JsonPropertyName("todo_list")]
    public required List<CoursesZjuTodoDto> TodoList { get; set; }
}
