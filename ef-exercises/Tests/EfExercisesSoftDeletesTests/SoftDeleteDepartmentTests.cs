using Microsoft.EntityFrameworkCore;
using tests.Data;
using tests.Interfaces;

namespace tests.Tests.EfExercisesSoftDeletesTests;

public class SoftDeleteDepartmentTests(CompanyDbContext context, IEfExercisesSoftDeletes exercises)
{
    [Fact]
    public async Task SoftDeleteDepartment_WithNoEmployees_ShouldMarkAsDeleted()
    {
        // Arrange - Use HR department which has employees, but soft delete them first
        var hrDept = await context.Departments
            .Include(d => d.Employees)
            .AsNoTracking()
            .FirstAsync(d => d.Id == 4);
        Assert.Equal("HR", hrDept.Name);

        // Soft delete all employees first
        foreach (var employee in hrDept.Employees)
        {
            await exercises.SoftDeleteEmployee(employee.Id);
        }

        var beforeDelete = DateTime.UtcNow;

        // Act
        var result = await exercises.SoftDeleteDepartment(4);

        var afterDelete = DateTime.UtcNow;

        // Assert - Check returned object
        Assert.True(result.IsDeleted);
        Assert.NotNull(result.DeletedAt);
        Assert.InRange(result.DeletedAt.Value, beforeDelete.AddSeconds(-1), afterDelete.AddSeconds(1));

        // Assert - Verify DB state
        var dbDept = await context.Departments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(d => d.Id == 4);
        Assert.True(dbDept.IsDeleted);
        Assert.NotNull(dbDept.DeletedAt);
    }

    [Fact]
    public async Task SoftDeleteDepartment_WithActiveEmployees_ShouldThrow()
    {
        // Arrange - Engineering department has employees
        var dept = await context.Departments
            .Include(d => d.Employees)
            .AsNoTracking()
            .FirstAsync(d => d.Id == 1);
        Assert.Equal("Engineering", dept.Name);
        Assert.True(dept.Employees.Count > 0, "Engineering should have employees");
        Assert.All(dept.Employees, e => Assert.False(e.IsDeleted));

        var originalIsDeleted = dept.IsDeleted;

        // Act & Assert - Should throw because department has active employees
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.SoftDeleteDepartment(1));

        // Verify department was not deleted
        var unchangedDept = await context.Departments
            .AsNoTracking()
            .FirstAsync(d => d.Id == 1);
        Assert.Equal(originalIsDeleted, unchangedDept.IsDeleted);
    }

    [Fact]
    public async Task SoftDeleteDepartment_WithOnlyDeletedEmployees_ShouldSucceed()
    {
        // Arrange - Engineering department exists
        var dept = await context.Departments
            .Include(d => d.Employees)
            .AsNoTracking()
            .FirstAsync(d => d.Id == 1);
        Assert.Equal("Engineering", dept.Name);

        // Soft delete all employees in the department
        foreach (var employee in dept.Employees)
        {
            await exercises.SoftDeleteEmployee(employee.Id);
        }

        // Verify all employees are deleted
        var deptWithDeletedEmployees = await context.Departments
            .IgnoreQueryFilters()
            .Include(d => d.Employees)
            .AsNoTracking()
            .FirstAsync(d => d.Id == 1);
        Assert.All(deptWithDeletedEmployees.Employees, e => Assert.True(e.IsDeleted));

        // Act - Now should be able to soft delete the department
        var result = await exercises.SoftDeleteDepartment(1);

        // Assert
        Assert.True(result.IsDeleted);
        Assert.NotNull(result.DeletedAt);

        var dbDept = await context.Departments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(d => d.Id == 1);
        Assert.True(dbDept.IsDeleted);
    }

    [Fact]
    public async Task SoftDeleteDepartment_NonExistent_ShouldThrow()
    {
        // Arrange
        var exists = await context.Departments
            .IgnoreQueryFilters()
            .AnyAsync(d => d.Id == 999);
        Assert.False(exists);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.SoftDeleteDepartment(999));
    }

    [Fact]
    public async Task RestoreDepartment_SoftDeletedDepartment_ShouldRestore()
    {
        // Arrange - Use Sales department, soft delete its employees first, then soft delete department
        var salesDept = await context.Departments
            .Include(d => d.Employees)
            .AsNoTracking()
            .FirstAsync(d => d.Id == 3);
        Assert.Equal("Sales", salesDept.Name);

        // Soft delete all employees in Sales
        foreach (var employee in salesDept.Employees)
        {
            await exercises.SoftDeleteEmployee(employee.Id);
        }

        // Soft delete the department
        await exercises.SoftDeleteDepartment(3);

        var deleted = await context.Departments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(d => d.Id == 3);
        Assert.True(deleted.IsDeleted);

        // Act
        var result = await exercises.RestoreDepartment(3);

        // Assert
        Assert.False(result.IsDeleted);
        Assert.Null(result.DeletedAt);

        var dbDept = await context.Departments
            .AsNoTracking()
            .FirstAsync(d => d.Id == 3);
        Assert.False(dbDept.IsDeleted);
        Assert.Null(dbDept.DeletedAt);
    }

    [Fact]
    public async Task RestoreDepartment_ActiveDepartment_ShouldBeIdempotent()
    {
        // Arrange - Marketing is active
        var dept = await context.Departments.AsNoTracking().FirstAsync(d => d.Id == 2);
        Assert.Equal("Marketing", dept.Name);
        Assert.False(dept.IsDeleted);

        // Act
        var result = await exercises.RestoreDepartment(2);

        // Assert - Should still be active
        Assert.False(result.IsDeleted);
        Assert.Null(result.DeletedAt);
    }

    [Fact]
    public async Task RestoreDepartment_NonExistent_ShouldThrow()
    {
        // Arrange
        var exists = await context.Departments
            .IgnoreQueryFilters()
            .AnyAsync(d => d.Id == 999);
        Assert.False(exists);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.RestoreDepartment(999));
    }

    [Fact]
    public async Task SoftDeleteDepartment_AlreadyDeleted_ShouldBeIdempotent()
    {
        // Arrange - Use Marketing department, soft delete its employees first
        var marketingDept = await context.Departments
            .Include(d => d.Employees)
            .AsNoTracking()
            .FirstAsync(d => d.Id == 2);
        Assert.Equal("Marketing", marketingDept.Name);

        // Soft delete all employees in Marketing
        foreach (var employee in marketingDept.Employees)
        {
            await exercises.SoftDeleteEmployee(employee.Id);
        }

        // Soft delete the department
        await exercises.SoftDeleteDepartment(2);

        var firstDelete = await context.Departments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(d => d.Id == 2);
        Assert.True(firstDelete.IsDeleted);

        // Act - Delete again
        var result = await exercises.SoftDeleteDepartment(2);

        // Assert - Should still be deleted
        Assert.True(result.IsDeleted);
        Assert.NotNull(result.DeletedAt);
    }
}
