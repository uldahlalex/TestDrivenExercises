using EfExercises.Data;
using EfExercises.DTOs;
using EfExercises.Exercises;
using Microsoft.EntityFrameworkCore;

namespace tests.Tests;

public class UpdateProjectTests(CompanyDbContext context, IEfExercisesIdempotentUpdates exercises)
{
    private readonly CompanyDbContext _context = context;
    private readonly IEfExercisesIdempotentUpdates _exercises = exercises;

    [Fact]
    public void UpdateProject_ScalarPropertiesOnly_ShouldUpdateCorrectly()
    {
        // Arrange - Verify Project Alpha exists with original values
        var originalProject = _context.Projects.AsNoTracking().First(p => p.Id == 1);
        Assert.Equal("Project Alpha", originalProject.Name);
        Assert.Equal("First major project", originalProject.Description);
        Assert.Equal(100000, originalProject.Budget);

        var dto = new UpdateProjectDto
        {
            Id = 1,
            Name = "Project Alpha v2",
            Description = "Updated first major project",
            Budget = 120000
        };

        // Act
        var result = _exercises.UpdateProject(dto);

        // Assert - Check returned object
        Assert.Equal("Project Alpha v2", result.Name);
        Assert.Equal("Updated first major project", result.Description);
        Assert.Equal(120000, result.Budget);

        // Assert - Verify DB state
        var dbProject = _context.Projects.AsNoTracking().First(p => p.Id == 1);
        Assert.Equal("Project Alpha v2", dbProject.Name);
        Assert.Equal("Updated first major project", dbProject.Description);
        Assert.Equal(120000, dbProject.Budget);

        // Verify StartDate and EndDate were NOT changed (not in DTO)
        Assert.Equal(originalProject.StartDate, dbProject.StartDate);
        Assert.Equal(originalProject.EndDate, dbProject.EndDate);
    }

    [Fact]
    public void UpdateProject_AssignEmployees_ShouldUpdateManyToMany()
    {
        // Arrange - Verify project and employees exist
        var projectExists = _context.Projects.AsNoTracking().Any(p => p.Id == 1);
        Assert.True(projectExists);

        var employee1Exists = _context.Employees.AsNoTracking().Any(e => e.Id == 1);
        var employee2Exists = _context.Employees.AsNoTracking().Any(e => e.Id == 2);
        var employee3Exists = _context.Employees.AsNoTracking().Any(e => e.Id == 3);
        Assert.True(employee1Exists);
        Assert.True(employee2Exists);
        Assert.True(employee3Exists);

        // Verify project has no employees initially
        var initialEmployeeCount = _context.Projects
            .AsNoTracking()
            .Include(p => p.Employees)
            .First(p => p.Id == 1)
            .Employees.Count;
        Assert.Equal(0, initialEmployeeCount);

        var dto = new UpdateProjectDto
        {
            Id = 1,
            EmployeeIds = new List<int> { 1, 2, 3 }
        };

        // Act
        var result = _exercises.UpdateProject(dto);

        // Assert - Check returned object
        Assert.Equal(3, result.Employees.Count);
        Assert.Contains(result.Employees, e => e.Id == 1);
        Assert.Contains(result.Employees, e => e.Id == 2);
        Assert.Contains(result.Employees, e => e.Id == 3);
        Assert.All(result.Employees, e => Assert.NotNull(e.FirstName)); // Verify navigation loaded

        // Assert - Verify DB state via fresh query
        var dbProject = _context.Projects
            .AsNoTracking()
            .Include(p => p.Employees)
            .First(p => p.Id == 1);
        Assert.Equal(3, dbProject.Employees.Count);
        Assert.Contains(dbProject.Employees, e => e.Id == 1 && e.FirstName == "John");
        Assert.Contains(dbProject.Employees, e => e.Id == 2 && e.FirstName == "Jane");
        Assert.Contains(dbProject.Employees, e => e.Id == 3 && e.FirstName == "Bob");
    }

