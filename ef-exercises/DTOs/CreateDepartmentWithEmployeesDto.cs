using System.ComponentModel.DataAnnotations;

namespace tests.DTOs;

public class CreateDepartmentWithEmployeesDto
{
    [Required]
    [MinLength(1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string Location { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public double Budget { get; set; }

    [Required]
    [MinLength(2)]
    public List<CreateEmployeeDto> Employees { get; set; } = new List<CreateEmployeeDto>();
}
