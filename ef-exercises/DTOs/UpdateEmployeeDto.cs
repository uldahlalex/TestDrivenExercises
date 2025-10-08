namespace EfExercises.DTOs;

/// <summary>
/// DTO representing the desired end state for an Employee.
/// All properties are optional - only specified properties will be updated.
/// </summary>
public class UpdateEmployeeDto
{
    /// <summary>
    /// The ID of the employee to update
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Desired first name (null = no change)
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Desired last name (null = no change)
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Desired email (null = no change)
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Desired salary (null = no change)
    /// </summary>
    public double? Salary { get; set; }

    /// <summary>
    /// Desired hire date (null = no change)
    /// </summary>
    public DateTime? HireDate { get; set; }

    /// <summary>
    /// Desired department ID (null = no change)
    /// Changes the one-to-many relationship
    /// </summary>
    public int? DepartmentId { get; set; }

    /// <summary>
    /// Desired project IDs (null = no change, empty list = remove all projects)
    /// Sets the exact many-to-many relationship state
    /// </summary>
    public List<int>? ProjectIds { get; set; }
}
