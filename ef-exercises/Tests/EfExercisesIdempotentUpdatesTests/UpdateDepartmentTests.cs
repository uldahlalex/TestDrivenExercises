using Microsoft.EntityFrameworkCore;
using tests.Data;
using tests.DTOs;
using tests.Interfaces;

namespace tests.Tests.EfExercisesIdempotentUpdatesTests;

public class UpdateDepartmentTests(CompanyDbContext context, IEfExercisesIdempotentUpdates exercises)
{
    [Fact]
    public async Task UpdateDepartment_AllProperties_ShouldUpdateCorrectly()
    {
        // Arrange - Verify Engineering department exists with original values
        var originalDepartment = await context.Departments.AsNoTracking().FirstAsync(d => d.Id == 1);
        Assert.Equal("Engineering", originalDepartment.Name);
        Assert.Equal("Building A", originalDepartment.Location);
        Assert.Equal(500000, originalDepartment.Budget);

        var dto = new UpdateDepartmentDto
        {
            Id = 1,
            Name = "Engineering & Tech",
            Location = "Building A - Floor 2",
            Budget = 550000,
            EmployeeIds = new List<int> { 1, 2, 8 }
        };

        // Act
        var result = await exercises.UpdateDepartment(dto);

        // Assert - Check returned object
        Assert.Equal("Engineering & Tech", result.Name);
        Assert.Equal("Building A - Floor 2", result.Location);
        Assert.Equal(550000, result.Budget);
        Assert.Equal(3, result.Employees.Count);

        // Assert - Verify DB state
        var dbDepartment = await context.Departments.AsNoTracking().FirstAsync(d => d.Id == 1);
        Assert.Equal("Engineering & Tech", dbDepartment.Name);
        Assert.Equal("Building A - Floor 2", dbDepartment.Location);
        Assert.Equal(550000, dbDepartment.Budget);
    }

    [Fact]
    public async Task UpdateDepartment_AddEmployees_ShouldUpdateOneToMany()
    {
        // Arrange - Verify initial state
        var marketingDept = await context.Departments
            .AsNoTracking()
            .Include(d => d.Employees).FirstAsync(d => d.Id == 2);
        var initialMarketingEmployeeIds = marketingDept.Employees.Select(e => e.Id).ToList();
        Assert.Contains(3, initialMarketingEmployeeIds); // Bob
        Assert.Contains(4, initialMarketingEmployeeIds); // Alice

        // Verify Charlie and Diana are in Sales
        var charlie = await context.Employees.AsNoTracking().FirstAsync(e => e.Id == 5);
        var diana = await context.Employees.AsNoTracking().FirstAsync(e => e.Id == 6);
        Assert.Equal(3, charlie.DepartmentId); // Sales
        Assert.Equal(3, diana.DepartmentId); // Sales

        var dto = new UpdateDepartmentDto
        {
            Id = 2,
            Name = "Marketing",
            Location = "Building B",
            Budget = 200000,
            EmployeeIds = new List<int> { 3, 4, 5, 6 } // Add Charlie and Diana
        };

        // Act
        var result = await exercises.UpdateDepartment(dto);

        // Assert - Check returned object
        Assert.Equal(4, result.Employees.Count);
        Assert.Contains(result.Employees, e => e.Id == 3);
        Assert.Contains(result.Employees, e => e.Id == 4);
        Assert.Contains(result.Employees, e => e.Id == 5);
        Assert.Contains(result.Employees, e => e.Id == 6);

        // Assert - Verify DB state
        var dbDepartment = await context.Departments
            .AsNoTracking()
            .Include(d => d.Employees).FirstAsync(d => d.Id == 2);
        Assert.Equal(4, dbDepartment.Employees.Count);
        Assert.Contains(dbDepartment.Employees, e => e.Id == 3 && e.FirstName == "Bob");
        Assert.Contains(dbDepartment.Employees, e => e.Id == 4 && e.FirstName == "Alice");
        Assert.Contains(dbDepartment.Employees, e => e.Id == 5 && e.FirstName == "Charlie");
        Assert.Contains(dbDepartment.Employees, e => e.Id == 6 && e.FirstName == "Diana");

        // Verify employees were actually transferred in DB
        var charlieAfter = await context.Employees.AsNoTracking().FirstAsync(e => e.Id == 5);
        var dianaAfter = await context.Employees.AsNoTracking().FirstAsync(e => e.Id == 6);
        Assert.Equal(2, charlieAfter.DepartmentId);
        Assert.Equal(2, dianaAfter.DepartmentId);
    }

