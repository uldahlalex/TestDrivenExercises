using EfExercises.Data;
using EfExercises.Entities;
using Microsoft.EntityFrameworkCore;

namespace EfExercises.Exercises;

public class EfExercisesSolution : IEfExercises
{
    private readonly CompanyDbContext _context;

    public EfExercisesSolution(CompanyDbContext context)
    {
        _context = context;
    }

    public async Task<List<Employee>> GetEmployeesByDepartmentAsync(string departmentName)
    {
        return await _context.Employees
            .Include(e => e.Department)
            .Where(e => e.Department.Name == departmentName)
            .ToListAsync();
    }

    public async Task<double> GetTotalSalaryByDepartmentAsync(string departmentName)
    {
        return await _context.Employees
            .Where(e => e.Department.Name == departmentName)
            .SumAsync(e => e.Salary);
    }

    public async Task<List<Employee>> GetEmployeesWithSalaryAboveAsync(double minSalary)
    {
        return await _context.Employees
            .Include(e => e.Department)
            .Where(e => e.Salary > minSalary)
            .ToListAsync();
    }

    public async Task<List<Employee>> GetEmployeesByHireYearAsync(int year)
    {
        return await _context.Employees
            .Include(e => e.Department)
            .Where(e => e.HireDate.Year == year)
            .ToListAsync();
    }

    public async Task<Department?> GetDepartmentWithHighestBudgetAsync()
    {
        return await _context.Departments
            .OrderByDescending(d => d.Budget)
            .FirstOrDefaultAsync();
    }
}