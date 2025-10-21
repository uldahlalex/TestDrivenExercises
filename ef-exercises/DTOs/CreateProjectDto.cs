using System.ComponentModel.DataAnnotations;

namespace tests.DTOs;

public class CreateProjectDto
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

    public List<int> EmployeeIds { get; set; } = new List<int>();
}
