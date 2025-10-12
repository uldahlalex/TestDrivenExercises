using System.Text.Json;
using System.Text.Json.Serialization;
using Docker.DotNet.Models;
using Microsoft.EntityFrameworkCore;
using tests.Data;
using tests.Entities;
using tests.Interfaces;

namespace tests.Exercises;

public class EfExercises(CompanyDbContext context) : IEfExercises
{
    public async Task<List<Employee>> GetEmployeesByDepartment(string departmentName)
    {
        return await context.Employees.Where(e => e.Department.Name.Equals(departmentName)).ToListAsync();
    }

    public async Task<double> GetTotalSalaryByDepartment(string departmentName)
    {
        return await context.Employees
            .Where(e => e.Department.Name.Equals(departmentName))
            .Select(e => e.Salary)
            .SumAsync();
    }

    public async Task<List<Employee>> GetEmployeesWithSalaryAbove(double minSalary)
    {
        return await context.Employees.Where(e => e.Salary > minSalary).ToListAsync();
    }

    public async Task<List<Employee>> GetEmployeesByHireYear(int year)
    {
        return await context.Employees.Where(e => e.HireDate.Year == year).ToListAsync();
    }

    public async Task<Department> GetDepartmentWithHighestBudget()
    {
        return await context.Departments.OrderByDescending(d => d.Budget).FirstAsync();
    }


    public async Task<List<Employee>> GetEmployeesHiredBetween(DateTime startDate, DateTime endDate)
    {
        return await context.Employees.Where(e => e.HireDate > startDate && e.HireDate < endDate).ToListAsync();
    }

    public async Task<List<Employee>> GetTopNHighestPaidEmployees(int count)
    {
        return await context.Employees.OrderByDescending(e => e.Salary).Take(count).ToListAsync();
    }

    public async Task<List<Department>> GetDepartmentsWithAverageSalaryAbove(double minAverageSalary)
    {
        return await context.Departments.Select(d =>
                new
                {
                    dept = d,
                    empAvgSalary = d.Employees.Sum(e => e.Salary) / d.Employees.Count,
                }).Where(d => d.empAvgSalary > minAverageSalary)
            .Select(d => d.dept)
            .ToListAsync();
    }
}
