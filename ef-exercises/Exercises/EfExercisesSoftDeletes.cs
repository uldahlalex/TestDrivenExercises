using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using tests.Data;
using tests.Entities;
using tests.Interfaces;

namespace tests.Exercises;

public class EfExercisesSoftDeletes(CompanyDbContext ctx) : IEfExercisesSoftDeletes
{
    public async Task<Employee> SoftDeleteEmployee(int id)
    {
        var employee = await ctx.Employees
            .IgnoreQueryFilters()
            .Include(e => e.Projects)
            .FirstAsync(e => e.Id == id);

        // Clear project assignments before soft deleting
        employee.Projects.Clear();

        employee.IsDeleted = true;
        employee.DeletedAt = DateTime.UtcNow;

        await ctx.SaveChangesAsync();
        return employee;
    }

    public async Task<Employee> RestoreEmployee(int id)
    {
        var employee = await ctx.Employees
            .IgnoreQueryFilters()
            .FirstAsync(e => e.Id == id);

        employee.IsDeleted = false;
        employee.DeletedAt = null;

        await ctx.SaveChangesAsync();
        return employee;
    }

    public async Task<Department> SoftDeleteDepartment(int id)
    {
        var department = await ctx.Departments
            .IgnoreQueryFilters()
            .Include(d => d.Employees)
            .FirstAsync(d => d.Id == id);

        // Check if department has any active (non-deleted) employees
        var hasActiveEmployees = department.Employees.Any(e => !e.IsDeleted);
        if (hasActiveEmployees)
        {
            throw new ValidationException(
                "Cannot soft delete department with active employees. " +
                "Please soft delete or reassign all employees first.");
        }

        department.IsDeleted = true;
        department.DeletedAt = DateTime.UtcNow;

        await ctx.SaveChangesAsync();
        return department;
    }

    public async Task<Department> RestoreDepartment(int id)
    {
        var department = await ctx.Departments
            .IgnoreQueryFilters()
            .FirstAsync(d => d.Id == id);

        department.IsDeleted = false;
        department.DeletedAt = null;

        await ctx.SaveChangesAsync();
        return department;
    }

    public async Task<Project> SoftDeleteProject(int id)
    {
        var project = await ctx.Projects
            .IgnoreQueryFilters()
            .Include(p => p.Employees)
            .FirstAsync(p => p.Id == id);

        // Remove all employee assignments before soft deleting
        project.Employees.Clear();

        project.IsDeleted = true;
        project.DeletedAt = DateTime.UtcNow;

        await ctx.SaveChangesAsync();
        return project;
    }

    public async Task<Project> RestoreProject(int id)
    {
        var project = await ctx.Projects
            .IgnoreQueryFilters()
            .Include(p => p.Employees)
            .FirstAsync(p => p.Id == id);

        project.IsDeleted = false;
        project.DeletedAt = null;

        await ctx.SaveChangesAsync();
        return project;
    }

    public async Task<List<Employee>> GetAllEmployeesIncludingDeleted()
    {
        return await ctx.Employees
            .IgnoreQueryFilters()
            .ToListAsync();
    }

    public async Task<List<Employee>> GetActiveEmployees()
    {
        // This should use the query filter automatically
        return await ctx.Employees.ToListAsync();
    }

    public async Task<List<Employee>> GetDeletedEmployees()
    {
        return await ctx.Employees
            .IgnoreQueryFilters()
            .Where(e => e.IsDeleted)
            .ToListAsync();
    }

    public async Task HardDeleteEmployee(int id)
    {
        var employee = await ctx.Employees
            .IgnoreQueryFilters()
            .FirstAsync(e => e.Id == id);

        // Only allow hard delete if already soft deleted
        if (!employee.IsDeleted)
        {
            throw new ValidationException(
                "Cannot hard delete an active employee. " +
                "Please soft delete first, then hard delete.");
        }

        ctx.Employees.Remove(employee);
        await ctx.SaveChangesAsync();
    }
}
