namespace EfExercises.DTOs;

/// <summary>
/// DTO representing the desired end state for a Project.
/// All properties are optional - only specified properties will be updated.
/// </summary>
public class UpdateProjectDto
{
    /// <summary>
    /// The ID of the project to update
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Desired name (null = no change)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Desired description (null = no change)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Desired start date (null = no change)
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Desired end date (null = no change, explicit null in DTO = set to null in DB)
    /// Use EndDateAction to control behavior
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Controls EndDate update behavior
    /// </summary>
    public NullableFieldAction? EndDateAction { get; set; }

    /// <summary>
    /// Desired budget (null = no change)
    /// </summary>
    public double? Budget { get; set; }

    /// <summary>
    /// Desired employee IDs (null = no change, empty list = remove all employees)
    /// Sets the exact many-to-many relationship state
    /// </summary>
    public List<int>? EmployeeIds { get; set; }
}

/// <summary>
/// Defines how to handle nullable field updates
/// </summary>
public enum NullableFieldAction
{
    /// <summary>
    /// Don't change the field
    /// </summary>
    NoChange,

    /// <summary>
    /// Set the field to null
    /// </summary>
    SetNull,

    /// <summary>
    /// Set the field to the provided value
    /// </summary>
    SetValue
}
