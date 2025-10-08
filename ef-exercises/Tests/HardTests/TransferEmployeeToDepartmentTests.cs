using EfExercises.Data;
using EfExercises.Exercises;
using Microsoft.EntityFrameworkCore;

namespace tests.Tests;

public class TransferEmployeeToDepartmentTests(CompanyDbContext context, IEfExercisesHard exercises)
{
    private readonly CompanyDbContext _context = context;
    private readonly IEfExercisesHard _exercises = exercises;

    [Fact]
    public void TransferEmployeeToDepartment_ShouldUpdateEmployeeDepartment()
    {
        // Arrange - Transfer John from Engineering (1) to Marketing (2)
        var employeeId = 1;
        var newDepartmentId = 2;

        // Act
        _exercises.TransferEmployeeToDepartment(employeeId, newDepartmentId);

        // Assert
        var employee = _context.Employees.Include(e => e.Department).First(e => e.Id == employeeId);
        Assert.Equal(newDepartmentId, employee.DepartmentId);
        Assert.Equal("Marketing", employee.Department.Name);
    }
}
