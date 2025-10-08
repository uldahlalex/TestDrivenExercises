using EfExercises.Data;
using EfExercises.Exercises;

namespace tests.Tests;

public class GetProjectsWithoutEmployeesTests(CompanyDbContext context, IEfExercisesHard exercises)
{
    private readonly CompanyDbContext _context = context;
    private readonly IEfExercisesHard _exercises = exercises;

    [Fact]
    public void GetProjectsWithoutEmployees_ShouldReturnUnassignedProjects()
    {
        // Arrange - Assign employees to only some projects
        _exercises.AssignEmployeeToProject(1, 1); // John to Alpha

        // Act
        var result = _exercises.GetProjectsWithoutEmployees();

        // Assert - Project Beta (2) and Gamma (3) should have no employees
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Id == 2);
        Assert.Contains(result, p => p.Id == 3);
        Assert.DoesNotContain(result, p => p.Id == 1);
    }
}
