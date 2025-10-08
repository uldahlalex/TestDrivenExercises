using EfExercises.Data;
using EfExercises.Entities;
using Microsoft.EntityFrameworkCore;

namespace EfExercises.Exercises;

public class EfExercisesHardSolution : IEfExercisesHard
{
    private readonly CompanyDbContext _context;

    public EfExercisesHardSolution(CompanyDbContext context)
    {
        _context = context;
    }

    public void AssignEmployeeToProject(int employeeId, int projectId)
    {
        var employee = _context.Employees.Include(e => e.Projects).FirstOrDefault(e => e.Id == employeeId);
        var project = _context.Projects.FirstOrDefault(p => p.Id == projectId);

        if (employee == null || project == null)
            throw new InvalidOperationException("Employee or Project not found");

        if (!employee.Projects.Contains(project))
        {
            employee.Projects.Add(project);
            _context.SaveChanges();
        }
    }

    public void RemoveEmployeeFromProject(int employeeId, int projectId)
    {
        var employee = _context.Employees.Include(e => e.Projects).FirstOrDefault(e => e.Id == employeeId);
        var project = employee?.Projects.FirstOrDefault(p => p.Id == projectId);

        if (employee == null || project == null)
            throw new InvalidOperationException("Employee or Project assignment not found");

        employee.Projects.Remove(project);
        _context.SaveChanges();
    }

    public void TransferEmployeeToDepartment(int employeeId, int newDepartmentId)
    {
        var employee = _context.Employees.FirstOrDefault(e => e.Id == employeeId);
        var department = _context.Departments.FirstOrDefault(d => d.Id == newDepartmentId);

        if (employee == null || department == null)
            throw new InvalidOperationException("Employee or Department not found");

        employee.DepartmentId = newDepartmentId;
        _context.SaveChanges();
    }

    public void ReplaceProjectEmployees(int projectId, List<int> newEmployeeIds)
    {
        var project = _context.Projects.Include(p => p.Employees).FirstOrDefault(p => p.Id == projectId);

        if (project == null)
            throw new InvalidOperationException("Project not found");

        // Clear existing employees
        project.Employees.Clear();

        // Add new employees
        var newEmployees = _context.Employees.Where(e => newEmployeeIds.Contains(e.Id)).ToList();
        foreach (var employee in newEmployees)
        {
            project.Employees.Add(employee);
        }

        _context.SaveChanges();
    }

    public List<Project> GetEmployeeProjects(int employeeId)
    {
        return _context.Employees
            .Where(e => e.Id == employeeId)
            .SelectMany(e => e.Projects)
            .ToList();
    }

    public List<Employee> GetProjectEmployees(int projectId)
    {
        return _context.Projects
            .Where(p => p.Id == projectId)
            .SelectMany(p => p.Employees)
            .ToList();
    }

    public void AssignMultipleEmployeesToProject(int projectId, List<int> employeeIds)
    {
        var project = _context.Projects.Include(p => p.Employees).FirstOrDefault(p => p.Id == projectId);

        if (project == null)
            throw new InvalidOperationException("Project not found");

        var employees = _context.Employees.Where(e => employeeIds.Contains(e.Id)).ToList();

        foreach (var employee in employees)
        {
            if (!project.Employees.Contains(employee))
            {
                project.Employees.Add(employee);
            }
        }

        _context.SaveChanges();
    }

    public List<Employee> GetEmployeesWithoutProjects()
    {
        return _context.Employees
            .Include(e => e.Projects)
            .Where(e => e.Projects.Count == 0)
            .ToList();
    }

    public List<Project> GetProjectsWithoutEmployees()
    {
        return _context.Projects
            .Include(p => p.Employees)
            .Where(p => p.Employees.Count == 0)
            .ToList();
    }

    public void TransferEmployeeAndAssignProject(int employeeId, int newDepartmentId, int projectId)
    {
        var employee = _context.Employees.Include(e => e.Projects).FirstOrDefault(e => e.Id == employeeId);
        var department = _context.Departments.FirstOrDefault(d => d.Id == newDepartmentId);
        var project = _context.Projects.FirstOrDefault(p => p.Id == projectId);

        if (employee == null || department == null || project == null)
            throw new InvalidOperationException("Employee, Department, or Project not found");

        // Update department
        employee.DepartmentId = newDepartmentId;

        // Assign to project if not already assigned
        if (!employee.Projects.Contains(project))
        {
            employee.Projects.Add(project);
        }

        _context.SaveChanges();
    }
}
