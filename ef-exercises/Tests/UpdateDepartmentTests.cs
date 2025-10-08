using EfExercises.Data;
using EfExercises.DTOs;
using EfExercises.Exercises;
using Microsoft.EntityFrameworkCore;

namespace tests.Tests;

public class UpdateDepartmentTests(CompanyDbContext context, IEfExercisesIdempotentUpdates exercises)
{
    private readonly CompanyDbContext _context = context;
    private readonly IEfExercisesIdempotentUpdates _exercises = exercises;

    [Fact]
    public void UpdateDepartment_ScalarPropertiesOnly_ShouldUpdateCorrectly()
    {
        // Arrange
        var dto = new UpdateDepartmentDto
        {
            Id = 1, // Engineering
            Name = "Engineering & Tech",
            Location = "Building A - Floor 2",
            Budget = 550000
        };

        // Act
        var result = _exercises.UpdateDepartment(dto);

        // Assert
        Assert.Equal("Engineering & Tech", result.Name);
        Assert.Equal("Building A - Floor 2", result.Location);
        Assert.Equal(550000, result.Budget);
    }

    [Fact]
    public void UpdateDepartment_AddEmployees_ShouldUpdateOneToMany()
    {
        // Arrange - Marketing (2) currently has Bob (3) and Alice (4)
        // Add Charlie (5) and Diana (6) from Sales
        var dto = new UpdateDepartmentDto
        {
            Id = 2,
            EmployeeIds = new List<int> { 3, 4, 5, 6 }
        };

        // Act
        var result = _exercises.UpdateDepartment(dto);

        // Assert
        Assert.Equal(4, result.Employees.Count);
        Assert.Contains(result.Employees, e => e.Id == 3);
        Assert.Contains(result.Employees, e => e.Id == 4);
        Assert.Contains(result.Employees, e => e.Id == 5);
        Assert.Contains(result.Employees, e => e.Id == 6);

        // Verify employees were actually transferred
        var charlie = _context.Employees.Find(5);
        var diana = _context.Employees.Find(6);
        Assert.Equal(2, charlie!.DepartmentId);
        Assert.Equal(2, diana!.DepartmentId);
    }

    [Fact]
    public void UpdateDepartment_TransferEmployeesBetweenDepartments_ShouldUpdateCorrectly()
    {
        // Arrange - Transfer John (1) from Engineering to Sales
        var salesDto = new UpdateDepartmentDto
        {
            Id = 3, // Sales
            EmployeeIds = new List<int> { 5, 6, 1 } // Charlie, Diana, John
        };

        // Act
        var result = _exercises.UpdateDepartment(salesDto);

        // Assert
        Assert.Equal(3, result.Employees.Count);
        Assert.Contains(result.Employees, e => e.Id == 1);

        // Verify John was actually transferred
        var john = _context.Employees.Find(1);
        Assert.Equal(3, john!.DepartmentId);
    }

    [Fact]
    public void UpdateDepartment_RemoveEmployees_ShouldThrow()
    {
        // Arrange - Try to remove employees from Engineering (currently has 3)
        // Attempting to set fewer employees should throw
        var dto = new UpdateDepartmentDto
        {
            Id = 1,
            EmployeeIds = new List<int> { 1 } // Only John, removing Jane and Frank
        };

        // Act & Assert
        // The implementation should prevent orphaning employees
        Assert.Throws<InvalidOperationException>(() => _exercises.UpdateDepartment(dto));
    }

    [Fact]
    public void UpdateDepartment_PartialUpdate_ShouldOnlyUpdateSpecifiedFields()
    {
        // Arrange - Only update budget
        var dto = new UpdateDepartmentDto
        {
            Id = 1,
            Budget = 600000
        };

        // Act
        var result = _exercises.UpdateDepartment(dto);

        // Assert
        Assert.Equal(600000, result.Budget);
        Assert.Equal("Engineering", result.Name); // Should remain unchanged
        Assert.Equal(3, result.Employees.Count); // Should remain unchanged
    }

    [Fact]
    public void UpdateDepartment_Idempotent_ShouldProduceSameResultWhenCalledTwice()
    {
        // Arrange
        var dto = new UpdateDepartmentDto
        {
            Id = 2, // Marketing
            Name = "Marketing & Sales",
            Budget = 250000,
            EmployeeIds = new List<int> { 3, 4, 5 } // Bob, Alice, Charlie
        };

        // Act - Call twice
        var result1 = _exercises.UpdateDepartment(dto);
        var result2 = _exercises.UpdateDepartment(dto);

        // Assert - Both calls should produce identical results
        Assert.Equal(result1.Name, result2.Name);
        Assert.Equal(result1.Budget, result2.Budget);
        Assert.Equal(result1.Employees.Count, result2.Employees.Count);
        Assert.All(result1.Employees, e => result2.Employees.Any(e2 => e2.Id == e.Id));
    }

