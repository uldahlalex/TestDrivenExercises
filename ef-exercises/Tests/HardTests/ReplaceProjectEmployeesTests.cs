using EfExercises.Data;
using EfExercises.Exercises;
using Microsoft.EntityFrameworkCore;

namespace tests.Tests;

public class ReplaceProjectEmployeesTests(CompanyDbContext context, IEfExercisesHard exercises)
{
    private readonly CompanyDbContext _context = context;
    private readonly IEfExercisesHard _exercises = exercises;

    [Fact]
    public void ReplaceProjectEmployees_ShouldReplaceAllEmployees()
    {
        // Arrange - Assign some employees first
        _exercises.AssignEmployeeToProject(1, 1); // John to Alpha
        _exercises.AssignEmployeeToProject(2, 1); // Jane to Alpha

        var projectId = 1;
        var newEmployeeIds = new List<int> { 3, 4, 5 }; // Bob, Alice, Charlie

        // Act
        _exercises.ReplaceProjectEmployees(projectId, newEmployeeIds);

        // Assert
        var project = _context.Projects.Include(p => p.Employees).First(p => p.Id == projectId);
        Assert.Equal(3, project.Employees.Count);
        Assert.Contains(project.Employees, e => e.Id == 3);
        Assert.Contains(project.Employees, e => e.Id == 4);
        Assert.Contains(project.Employees, e => e.Id == 5);
        Assert.DoesNotContain(project.Employees, e => e.Id == 1);
        Assert.DoesNotContain(project.Employees, e => e.Id == 2);
    }
}
