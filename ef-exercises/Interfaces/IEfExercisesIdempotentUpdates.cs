using tests.DTOs;
using tests.Entities;

namespace tests.Interfaces;

/// <summary>
/// Idempotent Updates with DTOs
/// Focus on proper entity state management and relationship handling
/// </summary>
public interface IEfExercisesIdempotentUpdates
{
    Task<Employee> UpdateEmployee(UpdateEmployeeDto dto);

    Task<Project> UpdateProject(UpdateProjectDto dto);

    Task<Department> UpdateDepartment(UpdateDepartmentDto dto);
}
