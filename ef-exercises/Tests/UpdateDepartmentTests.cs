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
        // Arrange - Verify Engineering department exists with original values
        var originalDepartment = _context.Departments.AsNoTracking().First(d => d.Id == 1);
        Assert.Equal("Engineering", originalDepartment.Name);
        Assert.Equal("Building A", originalDepartment.Location);
        Assert.Equal(500000, originalDepartment.Budget);

        var dto = new UpdateDepartmentDto
        {
            Id = 1,
            Name = "Engineering & Tech",
            Location = "Building A - Floor 2",
            Budget = 550000
        };

        // Act
        var result = _exercises.UpdateDepartment(dto);

        // Assert - Check returned object
        Assert.Equal("Engineering & Tech", result.Name);
        Assert.Equal("Building A - Floor 2", result.Location);
        Assert.Equal(550000, result.Budget);

        // Assert - Verify DB state
        var dbDepartment = _context.Departments.AsNoTracking().First(d => d.Id == 1);
        Assert.Equal("Engineering & Tech", dbDepartment.Name);
        Assert.Equal("Building A - Floor 2", dbDepartment.Location);
        Assert.Equal(550000, dbDepartment.Budget);
    }

    [Fact]
    public void UpdateDepartment_AddEmployees_ShouldUpdateOneToMany()
    {
        // Arrange - Verify initial state
        var marketingDept = _context.Departments
            .AsNoTracking()
            .Include(d => d.Employees)
            .First(d => d.Id == 2);
        var initialMarketingEmployeeIds = marketingDept.Employees.Select(e => e.Id).ToList();
        Assert.Contains(3, initialMarketingEmployeeIds); // Bob
        Assert.Contains(4, initialMarketingEmployeeIds); // Alice

        // Verify Charlie and Diana are in Sales
        var charlie = _context.Employees.AsNoTracking().First(e => e.Id == 5);
        var diana = _context.Employees.AsNoTracking().First(e => e.Id == 6);
        Assert.Equal(3, charlie.DepartmentId); // Sales
        Assert.Equal(3, diana.DepartmentId); // Sales

        var dto = new UpdateDepartmentDto
        {
            Id = 2,
            EmployeeIds = new List<int> { 3, 4, 5, 6 } // Add Charlie and Diana
        };

        // Act
        var result = _exercises.UpdateDepartment(dto);

        // Assert - Check returned object
        Assert.Equal(4, result.Employees.Count);
        Assert.Contains(result.Employees, e => e.Id == 3);
        Assert.Contains(result.Employees, e => e.Id == 4);
        Assert.Contains(result.Employees, e => e.Id == 5);
        Assert.Contains(result.Employees, e => e.Id == 6);

        // Assert - Verify DB state
        var dbDepartment = _context.Departments
            .AsNoTracking()
            .Include(d => d.Employees)
            .First(d => d.Id == 2);
        Assert.Equal(4, dbDepartment.Employees.Count);
        Assert.Contains(dbDepartment.Employees, e => e.Id == 3 && e.FirstName == "Bob");
        Assert.Contains(dbDepartment.Employees, e => e.Id == 4 && e.FirstName == "Alice");
        Assert.Contains(dbDepartment.Employees, e => e.Id == 5 && e.FirstName == "Charlie");
        Assert.Contains(dbDepartment.Employees, e => e.Id == 6 && e.FirstName == "Diana");

        // Verify employees were actually transferred in DB
        var charlieAfter = _context.Employees.AsNoTracking().First(e => e.Id == 5);
        var dianaAfter = _context.Employees.AsNoTracking().First(e => e.Id == 6);
        Assert.Equal(2, charlieAfter.DepartmentId);
        Assert.Equal(2, dianaAfter.DepartmentId);
    }

    [Fact]
    public void UpdateDepartment_TransferEmployeesBetweenDepartments_ShouldUpdateCorrectly()
    {
        // Arrange - Verify John is in Engineering
        var john = _context.Employees.AsNoTracking().First(e => e.Id == 1);
        Assert.Equal(1, john.DepartmentId); // Engineering

        // Get initial Sales employee count
        var initialSalesCount = _context.Employees.AsNoTracking().Count(e => e.DepartmentId == 3);

        var salesDto = new UpdateDepartmentDto
        {
            Id = 3, // Sales
            EmployeeIds = new List<int> { 5, 6, 1 } // Charlie, Diana, John
        };

        // Act
        var result = _exercises.UpdateDepartment(salesDto);

        // Assert - Check returned object
        Assert.Equal(3, result.Employees.Count);
        Assert.Contains(result.Employees, e => e.Id == 1);
        Assert.Contains(result.Employees, e => e.Id == 5);
        Assert.Contains(result.Employees, e => e.Id == 6);

        // Assert - Verify DB state
        var johnAfter = _context.Employees.AsNoTracking().First(e => e.Id == 1);
        Assert.Equal(3, johnAfter.DepartmentId);

        var salesDept = _context.Departments
            .AsNoTracking()
            .Include(d => d.Employees)
            .First(d => d.Id == 3);
        Assert.Equal(3, salesDept.Employees.Count);
        Assert.Contains(salesDept.Employees, e => e.Id == 1 && e.FirstName == "John");
    }

    [Fact]
    public void UpdateDepartment_RemoveEmployees_ShouldThrow()
    {
        // Arrange - Engineering currently has 3 employees
        var engineeringDept = _context.Departments
            .AsNoTracking()
            .Include(d => d.Employees)
            .First(d => d.Id == 1);
        var originalEmployeeIds = engineeringDept.Employees.Select(e => e.Id).OrderBy(id => id).ToList();
        Assert.True(originalEmployeeIds.Count >= 2); // Has John, Jane, Frank

        var dto = new UpdateDepartmentDto
        {
            Id = 1,
            EmployeeIds = new List<int> { 1 } // Try to remove others, keep only John
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _exercises.UpdateDepartment(dto));

        // Verify employees were not changed in DB
        var unchangedDept = _context.Departments
            .AsNoTracking()
            .Include(d => d.Employees)
            .First(d => d.Id == 1);
        var unchangedEmployeeIds = unchangedDept.Employees.Select(e => e.Id).OrderBy(id => id).ToList();
        Assert.Equal(originalEmployeeIds, unchangedEmployeeIds);
    }

    [Fact]
    public void UpdateDepartment_PartialUpdate_ShouldOnlyUpdateSpecifiedFields()
    {
        // Arrange - Get original state
        var originalDepartment = _context.Departments
            .AsNoTracking()
            .Include(d => d.Employees)
            .First(d => d.Id == 1);
        Assert.Equal("Engineering", originalDepartment.Name);
        Assert.Equal(500000, originalDepartment.Budget);
        var originalEmployeeCount = originalDepartment.Employees.Count;

        var dto = new UpdateDepartmentDto
        {
            Id = 1,
            Budget = 600000 // Only update budget
        };

        // Act
        var result = _exercises.UpdateDepartment(dto);

        // Assert - Budget changed
        Assert.Equal(600000, result.Budget);

        // Assert - Everything else unchanged
        Assert.Equal("Engineering", result.Name);
        Assert.Equal(originalDepartment.Location, result.Location);
        Assert.Equal(originalEmployeeCount, result.Employees.Count);

        // Assert - Verify DB state
        var dbDepartment = _context.Departments
            .AsNoTracking()
            .Include(d => d.Employees)
            .First(d => d.Id == 1);
        Assert.Equal(600000, dbDepartment.Budget);
        Assert.Equal("Engineering", dbDepartment.Name);
        Assert.Equal(originalEmployeeCount, dbDepartment.Employees.Count);
    }

    [Fact]
    public void UpdateDepartment_Idempotent_ShouldProduceSameResultWhenCalledTwice()
    {
        // Arrange - Verify initial state
        var initialDepartment = _context.Departments
            .AsNoTracking()
            .Include(d => d.Employees)
            .First(d => d.Id == 2);
        Assert.Equal("Marketing", initialDepartment.Name);
        Assert.Equal(200000, initialDepartment.Budget);

        var dto = new UpdateDepartmentDto
        {
            Id = 2,
            Name = "Marketing & Sales",
            Budget = 250000,
            EmployeeIds = new List<int> { 3, 4, 5 } // Bob, Alice, Charlie
        };

        // Act - Call twice
        var result1 = _exercises.UpdateDepartment(dto);

        // Verify first call worked
        var afterFirstCall = _context.Departments.AsNoTracking()
            .Include(d => d.Employees)
            .First(d => d.Id == 2);
        Assert.Equal("Marketing & Sales", afterFirstCall.Name);
        Assert.Equal(250000, afterFirstCall.Budget);
        Assert.Equal(3, afterFirstCall.Employees.Count);

        var result2 = _exercises.UpdateDepartment(dto);

        // Assert - Both calls should produce identical results
        Assert.Equal(result1.Name, result2.Name);
        Assert.Equal(result1.Budget, result2.Budget);
        Assert.Equal(result1.Employees.Count, result2.Employees.Count);
        Assert.All(result1.Employees, e => Assert.Contains(result2.Employees, e2 => e2.Id == e.Id));

        // Assert - DB state after second call is identical
        var afterSecondCall = _context.Departments.AsNoTracking()
            .Include(d => d.Employees)
            .First(d => d.Id == 2);
        Assert.Equal("Marketing & Sales", afterSecondCall.Name);
        Assert.Equal(250000, afterSecondCall.Budget);
        Assert.Equal(3, afterSecondCall.Employees.Count);
        Assert.Contains(afterSecondCall.Employees, e => e.Id == 3);
        Assert.Contains(afterSecondCall.Employees, e => e.Id == 4);
        Assert.Contains(afterSecondCall.Employees, e => e.Id == 5);
    }

    [Fact]
    public void UpdateDepartment_ReplaceEmployees_ShouldUpdateOneToMany()
    {
        // Arrange - HR currently has Eve (7)
        var hrDept = _context.Departments
            .AsNoTracking()
            .Include(d => d.Employees)
            .First(d => d.Id == 4);
        var originalHrEmployeeIds = hrDept.Employees.Select(e => e.Id).ToList();
        Assert.Contains(7, originalHrEmployeeIds);

        // First, move Eve to Sales to avoid orphaning
        var moveEveDto = new UpdateDepartmentDto
        {
            Id = 3, // Sales
            EmployeeIds = new List<int> { 5, 6, 7 } // Charlie, Diana, Eve
        };
        _exercises.UpdateDepartment(moveEveDto);

        // Verify Eve is now in Sales
        var eveAfterMove = _context.Employees.AsNoTracking().First(e => e.Id == 7);
        Assert.Equal(3, eveAfterMove.DepartmentId);

        // Now update HR with Bob and Alice
        var dto = new UpdateDepartmentDto
        {
            Id = 4, // HR
            EmployeeIds = new List<int> { 3, 4 }
        };

        // Act
        var result = _exercises.UpdateDepartment(dto);

        // Assert - Check returned object
        Assert.Equal(2, result.Employees.Count);
        Assert.Contains(result.Employees, e => e.Id == 3);
        Assert.Contains(result.Employees, e => e.Id == 4);
        Assert.DoesNotContain(result.Employees, e => e.Id == 7);

        // Assert - Verify DB state
        var dbDepartment = _context.Departments
            .AsNoTracking()
            .Include(d => d.Employees)
            .First(d => d.Id == 4);
        Assert.Equal(2, dbDepartment.Employees.Count);
        Assert.Contains(dbDepartment.Employees, e => e.Id == 3 && e.FirstName == "Bob");
        Assert.Contains(dbDepartment.Employees, e => e.Id == 4 && e.FirstName == "Alice");

        // Verify employees were transferred
        var bobAfter = _context.Employees.AsNoTracking().First(e => e.Id == 3);
        var aliceAfter = _context.Employees.AsNoTracking().First(e => e.Id == 4);
        Assert.Equal(4, bobAfter.DepartmentId);
        Assert.Equal(4, aliceAfter.DepartmentId);

        // Verify Eve stayed in Sales
        var eveAfter = _context.Employees.AsNoTracking().First(e => e.Id == 7);
        Assert.Equal(3, eveAfter.DepartmentId);
    }

    [Fact]
    public void UpdateDepartment_NonExistentDepartment_ShouldThrow()
    {
        // Arrange - Verify department doesn't exist
        var deptExists = _context.Departments.AsNoTracking().Any(d => d.Id == 999);
        Assert.False(deptExists);

        var dto = new UpdateDepartmentDto
        {
            Id = 999,
            Name = "NonExistent"
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _exercises.UpdateDepartment(dto));

        // Verify no department was created
        var stillDoesNotExist = _context.Departments.AsNoTracking().Any(d => d.Id == 999);
        Assert.False(stillDoesNotExist);
    }

    [Fact]
    public void UpdateDepartment_NonExistentEmployee_ShouldThrow()
    {
        // Arrange - Verify employee doesn't exist
        var employeeExists = _context.Employees.AsNoTracking().Any(e => e.Id == 999);
        Assert.False(employeeExists);

        var originalDepartment = _context.Departments
            .AsNoTracking()
            .Include(d => d.Employees)
            .First(d => d.Id == 1);
        var originalEmployeeIds = originalDepartment.Employees.Select(e => e.Id).OrderBy(id => id).ToList();

        var dto = new UpdateDepartmentDto
        {
            Id = 1,
            EmployeeIds = new List<int> { 1, 999 }
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _exercises.UpdateDepartment(dto));

        // Verify department employees were not changed
        var unchangedDepartment = _context.Departments
            .AsNoTracking()
            .Include(d => d.Employees)
            .First(d => d.Id == 1);
        var unchangedEmployeeIds = unchangedDepartment.Employees.Select(e => e.Id).OrderBy(id => id).ToList();
        Assert.Equal(originalEmployeeIds, unchangedEmployeeIds);
    }

    [Fact]
    public void UpdateDepartment_ComplexUpdate_ShouldUpdateAllSpecifiedFields()
    {
        // Arrange - Verify initial state of Sales
        var originalDepartment = _context.Departments
            .AsNoTracking()
            .Include(d => d.Employees)
            .First(d => d.Id == 3);
        Assert.Equal("Sales", originalDepartment.Name);
        Assert.Equal("Building C", originalDepartment.Location);
        Assert.Equal(300000, originalDepartment.Budget);

        // Verify target employees exist in various departments
        var employeeIds = new[] { 5, 6, 7, 8 };
        foreach (var id in employeeIds)
        {
            var employee = _context.Employees.AsNoTracking().FirstOrDefault(e => e.Id == id);
            Assert.NotNull(employee);
        }

        var dto = new UpdateDepartmentDto
        {
            Id = 3,
            Name = "Sales & Business Development",
            Location = "Building C - Floor 3",
            Budget = 350000,
            EmployeeIds = new List<int> { 5, 6, 7, 8 } // Charlie, Diana, Eve, Frank
        };

        // Act
        var result = _exercises.UpdateDepartment(dto);

        // Assert - Check returned object scalar properties
        Assert.Equal("Sales & Business Development", result.Name);
        Assert.Equal("Building C - Floor 3", result.Location);
        Assert.Equal(350000, result.Budget);

        // Assert - Check returned object one-to-many
        Assert.Equal(4, result.Employees.Count);
        Assert.Contains(result.Employees, e => e.Id == 5);
        Assert.Contains(result.Employees, e => e.Id == 6);
        Assert.Contains(result.Employees, e => e.Id == 7);
        Assert.Contains(result.Employees, e => e.Id == 8);

        // Assert - Verify complete DB state
        var dbDepartment = _context.Departments
            .AsNoTracking()
            .Include(d => d.Employees)
            .First(d => d.Id == 3);

        Assert.Equal("Sales & Business Development", dbDepartment.Name);
        Assert.Equal("Building C - Floor 3", dbDepartment.Location);
        Assert.Equal(350000, dbDepartment.Budget);
        Assert.Equal(4, dbDepartment.Employees.Count);
        Assert.Contains(dbDepartment.Employees, e => e.Id == 5 && e.FirstName == "Charlie");
        Assert.Contains(dbDepartment.Employees, e => e.Id == 6 && e.FirstName == "Diana");
        Assert.Contains(dbDepartment.Employees, e => e.Id == 7 && e.FirstName == "Eve");
        Assert.Contains(dbDepartment.Employees, e => e.Id == 8 && e.FirstName == "Frank");

        // Verify all employees have correct department ID in DB
        foreach (var employeeId in new[] { 5, 6, 7, 8 })
        {
            var employee = _context.Employees.AsNoTracking().First(e => e.Id == employeeId);
            Assert.Equal(3, employee.DepartmentId);
        }
    }

    [Fact]
    public void UpdateDepartment_KeepSameEmployees_ShouldBeIdempotent()
    {
        // Arrange - Get current Engineering employees
        var engineeringDept = _context.Departments
            .AsNoTracking()
            .Include(d => d.Employees)
            .First(d => d.Id == 1);
        var currentEmployeeIds = engineeringDept.Employees.Select(e => e.Id).OrderBy(id => id).ToList();
        Assert.True(currentEmployeeIds.Count > 0);

        var dto = new UpdateDepartmentDto
        {
            Id = 1,
            EmployeeIds = currentEmployeeIds // Same employees
        };

        // Act
        var result = _exercises.UpdateDepartment(dto);

        // Assert - Check returned object
        Assert.Equal(currentEmployeeIds.Count, result.Employees.Count);
        foreach (var id in currentEmployeeIds)
        {
            Assert.Contains(result.Employees, e => e.Id == id);
        }

        // Assert - Verify DB state unchanged
        var afterUpdate = _context.Departments
            .AsNoTracking()
            .Include(d => d.Employees)
            .First(d => d.Id == 1);
        var afterEmployeeIds = afterUpdate.Employees.Select(e => e.Id).OrderBy(id => id).ToList();
        Assert.Equal(currentEmployeeIds, afterEmployeeIds);

        // Verify all employees still have correct department
        foreach (var employeeId in currentEmployeeIds)
        {
            var employee = _context.Employees.AsNoTracking().First(e => e.Id == employeeId);
            Assert.Equal(1, employee.DepartmentId);
        }
    }
}
