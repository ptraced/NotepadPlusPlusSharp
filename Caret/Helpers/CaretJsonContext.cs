using System.Text.Json.Serialization;

namespace Caret.Helpers;

[JsonSerializable(typeof(SessionData))]
[JsonSerializable(typeof(List<string>))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class CaretJsonContext : JsonSerializerContext;
