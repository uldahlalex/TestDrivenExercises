using Microsoft.EntityFrameworkCore;
using tests.Data;
using tests.Interfaces;

namespace tests.Tests.EfExercisesSoftDeletesTests;

public class SoftDeleteProjectTests(CompanyDbContext context, IEfExercisesSoftDeletes exercises)
{
    [Fact]
    public async Task SoftDeleteProject_ValidProject_ShouldMarkAsDeleted()
    {
        // Arrange - Project Alpha exists
        var project = await context.Projects.AsNoTracking().FirstAsync(p => p.Id == 1);
        Assert.Equal("Project Alpha", project.Name);
        Assert.False(project.IsDeleted);

        var beforeDelete = DateTime.UtcNow;

        // Act
        var result = await exercises.SoftDeleteProject(1);

        var afterDelete = DateTime.UtcNow;

        // Assert - Check returned object
        Assert.True(result.IsDeleted);
        Assert.NotNull(result.DeletedAt);
        Assert.InRange(result.DeletedAt.Value, beforeDelete.AddSeconds(-1), afterDelete.AddSeconds(1));

        // Assert - Verify DB state
        var dbProject = await context.Projects
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(p => p.Id == 1);
        Assert.True(dbProject.IsDeleted);
        Assert.NotNull(dbProject.DeletedAt);
    }

    [Fact]
    public async Task SoftDeleteProject_WithAssignedEmployees_ShouldRemoveEmployeeAssignments()
    {
        // Arrange - Assign employees to project
        var project = await context.Projects
            .Include(p => p.Employees)
            .FirstAsync(p => p.Id == 1);

        var employee1 = await context.Employees.FirstAsync(e => e.Id == 1);
        var employee2 = await context.Employees.FirstAsync(e => e.Id == 2);
        project.Employees.Add(employee1);
        project.Employees.Add(employee2);
        await context.SaveChangesAsync();

        // Verify employees are assigned
        var withEmployees = await context.Projects
            .Include(p => p.Employees)
            .AsNoTracking()
            .FirstAsync(p => p.Id == 1);
        Assert.Equal(2, withEmployees.Employees.Count);

        // Act - Soft delete the project
        var result = await exercises.SoftDeleteProject(1);

        // Assert - Employees should be unassigned from project
        var deletedProject = await context.Projects
            .IgnoreQueryFilters()
            .Include(p => p.Employees)
            .AsNoTracking()
            .FirstAsync(p => p.Id == 1);
        Assert.Empty(deletedProject.Employees);

        // Verify employees still exist and are active
        var employee1Still = await context.Employees.FindAsync(1);
        var employee2Still = await context.Employees.FindAsync(2);
        Assert.NotNull(employee1Still);
        Assert.NotNull(employee2Still);
        Assert.False(employee1Still.IsDeleted);
        Assert.False(employee2Still.IsDeleted);
    }

    [Fact]
    public async Task SoftDeleteProject_AlreadyDeleted_ShouldBeIdempotent()
    {
        // Arrange - Soft delete the project first
        await exercises.SoftDeleteProject(1);

        var firstDelete = await context.Projects
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(p => p.Id == 1);
        Assert.True(firstDelete.IsDeleted);

        // Act - Delete again
        var result = await exercises.SoftDeleteProject(1);

        // Assert - Should still be deleted
        Assert.True(result.IsDeleted);
        Assert.NotNull(result.DeletedAt);
    }

    [Fact]
    public async Task SoftDeleteProject_NonExistent_ShouldThrow()
    {
        // Arrange
        var exists = await context.Projects
            .IgnoreQueryFilters()
            .AnyAsync(p => p.Id == 999);
        Assert.False(exists);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.SoftDeleteProject(999));
    }

    [Fact]
    public async Task RestoreProject_SoftDeletedProject_ShouldRestore()
    {
        // Arrange - Soft delete the project first
        await exercises.SoftDeleteProject(1);

        var deleted = await context.Projects
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(p => p.Id == 1);
        Assert.True(deleted.IsDeleted);
        Assert.NotNull(deleted.DeletedAt);

        // Act
        var result = await exercises.RestoreProject(1);

        // Assert - Check returned object
        Assert.False(result.IsDeleted);
        Assert.Null(result.DeletedAt);

        // Assert - Verify DB state
        var dbProject = await context.Projects
            .AsNoTracking()
            .FirstAsync(p => p.Id == 1);
        Assert.False(dbProject.IsDeleted);
        Assert.Null(dbProject.DeletedAt);
    }

    [Fact]
    public async Task RestoreProject_ActiveProject_ShouldBeIdempotent()
    {
        // Arrange - Project Beta is active
        var project = await context.Projects.AsNoTracking().FirstAsync(p => p.Id == 2);
        Assert.Equal("Project Beta", project.Name);
        Assert.False(project.IsDeleted);

        // Act
        var result = await exercises.RestoreProject(2);

        // Assert - Should still be active
        Assert.False(result.IsDeleted);
        Assert.Null(result.DeletedAt);
    }

    [Fact]
    public async Task RestoreProject_NonExistent_ShouldThrow()
    {
        // Arrange
        var exists = await context.Projects
            .IgnoreQueryFilters()
            .AnyAsync(p => p.Id == 999);
        Assert.False(exists);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.RestoreProject(999));
    }

    [Fact]
    public async Task RestoreProject_DoesNotRestoreEmployeeAssignments()
    {
        // Arrange - Assign employees to project, then soft delete
        var project = await context.Projects
            .Include(p => p.Employees)
            .FirstAsync(p => p.Id == 1);

        var employee1 = await context.Employees.FirstAsync(e => e.Id == 1);
        var employee2 = await context.Employees.FirstAsync(e => e.Id == 2);
        project.Employees.Add(employee1);
        project.Employees.Add(employee2);
        await context.SaveChangesAsync();

        // Verify employees are assigned
        var beforeDelete = await context.Projects
            .Include(p => p.Employees)
            .AsNoTracking()
            .FirstAsync(p => p.Id == 1);
        Assert.Equal(2, beforeDelete.Employees.Count);

        // Soft delete the project (which removes employee assignments)
        await exercises.SoftDeleteProject(1);

        // Act - Restore the project
        var result = await exercises.RestoreProject(1);

        // Assert - Employee assignments should NOT be automatically restored
        Assert.Empty(result.Employees);

        var dbProject = await context.Projects
            .Include(p => p.Employees)
            .AsNoTracking()
            .FirstAsync(p => p.Id == 1);
        Assert.Empty(dbProject.Employees);
    }

    [Fact]
    public async Task SoftDeleteProject_DeleteRestore_MultipleTimesWorks()
    {
        // Arrange
        var project = await context.Projects.AsNoTracking().FirstAsync(p => p.Id == 1);
        Assert.False(project.IsDeleted);

        // Act & Assert - Delete and restore multiple times
        await exercises.SoftDeleteProject(1);
        var afterFirstDelete = await context.Projects
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(p => p.Id == 1);
        Assert.True(afterFirstDelete.IsDeleted);

        await exercises.RestoreProject(1);
        var afterFirstRestore = await context.Projects.AsNoTracking().FirstAsync(p => p.Id == 1);
        Assert.False(afterFirstRestore.IsDeleted);

        await exercises.SoftDeleteProject(1);
        var afterSecondDelete = await context.Projects
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(p => p.Id == 1);
        Assert.True(afterSecondDelete.IsDeleted);

        await exercises.RestoreProject(1);
        var afterSecondRestore = await context.Projects.AsNoTracking().FirstAsync(p => p.Id == 1);
        Assert.False(afterSecondRestore.IsDeleted);
    }
}
