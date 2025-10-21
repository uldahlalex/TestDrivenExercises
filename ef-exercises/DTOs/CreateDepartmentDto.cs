using System.ComponentModel.DataAnnotations;

namespace tests.DTOs;

public class CreateDepartmentDto
{
    [Required]
    [MinLength(1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string Location { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public double Budget { get; set; }
}
