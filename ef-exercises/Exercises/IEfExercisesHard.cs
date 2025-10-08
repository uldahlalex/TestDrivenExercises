using EfExercises.Entities;

namespace EfExercises.Exercises;

/// <summary>
/// Level 3 - Hard exercises focusing on cardinality relationships and updates
/// </summary>
public interface IEfExercisesHard
{
    /// <summary>
    /// Exercise 11: Assign an employee to a project (many-to-many add)
    /// </summary>
    /// <param name="employeeId">ID of the employee</param>
    /// <param name="projectId">ID of the project</param>
    void AssignEmployeeToProject(int employeeId, int projectId);

    /// <summary>
    /// Exercise 12: Remove an employee from a project (many-to-many remove)
    /// </summary>
    /// <param name="employeeId">ID of the employee</param>
    /// <param name="projectId">ID of the project</param>
    void RemoveEmployeeFromProject(int employeeId, int projectId);

    /// <summary>
    /// Exercise 13: Transfer an employee to a different department (one-to-many update)
    /// </summary>
    /// <param name="employeeId">ID of the employee</param>
    /// <param name="newDepartmentId">ID of the new department</param>
    void TransferEmployeeToDepartment(int employeeId, int newDepartmentId);

    /// <summary>
    /// Exercise 14: Replace all employees on a project with a new set of employees
    /// </summary>
    /// <param name="projectId">ID of the project</param>
    /// <param name="newEmployeeIds">List of new employee IDs</param>
    void ReplaceProjectEmployees(int projectId, List<int> newEmployeeIds);

    /// <summary>
    /// Exercise 15: Get all projects an employee is assigned to with their details
    /// </summary>
    /// <param name="employeeId">ID of the employee</param>
    /// <returns>List of projects the employee is assigned to</returns>
    List<Project> GetEmployeeProjects(int employeeId);

    /// <summary>
    /// Exercise 16: Get all employees working on a specific project
    /// </summary>
    /// <param name="projectId">ID of the project</param>
    /// <returns>List of employees assigned to the project</returns>
    List<Employee> GetProjectEmployees(int projectId);

    /// <summary>
    /// Exercise 17: Bulk assign multiple employees to a project
    /// </summary>
    /// <param name="projectId">ID of the project</param>
    /// <param name="employeeIds">List of employee IDs to assign</param>
    void AssignMultipleEmployeesToProject(int projectId, List<int> employeeIds);

    /// <summary>
    /// Exercise 18: Get employees who are not assigned to any project
    /// </summary>
    /// <returns>List of employees without project assignments</returns>
    List<Employee> GetEmployeesWithoutProjects();

    /// <summary>
    /// Exercise 19: Get projects that have no employees assigned
    /// </summary>
    /// <returns>List of projects without any employees</returns>
    List<Project> GetProjectsWithoutEmployees();

    /// <summary>
    /// Exercise 20: Update an employee's department and assign them to a new project in one transaction
    /// </summary>
    /// <param name="employeeId">ID of the employee</param>
    /// <param name="newDepartmentId">ID of the new department</param>
    /// <param name="projectId">ID of the project to assign</param>
    void TransferEmployeeAndAssignProject(int employeeId, int newDepartmentId, int projectId);
}
