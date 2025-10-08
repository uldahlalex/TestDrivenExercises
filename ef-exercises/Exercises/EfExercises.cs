using System.Text.Json;
using System.Text.Json.Serialization;
using EfExercises.Data;
using EfExercises.Entities;
using Microsoft.EntityFrameworkCore;

namespace EfExercises.Exercises;

public class EfExercises : IEfExercises
{
    private readonly CompanyDbContext _context;

    public EfExercises(CompanyDbContext context)
    {
        _context = context;
    }

    public List<Employee> GetEmployeesByDepartment(string departmentName)
    {
        return _context.Employees.Where(e => e.Department.Name.Equals(departmentName)).ToList();
    }

    public double GetTotalSalaryByDepartment(string departmentName)
    {
        return _context.Employees
            .Where(e => e.Department.Name.Equals(departmentName))
            .Select(e => e.Salary)
            .Sum();
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
        return _context.Employees.Where(e => e.HireDate > startDate && e.HireDate < endDate).ToList();
    }

    public List<Employee> GetTopNHighestPaidEmployees(int count)
    {
        return _context.Employees.OrderByDescending(e => e.Salary).Take(count).ToList();
    }

    public List<Department> GetDepartmentsWithAverageSalaryAbove(double minAverageSalary)
    {
        var deptWithAvgSalary = _context.Departments.Include(d => d.Employees)
            .Select(d => new {dept = d, avgSalary = (d.Employees.Sum(e => e.Salary) / d.Employees.Count) });
        foreach (var VARIABLE in deptWithAvgSalary)
        {
            Console.WriteLine(JsonSerializer.Serialize(VARIABLE, new JsonSerializerOptions()
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            }));

        }
        return deptWithAvgSalary.Where(d => d.avgSalary > minAverageSalary).Select(d => d.dept).ToList();
    }
}