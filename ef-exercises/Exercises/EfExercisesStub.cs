using EfExercises.Data;
using EfExercises.Entities;

namespace EfExercises.Exercises;

public class EfExercisesStub : IEfExercises
{
    private readonly CompanyDbContext _context;

    public EfExercisesStub(CompanyDbContext context)
    {
        _context = context;
    }

    public Task<List<Employee>> GetEmployeesByDepartmentAsync(string departmentName)
    {
        // TODO: Implement this method
        // Hint: Use Include() to load the Department navigation property
        // Filter by department name and return the employees
        throw new NotImplementedException("Exercise 1 not implemented yet");
    }

    public Task<double> GetTotalSalaryByDepartmentAsync(string departmentName)
    {
        // TODO: Implement this method
        // Hint: Join employees with departments, filter by department name, and sum salaries
        throw new NotImplementedException("Exercise 2 not implemented yet");
    }

    public Task<List<Employee>> GetEmployeesWithSalaryAboveAsync(double minSalary)
    {
        // TODO: Implement this method
        // Hint: Use Where() to filter employees by salary and Include() to load Department
        throw new NotImplementedException("Exercise 3 not implemented yet");
    }

    public Task<List<Employee>> GetEmployeesByHireYearAsync(int year)
    {
        // TODO: Implement this method
        // Hint: Use Where() with DateTime.Year property to filter by hire year
        throw new NotImplementedException("Exercise 4 not implemented yet");
    }

    public Task<Department?> GetDepartmentWithHighestBudgetAsync()
    {
        // TODO: Implement this method
        // Hint: Use OrderByDescending() and FirstOrDefaultAsync()
        throw new NotImplementedException("Exercise 5 not implemented yet");
    }
}