namespace WebWasm.Models;

public record Suggestion(Guid Id, Guid UserInfoId, string Name, Dictionary<string, string> Data, DateTime Created, DateTime? Applied);
