using tests.DTOs;
using tests.Entities;

namespace tests.Interfaces;

/// <summary>
/// Soft Deletes with Query Filters
/// Focus on soft delete patterns, query filters, and maintaining referential integrity
/// </summary>
public interface IEfExercisesSoftDeletes
{
    /// <summary>
    /// Soft delete an employee by marking IsDeleted = true and setting DeletedAt timestamp.
    /// Should also handle cascading soft deletes for related entities if configured.
    /// </summary>
    Task<Employee> SoftDeleteEmployee(int id);

    /// <summary>
    /// Restore a soft-deleted employee by marking IsDeleted = false and clearing DeletedAt.
    /// Should validate that the employee was actually soft-deleted before restoring.
    /// </summary>
    Task<Employee> RestoreEmployee(int id);

    /// <summary>
    /// Soft delete a department. Must handle employees in that department appropriately.
    /// Should throw if department has non-deleted employees (referential integrity).
    /// </summary>
    Task<Department> SoftDeleteDepartment(int id);

    /// <summary>
    /// Restore a soft-deleted department.
    /// </summary>
    Task<Department> RestoreDepartment(int id);

    /// <summary>
    /// Soft delete a project. Must handle employees assigned to that project.
    /// Should remove many-to-many relationships before soft deleting.
    /// </summary>
    Task<Project> SoftDeleteProject(int id);

    /// <summary>
    /// Restore a soft-deleted project.
    /// </summary>
    Task<Project> RestoreProject(int id);

    /// <summary>
    /// Get all employees including soft-deleted ones (ignore query filter).
    /// Useful for admin views and audit trails.
    /// </summary>
    Task<List<Employee>> GetAllEmployeesIncludingDeleted();

    /// <summary>
    /// Get all active (non-deleted) employees.
    /// Should respect the query filter.
    /// </summary>
    Task<List<Employee>> GetActiveEmployees();

    /// <summary>
    /// Get only soft-deleted employees.
    /// </summary>
    Task<List<Employee>> GetDeletedEmployees();

    /// <summary>
    /// Permanently delete an employee (hard delete).
    /// Should only work on already soft-deleted employees.
    /// </summary>
    Task HardDeleteEmployee(int id);
}
