using Microsoft.EntityFrameworkCore;
using tests.Data;
using tests.DTOs;
using tests.Interfaces;

namespace tests.Tests.EfExercisesCreateWithBusinessRulesTests;

public class CreateProjectTests(CompanyDbContext context, IEfExercisesCreateWithBusinessRules exercises)
{
    [Fact]
    public async Task CreateProject_ValidData_ShouldCreateSuccessfully()
    {
        // Arrange
        var dto = new CreateProjectDto
        {
            Name = "Project Delta",
            Description = "New exciting project",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(90),
            Budget = 50000,
            EmployeeIds = new List<int> { 1 }
        };

        var initialCount = await context.Projects.CountAsync();

        // Act
        var result = await exercises.CreateProject(dto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("Project Delta", result.Name);
        Assert.Equal(50000, result.Budget);
        Assert.Single(result.Employees);

        var newCount = await context.Projects.CountAsync();
        Assert.Equal(initialCount + 1, newCount);
    }

    [Fact]
    public async Task CreateProject_DuplicateName_ShouldThrow()
    {
        // Arrange - Project Alpha already exists
        var existing = await context.Projects.FirstAsync(p => p.Id == 1);
        Assert.Equal("Project Alpha", existing.Name);

        var dto = new CreateProjectDto
        {
            Name = "Project Alpha", // Duplicate
            Description = "Test",
            StartDate = DateTime.UtcNow,
            Budget = 50000,
            EmployeeIds = new List<int>()
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.CreateProject(dto));
    }

    [Fact]
    public async Task CreateProject_StartDateInPast_ShouldThrow()
    {
        // Arrange
        var dto = new CreateProjectDto
        {
            Name = "Past Project",
            Description = "Test",
            StartDate = DateTime.UtcNow.AddDays(-10), // In the past
            Budget = 50000,
            EmployeeIds = new List<int>()
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.CreateProject(dto));
    }

    [Fact]
    public async Task CreateProject_StartDateToday_ShouldSucceed()
    {
        // Arrange
        var dto = new CreateProjectDto
        {
            Name = "Today Project",
            Description = "Starting today",
            StartDate = DateTime.UtcNow.Date, // Today
            Budget = 50000,
            EmployeeIds = new List<int>()
        };

        // Act
        var result = await exercises.CreateProject(dto);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task CreateProject_EndDateBeforeStartDate_ShouldThrow()
    {
        // Arrange
        var dto = new CreateProjectDto
        {
            Name = "Invalid Dates Project",
            Description = "Test",
            StartDate = DateTime.UtcNow.AddDays(10),
            EndDate = DateTime.UtcNow.AddDays(5), // Before start date
            Budget = 50000,
            EmployeeIds = new List<int>()
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.CreateProject(dto));
    }

    [Fact]
    public async Task CreateProject_BudgetLessThan10000_ShouldThrow()
    {
        // Arrange
        var dto = new CreateProjectDto
        {
            Name = "Small Budget Project",
            Description = "Test",
            StartDate = DateTime.UtcNow,
            Budget = 5000, // Less than $10,000 minimum
            EmployeeIds = new List<int>()
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.CreateProject(dto));
    }

    [Fact]
    public async Task CreateProject_ExceedsTotalBudgetLimit_ShouldThrow()
    {
        // Arrange - Get current total active project budgets
        var currentTotal = await context.Projects.SumAsync(p => p.Budget);

        // Try to create project that would exceed $1,000,000 total
        var dto = new CreateProjectDto
        {
            Name = "Huge Budget Project",
            Description = "Test",
            StartDate = DateTime.UtcNow,
            Budget = 1_000_000 - currentTotal + 1, // Would exceed limit
            EmployeeIds = new List<int>()
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.CreateProject(dto));
    }

    [Fact]
    public async Task CreateProject_WithinTotalBudgetLimit_ShouldSucceed()
    {
        // Arrange
        var currentTotal = await context.Projects.SumAsync(p => p.Budget);
        var availableBudget = 1_000_000 - currentTotal;

        // Use half of available budget (safe amount)
        var safeBudget = Math.Min(availableBudget / 2, 100000);

        var dto = new CreateProjectDto
        {
            Name = "Safe Budget Project",
            Description = "Test",
            StartDate = DateTime.UtcNow,
            Budget = safeBudget,
            EmployeeIds = new List<int>()
        };

        // Act
        var result = await exercises.CreateProject(dto);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task CreateProject_EmployeeFromSoftDeletedDepartment_ShouldThrow()
    {
        // Arrange - Soft delete an employee's department
        var employee = await context.Employees
            .Include(e => e.Department)
            .FirstAsync(e => e.Id == 1);

        var dept = employee.Department;

        // Soft delete all employees in the department
        var employeesInDept = await context.Employees
            .Where(e => e.DepartmentId == dept.Id)
            .ToListAsync();

        foreach (var emp in employeesInDept)
        {
            emp.IsDeleted = true;
        }

        dept.IsDeleted = true;
        dept.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        var dto = new CreateProjectDto
        {
            Name = "Project With Deleted Dept Employee",
            Description = "Test",
            StartDate = DateTime.UtcNow,
            Budget = 50000,
            EmployeeIds = new List<int> { 1 } // Employee from deleted department
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.CreateProject(dto));
    }

    [Fact]
    public async Task CreateProject_TooManyEmployeesForBudget_ShouldThrow()
    {
        // Arrange - 1 employee per $50k budget
        // Budget of $100k allows 2 employees max
        var dto = new CreateProjectDto
        {
            Name = "Overstaffed Project",
            Description = "Test",
            StartDate = DateTime.UtcNow,
            Budget = 100000, // Allows 2 employees
            EmployeeIds = new List<int> { 1, 2, 3 } // 3 employees - too many
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.CreateProject(dto));
    }

    [Fact]
    public async Task CreateProject_EmployeesMatchBudgetConstraint_ShouldSucceed()
    {
        // Arrange
        var dto = new CreateProjectDto
        {
            Name = "Properly Staffed Project",
            Description = "Test",
            StartDate = DateTime.UtcNow,
            Budget = 150000, // Allows 3 employees
            EmployeeIds = new List<int> { 1, 2, 3 } // Exactly 3
        };

        // Act
        var result = await exercises.CreateProject(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Employees.Count);
    }
}
