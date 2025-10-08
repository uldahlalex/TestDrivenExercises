using EfExercises.Data;
using EfExercises.Exercises;
using Microsoft.EntityFrameworkCore;

namespace tests.Tests;

public class TransferEmployeeAndAssignProjectTests(CompanyDbContext context, IEfExercisesHard exercises)
{
    private readonly CompanyDbContext _context = context;
    private readonly IEfExercisesHard _exercises = exercises;

    [Fact]
    public void TransferEmployeeAndAssignProject_ShouldUpdateBothRelationships()
    {
        // Arrange - Transfer John from Engineering (1) to Sales (3) and assign to Project Beta (2)
        var employeeId = 1;
        var newDepartmentId = 3;
        var projectId = 2;

        // Act
        _exercises.TransferEmployeeAndAssignProject(employeeId, newDepartmentId, projectId);

        // Assert - Check department transfer
        var employee = _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Projects)
            .First(e => e.Id == employeeId);

        Assert.Equal(newDepartmentId, employee.DepartmentId);
        Assert.Equal("Sales", employee.Department.Name);

        // Assert - Check project assignment
        Assert.Contains(employee.Projects, p => p.Id == projectId);
    }
}
