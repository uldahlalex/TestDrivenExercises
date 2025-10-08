using EfExercises.Data;
using EfExercises.Exercises;
using Microsoft.EntityFrameworkCore;

namespace tests.Tests;

public class AssignMultipleEmployeesToProjectTests(CompanyDbContext context, IEfExercisesHard exercises)
{
    private readonly CompanyDbContext _context = context;
    private readonly IEfExercisesHard _exercises = exercises;

    [Fact]
    public void AssignMultipleEmployeesToProject_ShouldAddAllEmployees()
    {
        // Arrange
        var projectId = 2; // Project Beta
        var employeeIds = new List<int> { 3, 4, 5 }; // Bob, Alice, Charlie

        // Act
        _exercises.AssignMultipleEmployeesToProject(projectId, employeeIds);

        // Assert
        var project = _context.Projects.Include(p => p.Employees).First(p => p.Id == projectId);
        Assert.Equal(3, project.Employees.Count);
        Assert.Contains(project.Employees, e => e.Id == 3);
        Assert.Contains(project.Employees, e => e.Id == 4);
        Assert.Contains(project.Employees, e => e.Id == 5);
    }

    [Fact]
    public void AssignMultipleEmployeesToProject_ShouldNotDuplicateExisting()
    {
        // Arrange - Assign John first
        var projectId = 1;
        _exercises.AssignEmployeeToProject(1, projectId);

        // Act - Try to assign John again along with others
        var employeeIds = new List<int> { 1, 2, 3 };
        _exercises.AssignMultipleEmployeesToProject(projectId, employeeIds);

        // Assert - John should only appear once
        var project = _context.Projects.Include(p => p.Employees).First(p => p.Id == projectId);
        Assert.Equal(3, project.Employees.Count);
        Assert.Single(project.Employees, e => e.Id == 1);
    }
}
