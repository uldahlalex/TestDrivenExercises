using EfExercises.Data;
using EfExercises.Exercises;

namespace tests.Tests;

public class GetEmployeeProjectsTests(CompanyDbContext context, IEfExercisesHard exercises)
{
    private readonly CompanyDbContext _context = context;
    private readonly IEfExercisesHard _exercises = exercises;

    [Fact]
    public void GetEmployeeProjects_ShouldReturnAllEmployeeProjects()
    {
        // Arrange - Assign John to multiple projects
        var employeeId = 1;
        _exercises.AssignEmployeeToProject(employeeId, 1); // Alpha
        _exercises.AssignEmployeeToProject(employeeId, 2); // Beta

        // Act
        var result = _exercises.GetEmployeeProjects(employeeId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Id == 1);
        Assert.Contains(result, p => p.Id == 2);
    }
}
