using EfExercises.Data;
using EfExercises.Exercises;

namespace tests.Tests;

public class GetEmployeesWithoutProjectsTests(CompanyDbContext context, IEfExercisesHard exercises)
{
    private readonly CompanyDbContext _context = context;
    private readonly IEfExercisesHard _exercises = exercises;

    [Fact]
    public void GetEmployeesWithoutProjects_ShouldReturnUnassignedEmployees()
    {
        // Arrange - Assign only some employees
        _exercises.AssignEmployeeToProject(1, 1); // John to Alpha
        _exercises.AssignEmployeeToProject(2, 1); // Jane to Alpha

        // Act
        var result = _exercises.GetEmployeesWithoutProjects();

        // Assert - 6 employees should have no projects (8 total - 2 assigned)
        Assert.Equal(6, result.Count);
        Assert.DoesNotContain(result, e => e.Id == 1);
        Assert.DoesNotContain(result, e => e.Id == 2);
    }
}
