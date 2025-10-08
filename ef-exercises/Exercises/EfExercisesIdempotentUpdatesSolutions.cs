using EfExercises.Data;
using EfExercises.DTOs;
using EfExercises.Entities;
using Microsoft.EntityFrameworkCore;

namespace EfExercises.Exercises;

public class EfExercisesIdempotentUpdatesSolutions : IEfExercisesIdempotentUpdates
{
    private readonly CompanyDbContext _context;

    public EfExercisesIdempotentUpdatesSolutions(CompanyDbContext context)
    {
        _context = context;
    }

    public Employee UpdateEmployee(UpdateEmployeeDto dto)
    {
        // Load the employee with all navigation properties
        var employee = _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Projects)
            .FirstOrDefault(e => e.Id == dto.Id);

        if (employee == null)
            throw new InvalidOperationException($"Employee with ID {dto.Id} not found");

        // Update scalar properties (only if specified in DTO)
        if (dto.FirstName != null)
            employee.FirstName = dto.FirstName;

        if (dto.LastName != null)
            employee.LastName = dto.LastName;

        if (dto.Email != null)
            employee.Email = dto.Email;

        if (dto.Salary.HasValue)
            employee.Salary = dto.Salary.Value;

        if (dto.HireDate.HasValue)
            employee.HireDate = dto.HireDate.Value;

        // Update one-to-many relationship (Department)
        if (dto.DepartmentId.HasValue)
        {
            var department = _context.Departments.Find(dto.DepartmentId.Value);
            if (department == null)
                throw new InvalidOperationException($"Department with ID {dto.DepartmentId.Value} not found");

            employee.DepartmentId = dto.DepartmentId.Value;
        }

        // Update many-to-many relationship (Projects)
        if (dto.ProjectIds != null)
        {
            // Get desired projects
            var desiredProjects = _context.Projects
                .Where(p => dto.ProjectIds.Contains(p.Id))
                .ToList();

            if (desiredProjects.Count != dto.ProjectIds.Count)
                throw new InvalidOperationException("One or more project IDs not found");

            // Clear current projects and add desired ones
            employee.Projects.Clear();
            foreach (var project in desiredProjects)
            {
                employee.Projects.Add(project);
            }
        }

        _context.SaveChanges();

        // Reload to ensure navigation properties are fresh
        _context.Entry(employee).Reload();
        _context.Entry(employee).Reference(e => e.Department).Load();
        _context.Entry(employee).Collection(e => e.Projects).Load();

        return employee;
    }

    public Project UpdateProject(UpdateProjectDto dto)
    {
        // Load the project with all navigation properties
        var project = _context.Projects
            .Include(p => p.Employees)
            .FirstOrDefault(p => p.Id == dto.Id);

        if (project == null)
            throw new InvalidOperationException($"Project with ID {dto.Id} not found");

        // Update scalar properties (only if specified in DTO)
        if (dto.Name != null)
            project.Name = dto.Name;

        if (dto.Description != null)
            project.Description = dto.Description;

        if (dto.StartDate.HasValue)
            project.StartDate = dto.StartDate.Value;

        if (dto.Budget.HasValue)
            project.Budget = dto.Budget.Value;

        // Handle nullable EndDate with explicit action
        if (dto.EndDateAction.HasValue)
        {
            switch (dto.EndDateAction.Value)
            {
                case NullableFieldAction.SetNull:
                    project.EndDate = null;
                    break;
                case NullableFieldAction.SetValue:
                    if (!dto.EndDate.HasValue)
                        throw new InvalidOperationException("EndDateAction is SetValue but EndDate is null");
                    project.EndDate = dto.EndDate.Value;
                    break;
                case NullableFieldAction.NoChange:
                    // Do nothing
                    break;
            }
        }

        // Update many-to-many relationship (Employees)
        if (dto.EmployeeIds != null)
        {
            // Get desired employees
            var desiredEmployees = _context.Employees
                .Where(e => dto.EmployeeIds.Contains(e.Id))
                .ToList();

            if (desiredEmployees.Count != dto.EmployeeIds.Count)
                throw new InvalidOperationException("One or more employee IDs not found");

            // Clear current employees and add desired ones
            project.Employees.Clear();
            foreach (var employee in desiredEmployees)
            {
                project.Employees.Add(employee);
            }
        }

        _context.SaveChanges();

        // Reload to ensure navigation properties are fresh
        _context.Entry(project).Reload();
        _context.Entry(project).Collection(p => p.Employees).Load();

        return project;
    }

    public Department UpdateDepartment(UpdateDepartmentDto dto)
    {
        // Load the department with all navigation properties
        var department = _context.Departments
            .Include(d => d.Employees)
            .FirstOrDefault(d => d.Id == dto.Id);

        if (department == null)
            throw new InvalidOperationException($"Department with ID {dto.Id} not found");

        // Update scalar properties (only if specified in DTO)
        if (dto.Name != null)
            department.Name = dto.Name;

        if (dto.Location != null)
            department.Location = dto.Location;

        if (dto.Budget.HasValue)
            department.Budget = dto.Budget.Value;

        // Update one-to-many relationship (Employees)
        if (dto.EmployeeIds != null)
        {
            // Get desired employees
            var desiredEmployees = _context.Employees
                .Where(e => dto.EmployeeIds.Contains(e.Id))
                .ToList();

            if (desiredEmployees.Count != dto.EmployeeIds.Count)
                throw new InvalidOperationException("One or more employee IDs not found");

            // Get current employee IDs
            var currentEmployeeIds = department.Employees.Select(e => e.Id).ToHashSet();
            var desiredEmployeeIds = dto.EmployeeIds.ToHashSet();

            // Remove employees that should no longer be in this department
            var employeesToRemove = department.Employees
                .Where(e => !desiredEmployeeIds.Contains(e.Id))
                .ToList();

            foreach (var employee in employeesToRemove)
            {
                // Note: Setting DepartmentId to null would violate FK constraint in typical scenarios
                // In a real system, you'd need a strategy (e.g., move to "Unassigned" department)
                // For this exercise, we'll throw if trying to orphan employees
                throw new InvalidOperationException(
                    $"Cannot remove employee {employee.Id} without assigning to another department. " +
                    "Employees must always belong to a department.");
            }

            // Add employees that should be in this department
            var employeesToAdd = desiredEmployees
                .Where(e => !currentEmployeeIds.Contains(e.Id))
                .ToList();

            foreach (var employee in employeesToAdd)
            {
                employee.DepartmentId = department.Id;
            }
        }

        _context.SaveChanges();

        // Reload to ensure navigation properties are fresh
        _context.Entry(department).Reload();
        _context.Entry(department).Collection(d => d.Employees).Load();

        return department;
    }
}
