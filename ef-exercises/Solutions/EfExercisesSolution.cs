using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using tests.Data;
using tests.Entities;
using tests.Interfaces;

namespace tests.Solutions;

public class EfExercisesSolution : IEfExercises
{
    private readonly CompanyDbContext _context;

    public EfExercisesSolution(CompanyDbContext context)
    {
        _context = context;
    }

    public List<Employee> GetEmployeesByDepartment(string departmentName)
    {
        return _context.Employees.Where(e => e.Department.Name.Equals(departmentName)).ToList();
    }

    public double GetTotalSalaryByDepartment(string departmentName)
    {
        var employees = _context.Employees.Where(e => e.Department.Name.Equals(departmentName));
        var result = employees.Sum(e => e.Salary);
        return result;
    }

    public List<Employee> GetEmployeesWithSalaryAbove(double minSalary)
    {
        return _context.Employees.Where(e => e.Salary > minSalary).ToList();
    }

    public List<Employee> GetEmployeesByHireYear(int year)
    {
        return _context.Employees.Where(e => e.HireDate.Year == year).ToList();
    }

    public Department GetDepartmentWithHighestBudget()
    {
        return _context.Departments.OrderByDescending(d => d.Budget).First();
    }



    public List<Employee> GetEmployeesHiredBetween(DateTime startDate, DateTime endDate)
    {
        return _context.Employees
            .Where(e => e.HireDate >= startDate && e.HireDate <= endDate)
            .OrderBy(e => e.HireDate)
            .ToList();
    }

    public List<Employee> GetTopNHighestPaidEmployees(int count)
    {
        return _context.Employees
            .OrderByDescending(e => e.Salary)
            .Take(count)
            .ToList();
    }

    public List<Department> GetDepartmentsWithAverageSalaryAbove(double minAverageSalary)
    {
        var deptWithAvgSalary = _context.Departments.Include(d => d.Employees)
            .Select(d => new {dept = d, avgSalary = (d.Employees.Sum(e => e.Salary) / d.Employees.Count) });
        foreach (var VARIABLE in deptWithAvgSalary)
        {
            Console.WriteLine(JsonSerializer.Serialize(VARIABLE));

        }
        return deptWithAvgSalary.Where(d => d.avgSalary > minAverageSalary).Select(d => d.dept).ToList();
    }
}