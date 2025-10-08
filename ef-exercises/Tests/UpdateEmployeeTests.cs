using EfExercises.Data;
using EfExercises.DTOs;
using EfExercises.Exercises;
using Microsoft.EntityFrameworkCore;

namespace tests.Tests;

public class UpdateEmployeeTests(CompanyDbContext context, IEfExercisesIdempotentUpdates exercises)
{
    private readonly CompanyDbContext _context = context;
    private readonly IEfExercisesIdempotentUpdates _exercises = exercises;

    [Fact]
    public void UpdateEmployee_ScalarPropertiesOnly_ShouldUpdateCorrectly()
    {
        // Arrange
        var dto = new UpdateEmployeeDto
        {
            Id = 1, // John
            FirstName = "Jonathan",
            LastName = "Doe-Smith",
            Email = "jonathan.doesmith@company.com",
            Salary = 80000
        };

        // Act
        var result = _exercises.UpdateEmployee(dto);

        // Assert
        Assert.Equal("Jonathan", result.FirstName);
        Assert.Equal("Doe-Smith", result.LastName);
        Assert.Equal("jonathan.doesmith@company.com", result.Email);
        Assert.Equal(80000, result.Salary);
        // Department should remain unchanged (Engineering)
        Assert.Equal(1, result.DepartmentId);
    }

    [Fact]
    public void UpdateEmployee_TransferDepartment_ShouldUpdateOneToMany()
    {
        // Arrange - Transfer John from Engineering (1) to Marketing (2)
        var dto = new UpdateEmployeeDto
        {
            Id = 1,
            DepartmentId = 2
        };

        // Act
        var result = _exercises.UpdateEmployee(dto);

        // Assert
        Assert.Equal(2, result.DepartmentId);
        Assert.Equal("Marketing", result.Department.Name);
    }

    [Fact]
    public void UpdateEmployee_AssignProjects_ShouldUpdateManyToMany()
    {
        // Arrange - Assign John to Project Alpha (1) and Beta (2)
        var dto = new UpdateEmployeeDto
        {
            Id = 1,
            ProjectIds = new List<int> { 1, 2 }
        };

        // Act
        var result = _exercises.UpdateEmployee(dto);

        // Assert
        Assert.Equal(2, result.Projects.Count);
        Assert.Contains(result.Projects, p => p.Id == 1);
        Assert.Contains(result.Projects, p => p.Id == 2);
    }

    [Fact]
    public void UpdateEmployee_RemoveAllProjects_ShouldClearManyToMany()
    {
        // Arrange - First assign projects, then remove them
        var assignDto = new UpdateEmployeeDto
        {
            Id = 1,
            ProjectIds = new List<int> { 1, 2 }
        };
        _exercises.UpdateEmployee(assignDto);

        var removeDto = new UpdateEmployeeDto
        {
            Id = 1,
            ProjectIds = new List<int>() // Empty list = remove all
        };

        // Act
        var result = _exercises.UpdateEmployee(removeDto);

        // Assert
        Assert.Empty(result.Projects);
    }

    [Fact]
    public void UpdateEmployee_PartialUpdate_ShouldOnlyUpdateSpecifiedFields()
    {
        // Arrange - Only update salary
        var dto = new UpdateEmployeeDto
        {
            Id = 1,
            Salary = 95000
        };

        // Act
        var result = _exercises.UpdateEmployee(dto);

        // Assert
        Assert.Equal(95000, result.Salary);
        Assert.Equal("John", result.FirstName); // Should remain unchanged
        Assert.Equal("Doe", result.LastName); // Should remain unchanged
    }

    [Fact]
    public void UpdateEmployee_Idempotent_ShouldProduceSameResultWhenCalledTwice()
    {
        // Arrange
        var dto = new UpdateEmployeeDto
        {
            Id = 1,
            FirstName = "Jonathan",
            Salary = 85000,
            DepartmentId = 3,
            ProjectIds = new List<int> { 1, 3 }
        };

        // Act - Call twice
        var result1 = _exercises.UpdateEmployee(dto);
        var result2 = _exercises.UpdateEmployee(dto);

        // Assert - Both calls should produce identical results
        Assert.Equal(result1.FirstName, result2.FirstName);
        Assert.Equal(result1.Salary, result2.Salary);
        Assert.Equal(result1.DepartmentId, result2.DepartmentId);
        Assert.Equal(result1.Projects.Count, result2.Projects.Count);
        Assert.All(result1.Projects, p => result2.Projects.Any(p2 => p2.Id == p.Id));
    }

    [Fact]
    public void UpdateEmployee_ReplaceProjects_ShouldUpdateManyToMany()
    {
        // Arrange - First assign some projects
        var initialDto = new UpdateEmployeeDto
        {
            Id = 1,
            ProjectIds = new List<int> { 1, 2 }
        };
        _exercises.UpdateEmployee(initialDto);

        // Replace with different projects
        var replaceDto = new UpdateEmployeeDto
        {
            Id = 1,
            ProjectIds = new List<int> { 2, 3 } // Remove 1, keep 2, add 3
        };

        // Act
        var result = _exercises.UpdateEmployee(replaceDto);

        // Assert
        Assert.Equal(2, result.Projects.Count);
        Assert.DoesNotContain(result.Projects, p => p.Id == 1);
        Assert.Contains(result.Projects, p => p.Id == 2);
        Assert.Contains(result.Projects, p => p.Id == 3);
    }

    [Fact]
    public void UpdateEmployee_NonExistentEmployee_ShouldThrow()
    {
        // Arrange
        var dto = new UpdateEmployeeDto
        {
            Id = 999,
            FirstName = "NonExistent"
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _exercises.UpdateEmployee(dto));
    }

    [Fact]
    public void UpdateEmployee_NonExistentDepartment_ShouldThrow()
    {
        // Arrange
        var dto = new UpdateEmployeeDto
        {
            Id = 1,
            DepartmentId = 999
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _exercises.UpdateEmployee(dto));
    }

    [Fact]
    public void UpdateEmployee_NonExistentProject_ShouldThrow()
    {
        // Arrange
        var dto = new UpdateEmployeeDto
        {
            Id = 1,
            ProjectIds = new List<int> { 1, 999 }
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _exercises.UpdateEmployee(dto));
    }

    [Fact]
    public void UpdateEmployee_ComplexUpdate_ShouldUpdateAllSpecifiedRelationships()
    {
        // Arrange - Update scalar properties, department, and projects all at once
        var dto = new UpdateEmployeeDto
        {
            Id = 1,
            FirstName = "Jonathan",
            Email = "j.doe@newcompany.com",
            Salary = 100000,
            DepartmentId = 3, // Sales
            ProjectIds = new List<int> { 1, 2, 3 } // All three projects
        };

        // Act
        var result = _exercises.UpdateEmployee(dto);

        // Assert scalar properties
        Assert.Equal("Jonathan", result.FirstName);
        Assert.Equal("j.doe@newcompany.com", result.Email);
        Assert.Equal(100000, result.Salary);

        // Assert one-to-many
        Assert.Equal(3, result.DepartmentId);
        Assert.Equal("Sales", result.Department.Name);

        // Assert many-to-many
        Assert.Equal(3, result.Projects.Count);
        Assert.Contains(result.Projects, p => p.Id == 1);
        Assert.Contains(result.Projects, p => p.Id == 2);
        Assert.Contains(result.Projects, p => p.Id == 3);
    }
}
