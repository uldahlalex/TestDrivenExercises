using Microsoft.EntityFrameworkCore;
using tests.Data;
using tests.DTOs;
using tests.Interfaces;

namespace tests.Tests.EfExercisesCreateWithBusinessRulesTests;

public class CreateDepartmentTests(CompanyDbContext context, IEfExercisesCreateWithBusinessRules exercises)
{
    [Fact]
    public async Task CreateDepartment_ValidData_ShouldCreateSuccessfully()
    {
        // Arrange
        var dto = new CreateDepartmentDto
        {
            Name = "Research & Development",
            Location = "Building E",
            Budget = 250000
        };

        var initialCount = await context.Departments.CountAsync();

        // Act
        var result = await exercises.CreateDepartment(dto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("Research & Development", result.Name);
        Assert.Equal("Building E", result.Location);
        Assert.Equal(250000, result.Budget);

        var newCount = await context.Departments.CountAsync();
        Assert.Equal(initialCount + 1, newCount);
    }

    [Fact]
    public async Task CreateDepartment_DuplicateName_ShouldThrow()
    {
        // Arrange - Engineering already exists
        var existing = await context.Departments.FirstAsync(d => d.Id == 1);
        Assert.Equal("Engineering", existing.Name);

        var dto = new CreateDepartmentDto
        {
            Name = "Engineering", // Duplicate
            Location = "Building F",
            Budget = 300000
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.CreateDepartment(dto));
    }

    [Fact]
    public async Task CreateDepartment_DuplicateNameCaseInsensitive_ShouldThrow()
    {
        // Arrange
        var dto = new CreateDepartmentDto
        {
            Name = "ENGINEERING", // Case-insensitive duplicate
            Location = "Building F",
            Budget = 300000
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.CreateDepartment(dto));
    }

    [Fact]
    public async Task CreateDepartment_BudgetLessThan50000_ShouldThrow()
    {
        // Arrange
        var dto = new CreateDepartmentDto
        {
            Name = "Small Department",
            Location = "Building Z",
            Budget = 40000 // Less than minimum $50,000
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.CreateDepartment(dto));
    }

    [Fact]
    public async Task CreateDepartment_BudgetExactly50000_ShouldSucceed()
    {
        // Arrange
        var dto = new CreateDepartmentDto
        {
            Name = "Minimum Budget Dept",
            Location = "Building F",
            Budget = 50000 // Exactly minimum
        };

        // Act
        var result = await exercises.CreateDepartment(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(50000, result.Budget);
    }

    [Fact]
    public async Task CreateDepartment_LocationWith3Departments_ShouldThrow()
    {
        // Arrange - Create 3 departments in same location
        await exercises.CreateDepartment(new CreateDepartmentDto
        {
            Name = "Dept1",
            Location = "Building X",
            Budget = 100000
        });
        await exercises.CreateDepartment(new CreateDepartmentDto
        {
            Name = "Dept2",
            Location = "Building X",
            Budget = 100000
        });
        await exercises.CreateDepartment(new CreateDepartmentDto
        {
            Name = "Dept3",
            Location = "Building X",
            Budget = 100000
        });

        // Now try to add 4th department to same location
        var dto = new CreateDepartmentDto
        {
            Name = "Dept4",
            Location = "Building X",
            Budget = 100000
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.CreateDepartment(dto));
    }

    [Fact]
    public async Task CreateDepartment_MoreThan10ActiveDepartments_ShouldThrow()
    {
        // Arrange - Create departments until we hit the limit
        var currentCount = await context.Departments.CountAsync();
        var departmentsToCreate = 10 - currentCount;

        for (int i = 0; i < departmentsToCreate; i++)
        {
            await exercises.CreateDepartment(new CreateDepartmentDto
            {
                Name = $"Department {i + 100}",
                Location = $"Building {i + 100}",
                Budget = 100000
            });
        }

        // Verify we have 10 departments
        var count = await context.Departments.CountAsync();
        Assert.Equal(10, count);

        // Try to create 11th
        var dto = new CreateDepartmentDto
        {
            Name = "Eleventh Department",
            Location = "Building 999",
            Budget = 100000
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.CreateDepartment(dto));
    }

    [Fact]
    public async Task CreateDepartment_With10ActiveAfterSoftDeletes_ShouldAllowMore()
    {
        // Arrange - Soft delete a department
        var deptToDelete = await context.Departments
            .Include(d => d.Employees)
            .FirstAsync(d => d.Id == 4);

        foreach (var emp in deptToDelete.Employees.ToList())
        {
            emp.IsDeleted = true;
        }

        deptToDelete.IsDeleted = true;
        deptToDelete.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Active count should now be less than 10
        var activeCount = await context.Departments.CountAsync();
        Assert.True(activeCount < 10);

        // Should be able to create new department
        var dto = new CreateDepartmentDto
        {
            Name = "New Department After Delete",
            Location = "Building New",
            Budget = 100000
        };

        // Act
        var result = await exercises.CreateDepartment(dto);

        // Assert
        Assert.NotNull(result);
    }
}
