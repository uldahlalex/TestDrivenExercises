namespace tests.DTOs;

/// <summary>
/// DTO for idempotent employee updates using the "desired end state" pattern.
/// This update is idempotent: calling it multiple times with the same DTO produces the same final state.
/// </summary>
public class UpdateEmployeeDto
{
    /// <summary>
    /// Required: The unique identifier of the employee to update.
    /// Must reference an existing employee or the update will throw InvalidOperationException.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The desired first name for this employee.
    /// </summary>
    public string FirstName { get; set; }

    /// <summary>
    /// The desired last name for this employee.
    /// </summary>
    public string LastName { get; set; }

    /// <summary>
    /// The desired email address for this employee.
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// The desired salary for this employee.
    /// </summary>
    public double Salary { get; set; }

    /// <summary>
    /// The desired hire date for this employee.
    /// </summary>
    public DateTime HireDate { get; set; }

    /// <summary>
    /// The desired department ID (one-to-many relationship).
    /// </summary>
    public int DepartmentId { get; set; }

    /// <summary>
    /// The desired complete set of project IDs (many-to-many relationship).
    /// This property defines the EXACT final state of the employee's project assignments.
    /// All IDs must reference existing projects
    /// </summary>
    public List<int> ProjectIds { get; set; }
}
