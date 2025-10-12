using tests.Data;
using tests.Interfaces;

namespace tests.Tests.EfExercisesTests;

public class EfExercisesTests(CompanyDbContext _context, IEfExercises _exercises)
{


    [Fact]
    public async Task Exercise1_GetEmployeesByDepartment_ShouldReturnCorrectEmployees()
    {
        // Act
        var result = await _exercises.GetEmployeesByDepartment("Engineering");

        // Assert
        Assert.Equal(3, result.Count);
        var firstNames = result.Select(e => e.FirstName).ToList();
        Assert.Contains("John", firstNames);
        Assert.Contains("Jane", firstNames);
        Assert.Contains("Frank", firstNames);
        Assert.All(result, e => Assert.Equal("Engineering", e.Department.Name));
    }

    [Fact]
    public async Task Exercise2_GetTotalSalaryByDepartment_ShouldReturnCorrectSum()
    {
        // Act
        var result = await _exercises.GetTotalSalaryByDepartment("Engineering");

        // Assert - John (75000) + Jane (85000) + Frank (90000) = 250000
        Assert.Equal(250000, result);
    }

    [Fact]
    public async Task Exercise3_GetEmployeesWithSalaryAbove_ShouldReturnCorrectEmployees()
    {
        // Act
        var result = await _exercises.GetEmployeesWithSalaryAbove(70000);

        // Assert - Jane (85000), Diana (80000), Frank (90000), John (75000)
        Assert.Equal(4, result.Count);
        Assert.All(result, e => Assert.True(e.Salary > 70000));
        var firstNames = result.Select(e => e.FirstName).ToList();
        Assert.Contains("Jane", firstNames);
        Assert.Contains("Diana", firstNames);
        Assert.Contains("Frank", firstNames);
        Assert.Contains("John", firstNames);
    }

    [Fact]
    public async Task Exercise4_GetEmployeesByHireYear_ShouldReturnCorrectEmployees()
    {
        // Act
        var result = await _exercises.GetEmployeesByHireYear(2020);

        // Assert - John (2020-01-15), Alice (2020-08-25)
        Assert.Equal(2, result.Count);
        var firstNames = result.Select(e => e.FirstName).ToList();
        Assert.Contains("John", firstNames);
        Assert.Contains("Alice", firstNames);
        Assert.All(result, e => Assert.Equal(2020, e.HireDate.Year));
    }

    [Fact]
    public async Task Exercise5_GetDepartmentWithHighestBudget_ShouldReturnEngineeringDepartment()
    {
        // Act
        var result = await _exercises.GetDepartmentWithHighestBudget();

        // Assert - Engineering has budget of 500000 (highest)
        Assert.NotNull(result);
        Assert.Equal("Engineering", result!.Name);
        Assert.Equal(500000, result.Budget);
    }

    [Fact]
    public async Task Exercise1_GetEmployeesByDepartment_NonExistentDepartment_ShouldReturnEmpty()
    {
        // Act
        var result = await _exercises.GetEmployeesByDepartment("NonExistent");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task Exercise3_GetEmployeesWithSalaryAbove_VeryHighSalary_ShouldReturnEmpty()
    {
        // Act
        var result = await _exercises.GetEmployeesWithSalaryAbove(100000);

        // Assert
        Assert.Empty(result);
    }

    // Level 2 Tests - Slightly harder exercises


    [Fact]
    public async Task Exercise8_GetEmployeesHiredBetween_ShouldReturnCorrectEmployees()
    {
        // Act
        var result = await _exercises.GetEmployeesHiredBetween(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2020, 12, 31, 0, 0, 0, DateTimeKind.Utc));

        // Assert - John (2020-01-15) and Alice (2020-08-25)
        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal(2020, e.HireDate.Year));
        // Should be ordered by hire date
        Assert.Equal("John", result.First().FirstName); // 2020-01-15
        Assert.Equal("Alice", result.Last().FirstName); // 2020-08-25
    }

    [Fact]
    public async Task Exercise9_GetTopNHighestPaidEmployees_ShouldReturnCorrectEmployees()
    {
        // Act
        var result = await _exercises.GetTopNHighestPaidEmployees(3);

        // Assert - Top 3: Frank (90000), Jane (85000), Diana (80000)
        Assert.Equal(3, result.Count);
        Assert.Equal("Frank", result.First().FirstName); // 90000
        Assert.Equal("Jane", result.Skip(1).First().FirstName); // 85000
        Assert.Equal("Diana", result.Last().FirstName); // 80000
    }

    [Fact]
    public async Task Exercise10_GetDepartmentsWithAverageSalaryAbove_ShouldReturnCorrectDepartments()
    {
        // Act
        var result = await _exercises.GetDepartmentsWithAverageSalaryAbove(70000);

        // Assert - Engineering avg: 83333.33, Sales avg: 70000, Marketing avg: 67500
        // Only Engineering should be above 70000
        Assert.Single(result);
        Assert.Equal("Engineering", result.First().Name);
    }
}
