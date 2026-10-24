using System.ComponentModel.DataAnnotations;

namespace DevTrack.Web.Models;

public sealed class SprintFormViewModel : IValidatableObject
{
    public int Id { get; set; }
    [Required, StringLength(200)] public string Name { get; set; } = string.Empty;
    [DataType(DataType.Date)] public DateTime StartDate { get; set; }
    [DataType(DataType.Date)] public DateTime EndDate { get; set; }
    [Range(1, int.MaxValue)] public int ProjectId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate.Date < StartDate.Date)
            yield return new ValidationResult("End date must be on or after the start date.", [nameof(EndDate)]);
    }
}
