using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using tests.Data;
using tests.DTOs;
using tests.Entities;
using tests.Interfaces;

namespace tests.Exercises;

public class EfExercisesIdempotentUpdates(CompanyDbContext ctx) : IEfExercisesIdempotentUpdates
{
    public async Task<Employee> UpdateEmployee(UpdateEmployeeDto dto)
    {
        var employee = await ctx.Employees
            .Include(e => e.Department)
            .Include(e => e.Projects)
            .ThenInclude(p => p.Employees)
            .FirstAsync(e => e.Id == dto.Id);


        employee.Department = await ctx.Departments.FirstAsync(d => d.Id == dto.DepartmentId);


        employee.Projects.Clear();
        var projects = await ctx.Projects.Where(p => dto.ProjectIds.Contains(p.Id)).ToListAsync();
        if (dto.ProjectIds.Count != projects.Count)
            throw new ValidationException("One or more projects not found");
        foreach (var project in projects)
        {
            if (project == null)
                throw new ValidationException("Project not found");
            employee.Projects.Add(project);
        }


        employee.Salary = dto.Salary;
        employee.Email = dto.Email;
        employee.HireDate = dto.HireDate;
        employee.FirstName = dto.FirstName;
        employee.LastName = dto.LastName;
        await ctx.SaveChangesAsync();
        return employee;
    }

    public async Task<Project> UpdateProject(UpdateProjectDto dto)
    {
        var project = await ctx.Projects
            .Include(p => p.Employees)
            .ThenInclude(e => e.Department)
            .FirstAsync(p => p.Id == dto.Id);


        project.Employees.Clear();
        var employees = await ctx.Employees.Where(e => dto.EmployeeIds.Contains(e.Id)).ToListAsync();
        if (dto.EmployeeIds.Count != employees.Count)
            throw new ValidationException("One or more projects not found");
        foreach (var employee in employees)
        {
            if (employee == null)
                throw new ValidationException("Employee not found");
            project.Employees.Add(employee);
        }


        project.Budget = dto.Budget;
        project.Description = dto.Description;
        project.Name = dto.Name;
        project.EndDate = dto.EndDate;
        project.StartDate = dto.StartDate;
        project.EndDate = dto.EndDate;

        await ctx.SaveChangesAsync();

        return project;
    }

    public async Task<Department> UpdateDepartment(UpdateDepartmentDto dto)
    {
        var department = await ctx.Departments
            .Include(d => d.Employees)
            .ThenInclude(e => e.Projects)
            .FirstAsync(d => d.Id == dto.Id);


        var desiredEmployees = await ctx.Employees.Where(e => dto.EmployeeIds.Contains(e.Id)).ToListAsync();
        if (desiredEmployees.Count != dto.EmployeeIds.Count)
            throw new ValidationException("One ID does not exist");
        department.Employees.Clear();
        foreach (var desiredEmployee in desiredEmployees)
        {
            if (desiredEmployee == null)
                throw new ValidationException("Employee is null");
            department.Employees.Add(desiredEmployee);
        }


        department.Budget = dto.Budget;
        department.Name = dto.Name;
        department.Location = dto.Location;
        await ctx.SaveChangesAsync();
        return department;
    }
}
