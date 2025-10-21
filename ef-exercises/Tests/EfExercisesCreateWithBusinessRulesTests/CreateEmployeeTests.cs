using Microsoft.EntityFrameworkCore;
using tests.Data;
using tests.DTOs;
using tests.Interfaces;

namespace tests.Tests.EfExercisesCreateWithBusinessRulesTests;

public class CreateEmployeeTests(CompanyDbContext context, IEfExercisesCreateWithBusinessRules exercises)
{
    [Fact]
    public async Task CreateEmployee_ValidData_ShouldCreateSuccessfully()
    {
        // Arrange
        var dto = new CreateEmployeeDto
        {
            FirstName = "Alice",
            LastName = "Anderson",
            Email = "alice.anderson@company.com",
            Salary = 70000,
            HireDate = DateTime.UtcNow,
            DepartmentId = 1, // Engineering
            ProjectIds = new List<int> { 1 }
        };

        var initialCount = await context.Employees.CountAsync();

        // Act
        var result = await exercises.CreateEmployee(dto);

        // Assert - Check returned object
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("Alice", result.FirstName);
        Assert.Equal("Anderson", result.LastName);
        Assert.Equal("alice.anderson@company.com", result.Email);
        Assert.Equal(70000, result.Salary);
        Assert.Equal(1, result.DepartmentId);
        Assert.Single(result.Projects);
        Assert.Contains(result.Projects, p => p.Id == 1);

        // Assert - Verify DB state
        var dbEmployee = await context.Employees
            .Include(e => e.Projects)
            .AsNoTracking()
            .FirstAsync(e => e.Email == "alice.anderson@company.com");
        Assert.Equal("Alice", dbEmployee.FirstName);
        Assert.Single(dbEmployee.Projects);

        var newCount = await context.Employees.CountAsync();
        Assert.Equal(initialCount + 1, newCount);
    }

    [Fact]
    public async Task CreateEmployee_DuplicateEmail_ShouldThrow()
    {
        // Arrange - John Doe already exists with john.doe@company.com
        var existing = await context.Employees.FirstAsync(e => e.Id == 1);
        Assert.Equal("john.doe@company.com", existing.Email);

        var dto = new CreateEmployeeDto
        {
            FirstName = "Johnny",
            LastName = "Doe",
            Email = "john.doe@company.com", // Duplicate!
            Salary = 60000,
            HireDate = DateTime.UtcNow,
            DepartmentId = 2,
            ProjectIds = new List<int>()
        };

        var initialCount = await context.Employees.CountAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.CreateEmployee(dto));

