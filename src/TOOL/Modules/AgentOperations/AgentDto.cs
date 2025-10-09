// DTOs for Agent API
public record ChatRequest(
    string Prompt,
    int? TopK = null,
    double? Temperature = null,
    string? Model = null
);
