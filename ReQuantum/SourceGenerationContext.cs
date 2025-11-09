using ReQuantum.Models;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReQuantum.Client;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ZjuSsoState))]
[JsonSerializable(typeof(List<CalendarNote>))]
[JsonSerializable(typeof(List<CalendarTodo>))]
[JsonSerializable(typeof(List<CalendarEvent>))]
public partial class SourceGenerationContext : JsonSerializerContext;