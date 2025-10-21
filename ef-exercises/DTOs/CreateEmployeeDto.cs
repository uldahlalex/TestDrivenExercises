using System.ComponentModel.DataAnnotations;

namespace tests.DTOs;

public class CreateEmployeeDto
{
    [Required]
    [MinLength(1)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public double Salary { get; set; }

    public DateTime HireDate { get; set; }

    [Required]
    public int DepartmentId { get; set; }

    public List<int> ProjectIds { get; set; } = new List<int>();
}
