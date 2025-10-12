namespace tests.DTOs;

/// <summary>
/// DTO representing the desired end state for a Department.
/// </summary>
public class UpdateDepartmentDto
{
    /// <summary>
    /// The ID of the department to update
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Desired name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Desired location
    /// </summary>
    public string Location { get; set; }

    /// <summary>
    /// Desired budget
    /// </summary>
    public double Budget { get; set; }

    /// <summary>
    /// Desired employee IDs
    /// Sets which employees should belong to this department (one-to-many relationship)
    /// Note: This will update the DepartmentId on the employees
    /// </summary>
    public List<int> EmployeeIds { get; set; }
}