    [Fact]
    public async Task UpdateDepartment_TransferEmployeesBetweenDepartments_ShouldUpdateCorrectly()
    {
        // Arrange - Verify John is in Engineering
        var john = await context.Employees.AsNoTracking().FirstAsync(e => e.Id == 1);
        Assert.Equal(1, john.DepartmentId); // Engineering

        // Get initial Sales employee count
        var initialSalesCount = await context.Employees.AsNoTracking().CountAsync(e => e.DepartmentId == 3);

        var salesDto = new UpdateDepartmentDto
        {
            Id = 3, // Sales
            Name = "Sales",
            Location = "Building C",
            Budget = 300000,
            EmployeeIds = new List<int> { 5, 6, 1 } // Charlie, Diana, John
        };

        // Act
        var result = await exercises.UpdateDepartment(salesDto);

        // Assert - Check returned object
        Assert.Equal(3, result.Employees.Count);
        Assert.Contains(result.Employees, e => e.Id == 1);
        Assert.Contains(result.Employees, e => e.Id == 5);
        Assert.Contains(result.Employees, e => e.Id == 6);

        // Assert - Verify DB state
        var johnAfter = await context.Employees.AsNoTracking().FirstAsync(e => e.Id == 1);
        Assert.Equal(3, johnAfter.DepartmentId);

        var salesDept = await context.Departments
            .AsNoTracking()
            .Include(d => d.Employees).FirstAsync(d => d.Id == 3);
        Assert.Equal(3, salesDept.Employees.Count);
        Assert.Contains(salesDept.Employees, e => e.Id == 1 && e.FirstName == "John");
    }

    [Fact]
    public async Task UpdateDepartment_Idempotent_ShouldProduceSameResultWhenCalledTwice()
    {
        // Arrange - Verify initial state
        var initialDepartment = await context.Departments
            .AsNoTracking()
            .Include(d => d.Employees).FirstAsync(d => d.Id == 2);
        Assert.Equal("Marketing", initialDepartment.Name);
        Assert.Equal(200000, initialDepartment.Budget);

        var dto = new UpdateDepartmentDto
        {
            Id = 2,
            Name = "Marketing & Sales",
            Location = "Building B",
            Budget = 250000,
            EmployeeIds = new List<int> { 3, 4, 5 } // Bob, Alice, Charlie
        };

        // Act - Call twice
        var result1 = await exercises.UpdateDepartment(dto);

        // Verify first call worked
        var afterFirstCall = await context.Departments.AsNoTracking()
            .Include(d => d.Employees).FirstAsync(d => d.Id == 2);
        Assert.Equal("Marketing & Sales", afterFirstCall.Name);
        Assert.Equal(250000, afterFirstCall.Budget);
        Assert.Equal(3, afterFirstCall.Employees.Count);

        var result2 = await exercises.UpdateDepartment(dto);

        // Assert - Both calls should produce identical results
        Assert.Equal(result1.Name, result2.Name);
        Assert.Equal(result1.Budget, result2.Budget);
        Assert.Equal(result1.Employees.Count, result2.Employees.Count);
        Assert.All(result1.Employees, e => Assert.Contains(result2.Employees, e2 => e2.Id == e.Id));

        // Assert - DB state after second call is identical
        var afterSecondCall = await context.Departments.AsNoTracking()
            .Include(d => d.Employees).FirstAsync(d => d.Id == 2);
        Assert.Equal("Marketing & Sales", afterSecondCall.Name);
        Assert.Equal(250000, afterSecondCall.Budget);
        Assert.Equal(3, afterSecondCall.Employees.Count);
        Assert.Contains(afterSecondCall.Employees, e => e.Id == 3);
        Assert.Contains(afterSecondCall.Employees, e => e.Id == 4);
        Assert.Contains(afterSecondCall.Employees, e => e.Id == 5);
    }

