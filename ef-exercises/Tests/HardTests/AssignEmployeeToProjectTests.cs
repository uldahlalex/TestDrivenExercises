using EfExercises.Data;
using EfExercises.Exercises;
using Microsoft.EntityFrameworkCore;

namespace tests.Tests;

public class AssignEmployeeToProjectTests(CompanyDbContext context, IEfExercisesHard exercises)
{
    private readonly CompanyDbContext _context = context;
    private readonly IEfExercisesHard _exercises = exercises;

    [Fact]
    public void AssignEmployeeToProject_ShouldAddEmployeeToProject()
    {
        // Arrange - John (ID 1) to Project Alpha (ID 1)
        var employeeId = 1;
        var projectId = 1;

        // Act
        _exercises.AssignEmployeeToProject(employeeId, projectId);

        // Assert
        var project = _context.Projects.Include(p => p.Employees).First(p => p.Id == projectId);
        Assert.Contains(project.Employees, e => e.Id == employeeId);
    }

    [Fact]
    public void AssignEmployeeToProject_NonExistentEmployee_ShouldThrow()
    {
        // Arrange
        var invalidEmployeeId = 999;
        var projectId = 1;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            _exercises.AssignEmployeeToProject(invalidEmployeeId, projectId));
    }

    [Fact]
    public void AssignEmployeeToProject_AlreadyAssigned_ShouldNotDuplicate()
    {
        // Arrange - Assign John first
        var employeeId = 1;
        var projectId = 1;
        _exercises.AssignEmployeeToProject(employeeId, projectId);

        // Act - Try to assign John again
        _exercises.AssignEmployeeToProject(employeeId, projectId);

        // Assert - John should only appear once
        var project = _context.Projects.Include(p => p.Employees).First(p => p.Id == projectId);
        Assert.Single(project.Employees, e => e.Id == employeeId);
    }
}
