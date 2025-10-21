using tests.DTOs;
using tests.Entities;

namespace tests.Interfaces;

/// <summary>
/// Level 6 - Create Operations with Complex Business Rules
/// Focus on validating database state before creating entities and enforcing business constraints
/// </summary>
public interface IEfExercisesCreateWithBusinessRules
{
    /// <summary>
    /// Create a new employee with business rules:
    /// 1. Email must be unique across all employees (including soft-deleted)
    /// 2. Department must exist and not be soft-deleted
    /// 3. Salary must be within department budget constraints (no employee salary > 30% of dept budget)
    /// 4. Cannot assign more than 3 projects to a new employee
    /// 5. All assigned projects must exist and not be soft-deleted
    /// </summary>
    Task<Employee> CreateEmployee(CreateEmployeeDto dto);

    /// <summary>
    /// Create a new department with business rules:
    /// 1. Name must be unique (case-insensitive) across active departments
    /// 2. Budget must be at least $50,000
    /// 3. Location must not already have 3+ departments (facility constraint)
    /// 4. Cannot create more than 10 total active departments (company policy)
    /// </summary>
    Task<Department> CreateDepartment(CreateDepartmentDto dto);

    /// <summary>
    /// Create a new project with business rules:
    /// 1. Name must be unique across active projects
    /// 2. StartDate must be in the future or today
    /// 3. EndDate (if provided) must be after StartDate
    /// 4. Budget must be positive and at least $10,000
    /// 5. Total active project budgets cannot exceed $1,000,000 (company constraint)
    /// 6. Cannot assign employees from soft-deleted departments
    /// 7. Cannot assign more employees than the project can support (1 employee per $50k budget)
    /// </summary>
    Task<Project> CreateProject(CreateProjectDto dto);

    /// <summary>
    /// Hire employee with complex onboarding rules:
    /// 1. All CreateEmployee rules apply
    /// 2. If department is at capacity (>= 5 employees), cannot hire
    /// 3. Must assign to at least one active project
    /// 4. HireDate must be a weekday (Monday-Friday)
    /// 5. Cannot hire more than 2 employees on the same day (HR bandwidth constraint)
    /// </summary>
    Task<Employee> HireEmployeeWithOnboarding(CreateEmployeeDto dto);

    /// <summary>
    /// Create project with auto-assignment:
    /// 1. All CreateProject rules apply
    /// 2. Automatically assign the least-busy employees from specified department
    /// 3. "Least busy" = employees with fewest current project assignments
    /// 4. Number of employees to assign = floor(budget / $50,000)
    /// 5. Department must have enough available employees
    /// </summary>
    Task<Project> CreateProjectWithAutoAssignment(CreateProjectWithAutoAssignmentDto dto);

    /// <summary>
    /// Transfer employee to new department with validation:
    /// 1. Employee must exist and not be soft-deleted
    /// 2. Target department must exist and not be soft-deleted
    /// 3. Target department must have capacity (< 5 employees)
    /// 4. Employee salary must fit within target department budget constraints
    /// 5. If employee has projects, those projects must allow the department change
    ///    (project employees must be from max 2 different departments)
    /// </summary>
    Task<Employee> TransferEmployeeToDepartment(int employeeId, int targetDepartmentId);

    /// <summary>
    /// Promote employee with business rules:
    /// 1. Employee must exist, not be soft-deleted, and employed for at least 6 months
    /// 2. New salary must be 10-30% higher than current salary
    /// 3. New salary must still fit department budget constraints
    /// 4. Cannot promote more than 1 employee per department per month
    /// 5. Employee must be assigned to at least one project
    /// </summary>
    Task<Employee> PromoteEmployee(int employeeId, double newSalary);

    /// <summary>
    /// Close project with cleanup:
    /// 1. Project must exist and not be soft-deleted
    /// 2. Set EndDate to today if not already set
    /// 3. Unassign all employees from the project
    /// 4. Soft delete the project
    /// 5. Cannot close a project that started less than 30 days ago (minimum duration)
    /// </summary>
    Task<Project> CloseProject(int projectId);

    /// <summary>
    /// Restructure department (change employees):
    /// 1. Department must exist and not be soft-deleted
    /// 2. All new employees must exist and not be soft-deleted
    /// 3. Cannot remove an employee who is the only one on a critical project
    ///    (critical = budget > $100,000)
    /// 4. New department size must be between 1 and 5 employees
    /// 5. Total salary of new employees must fit department budget
    /// </summary>
    Task<Department> RestructureDepartment(int departmentId, List<int> newEmployeeIds);

    /// <summary>
    /// Batch hire employees for new department:
    /// 1. All CreateDepartment rules apply
    /// 2. Must hire at least 2 employees
    /// 3. All CreateEmployee rules apply to each employee
    /// 4. Total salaries must not exceed 80% of department budget
    /// 5. At least one employee must have salary >= $75,000 (manager requirement)
    /// 6. This is a transaction - if any employee fails validation, rollback department creation
    /// </summary>
    Task<Department> CreateDepartmentWithEmployees(CreateDepartmentWithEmployeesDto dto);
}
