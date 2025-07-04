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

    public  List<Employee> GetEmployeesByDepartmentAsync(string departmentName)
    {
        return   _context.Employees.Where(e => e.Department.Name.Equals(departmentName)).ToList();
    }

    public double GetTotalSalaryByDepartmentAsync(string departmentName)
    {
        var employees =  _context.Employees.Where(e => e.Department.Name.Equals(departmentName));
        var result = employees.Sum(e => e.Salary);
        return result;
    }

    public List<Employee> GetEmployeesWithSalaryAboveAsync(double minSalary)
    {
        return _context.Employees.Where(e => e.Salary > minSalary).ToList();
    }

    public List<Employee> GetEmployeesByHireYearAsync(int year)
    {
        return _context.Employees.Where(e => e.HireDate.Year == year).ToList();
    }

    public Department GetDepartmentWithHighestBudgetAsync()
    {
        return _context.Departments.OrderByDescending(d => d.Budget).First();
    }
}