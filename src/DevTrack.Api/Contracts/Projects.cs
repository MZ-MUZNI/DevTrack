using System.ComponentModel.DataAnnotations;

namespace DevTrack.Api.Contracts;

public sealed class ProjectRequest
{
    [Required, StringLength(200)]
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
}

public sealed record ProjectResponse(int Id, string Name, string? Description, DateTime CreatedAt);