    [Fact]
    public void UpdateDepartment_ReplaceEmployees_ShouldUpdateOneToMany()
    {
        // Arrange - Change HR department employees
        // Currently has Eve (7), replace with Bob (3) and Alice (4)
        // We need to move Eve to another department first to avoid orphaning

        // First, move Eve to Sales before updating HR
        var moveEveDto = new UpdateDepartmentDto
        {
            Id = 3, // Sales
            EmployeeIds = new List<int> { 5, 6, 7 } // Charlie, Diana, Eve
        };
        _exercises.UpdateDepartment(moveEveDto);

        // Now update HR with Bob and Alice
        var dto = new UpdateDepartmentDto
        {
            Id = 4, // HR
            EmployeeIds = new List<int> { 3, 4 }
        };

        // Act
        var result = _exercises.UpdateDepartment(dto);

        // Assert
        Assert.Equal(2, result.Employees.Count);
        Assert.Contains(result.Employees, e => e.Id == 3);
        Assert.Contains(result.Employees, e => e.Id == 4);
        Assert.DoesNotContain(result.Employees, e => e.Id == 7);

        // Verify employees were transferred
        var bob = _context.Employees.Find(3);
        var alice = _context.Employees.Find(4);
        Assert.Equal(4, bob!.DepartmentId);
        Assert.Equal(4, alice!.DepartmentId);

        // Verify Eve stayed in Sales
        var eve = _context.Employees.Find(7);
        Assert.Equal(3, eve!.DepartmentId);
    }

    [Fact]
    public void UpdateDepartment_NonExistentDepartment_ShouldThrow()
    {
        // Arrange
        var dto = new UpdateDepartmentDto
        {
            Id = 999,
            Name = "NonExistent"
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _exercises.UpdateDepartment(dto));
    }

    [Fact]
    public void UpdateDepartment_NonExistentEmployee_ShouldThrow()
    {
        // Arrange
        var dto = new UpdateDepartmentDto
        {
            Id = 1,
            EmployeeIds = new List<int> { 1, 999 }
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _exercises.UpdateDepartment(dto));
    }

    [Fact]
    public void UpdateDepartment_ComplexUpdate_ShouldUpdateAllSpecifiedFields()
    {
        // Arrange - Update scalar properties and employees
        var dto = new UpdateDepartmentDto
        {
            Id = 3, // Sales
            Name = "Sales & Business Development",
            Location = "Building C - Floor 3",
            Budget = 350000,
            EmployeeIds = new List<int> { 5, 6, 7, 8 } // Charlie, Diana, Eve, Frank
        };

        // Act
        var result = _exercises.UpdateDepartment(dto);

        // Assert scalar properties
        Assert.Equal("Sales & Business Development", result.Name);
        Assert.Equal("Building C - Floor 3", result.Location);
        Assert.Equal(350000, result.Budget);

        // Assert one-to-many
        Assert.Equal(4, result.Employees.Count);
        Assert.Contains(result.Employees, e => e.Id == 5);
        Assert.Contains(result.Employees, e => e.Id == 6);
        Assert.Contains(result.Employees, e => e.Id == 7);
        Assert.Contains(result.Employees, e => e.Id == 8);

        // Verify all employees have correct department
        foreach (var employeeId in new[] { 5, 6, 7, 8 })
        {
            var employee = _context.Employees.Find(employeeId);
            Assert.Equal(3, employee!.DepartmentId);
        }
    }

    [Fact]
    public void UpdateDepartment_KeepSameEmployees_ShouldBeIdempotent()
    {
        // Arrange - Engineering currently has John (1), Jane (2), Frank (8)
        var dto = new UpdateDepartmentDto
        {
            Id = 1,
            EmployeeIds = new List<int> { 1, 2, 8 } // Same employees
        };

        // Act
        var result = _exercises.UpdateDepartment(dto);

        // Assert
        Assert.Equal(3, result.Employees.Count);
        Assert.Contains(result.Employees, e => e.Id == 1);
        Assert.Contains(result.Employees, e => e.Id == 2);
        Assert.Contains(result.Employees, e => e.Id == 8);
    }
}
