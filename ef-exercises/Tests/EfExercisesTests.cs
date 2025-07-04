using EfExercises.Data;
using EfExercises.Exercises;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TUnit.Assertions;
using TUnit.Core;

namespace EfExercises.Tests;

public class EfExercisesTests
{
    private CompanyDbContext _context = null!;
    private IEfExercises _exercises = null!;

    [Before(Test)]
    public async Task SetUp()
    {
        // Create in-memory SQLite database
        var options = new DbContextOptionsBuilder<CompanyDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        // Enable the logger for tests

        _context = new CompanyDbContext(options);
        await _context.Database.OpenConnectionAsync();
        
        // Seed test data
        await SeedData.SeedAsync(_context);

        // Switch between stub and solution implementations
        // Use EfExercisesStub for students to implement
        // Use EfExercisesSolution to verify tests pass
        _exercises = new Exercises.EfExercises(_context);
        // _exercises = new EfExercisesSolution(_context);
    }

    [After(Test)]
    public async Task TearDown()
    {
        await _context.Database.CloseConnectionAsync();
        await _context.DisposeAsync();
    }

    [Test]
    public async Task Exercise1_GetEmployeesByDepartment_ShouldReturnCorrectEmployees()
    {
        // Act
        var result = _exercises.GetEmployeesByDepartment("Engineering");

        // Assert
        await Assert.That(result).HasCount().EqualTo(3);
        var firstNames = result.Select(e => e.FirstName).ToList();
        await Assert.That(firstNames.Contains("John")).IsTrue();
        await Assert.That(firstNames.Contains("Jane")).IsTrue();
        await Assert.That(firstNames.Contains("Frank")).IsTrue();
        await Assert.That(result.All(e => e.Department.Name == "Engineering")).IsTrue();
    }

    [Test]
    public async Task Exercise2_GetTotalSalaryByDepartment_ShouldReturnCorrectSum()
    {
        // Act
        var result = _exercises.GetTotalSalaryByDepartment("Engineering");

        // Assert - John (75000) + Jane (85000) + Frank (90000) = 250000
        await Assert.That(result).IsEqualTo(250000);
    }

    [Test]
    public async Task Exercise3_GetEmployeesWithSalaryAbove_ShouldReturnCorrectEmployees()
    {
        // Act
        var result = _exercises.GetEmployeesWithSalaryAbove(70000);

        // Assert - Jane (85000), Diana (80000), Frank (90000), John (75000)
        await Assert.That(result).HasCount().EqualTo(4);
        await Assert.That(result.All(e => e.Salary > 70000)).IsTrue();
        var firstNames = result.Select(e => e.FirstName).ToList();
        await Assert.That(firstNames.Contains("Jane")).IsTrue();
        await Assert.That(firstNames.Contains("Diana")).IsTrue();
        await Assert.That(firstNames.Contains("Frank")).IsTrue();
        await Assert.That(firstNames.Contains("John")).IsTrue();
    }

    [Test]
    public async Task Exercise4_GetEmployeesByHireYear_ShouldReturnCorrectEmployees()
    {
        // Act
        var result = _exercises.GetEmployeesByHireYear(2020);

        // Assert - John (2020-01-15), Alice (2020-08-25)
        await Assert.That(result).HasCount().EqualTo(2);
        var firstNames = result.Select(e => e.FirstName).ToList();
        await Assert.That(firstNames.Contains("John")).IsTrue();
        await Assert.That(firstNames.Contains("Alice")).IsTrue();
        await Assert.That(result.All(e => e.HireDate.Year == 2020)).IsTrue();
    }

    [Test]
    public async Task Exercise5_GetDepartmentWithHighestBudget_ShouldReturnEngineeringDepartment()
    {
        // Act
        var result = _exercises.GetDepartmentWithHighestBudget();

        // Assert - Engineering has budget of 500000 (highest)
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("Engineering");
        await Assert.That(result.Budget).IsEqualTo(500000);
    }

    [Test]
    public async Task Exercise1_GetEmployeesByDepartment_NonExistentDepartment_ShouldReturnEmpty()
    {
        // Act
        var result = _exercises.GetEmployeesByDepartment("NonExistent");

        // Assert
        await Assert.That(result).HasCount().EqualTo(0);
    }

    [Test]
    public async Task Exercise3_GetEmployeesWithSalaryAbove_VeryHighSalary_ShouldReturnEmpty()
    {
        // Act
        var result = _exercises.GetEmployeesWithSalaryAbove(100000);

        // Assert
        await Assert.That(result).HasCount().EqualTo(0);
    }

    // Level 2 Tests - Slightly harder exercises

    
    [Test]
    public async Task Exercise8_GetEmployeesHiredBetween_ShouldReturnCorrectEmployees()
    {
        // Act
        var result = _exercises.GetEmployeesHiredBetween(new DateTime(2020, 1, 1), new DateTime(2020, 12, 31));

        // Assert - John (2020-01-15) and Alice (2020-08-25)
        await Assert.That(result).HasCount().EqualTo(2);
        await Assert.That(result.All(e => e.HireDate.Year == 2020)).IsTrue();
        // Should be ordered by hire date
        await Assert.That(result.First().FirstName).IsEqualTo("John"); // 2020-01-15
        await Assert.That(result.Last().FirstName).IsEqualTo("Alice"); // 2020-08-25
    }

    [Test]
    public async Task Exercise9_GetTopNHighestPaidEmployees_ShouldReturnCorrectEmployees()
    {
        // Act
        var result = _exercises.GetTopNHighestPaidEmployees(3);

        // Assert - Top 3: Frank (90000), Jane (85000), Diana (80000)
        await Assert.That(result).HasCount().EqualTo(3);
        await Assert.That(result.First().FirstName).IsEqualTo("Frank"); // 90000
        await Assert.That(result.Skip(1).First().FirstName).IsEqualTo("Jane"); // 85000
        await Assert.That(result.Last().FirstName).IsEqualTo("Diana"); // 80000
    }

    [Test]
    public async Task Exercise10_GetDepartmentsWithAverageSalaryAbove_ShouldReturnCorrectDepartments()
    {
        // Act
        var result = _exercises.GetDepartmentsWithAverageSalaryAbove(70000);

        // Assert - Engineering avg: 83333.33, Sales avg: 70000, Marketing avg: 67500
        // Only Engineering should be above 70000
        await Assert.That(result).HasCount().EqualTo(1);
         await Assert.That(result.First().Name).IsEqualTo("Engineering");
    }
}