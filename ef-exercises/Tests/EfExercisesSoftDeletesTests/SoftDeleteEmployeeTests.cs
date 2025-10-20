using Microsoft.EntityFrameworkCore;
using tests.Data;
using tests.Interfaces;

namespace tests.Tests.EfExercisesSoftDeletesTests;

public class SoftDeleteEmployeeTests(CompanyDbContext context, IEfExercisesSoftDeletes exercises)
{
    [Fact]
    public async Task SoftDeleteEmployee_ValidEmployee_ShouldMarkAsDeleted()
    {
        // Arrange - Verify John exists and is not deleted
        var employee = await context.Employees.AsNoTracking().FirstAsync(e => e.Id == 1);
        Assert.Equal("John", employee.FirstName);
        Assert.False(employee.IsDeleted);
        Assert.Null(employee.DeletedAt);

        var beforeDelete = DateTime.UtcNow;

        // Act
        var result = await exercises.SoftDeleteEmployee(1);

        var afterDelete = DateTime.UtcNow;

        // Assert - Check returned object
        Assert.True(result.IsDeleted);
        Assert.NotNull(result.DeletedAt);
        Assert.InRange(result.DeletedAt.Value, beforeDelete.AddSeconds(-1), afterDelete.AddSeconds(1));

        // Assert - Verify DB state
        var dbEmployee = await context.Employees
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(e => e.Id == 1);
        Assert.True(dbEmployee.IsDeleted);
        Assert.NotNull(dbEmployee.DeletedAt);
        Assert.InRange(dbEmployee.DeletedAt.Value, beforeDelete.AddSeconds(-1), afterDelete.AddSeconds(1));
    }

    [Fact]
    public async Task SoftDeleteEmployee_AlreadyDeleted_ShouldBeIdempotent()
    {
        // Arrange - Soft delete the employee first
        await exercises.SoftDeleteEmployee(1);

        var afterFirstDelete = await context.Employees
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(e => e.Id == 1);
        Assert.True(afterFirstDelete.IsDeleted);
        var firstDeletedAt = afterFirstDelete.DeletedAt;

        // Act - Delete again
        var result = await exercises.SoftDeleteEmployee(1);

        // Assert - Should still be deleted with same or updated timestamp
        Assert.True(result.IsDeleted);
        Assert.NotNull(result.DeletedAt);

        var dbEmployee = await context.Employees
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(e => e.Id == 1);
        Assert.True(dbEmployee.IsDeleted);
        Assert.NotNull(dbEmployee.DeletedAt);
    }

