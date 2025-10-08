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
        // Arrange
        var dto = new UpdateProjectDto
        {
            Id = 1, // Project Alpha
            Name = "Project Alpha v2",
            Description = "Updated first major project",
            Budget = 120000
        };

        // Act
        var result = _exercises.UpdateProject(dto);

        // Assert
        Assert.Equal("Project Alpha v2", result.Name);
        Assert.Equal("Updated first major project", result.Description);
        Assert.Equal(120000, result.Budget);
    }

    [Fact]
    public void UpdateProject_AssignEmployees_ShouldUpdateManyToMany()
    {
        // Arrange - Assign John (1), Jane (2), and Bob (3) to Project Alpha
        var dto = new UpdateProjectDto
        {
            Id = 1,
            EmployeeIds = new List<int> { 1, 2, 3 }
        };

        // Act
        var result = _exercises.UpdateProject(dto);

        // Assert
        Assert.Equal(3, result.Employees.Count);
        Assert.Contains(result.Employees, e => e.Id == 1);
        Assert.Contains(result.Employees, e => e.Id == 2);
        Assert.Contains(result.Employees, e => e.Id == 3);
    }

    [Fact]
    public void UpdateProject_RemoveAllEmployees_ShouldClearManyToMany()
    {
        // Arrange - First assign employees, then remove them
        var assignDto = new UpdateProjectDto
        {
            Id = 1,
            EmployeeIds = new List<int> { 1, 2 }
        };
        _exercises.UpdateProject(assignDto);

        var removeDto = new UpdateProjectDto
        {
            Id = 1,
            EmployeeIds = new List<int>() // Empty list = remove all
        };

        // Act
        var result = _exercises.UpdateProject(removeDto);

        // Assert
        Assert.Empty(result.Employees);
    }

    [Fact]
    public void UpdateProject_SetEndDateToNull_ShouldClearNullableField()
    {
        // Arrange - Project Gamma already has an EndDate
        var dto = new UpdateProjectDto
        {
            Id = 3, // Project Gamma
            EndDateAction = NullableFieldAction.SetNull
        };

        // Act
        var result = _exercises.UpdateProject(dto);

        // Assert
        Assert.Null(result.EndDate);
    }

    [Fact]
    public void UpdateProject_SetEndDateToValue_ShouldUpdateNullableField()
    {
        // Arrange - Project Alpha has no EndDate
        var newEndDate = new DateTime(2024, 6, 30, 0, 0, 0, DateTimeKind.Utc);
        var dto = new UpdateProjectDto
        {
            Id = 1, // Project Alpha
            EndDate = newEndDate,
            EndDateAction = NullableFieldAction.SetValue
        };

        // Act
        var result = _exercises.UpdateProject(dto);

        // Assert
        Assert.NotNull(result.EndDate);
        Assert.Equal(newEndDate, result.EndDate);
    }

    [Fact]
    public void UpdateProject_NoChangeToEndDate_ShouldLeaveFieldUnchanged()
    {
        // Arrange - Project Gamma has EndDate = 2023-12-31
        var originalEndDate = new DateTime(2023, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var dto = new UpdateProjectDto
        {
            Id = 3, // Project Gamma
            Name = "Updated Gamma",
            EndDateAction = NullableFieldAction.NoChange
        };

        // Act
        var result = _exercises.UpdateProject(dto);

        // Assert
        Assert.Equal("Updated Gamma", result.Name);
        Assert.Equal(originalEndDate, result.EndDate); // Should remain unchanged
    }

    [Fact]
    public void UpdateProject_PartialUpdate_ShouldOnlyUpdateSpecifiedFields()
    {
        // Arrange - Only update budget
        var dto = new UpdateProjectDto
        {
            Id = 1,
            Budget = 200000
        };

        // Act
        var result = _exercises.UpdateProject(dto);

        // Assert
        Assert.Equal(200000, result.Budget);
        Assert.Equal("Project Alpha", result.Name); // Should remain unchanged
    }

    [Fact]
    public void UpdateProject_Idempotent_ShouldProduceSameResultWhenCalledTwice()
    {
        // Arrange
        var dto = new UpdateProjectDto
        {
            Id = 1,
            Name = "Project Alpha Updated",
            Budget = 150000,
            EmployeeIds = new List<int> { 1, 2, 3 }
        };

        // Act - Call twice
        var result1 = _exercises.UpdateProject(dto);
        var result2 = _exercises.UpdateProject(dto);

        // Assert - Both calls should produce identical results
        Assert.Equal(result1.Name, result2.Name);
        Assert.Equal(result1.Budget, result2.Budget);
        Assert.Equal(result1.Employees.Count, result2.Employees.Count);
        Assert.All(result1.Employees, e => result2.Employees.Any(e2 => e2.Id == e.Id));
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

        // Replace with different employees
        var replaceDto = new UpdateProjectDto
        {
            Id = 1,
            EmployeeIds = new List<int> { 2, 3, 4 } // Remove 1, keep 2, add 3 and 4
        };

        // Act
        var result = _exercises.UpdateProject(replaceDto);

        // Assert
        Assert.Equal(3, result.Employees.Count);
        Assert.DoesNotContain(result.Employees, e => e.Id == 1);
        Assert.Contains(result.Employees, e => e.Id == 2);
        Assert.Contains(result.Employees, e => e.Id == 3);
        Assert.Contains(result.Employees, e => e.Id == 4);
    }

    [Fact]
    public void UpdateProject_NonExistentProject_ShouldThrow()
    {
        // Arrange
        var dto = new UpdateProjectDto
        {
            Id = 999,
            Name = "NonExistent"
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _exercises.UpdateProject(dto));
    }

    [Fact]
    public void UpdateProject_NonExistentEmployee_ShouldThrow()
    {
        // Arrange
        var dto = new UpdateProjectDto
        {
            Id = 1,
            EmployeeIds = new List<int> { 1, 999 }
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _exercises.UpdateProject(dto));
    }

    [Fact]
    public void UpdateProject_SetValueWithoutEndDate_ShouldThrow()
    {
        // Arrange
        var dto = new UpdateProjectDto
        {
            Id = 1,
            EndDateAction = NullableFieldAction.SetValue
            // EndDate is null but action is SetValue
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _exercises.UpdateProject(dto));
    }

    [Fact]
    public void UpdateProject_ComplexUpdate_ShouldUpdateAllSpecifiedRelationships()
    {
        // Arrange - Update scalar properties, nullable field, and employees all at once
        var newEndDate = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var dto = new UpdateProjectDto
        {
            Id = 2, // Project Beta
            Name = "Project Beta v2.0",
            Description = "Major Beta update",
            Budget = 200000,
            EndDate = newEndDate,
            EndDateAction = NullableFieldAction.SetValue,
            EmployeeIds = new List<int> { 1, 2, 3, 4, 5 }
        };

        // Act
        var result = _exercises.UpdateProject(dto);

        // Assert scalar properties
        Assert.Equal("Project Beta v2.0", result.Name);
        Assert.Equal("Major Beta update", result.Description);
        Assert.Equal(200000, result.Budget);

        // Assert nullable field
        Assert.Equal(newEndDate, result.EndDate);

        // Assert many-to-many
        Assert.Equal(5, result.Employees.Count);
        for (int i = 1; i <= 5; i++)
        {
            Assert.Contains(result.Employees, e => e.Id == i);
        }
    }
}