    [Fact]
    public async Task UpdateDepartment_ReplaceEmployees_ShouldUpdateOneToMany()
    {
        // Arrange - HR currently has Eve (7)
        var hrDept = await context.Departments
            .AsNoTracking()
            .Include(d => d.Employees).FirstAsync(d => d.Id == 4);
        var originalHrEmployeeIds = hrDept.Employees.Select(e => e.Id).ToList();
        Assert.Contains(7, originalHrEmployeeIds);

        // First, move Eve to Sales to avoid orphaning
        var moveEveDto = new UpdateDepartmentDto
        {
            Id = 3, // Sales
            Name = "Sales",
            Location = "Building C",
            Budget = 300000,
            EmployeeIds = new List<int> { 5, 6, 7 } // Charlie, Diana, Eve
        };
        await exercises.UpdateDepartment(moveEveDto);

        // Verify Eve is now in Sales
        var eveAfterMove = await context.Employees.AsNoTracking().FirstAsync(e => e.Id == 7);
        Assert.Equal(3, eveAfterMove.DepartmentId);

        // Now update HR with Bob and Alice
        var dto = new UpdateDepartmentDto
        {
            Id = 4, // HR
            Name = "HR",
            Location = "Building D",
            Budget = 150000,
            EmployeeIds = new List<int> { 3, 4 }
        };

        // Act
        var result = await exercises.UpdateDepartment(dto);

        // Assert - Check returned object
        Assert.Equal(2, result.Employees.Count);
        Assert.Contains(result.Employees, e => e.Id == 3);
        Assert.Contains(result.Employees, e => e.Id == 4);
        Assert.DoesNotContain(result.Employees, e => e.Id == 7);

        // Assert - Verify DB state
        var dbDepartment = await context.Departments
            .AsNoTracking()
            .Include(d => d.Employees).FirstAsync(d => d.Id == 4);
        Assert.Equal(2, dbDepartment.Employees.Count);
        Assert.Contains(dbDepartment.Employees, e => e.Id == 3 && e.FirstName == "Bob");
        Assert.Contains(dbDepartment.Employees, e => e.Id == 4 && e.FirstName == "Alice");

        // Verify employees were transferred
        var bobAfter = await context.Employees.AsNoTracking().FirstAsync(e => e.Id == 3);
        var aliceAfter = await context.Employees.AsNoTracking().FirstAsync(e => e.Id == 4);
        Assert.Equal(4, bobAfter.DepartmentId);
        Assert.Equal(4, aliceAfter.DepartmentId);

        // Verify Eve stayed in Sales
        var eveAfter = await context.Employees.AsNoTracking().FirstAsync(e => e.Id == 7);
        Assert.Equal(3, eveAfter.DepartmentId);
    }