    [Fact]
    public void UpdateProject_RemoveAllEmployees_ShouldClearManyToMany()
    {
        // Arrange - First assign employees
        var assignDto = new UpdateProjectDto
        {
            Id = 1,
            EmployeeIds = new List<int> { 1, 2 }
        };
        _exercises.UpdateProject(assignDto);

        // Verify employees were assigned
        var projectWithEmployees = _context.Projects
            .AsNoTracking()
            .Include(p => p.Employees)
            .First(p => p.Id == 1);
        Assert.Equal(2, projectWithEmployees.Employees.Count);

        var removeDto = new UpdateProjectDto
        {
            Id = 1,
            EmployeeIds = new List<int>() // Empty list = remove all
        };

        // Act
        var result = _exercises.UpdateProject(removeDto);

        // Assert - Check returned object
        Assert.Empty(result.Employees);

        // Assert - Verify DB state
        var dbProject = _context.Projects
            .AsNoTracking()
            .Include(p => p.Employees)
            .First(p => p.Id == 1);
        Assert.Empty(dbProject.Employees);

        // Verify employees still exist (not deleted, just unassigned)
        var employee1StillExists = _context.Employees.AsNoTracking().Any(e => e.Id == 1);
        var employee2StillExists = _context.Employees.AsNoTracking().Any(e => e.Id == 2);
        Assert.True(employee1StillExists);
        Assert.True(employee2StillExists);
    }

    [Fact]
    public void UpdateProject_SetEndDateToNull_ShouldClearNullableField()
    {
        // Arrange - Verify Project Gamma has an EndDate
        var originalProject = _context.Projects.AsNoTracking().First(p => p.Id == 3);
        Assert.NotNull(originalProject.EndDate);
        Assert.Equal(new DateTime(2023, 12, 31, 0, 0, 0, DateTimeKind.Utc), originalProject.EndDate);

        var dto = new UpdateProjectDto
        {
            Id = 3,
            EndDateAction = NullableFieldAction.SetNull
        };

        // Act
        var result = _exercises.UpdateProject(dto);

        // Assert - Check returned object
        Assert.Null(result.EndDate);

        // Assert - Verify DB state
        var dbProject = _context.Projects.AsNoTracking().First(p => p.Id == 3);
        Assert.Null(dbProject.EndDate);

        // Verify other fields were not changed
        Assert.Equal(originalProject.Name, dbProject.Name);
        Assert.Equal(originalProject.StartDate, dbProject.StartDate);
        Assert.Equal(originalProject.Budget, dbProject.Budget);
    }

    [Fact]
    public void UpdateProject_SetEndDateToValue_ShouldUpdateNullableField()
    {
        // Arrange - Verify Project Alpha has no EndDate
        var originalProject = _context.Projects.AsNoTracking().First(p => p.Id == 1);
        Assert.Null(originalProject.EndDate);

        var newEndDate = new DateTime(2024, 6, 30, 0, 0, 0, DateTimeKind.Utc);
        var dto = new UpdateProjectDto
        {
            Id = 1,
            EndDate = newEndDate,
            EndDateAction = NullableFieldAction.SetValue
        };

        // Act
        var result = _exercises.UpdateProject(dto);

        // Assert - Check returned object
        Assert.NotNull(result.EndDate);
        Assert.Equal(newEndDate, result.EndDate);

        // Assert - Verify DB state
        var dbProject = _context.Projects.AsNoTracking().First(p => p.Id == 1);
        Assert.NotNull(dbProject.EndDate);
        Assert.Equal(newEndDate, dbProject.EndDate);

        // Verify other fields were not changed
        Assert.Equal(originalProject.Name, dbProject.Name);
        Assert.Equal(originalProject.StartDate, dbProject.StartDate);
        Assert.Equal(originalProject.Budget, dbProject.Budget);
    }