        // Verify no employee was created
        var newCount = await context.Employees.CountAsync();
        Assert.Equal(initialCount, newCount);
    }

    [Fact]
    public async Task CreateEmployee_NonExistentDepartment_ShouldThrow()
    {
        // Arrange
        var dto = new CreateEmployeeDto
        {
            FirstName = "Bob",
            LastName = "Builder",
            Email = "bob.builder@company.com",
            Salary = 65000,
            HireDate = DateTime.UtcNow,
            DepartmentId = 999, // Does not exist
            ProjectIds = new List<int>()
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.CreateEmployee(dto));
    }

    [Fact]
    public async Task CreateEmployee_SalaryExceedsDepartmentBudgetConstraint_ShouldThrow()
    {
        // Arrange - Engineering has budget of 500,000
        var engineering = await context.Departments.FirstAsync(d => d.Id == 1);
        Assert.Equal(500000, engineering.Budget);

        var excessiveSalary = engineering.Budget * 0.35; // 35% exceeds 30% limit

        var dto = new CreateEmployeeDto
        {
            FirstName = "Rich",
            LastName = "Person",
            Email = "rich.person@company.com",
            Salary = excessiveSalary,
            HireDate = DateTime.UtcNow,
            DepartmentId = 1,
            ProjectIds = new List<int>()
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.CreateEmployee(dto));
    }

    [Fact]
    public async Task CreateEmployee_WithinDepartmentBudgetConstraint_ShouldSucceed()
    {
        // Arrange - Engineering has budget of 500,000
        var engineering = await context.Departments.FirstAsync(d => d.Id == 1);
        Assert.Equal(500000, engineering.Budget);

        var validSalary = engineering.Budget * 0.25; // 25% is within 30% limit

        var dto = new CreateEmployeeDto
        {
            FirstName = "Reasonable",
            LastName = "Salary",
            Email = "reasonable@company.com",
            Salary = validSalary,
            HireDate = DateTime.UtcNow,
            DepartmentId = 1,
            ProjectIds = new List<int>()
        };

        // Act
        var result = await exercises.CreateEmployee(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(validSalary, result.Salary);
    }

    [Fact]
    public async Task CreateEmployee_MoreThan3Projects_ShouldThrow()
    {
        // Arrange
        var dto = new CreateEmployeeDto
        {
            FirstName = "Busy",
            LastName = "Person",
            Email = "busy@company.com",
            Salary = 70000,
            HireDate = DateTime.UtcNow,
            DepartmentId = 1,
            ProjectIds = new List<int> { 1, 2, 3, 1 } // 4 projects (even with duplicate) - too many
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.CreateEmployee(dto));
    }

    [Fact]
    public async Task CreateEmployee_Exactly3Projects_ShouldSucceed()
    {
        // Arrange
        var dto = new CreateEmployeeDto
        {
            FirstName = "Busy",
            LastName = "ButOk",
            Email = "busyok@company.com",
            Salary = 70000,
            HireDate = DateTime.UtcNow,
            DepartmentId = 1,
            ProjectIds = new List<int> { 1, 2, 3 } // Exactly 3 - should be OK
        };

        // Act
        var result = await exercises.CreateEmployee(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Projects.Count);
    }

    [Fact]
    public async Task CreateEmployee_NonExistentProject_ShouldThrow()
    {
        // Arrange
        var dto = new CreateEmployeeDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@company.com",
            Salary = 70000,
            HireDate = DateTime.UtcNow,
            DepartmentId = 1,
            ProjectIds = new List<int> { 999 } // Does not exist
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.CreateEmployee(dto));
    }

    [Fact]
    public async Task CreateEmployee_SoftDeletedDepartment_ShouldThrow()
    {
        // Arrange - Soft delete a department first
        var deptToDelete = await context.Departments
            .Include(d => d.Employees)
            .FirstAsync(d => d.Id == 4); // HR

        // Soft delete all employees first
        foreach (var emp in deptToDelete.Employees.ToList())
        {
            emp.IsDeleted = true;
            emp.DeletedAt = DateTime.UtcNow;
        }

        // Soft delete the department
        deptToDelete.IsDeleted = true;
        deptToDelete.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        var dto = new CreateEmployeeDto
        {
            FirstName = "New",
            LastName = "Hire",
            Email = "newhire@company.com",
            Salary = 50000,
            HireDate = DateTime.UtcNow,
            DepartmentId = 4, // Soft-deleted department
            ProjectIds = new List<int>()
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.CreateEmployee(dto));
    }

    [Fact]
    public async Task CreateEmployee_SoftDeletedProject_ShouldThrow()
    {
        // Arrange - Soft delete a project
        var projectToDelete = await context.Projects
            .Include(p => p.Employees)
            .FirstAsync(p => p.Id == 1);

        projectToDelete.Employees.Clear();
        projectToDelete.IsDeleted = true;
        projectToDelete.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        var dto = new CreateEmployeeDto
        {
            FirstName = "New",
            LastName = "Employee",
            Email = "newemployee@company.com",
            Salary = 70000,
            HireDate = DateTime.UtcNow,
            DepartmentId = 2,
            ProjectIds = new List<int> { 1 } // Soft-deleted project
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.CreateEmployee(dto));
    }
}
