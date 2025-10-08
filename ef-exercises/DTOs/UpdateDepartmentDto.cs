namespace EfExercises.DTOs;

/// <summary>
/// DTO representing the desired end state for a Department.
/// All properties are optional - only specified properties will be updated.
/// </summary>
public class UpdateDepartmentDto
{
    /// <summary>
    /// The ID of the department to update
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Desired name (null = no change)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Desired location (null = no change)
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Desired budget (null = no change)
    /// </summary>
    public double? Budget { get; set; }

    /// <summary>
    /// Desired employee IDs (null = no change, empty list = transfer all employees away)
    /// Sets which employees should belong to this department (one-to-many relationship)
    /// Note: This will update the DepartmentId on the employees
    /// </summary>
    public List<int>? EmployeeIds { get; set; }
}