    [Fact]
    public void UpdateProject_NoChangeToEndDate_ShouldLeaveFieldUnchanged()
    {
        // Arrange - Verify Project Gamma has EndDate
        var originalProject = _context.Projects.AsNoTracking().First(p => p.Id == 3);
        var originalEndDate = new DateTime(2023, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal(originalEndDate, originalProject.EndDate);

        var dto = new UpdateProjectDto
        {
            Id = 3,
            Name = "Updated Gamma",
            EndDateAction = NullableFieldAction.NoChange
        };

        // Act
        var result = _exercises.UpdateProject(dto);

        // Assert - Check returned object
        Assert.Equal("Updated Gamma", result.Name);
        Assert.Equal(originalEndDate, result.EndDate); // Should remain unchanged

        // Assert - Verify DB state
        var dbProject = _context.Projects.AsNoTracking().First(p => p.Id == 3);
        Assert.Equal("Updated Gamma", dbProject.Name);
        Assert.Equal(originalEndDate, dbProject.EndDate);
    }

    [Fact]
    public void UpdateProject_PartialUpdate_ShouldOnlyUpdateSpecifiedFields()
    {
        // Arrange - Get original state
        var originalProject = _context.Projects.AsNoTracking().First(p => p.Id == 1);
        Assert.Equal("Project Alpha", originalProject.Name);
        Assert.Equal(100000, originalProject.Budget);

        var dto = new UpdateProjectDto
        {
            Id = 1,
            Budget = 200000 // Only update budget
        };

        // Act
        var result = _exercises.UpdateProject(dto);

        // Assert - Budget changed
        Assert.Equal(200000, result.Budget);

        // Assert - Everything else unchanged
        Assert.Equal("Project Alpha", result.Name);
        Assert.Equal(originalProject.Description, result.Description);
        Assert.Equal(originalProject.StartDate, result.StartDate);
        Assert.Equal(originalProject.EndDate, result.EndDate);

        // Assert - Verify DB state
        var dbProject = _context.Projects.AsNoTracking().First(p => p.Id == 1);
        Assert.Equal(200000, dbProject.Budget);
        Assert.Equal("Project Alpha", dbProject.Name);
        Assert.Equal(originalProject.Description, dbProject.Description);
    }

    [Fact]
    public void UpdateProject_Idempotent_ShouldProduceSameResultWhenCalledTwice()
    {
        // Arrange - Verify initial state
        var initialProject = _context.Projects.AsNoTracking().First(p => p.Id == 1);
        Assert.Equal("Project Alpha", initialProject.Name);
        Assert.Equal(100000, initialProject.Budget);

        var dto = new UpdateProjectDto
        {
            Id = 1,
            Name = "Project Alpha Updated",
            Budget = 150000,
            EmployeeIds = new List<int> { 1, 2, 3 }
        };

        // Act - Call twice
        var result1 = _exercises.UpdateProject(dto);

        // Verify first call worked
        var afterFirstCall = _context.Projects.AsNoTracking()
            .Include(p => p.Employees)
            .First(p => p.Id == 1);
        Assert.Equal("Project Alpha Updated", afterFirstCall.Name);
        Assert.Equal(150000, afterFirstCall.Budget);
        Assert.Equal(3, afterFirstCall.Employees.Count);

        var result2 = _exercises.UpdateProject(dto);

        // Assert - Both calls should produce identical results
        Assert.Equal(result1.Name, result2.Name);
        Assert.Equal(result1.Budget, result2.Budget);
        Assert.Equal(result1.Employees.Count, result2.Employees.Count);
        Assert.All(result1.Employees, e => Assert.Contains(result2.Employees, e2 => e2.Id == e.Id));

        // Assert - DB state after second call is identical
        var afterSecondCall = _context.Projects.AsNoTracking()
            .Include(p => p.Employees)
            .First(p => p.Id == 1);
        Assert.Equal("Project Alpha Updated", afterSecondCall.Name);
        Assert.Equal(150000, afterSecondCall.Budget);
        Assert.Equal(3, afterSecondCall.Employees.Count);
        Assert.Contains(afterSecondCall.Employees, e => e.Id == 1);
        Assert.Contains(afterSecondCall.Employees, e => e.Id == 2);
        Assert.Contains(afterSecondCall.Employees, e => e.Id == 3);
    }

    [Fact]
    public void UpdateProject_ReplaceEmployees_ShouldUpdateManyToMany()
    {
        // Arrange - First assign some employees
        var initialDto = new UpdateProjectDto
        {
            Id = 1,
            EmployeeIds = new List<int> { 1, 2 }
        };
        _exercises.UpdateProject(initialDto);

        // Verify initial assignment
        var afterInitial = _context.Projects
            .AsNoTracking()
            .Include(p => p.Employees)
            .First(p => p.Id == 1);
        Assert.Equal(2, afterInitial.Employees.Count);
        Assert.Contains(afterInitial.Employees, e => e.Id == 1);
        Assert.Contains(afterInitial.Employees, e => e.Id == 2);

        // Replace with different employees
        var replaceDto = new UpdateProjectDto
        {
            Id = 1,
            EmployeeIds = new List<int> { 2, 3, 4 } // Remove 1, keep 2, add 3 and 4
        };

        // Act
        var result = _exercises.UpdateProject(replaceDto);

        // Assert - Check returned object
        Assert.Equal(3, result.Employees.Count);
        Assert.DoesNotContain(result.Employees, e => e.Id == 1);
        Assert.Contains(result.Employees, e => e.Id == 2);
        Assert.Contains(result.Employees, e => e.Id == 3);
        Assert.Contains(result.Employees, e => e.Id == 4);

        // Assert - Verify DB state
        var dbProject = _context.Projects
            .AsNoTracking()
            .Include(p => p.Employees)
            .First(p => p.Id == 1);
        Assert.Equal(3, dbProject.Employees.Count);
        Assert.DoesNotContain(dbProject.Employees, e => e.Id == 1);
        Assert.Contains(dbProject.Employees, e => e.Id == 2 && e.FirstName == "Jane");
        Assert.Contains(dbProject.Employees, e => e.Id == 3 && e.FirstName == "Bob");
        Assert.Contains(dbProject.Employees, e => e.Id == 4 && e.FirstName == "Alice");

        // Verify Employee 1 still exists and wasn't deleted
        var employee1 = _context.Employees.AsNoTracking().FirstOrDefault(e => e.Id == 1);
        Assert.NotNull(employee1);
        Assert.Equal("John", employee1.FirstName);
    }

    [Fact]
    public void UpdateProject_NonExistentProject_ShouldThrow()
    {
        // Arrange - Verify project doesn't exist
        var projectExists = _context.Projects.AsNoTracking().Any(p => p.Id == 999);
        Assert.False(projectExists);

        var dto = new UpdateProjectDto
        {
            Id = 999,
            Name = "NonExistent"
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _exercises.UpdateProject(dto));

        // Verify no project was created
        var stillDoesNotExist = _context.Projects.AsNoTracking().Any(p => p.Id == 999);
        Assert.False(stillDoesNotExist);
    }

    [Fact]
    public void UpdateProject_NonExistentEmployee_ShouldThrow()
    {
        // Arrange - Verify employee doesn't exist
        var employeeExists = _context.Employees.AsNoTracking().Any(e => e.Id == 999);
        Assert.False(employeeExists);

        var originalProject = _context.Projects
            .AsNoTracking()
            .Include(p => p.Employees)
            .First(p => p.Id == 1);
        var originalEmployeeCount = originalProject.Employees.Count;

        var dto = new UpdateProjectDto
        {
            Id = 1,
            EmployeeIds = new List<int> { 1, 999 }
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _exercises.UpdateProject(dto));

        // Verify project employees were not changed
        var unchangedProject = _context.Projects
            .AsNoTracking()
            .Include(p => p.Employees)
            .First(p => p.Id == 1);
        Assert.Equal(originalEmployeeCount, unchangedProject.Employees.Count);
    }

    [Fact]
    public void UpdateProject_SetValueWithoutEndDate_ShouldThrow()
    {
        // Arrange
        var originalProject = _context.Projects.AsNoTracking().First(p => p.Id == 1);

        var dto = new UpdateProjectDto
        {
            Id = 1,
            EndDateAction = NullableFieldAction.SetValue
            // EndDate is null but action is SetValue
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _exercises.UpdateProject(dto));

        // Verify project was not changed
        var unchangedProject = _context.Projects.AsNoTracking().First(p => p.Id == 1);
        Assert.Equal(originalProject.Name, unchangedProject.Name);
        Assert.Equal(originalProject.EndDate, unchangedProject.EndDate);
    }

    [Fact]
    public void UpdateProject_ComplexUpdate_ShouldUpdateAllSpecifiedRelationships()
    {
        // Arrange - Verify initial state of Project Beta
        var originalProject = _context.Projects
            .AsNoTracking()
            .Include(p => p.Employees)
            .First(p => p.Id == 2);
        Assert.Equal("Project Beta", originalProject.Name);
        Assert.Equal("Second major project", originalProject.Description);
        Assert.Equal(150000, originalProject.Budget);
        Assert.Null(originalProject.EndDate);
        Assert.Equal(0, originalProject.Employees.Count);

        // Verify target employees exist
        var employeeCount = _context.Employees.AsNoTracking().Count(e => e.Id >= 1 && e.Id <= 5);
        Assert.Equal(5, employeeCount);

        var newEndDate = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var dto = new UpdateProjectDto
        {
            Id = 2,
            Name = "Project Beta v2.0",
            Description = "Major Beta update",
            Budget = 200000,
            EndDate = newEndDate,
            EndDateAction = NullableFieldAction.SetValue,
            EmployeeIds = new List<int> { 1, 2, 3, 4, 5 }
        };

        // Act
        var result = _exercises.UpdateProject(dto);

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
        var dbProject = _context.Projects
            .AsNoTracking()
            .Include(p => p.Employees)
            .First(p => p.Id == 2);

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

        // Verify StartDate was NOT changed (not in DTO)
        Assert.Equal(originalProject.StartDate, dbProject.StartDate);
    }
}
