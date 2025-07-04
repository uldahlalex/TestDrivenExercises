using EfExercises.Entities;

namespace EfExercises.Exercises;

public interface IEfExercises
{
    /// <summary>
    /// Exercise 1: Get all employees from a specific department
    /// </summary>
    /// <param name="departmentName">Name of the department</param>
    /// <returns>List of employees in the specified department</returns>
    Task<List<Employee>> GetEmployeesByDepartmentAsync(string departmentName);

    /// <summary>
    /// Exercise 2: Get the total salary expense for a department
    /// </summary>
    /// <param name="departmentName">Name of the department</param>
    /// <returns>Total salary expense for the department</returns>
    Task<double> GetTotalSalaryByDepartmentAsync(string departmentName);

    /// <summary>
    /// Exercise 3: Find employees with salary above a certain amount
    /// </summary>
    /// <param name="minSalary">Minimum salary threshold</param>
    /// <returns>List of employees with salary above the threshold</returns>
    Task<List<Employee>> GetEmployeesWithSalaryAboveAsync(double minSalary);

    /// <summary>
    /// Exercise 4: Get employees who joined in a specific year
    /// </summary>
    /// <param name="year">The year to filter by</param>
    /// <returns>List of employees who joined in the specified year</returns>
    Task<List<Employee>> GetEmployeesByHireYearAsync(int year);

    /// <summary>
    /// Exercise 5: Get the department with the highest budget
    /// </summary>
    /// <returns>Department with the highest budget</returns>
    Task<Department?> GetDepartmentWithHighestBudgetAsync();
}