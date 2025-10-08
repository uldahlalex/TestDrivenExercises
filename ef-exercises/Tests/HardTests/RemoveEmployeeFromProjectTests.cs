using EfExercises.Data;
using EfExercises.Exercises;
using Microsoft.EntityFrameworkCore;

namespace tests.Tests;

public class RemoveEmployeeFromProjectTests(CompanyDbContext context, IEfExercisesHard exercises)
{
    private readonly CompanyDbContext _context = context;
    private readonly IEfExercisesHard _exercises = exercises;

    [Fact]
    public void RemoveEmployeeFromProject_ShouldRemoveEmployeeFromProject()
    {
        // Arrange - First assign John to Project Alpha
        var employeeId = 1;
        var projectId = 1;
        _exercises.AssignEmployeeToProject(employeeId, projectId);

        // Act
        _exercises.RemoveEmployeeFromProject(employeeId, projectId);

        // Assert
        var project = _context.Projects.Include(p => p.Employees).First(p => p.Id == projectId);
        Assert.DoesNotContain(project.Employees, e => e.Id == employeeId);
    }

    [Fact]
    public void RemoveEmployeeFromProject_NonExistentAssignment_ShouldThrow()
    {
        // Arrange - Employee 1 not assigned to Project 3
        var employeeId = 1;
        var projectId = 3;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            _exercises.RemoveEmployeeFromProject(employeeId, projectId));
    }
}