    [Fact]
    public async Task SoftDeleteEmployee_NonExistent_ShouldThrow()
    {
        // Arrange - Verify employee doesn't exist
        var exists = await context.Employees
            .IgnoreQueryFilters()
            .AnyAsync(e => e.Id == 999);
        Assert.False(exists);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.SoftDeleteEmployee(999));
    }

    [Fact]
    public async Task RestoreEmployee_SoftDeletedEmployee_ShouldRestore()
    {
        // Arrange - Soft delete the employee first
        await exercises.SoftDeleteEmployee(1);

        var deleted = await context.Employees
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(e => e.Id == 1);
        Assert.True(deleted.IsDeleted);
        Assert.NotNull(deleted.DeletedAt);

        // Act
        var result = await exercises.RestoreEmployee(1);

        // Assert - Check returned object
        Assert.False(result.IsDeleted);
        Assert.Null(result.DeletedAt);

        // Assert - Verify DB state
        var dbEmployee = await context.Employees
            .AsNoTracking()
            .FirstAsync(e => e.Id == 1);
        Assert.False(dbEmployee.IsDeleted);
        Assert.Null(dbEmployee.DeletedAt);
    }

    [Fact]
    public async Task RestoreEmployee_ActiveEmployee_ShouldBeIdempotent()
    {
        // Arrange - Verify employee is active
        var employee = await context.Employees.AsNoTracking().FirstAsync(e => e.Id == 1);
        Assert.False(employee.IsDeleted);
        Assert.Null(employee.DeletedAt);

        // Act - Restore an already active employee
        var result = await exercises.RestoreEmployee(1);

        // Assert - Should still be active
        Assert.False(result.IsDeleted);
        Assert.Null(result.DeletedAt);

        var dbEmployee = await context.Employees.AsNoTracking().FirstAsync(e => e.Id == 1);
        Assert.False(dbEmployee.IsDeleted);
        Assert.Null(dbEmployee.DeletedAt);
    }

    [Fact]
    public async Task RestoreEmployee_NonExistent_ShouldThrow()
    {
        // Arrange - Verify employee doesn't exist
        var exists = await context.Employees
            .IgnoreQueryFilters()
            .AnyAsync(e => e.Id == 999);
        Assert.False(exists);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.RestoreEmployee(999));
    }

    [Fact]
    public async Task GetActiveEmployees_WithSoftDeletedEmployees_ShouldReturnOnlyActive()
    {
        // Arrange - Get initial count
        var initialCount = await context.Employees.CountAsync();
        Assert.True(initialCount >= 3, "Need at least 3 employees for this test");

        // Soft delete one employee
        await exercises.SoftDeleteEmployee(1);

        // Act
        var activeEmployees = await exercises.GetActiveEmployees();

        // Assert
        Assert.Equal(initialCount - 1, activeEmployees.Count);
        Assert.DoesNotContain(activeEmployees, e => e.Id == 1);
        Assert.All(activeEmployees, e => Assert.False(e.IsDeleted));
    }

    [Fact]
    public async Task GetDeletedEmployees_WithSoftDeletedEmployees_ShouldReturnOnlyDeleted()
    {
        // Arrange - Soft delete two employees
        await exercises.SoftDeleteEmployee(1);
        await exercises.SoftDeleteEmployee(2);

        // Act
        var deletedEmployees = await exercises.GetDeletedEmployees();

        // Assert
        Assert.True(deletedEmployees.Count >= 2);
        Assert.Contains(deletedEmployees, e => e.Id == 1);
        Assert.Contains(deletedEmployees, e => e.Id == 2);
        Assert.All(deletedEmployees, e => Assert.True(e.IsDeleted));
        Assert.All(deletedEmployees, e => Assert.NotNull(e.DeletedAt));
    }

    [Fact]
    public async Task GetAllEmployeesIncludingDeleted_ShouldReturnAll()
    {
        // Arrange - Get total count before any deletes
        var totalCount = await context.Employees
            .IgnoreQueryFilters()
            .CountAsync();

        // Soft delete some employees
        await exercises.SoftDeleteEmployee(1);
        await exercises.SoftDeleteEmployee(2);

        // Act
        var allEmployees = await exercises.GetAllEmployeesIncludingDeleted();

        // Assert - Should still return all employees
        Assert.Equal(totalCount, allEmployees.Count);
        Assert.Contains(allEmployees, e => e.Id == 1 && e.IsDeleted);
        Assert.Contains(allEmployees, e => e.Id == 2 && e.IsDeleted);
        Assert.Contains(allEmployees, e => !e.IsDeleted);
    }

    [Fact]
    public async Task HardDeleteEmployee_SoftDeletedEmployee_ShouldPermanentlyDelete()
    {
        // Arrange - Soft delete the employee first
        await exercises.SoftDeleteEmployee(1);

        var softDeleted = await context.Employees
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == 1);
        Assert.NotNull(softDeleted);
        Assert.True(softDeleted.IsDeleted);

        // Act
        await exercises.HardDeleteEmployee(1);

        // Assert - Employee should no longer exist
        var exists = await context.Employees
            .IgnoreQueryFilters()
            .AnyAsync(e => e.Id == 1);
        Assert.False(exists);
    }

    [Fact]
    public async Task HardDeleteEmployee_ActiveEmployee_ShouldThrow()
    {
        // Arrange - Verify employee is active
        var employee = await context.Employees.AsNoTracking().FirstAsync(e => e.Id == 1);
        Assert.False(employee.IsDeleted);

        // Act & Assert - Should not allow hard delete of active employee
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.HardDeleteEmployee(1));

        // Verify employee still exists
        var stillExists = await context.Employees.AnyAsync(e => e.Id == 1);
        Assert.True(stillExists);
    }

    [Fact]
    public async Task HardDeleteEmployee_NonExistent_ShouldThrow()
    {
        // Arrange - Verify employee doesn't exist
        var exists = await context.Employees
            .IgnoreQueryFilters()
            .AnyAsync(e => e.Id == 999);
        Assert.False(exists);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.HardDeleteEmployee(999));
    }

    [Fact]
    public async Task SoftDeleteEmployee_WithProjects_ShouldRemoveProjectAssignments()
    {
        // Arrange - Assign projects to employee first
        var employee = await context.Employees
            .Include(e => e.Projects)
            .FirstAsync(e => e.Id == 1);

        var project1 = await context.Projects.FirstAsync(p => p.Id == 1);
        var project2 = await context.Projects.FirstAsync(p => p.Id == 2);
        employee.Projects.Add(project1);
        employee.Projects.Add(project2);
        await context.SaveChangesAsync();

        // Verify projects are assigned
        var withProjects = await context.Employees
            .Include(e => e.Projects)
            .AsNoTracking()
            .FirstAsync(e => e.Id == 1);
        Assert.Equal(2, withProjects.Projects.Count);

        // Act - Soft delete the employee
        await exercises.SoftDeleteEmployee(1);

        // Assert - Projects should be unassigned
        var deletedEmployee = await context.Employees
            .IgnoreQueryFilters()
            .Include(e => e.Projects)
            .AsNoTracking()
            .FirstAsync(e => e.Id == 1);
        Assert.Empty(deletedEmployee.Projects);

        // Verify projects still exist (not deleted)
        var project1Exists = await context.Projects.AnyAsync(p => p.Id == 1);
        var project2Exists = await context.Projects.AnyAsync(p => p.Id == 2);
        Assert.True(project1Exists);
        Assert.True(project2Exists);
    }

    [Fact]
    public async Task SoftDelete_Restore_SoftDelete_ShouldWorkCorrectly()
    {
        // Arrange - Employee starts active
        var employee = await context.Employees.AsNoTracking().FirstAsync(e => e.Id == 1);
        Assert.False(employee.IsDeleted);

        // Act - Delete, Restore, Delete again
        await exercises.SoftDeleteEmployee(1);
        var afterFirstDelete = await context.Employees
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(e => e.Id == 1);
        Assert.True(afterFirstDelete.IsDeleted);

        await exercises.RestoreEmployee(1);
        var afterRestore = await context.Employees.AsNoTracking().FirstAsync(e => e.Id == 1);
        Assert.False(afterRestore.IsDeleted);

        await exercises.SoftDeleteEmployee(1);
        var afterSecondDelete = await context.Employees
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(e => e.Id == 1);
        Assert.True(afterSecondDelete.IsDeleted);

        // Assert - Final state should be deleted
        Assert.True(afterSecondDelete.IsDeleted);
        Assert.NotNull(afterSecondDelete.DeletedAt);
    }
}
