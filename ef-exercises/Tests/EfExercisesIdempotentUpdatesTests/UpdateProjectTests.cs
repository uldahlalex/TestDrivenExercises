using Microsoft.EntityFrameworkCore;
using tests.Data;
using tests.DTOs;
using tests.Interfaces;

namespace tests.Tests.EfExercisesIdempotentUpdatesTests;

public class UpdateProjectTests(CompanyDbContext context, IEfExercisesIdempotentUpdates exercises)
{
    [Fact]
    public async Task UpdateProject_AllProperties_ShouldUpdateCorrectly()
    {
        // Arrange - Verify Project Alpha exists with original values
        var originalProject = await context.Projects.AsNoTracking().FirstAsync(p => p.Id == 1);
        Assert.Equal("Project Alpha", originalProject.Name);
        Assert.Equal("First major project", originalProject.Description);
        Assert.Equal(100000, originalProject.Budget);
        Assert.Null(originalProject.EndDate);

        var dto = new UpdateProjectDto
        {
            Id = 1,
            Name = "Project Alpha v2",
            Description = "Updated first major project",
            StartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = null,
            Budget = 120000,
            EmployeeIds = new List<int>()
        };

        // Act
        var result = await exercises.UpdateProject(dto);

        // Assert - Check returned object
        Assert.Equal("Project Alpha v2", result.Name);
        Assert.Equal("Updated first major project", result.Description);
        Assert.Equal(120000, result.Budget);
        Assert.Null(result.EndDate);
        Assert.Equal(new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc), result.StartDate);