    [Fact]
    public async Task UpdateDepartment_NonExistentDepartment_ShouldThrow()
    {
        // Arrange - Verify department doesn't exist
        var deptExists = await context.Departments.AsNoTracking().AnyAsync(d => d.Id == 999, cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(deptExists);

        var dto = new UpdateDepartmentDto
        {
            Id = 999,
            Name = "NonExistent",
            Location = "Nowhere",
            Budget = 100000,
            EmployeeIds = new List<int>()
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await exercises.UpdateDepartment(dto));

        // Verify no department was created
        var stillDoesNotExist = await context.Departments.AsNoTracking().AnyAsync(d => d.Id == 999, cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(stillDoesNotExist);
    }

    [Fact]
    public async Task UpdateDepartment_NonExistentEmployee_ShouldThrow()
    {
        // Arrange - Verify employee doesn't exist
        var employeeExists = await context.Employees.AsNoTracking().AnyAsync(e => e.Id == 999, cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(employeeExists);

        var originalDepartment = await context.Departments
            .AsNoTracking()
            .Include(d => d.Employees).FirstAsync(d => d.Id == 1, cancellationToken: TestContext.Current.CancellationToken);
        var originalEmployeeIds = originalDepartment.Employees.Select(e => e.Id).OrderBy(id => id).ToList();

        var dto = new UpdateDepartmentDto
        {
            Id = 1,
            Name = "Engineering",
            Location = "Building A",
            Budget = 500000,
            EmployeeIds = new List<int> { 1, 999 }
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await exercises.UpdateDepartment(dto));

        // Verify department employees were not changed
        var unchangedDepartment = await context.Departments
            .AsNoTracking()
            .Include(d => d.Employees).FirstAsync(d => d.Id == 1, cancellationToken: TestContext.Current.CancellationToken);
        var unchangedEmployeeIds = unchangedDepartment.Employees.Select(e => e.Id).OrderBy(id => id).ToList();
        Assert.Equal(originalEmployeeIds, unchangedEmployeeIds);
    }

    [Fact]
    public async Task UpdateDepartment_ComplexUpdate_ShouldUpdateAllSpecifiedFields()
    {
        // Arrange - Verify initial state of Sales
        var originalDepartment = await context.Departments
            .AsNoTracking()
            .Include(d => d.Employees).FirstAsync(d => d.Id == 3, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("Sales", originalDepartment.Name);
        Assert.Equal("Building C", originalDepartment.Location);
        Assert.Equal(300000, originalDepartment.Budget);

        // Verify target employees exist in various departments
        var employeeIds = new[] { 5, 6, 7, 8 };
        foreach (var id in employeeIds)
        {
            var employee = await context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
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
        var result = await exercises.UpdateDepartment(dto);

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
        var dbDepartment = await context.Departments
            .AsNoTracking()
            .Include(d => d.Employees).FirstAsync(d => d.Id == 3, cancellationToken: TestContext.Current.CancellationToken);

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
            var employee = await context.Employees.AsNoTracking().FirstAsync(e => e.Id == employeeId);
            Assert.Equal(3, employee.DepartmentId);
        }
    }

    [Fact]
    public async Task UpdateDepartment_KeepSameEmployees_ShouldBeIdempotent()
    {
        // Arrange - Get current Engineering employees
        var engineeringDept = await context.Departments
            .AsNoTracking()
            .Include(d => d.Employees).FirstAsync(d => d.Id == 1, cancellationToken: TestContext.Current.CancellationToken);
        var currentEmployeeIds = engineeringDept.Employees.Select(e => e.Id).OrderBy(id => id).ToList();
        Assert.True(currentEmployeeIds.Count > 0);

        var dto = new UpdateDepartmentDto
        {
            Id = 1,
            Name = "Engineering",
            Location = "Building A",
            Budget = 500000,
            EmployeeIds = currentEmployeeIds // Same employees
        };

        // Act
        var result = await exercises.UpdateDepartment(dto);

        // Assert - Check returned object
        Assert.Equal(currentEmployeeIds.Count, result.Employees.Count);
        foreach (var id in currentEmployeeIds)
        {
            Assert.Contains(result.Employees, e => e.Id == id);
        }

        // Assert - Verify DB state unchanged
        var afterUpdate = await context.Departments
            .AsNoTracking()
            .Include(d => d.Employees).FirstAsync(d => d.Id == 1, cancellationToken: TestContext.Current.CancellationToken);
        var afterEmployeeIds = afterUpdate.Employees.Select(e => e.Id).OrderBy(id => id).ToList();
        Assert.Equal(currentEmployeeIds, afterEmployeeIds);

        // Verify all employees still have correct department
        foreach (var employeeId in currentEmployeeIds)
        {
            var employee = await context.Employees.AsNoTracking().FirstAsync(e => e.Id == employeeId);
            Assert.Equal(1, employee.DepartmentId);
        }
    }

   
}
