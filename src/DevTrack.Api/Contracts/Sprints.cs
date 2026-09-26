using System.ComponentModel.DataAnnotations;

namespace DevTrack.Api.Contracts;

public sealed class SprintRequest : IValidatableObject
{
    [Required]
    public string Name { get; init; } = string.Empty;

    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }

    [Range(1, int.MaxValue)]
    public int ProjectId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate < StartDate)
        {
            yield return new ValidationResult(
                "EndDate must be on or after StartDate.",
                [nameof(EndDate)]);
        }
    }
}

public sealed record SprintResponse(
    int Id,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    int ProjectId,
    string? ProjectName);
