using EfExercises.Data;
using EfExercises.Exercises;
using Microsoft.EntityFrameworkCore;
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

        _context = new CompanyDbContext(options);
        await _context.Database.OpenConnectionAsync();
        
        // Seed test data
        await SeedData.SeedAsync(_context);

        // Switch between stub and solution implementations
        // Use EfExercisesStub for students to implement
        // Use EfExercisesSolution to verify tests pass
        _exercises = new EfExercisesStub(_context);
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
        var result = await _exercises.GetEmployeesByDepartmentAsync("Engineering");

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
        var result = await _exercises.GetTotalSalaryByDepartmentAsync("Engineering");

        // Assert - John (75000) + Jane (85000) + Frank (90000) = 250000
        await Assert.That(result).IsEqualTo(250000);
    }

    [Test]
    public async Task Exercise3_GetEmployeesWithSalaryAbove_ShouldReturnCorrectEmployees()
    {
        // Act
        var result = await _exercises.GetEmployeesWithSalaryAboveAsync(70000);

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
        var result = await _exercises.GetEmployeesByHireYearAsync(2020);

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
        var result = await _exercises.GetDepartmentWithHighestBudgetAsync();

        // Assert - Engineering has budget of 500000 (highest)
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("Engineering");
        await Assert.That(result.Budget).IsEqualTo(500000);
    }

    [Test]
    public async Task Exercise1_GetEmployeesByDepartment_NonExistentDepartment_ShouldReturnEmpty()
    {
        // Act
        var result = await _exercises.GetEmployeesByDepartmentAsync("NonExistent");

        // Assert
        await Assert.That(result).HasCount().EqualTo(0);
    }

    [Test]
    public async Task Exercise3_GetEmployeesWithSalaryAbove_VeryHighSalary_ShouldReturnEmpty()
    {
        // Act
        var result = await _exercises.GetEmployeesWithSalaryAboveAsync(100000);

        // Assert
        await Assert.That(result).HasCount().EqualTo(0);
    }
}