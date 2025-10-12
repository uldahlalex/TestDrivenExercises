namespace tests.DTOs;

/// <summary>
/// DTO representing the desired end state for a Project.
/// </summary>
public class UpdateProjectDto
{
    /// <summary>
    /// The ID of the project to update
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Desired name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Desired description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Desired start date
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Desired end date (null = set the value to null in DB)
    /// </summary>
    public DateTime? EndDate { get; set; }



    /// <summary>
    /// Desired budget
    /// </summary>
    public double Budget { get; set; }

    /// <summary>
    /// Desired employee IDs (empty list = remove all employees)
    /// Sets the exact many-to-many relationship state
    /// </summary>
    public List<int> EmployeeIds { get; set; }
}
