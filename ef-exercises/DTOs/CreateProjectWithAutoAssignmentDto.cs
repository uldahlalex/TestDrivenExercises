using System.ComponentModel.DataAnnotations;

namespace tests.DTOs;

public class CreateProjectWithAutoAssignmentDto
{
    [Required]
    [MinLength(1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [Range(0, double.MaxValue)]
    public double Budget { get; set; }

    [Required]
    public int DepartmentId { get; set; }
}