        // Assert - Verify DB state
        var dbProject = await context.Projects.AsNoTracking().FirstAsync(p => p.Id == 1);
        Assert.Equal("Project Alpha v2", dbProject.Name);
        Assert.Equal("Updated first major project", dbProject.Description);
        Assert.Equal(120000, dbProject.Budget);
        Assert.Null(dbProject.EndDate);
        Assert.Equal(new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc), dbProject.StartDate);
    }

    [Fact]
    public async Task UpdateProject_SetNullableEndDate_ShouldSetCorrectly()
    {
        // Arrange - Verify Project Alpha has no end date
        var originalProject = await context.Projects.AsNoTracking().FirstAsync(p => p.Id == 1);
        Assert.Null(originalProject.EndDate);

        var newEndDate = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var dto = new UpdateProjectDto
        {
            Id = 1,
            Name = "Project Alpha",
            Description = "First major project",
            StartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = newEndDate,
            Budget = 100000,
            EmployeeIds = new List<int>()
        };

        // Act
        var result = await exercises.UpdateProject(dto);

        // Assert - Check returned object
        Assert.NotNull(result.EndDate);
        Assert.Equal(newEndDate, result.EndDate);

        // Assert - Verify DB state
        var dbProject = await context.Projects.AsNoTracking().FirstAsync(p => p.Id == 1);
        Assert.NotNull(dbProject.EndDate);
        Assert.Equal(newEndDate, dbProject.EndDate);
    }

    [Fact]
    public async Task UpdateProject_ClearNullableEndDate_ShouldSetToNull()
    {
        // Arrange - First set an end date
        var setEndDateDto = new UpdateProjectDto
        {
            Id = 1,
            Name = "Project Alpha",
            Description = "First major project",
            StartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            Budget = 100000,
            EmployeeIds = new List<int>()
        };
        await exercises.UpdateProject(setEndDateDto);

        // Verify end date was set
        var projectWithEndDate = await context.Projects.AsNoTracking().FirstAsync(p => p.Id == 1);
        Assert.NotNull(projectWithEndDate.EndDate);

        // Now clear the end date by passing null
        var clearEndDateDto = new UpdateProjectDto
        {
            Id = 1,
            Name = "Project Alpha",
            Description = "First major project",
            StartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = null,
            Budget = 100000,
            EmployeeIds = new List<int>()
        };

        // Act
        var result = await exercises.UpdateProject(clearEndDateDto);

        // Assert - Check returned object
        Assert.Null(result.EndDate);

        // Assert - Verify DB state
        var dbProject = await context.Projects.AsNoTracking().FirstAsync(p => p.Id == 1);
        Assert.Null(dbProject.EndDate);
    }

    [Fact]
    public async Task UpdateProject_AssignEmployees_ShouldUpdateManyToMany()
    {
        // Arrange - Verify project and employees exist
        var projectExists = await context.Projects.AsNoTracking().AnyAsync(p => p.Id == 1);
        Assert.True(projectExists);

        var employee1Exists = await context.Employees.AsNoTracking().AnyAsync(e => e.Id == 1);
        var employee2Exists = await context.Employees.AsNoTracking().AnyAsync(e => e.Id == 2);
        var employee3Exists = await context.Employees.AsNoTracking().AnyAsync(e => e.Id == 3);
        Assert.True(employee1Exists);
        Assert.True(employee2Exists);
        Assert.True(employee3Exists);

        // Verify project has no employees initially
        var initialEmployeeCount = (await context.Projects
            .AsNoTracking()
            .Include(p => p.Employees)
            .FirstAsync(p => p.Id == 1))
            .Employees.Count;
        Assert.Equal(0, initialEmployeeCount);

        var dto = new UpdateProjectDto
        {
            Id = 1,
            Name = "Project Alpha",
            Description = "First major project",
            StartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = null,
            Budget = 100000,
            EmployeeIds = new List<int> { 1, 2, 3 }
        };

        // Act
        var result = await exercises.UpdateProject(dto);

        // Assert - Check returned object
        Assert.Equal(3, result.Employees.Count);
        Assert.Contains(result.Employees, e => e.Id == 1);
        Assert.Contains(result.Employees, e => e.Id == 2);
        Assert.Contains(result.Employees, e => e.Id == 3);
        Assert.All(result.Employees, e => Assert.NotNull(e.FirstName)); // Verify navigation loaded

        // Assert - Verify DB state via fresh query
        var dbProject = await context.Projects
            .AsNoTracking()
            .Include(p => p.Employees)
            .FirstAsync(p => p.Id == 1);
        Assert.Equal(3, dbProject.Employees.Count);
        Assert.Contains(dbProject.Employees, e => e.Id == 1 && e.FirstName == "John");
        Assert.Contains(dbProject.Employees, e => e.Id == 2 && e.FirstName == "Jane");
        Assert.Contains(dbProject.Employees, e => e.Id == 3 && e.FirstName == "Bob");
    }

    [Fact]
    public async Task UpdateProject_RemoveAllEmployees_ShouldClearManyToMany()
    {
        // Arrange - First assign employees
        var assignDto = new UpdateProjectDto
        {
            Id = 1,
            Name = "Project Alpha",
            Description = "First major project",
            StartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = null,
            Budget = 100000,
            EmployeeIds = new List<int> { 1, 2 }
        };
        await exercises.UpdateProject(assignDto);

        // Verify employees were assigned
        var projectWithEmployees = await context.Projects
            .AsNoTracking()
            .Include(p => p.Employees)
            .FirstAsync(p => p.Id == 1);
        Assert.Equal(2, projectWithEmployees.Employees.Count);

        var removeDto = new UpdateProjectDto
        {
            Id = 1,
            Name = "Project Alpha",
            Description = "First major project",
            StartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = null,
            Budget = 100000,
            EmployeeIds = new List<int>() // Empty list = remove all
        };

        // Act
        var result = await exercises.UpdateProject(removeDto);

        // Assert - Check returned object
        Assert.Empty(result.Employees);

        // Assert - Verify DB state
        var dbProject = await context.Projects
            .AsNoTracking()
            .Include(p => p.Employees)
            .FirstAsync(p => p.Id == 1);
        Assert.Empty(dbProject.Employees);

        // Verify employees still exist (not deleted, just unassigned)
        var employee1StillExists = await context.Employees.AsNoTracking().AnyAsync(e => e.Id == 1);
        var employee2StillExists = await context.Employees.AsNoTracking().AnyAsync(e => e.Id == 2);
        Assert.True(employee1StillExists);
        Assert.True(employee2StillExists);
    }

    [Fact]
    public async Task UpdateProject_Idempotent_ShouldProduceSameResultWhenCalledTwice()
    {
        // Arrange - Verify initial state
        var initialProject = await context.Projects.AsNoTracking().FirstAsync(p => p.Id == 1);
        Assert.Equal("Project Alpha", initialProject.Name);
        Assert.Equal(100000, initialProject.Budget);

        var dto = new UpdateProjectDto
        {
            Id = 1,
            Name = "Project Alpha Updated",
            Description = "Updated description",
            StartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            Budget = 150000,
            EmployeeIds = new List<int> { 1, 2, 3 }
        };

        // Act - Call twice
        var result1 = await exercises.UpdateProject(dto);

        // Verify first call worked
        var afterFirstCall = await context.Projects.AsNoTracking()
            .Include(p => p.Employees)
            .FirstAsync(p => p.Id == 1);
        Assert.Equal("Project Alpha Updated", afterFirstCall.Name);
        Assert.Equal(150000, afterFirstCall.Budget);
        Assert.Equal(3, afterFirstCall.Employees.Count);

        var result2 = await exercises.UpdateProject(dto);

        // Assert - Both calls should produce identical results
        Assert.Equal(result1.Name, result2.Name);
        Assert.Equal(result1.Budget, result2.Budget);
        Assert.Equal(result1.EndDate, result2.EndDate);
        Assert.Equal(result1.Employees.Count, result2.Employees.Count);
        Assert.All(result1.Employees, e => Assert.Contains(result2.Employees, e2 => e2.Id == e.Id));

        // Assert - DB state after second call is identical
        var afterSecondCall = await context.Projects.AsNoTracking()
            .Include(p => p.Employees)
            .FirstAsync(p => p.Id == 1);
        Assert.Equal("Project Alpha Updated", afterSecondCall.Name);
        Assert.Equal(150000, afterSecondCall.Budget);
        Assert.Equal(new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc), afterSecondCall.EndDate);
        Assert.Equal(3, afterSecondCall.Employees.Count);
        Assert.Contains(afterSecondCall.Employees, e => e.Id == 1);
        Assert.Contains(afterSecondCall.Employees, e => e.Id == 2);
        Assert.Contains(afterSecondCall.Employees, e => e.Id == 3);
    }

    [Fact]
    public async Task UpdateProject_ReplaceEmployees_ShouldUpdateManyToMany()
    {
        // Arrange - First assign some employees
        var initialDto = new UpdateProjectDto
        {
            Id = 1,
            Name = "Project Alpha",
            Description = "First major project",
            StartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = null,
            Budget = 100000,
            EmployeeIds = new List<int> { 1, 2 }
        };
        await exercises.UpdateProject(initialDto);

        // Verify initial assignment
        var afterInitial = await context.Projects
            .AsNoTracking()
            .Include(p => p.Employees)
            .FirstAsync(p => p.Id == 1);
        Assert.Equal(2, afterInitial.Employees.Count);
        Assert.Contains(afterInitial.Employees, e => e.Id == 1);
        Assert.Contains(afterInitial.Employees, e => e.Id == 2);

        // Replace with different employees
        var replaceDto = new UpdateProjectDto
        {
            Id = 1,
            Name = "Project Alpha",
            Description = "First major project",
            StartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = null,
            Budget = 100000,
            EmployeeIds = new List<int> { 2, 3, 4 } // Remove 1, keep 2, add 3 and 4
        };

        // Act
        var result = await exercises.UpdateProject(replaceDto);

        // Assert - Check returned object
        Assert.Equal(3, result.Employees.Count);
        Assert.DoesNotContain(result.Employees, e => e.Id == 1);
        Assert.Contains(result.Employees, e => e.Id == 2);
        Assert.Contains(result.Employees, e => e.Id == 3);
        Assert.Contains(result.Employees, e => e.Id == 4);

        // Assert - Verify DB state
        var dbProject = await context.Projects
            .AsNoTracking()
            .Include(p => p.Employees)
            .FirstAsync(p => p.Id == 1);
        Assert.Equal(3, dbProject.Employees.Count);
        Assert.DoesNotContain(dbProject.Employees, e => e.Id == 1);
        Assert.Contains(dbProject.Employees, e => e.Id == 2 && e.FirstName == "Jane");
        Assert.Contains(dbProject.Employees, e => e.Id == 3 && e.FirstName == "Bob");
        Assert.Contains(dbProject.Employees, e => e.Id == 4 && e.FirstName == "Alice");

        // Verify Employee 1 still exists and wasn't deleted
        var employee1 = await context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == 1);
        Assert.NotNull(employee1);
        Assert.Equal("John", employee1.FirstName);
    }

    [Fact]
    public async Task UpdateProject_NonExistentProject_ShouldThrow()
    {
        // Arrange - Verify project doesn't exist
        var projectExists = await context.Projects.AsNoTracking().AnyAsync(p => p.Id == 999);
        Assert.False(projectExists);

        var dto = new UpdateProjectDto
        {
            Id = 999,
            Name = "NonExistent",
            Description = "Does not exist",
            StartDate = DateTime.UtcNow,
            EndDate = null,
            Budget = 100000,
            EmployeeIds = new List<int>()
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await exercises.UpdateProject(dto));

        // Verify no project was created
        var stillDoesNotExist = await context.Projects.AsNoTracking().AnyAsync(p => p.Id == 999);
        Assert.False(stillDoesNotExist);
    }

    [Fact]
    public async Task UpdateProject_NonExistentEmployee_ShouldThrow()
    {
        // Arrange - Verify employee doesn't exist
        var employeeExists = await context.Employees.AsNoTracking().AnyAsync(e => e.Id == 999);
        Assert.False(employeeExists);

        var originalProject = await context.Projects
            .AsNoTracking()
            .Include(p => p.Employees)
            .FirstAsync(p => p.Id == 1);
        var originalEmployeeCount = originalProject.Employees.Count;

        var dto = new UpdateProjectDto
        {
            Id = 1,
            Name = "Project Alpha",
            Description = "First major project",
            StartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = null,
            Budget = 100000,
            EmployeeIds = new List<int> { 1, 999 }
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await exercises.UpdateProject(dto));

        // Verify project employees were not changed
        var unchangedProject = await context.Projects
            .AsNoTracking()
            .Include(p => p.Employees)
            .FirstAsync(p => p.Id == 1);
        Assert.Equal(originalEmployeeCount, unchangedProject.Employees.Count);
    }

    [Fact]
    public async Task UpdateProject_ComplexUpdate_ShouldUpdateAllSpecifiedRelationships()
    {
        // Arrange - Verify initial state of Project Beta
        var originalProject = await context.Projects
            .AsNoTracking()
            .Include(p => p.Employees)
            .FirstAsync(p => p.Id == 2);
        Assert.Equal("Project Beta", originalProject.Name);
        Assert.Equal("Second major project", originalProject.Description);
        Assert.Equal(150000, originalProject.Budget);
        Assert.Null(originalProject.EndDate);
        Assert.Equal(0, originalProject.Employees.Count);

        // Verify target employees exist
        var employeeCount = await context.Employees.AsNoTracking().CountAsync(e => e.Id >= 1 && e.Id <= 5);
        Assert.Equal(5, employeeCount);

        var newEndDate = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var dto = new UpdateProjectDto
        {
            Id = 2,
            Name = "Project Beta v2.0",
            Description = "Major Beta update",
            StartDate = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = newEndDate,
            Budget = 200000,
            EmployeeIds = new List<int> { 1, 2, 3, 4, 5 }
        };

        // Act
        var result = await exercises.UpdateProject(dto);

        // Assert - Check returned object scalar properties
        Assert.Equal("Project Beta v2.0", result.Name);
        Assert.Equal("Major Beta update", result.Description);
        Assert.Equal(200000, result.Budget);

        // Assert - Check returned object nullable field
        Assert.NotNull(result.EndDate);
        Assert.Equal(newEndDate, result.EndDate);

        // Assert - Check returned object many-to-many
        Assert.Equal(5, result.Employees.Count);
        for (int i = 1; i <= 5; i++)
        {
            Assert.Contains(result.Employees, e => e.Id == i);
        }

        // Assert - Verify complete DB state
        var dbProject = await context.Projects
            .AsNoTracking()
            .Include(p => p.Employees)
            .FirstAsync(p => p.Id == 2);

        Assert.Equal("Project Beta v2.0", dbProject.Name);
        Assert.Equal("Major Beta update", dbProject.Description);
        Assert.Equal(200000, dbProject.Budget);
        Assert.Equal(newEndDate, dbProject.EndDate);
        Assert.Equal(5, dbProject.Employees.Count);
        Assert.Contains(dbProject.Employees, e => e.Id == 1 && e.FirstName == "John");
        Assert.Contains(dbProject.Employees, e => e.Id == 2 && e.FirstName == "Jane");
        Assert.Contains(dbProject.Employees, e => e.Id == 3 && e.FirstName == "Bob");
        Assert.Contains(dbProject.Employees, e => e.Id == 4 && e.FirstName == "Alice");
        Assert.Contains(dbProject.Employees, e => e.Id == 5 && e.FirstName == "Charlie");
    }
}
