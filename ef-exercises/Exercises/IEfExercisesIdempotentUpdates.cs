using EfExercises.DTOs;
using EfExercises.Entities;

namespace EfExercises.Exercises;

/// <summary>
/// Level 4 - Idempotent Updates with DTOs
/// Focus on proper entity state management and relationship handling
/// </summary>
public interface IEfExercisesIdempotentUpdates
{
    /// <summary>
    /// Exercise 21: Update an employee to match the desired state specified in the DTO.
    /// Should be idempotent - calling multiple times with same DTO produces same result.
    /// Must properly handle:
    /// - Scalar property updates
    /// - One-to-many relationship changes (Department)
    /// - Many-to-many relationship changes (Projects)
    /// </summary>
    /// <param name="dto">DTO specifying the desired end state</param>
    /// <returns>The updated employee with all navigation properties loaded</returns>
    Employee UpdateEmployee(UpdateEmployeeDto dto);

    /// <summary>
    /// Exercise 22: Update a project to match the desired state specified in the DTO.
    /// Should be idempotent - calling multiple times with same DTO produces same result.
    /// Must properly handle:
    /// - Scalar property updates
    /// - Nullable field updates with explicit null handling
    /// - Many-to-many relationship changes (Employees)
    /// </summary>
    /// <param name="dto">DTO specifying the desired end state</param>
    /// <returns>The updated project with all navigation properties loaded</returns>
    Project UpdateProject(UpdateProjectDto dto);

    /// <summary>
    /// Exercise 23: Update a department to match the desired state specified in the DTO.
    /// Should be idempotent - calling multiple times with same DTO produces same result.
    /// Must properly handle:
    /// - Scalar property updates
    /// - One-to-many relationship changes (Employees)
    /// - Transferring employees between departments correctly
    /// </summary>
    /// <param name="dto">DTO specifying the desired end state</param>
    /// <returns>The updated department with all navigation properties loaded</returns>
    Department UpdateDepartment(UpdateDepartmentDto dto);
}
