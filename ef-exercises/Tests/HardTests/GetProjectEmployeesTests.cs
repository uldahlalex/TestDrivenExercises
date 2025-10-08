using EfExercises.Data;
using EfExercises.Exercises;

namespace tests.Tests;

public class GetProjectEmployeesTests(CompanyDbContext context, IEfExercisesHard exercises)
{
    private readonly CompanyDbContext _context = context;
    private readonly IEfExercisesHard _exercises = exercises;

    [Fact]
    public void GetProjectEmployees_ShouldReturnAllProjectEmployees()
    {
        // Arrange - Assign multiple employees to Project Alpha
        var projectId = 1;
        _exercises.AssignEmployeeToProject(1, projectId); // John
        _exercises.AssignEmployeeToProject(2, projectId); // Jane
        _exercises.AssignEmployeeToProject(8, projectId); // Frank

        // Act
        var result = _exercises.GetProjectEmployees(projectId);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, e => e.Id == 1);
        Assert.Contains(result, e => e.Id == 2);
        Assert.Contains(result, e => e.Id == 8);
    }
}
