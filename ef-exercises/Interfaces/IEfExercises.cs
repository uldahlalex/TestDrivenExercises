using tests.Entities;

namespace tests.Interfaces;

public interface IEfExercises
{
    /// <summary>
    /// Exercise 1: Get all employees from a specific department
    /// </summary>
    /// <param name="departmentName">Name of the department</param>
    /// <returns>List of employees in the specified department</returns>
    Task<List<Employee>> GetEmployeesByDepartment(string departmentName);

    /// <summary>
    /// Exercise 2: Get the total salary expense for a department
    /// </summary>
    /// <param name="departmentName">Name of the department</param>
    /// <returns>Total salary expense for the department</returns>
    Task<double> GetTotalSalaryByDepartment(string departmentName);

    /// <summary>
    /// Exercise 3: Find employees with salary above a certain amount
    /// </summary>
    /// <param name="minSalary">Minimum salary threshold</param>
    /// <returns>List of employees with salary above the threshold</returns>
    Task<List<Employee>> GetEmployeesWithSalaryAbove(double minSalary);

    /// <summary>
    /// Exercise 4: Get employees who joined in a specific year
    /// </summary>
    /// <param name="year">The year to filter by</param>
    /// <returns>List of employees who joined in the specified year</returns>
    Task<List<Employee>> GetEmployeesByHireYear(int year);

    /// <summary>
    /// Exercise 5: Get the department with the highest budget
    /// </summary>
    /// <returns>Department with the highest budget</returns>
    Task<Department> GetDepartmentWithHighestBudget();

    // Level 2 Exercises - Slightly harder queries

    Task<List<Employee>> GetEmployeesHiredBetween(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Exercise 9: Get the top N highest paid employees across all departments
    /// </summary>
    /// <param name="count">Number of top employees to return</param>
    /// <returns>List of top N highest paid employees</returns>
    Task<List<Employee>> GetTopNHighestPaidEmployees(int count);

    /// <summary>
    /// Exercise 10: Get departments with average salary above a threshold
    /// </summary>
    /// <param name="minAverageSalary">Minimum average salary threshold</param>
    /// <returns>List of departments with average salary above threshold</returns>
    Task<List<Department>> GetDepartmentsWithAverageSalaryAbove(double minAverageSalary);
}
