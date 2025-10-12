using Microsoft.EntityFrameworkCore;
using tests.Data;
using tests.DTOs;
using tests.Interfaces;

namespace tests.Tests.EfExercisesIdempotentUpdatesTests;

public class UpdateEmployeeTests(CompanyDbContext context, IEfExercisesIdempotentUpdates exercises)
{
    [Fact]
    public async Task UpdateEmployee_AllProperties_ShouldUpdateCorrectly()
    {
        // Arrange - Verify John exists with original values
        var originalEmployee = await context.Employees.AsNoTracking().FirstAsync(e => e.Id == 1);
        Assert.Equal("John", originalEmployee.FirstName);
        Assert.Equal("Doe", originalEmployee.LastName);
        Assert.Equal("john.doe@company.com", originalEmployee.Email);
        Assert.Equal(75000, originalEmployee.Salary);
        Assert.Equal(1, originalEmployee.DepartmentId); // Engineering

        var dto = new UpdateEmployeeDto
        {
            Id = 1,
            FirstName = "Jonathan",
            LastName = "Doe-Smith",
            Email = "jonathan.doesmith@company.com",
            Salary = 80000,
            HireDate = new DateTime(2020, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            DepartmentId = 1,
            ProjectIds = new List<int>()
        };

        // Act
        var result = await exercises.UpdateEmployee(dto);

        // Assert - Check returned object
        Assert.Equal("Jonathan", result.FirstName);
        Assert.Equal("Doe-Smith", result.LastName);
        Assert.Equal("jonathan.doesmith@company.com", result.Email);
        Assert.Equal(80000, result.Salary);
        Assert.Equal(1, result.DepartmentId);
        Assert.Equal(new DateTime(2020, 1, 15, 0, 0, 0, DateTimeKind.Utc), result.HireDate);

        // Assert - Verify DB state
        var dbEmployee = await context.Employees.AsNoTracking().FirstAsync(e => e.Id == 1);
        Assert.Equal("Jonathan", dbEmployee.FirstName);
        Assert.Equal("Doe-Smith", dbEmployee.LastName);
        Assert.Equal("jonathan.doesmith@company.com", dbEmployee.Email);
        Assert.Equal(80000, dbEmployee.Salary);
        Assert.Equal(1, dbEmployee.DepartmentId);
        Assert.Equal(new DateTime(2020, 1, 15, 0, 0, 0, DateTimeKind.Utc), dbEmployee.HireDate);
    }

    [Fact]
    public async Task UpdateEmployee_TransferDepartment_ShouldUpdateOneToMany()
    {
        // Arrange - Verify initial state
        var originalEmployee = await context.Employees.AsNoTracking().FirstAsync(e => e.Id == 1);
        Assert.Equal(1, originalEmployee.DepartmentId); // Engineering

        var marketingDept = await context.Departments.AsNoTracking().FirstAsync(d => d.Id == 2);
        Assert.Equal("Marketing", marketingDept.Name);

        var originalMarketingCount = await context.Employees.AsNoTracking().CountAsync(e => e.DepartmentId == 2);
        var originalEngineeringCount = await context.Employees.AsNoTracking().CountAsync(e => e.DepartmentId == 1);

        var dto = new UpdateEmployeeDto
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@company.com",
            Salary = 75000,
            HireDate = new DateTime(2020, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            DepartmentId = 2,
            ProjectIds = new List<int>()
        };

        // Act
        var result = await exercises.UpdateEmployee(dto);

        // Assert - Check returned object
        Assert.Equal(2, result.DepartmentId);
        Assert.NotNull(result.Department);
        Assert.Equal("Marketing", result.Department.Name);

        // Assert - Verify DB state
        var dbEmployee = await context.Employees.AsNoTracking().FirstAsync(e => e.Id == 1);
        Assert.Equal(2, dbEmployee.DepartmentId);

        // Verify department employee counts changed correctly
        var newMarketingCount = await context.Employees.AsNoTracking().CountAsync(e => e.DepartmentId == 2);
        var newEngineeringCount = await context.Employees.AsNoTracking().CountAsync(e => e.DepartmentId == 1);
        Assert.Equal(originalMarketingCount + 1, newMarketingCount);
        Assert.Equal(originalEngineeringCount - 1, newEngineeringCount);
    }

    [Fact]
    public async Task UpdateEmployee_AssignProjects_ShouldUpdateManyToMany()
    {
        // Arrange - Verify employee and projects exist
        var employeeExists = await context.Employees.AsNoTracking().AnyAsync(e => e.Id == 1);
        Assert.True(employeeExists);

        var project1Exists = await context.Projects.AsNoTracking().AnyAsync(p => p.Id == 1);
        var project2Exists = await context.Projects.AsNoTracking().AnyAsync(p => p.Id == 2);
        Assert.True(project1Exists);
        Assert.True(project2Exists);

        // Verify John has no projects initially
        var initialProjectCount = (await context.Employees
            .AsNoTracking()
            .Include(e => e.Projects)
            .FirstAsync(e => e.Id == 1))
            .Projects.Count;
        Assert.Equal(0, initialProjectCount);

        var dto = new UpdateEmployeeDto
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@company.com",
            Salary = 75000,
            HireDate = new DateTime(2020, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            DepartmentId = 1,
            ProjectIds = new List<int> { 1, 2 }
        };

        // Act
        var result = await exercises.UpdateEmployee(dto);

        // Assert - Check returned object
        Assert.Equal(2, result.Projects.Count);
        Assert.Contains(result.Projects, p => p.Id == 1);
        Assert.Contains(result.Projects, p => p.Id == 2);
        Assert.All(result.Projects, p => Assert.NotNull(p.Name)); // Verify navigation loaded

        // Assert - Verify DB state via fresh query
        var dbEmployee = await context.Employees
            .AsNoTracking()
            .Include(e => e.Projects).FirstAsync(e => e.Id == 1);
        Assert.Equal(2, dbEmployee.Projects.Count);
        Assert.Contains(dbEmployee.Projects, p => p.Id == 1 && p.Name == "Project Alpha");
        Assert.Contains(dbEmployee.Projects, p => p.Id == 2 && p.Name == "Project Beta");
    }

    [Fact]
    public async Task UpdateEmployee_RemoveAllProjects_ShouldClearManyToMany()
    {
        // Arrange - First assign projects
        var assignDto = new UpdateEmployeeDto
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@company.com",
            Salary = 75000,
            HireDate = new DateTime(2020, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            DepartmentId = 1,
            ProjectIds = new List<int> { 1, 2 }
        };
        await exercises.UpdateEmployee(assignDto);

        // Verify projects were assigned
        var employeeWithProjects = await context.Employees
            .AsNoTracking()
            .Include(e => e.Projects).FirstAsync(e => e.Id == 1);
        Assert.Equal(2, employeeWithProjects.Projects.Count);

        var removeDto = new UpdateEmployeeDto
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@company.com",
            Salary = 75000,
            HireDate = new DateTime(2020, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            DepartmentId = 1,
            ProjectIds = new List<int>() // Empty list = remove all
        };

        // Act
        var result = await exercises.UpdateEmployee(removeDto);

        // Assert - Check returned object
        Assert.Empty(result.Projects);

        // Assert - Verify DB state
        var dbEmployee = await context.Employees
            .AsNoTracking()
            .Include(e => e.Projects).FirstAsync(e => e.Id == 1);
        Assert.Empty(dbEmployee.Projects);

        // Verify projects still exist (not deleted, just unassigned)
        var project1StillExists = await context.Projects.AsNoTracking().AnyAsync(p => p.Id == 1);
        var project2StillExists = await context.Projects.AsNoTracking().AnyAsync(p => p.Id == 2);
        Assert.True(project1StillExists);
        Assert.True(project2StillExists);
    }

    [Fact]
    public async Task UpdateEmployee_Idempotent_ShouldProduceSameResultWhenCalledTwice()
    {
        // Arrange - Verify initial state
        var initialEmployee = await context.Employees.AsNoTracking().FirstAsync(e => e.Id == 1);
        Assert.Equal("John", initialEmployee.FirstName);
        Assert.Equal(75000, initialEmployee.Salary);
        Assert.Equal(1, initialEmployee.DepartmentId);

        var dto = new UpdateEmployeeDto
        {
            Id = 1,
            FirstName = "Jonathan",
            LastName = "Doe",
            Email = "john.doe@company.com",
            Salary = 85000,
            HireDate = new DateTime(2020, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            DepartmentId = 3,
            ProjectIds = new List<int> { 1, 3 }
        };

        // Act - Call twice
        var result1 = await exercises.UpdateEmployee(dto);

        // Verify first call worked
        var afterFirstCall = await context.Employees.AsNoTracking()
            .Include(e => e.Projects).FirstAsync(e => e.Id == 1);
        Assert.Equal("Jonathan", afterFirstCall.FirstName);
        Assert.Equal(85000, afterFirstCall.Salary);
        Assert.Equal(3, afterFirstCall.DepartmentId);
        Assert.Equal(2, afterFirstCall.Projects.Count);

        var result2 = await exercises.UpdateEmployee(dto);

        // Assert - Both calls should produce identical results
        Assert.Equal(result1.FirstName, result2.FirstName);
        Assert.Equal(result1.Salary, result2.Salary);
        Assert.Equal(result1.DepartmentId, result2.DepartmentId);
        Assert.Equal(result1.Projects.Count, result2.Projects.Count);
        Assert.All(result1.Projects, p => Assert.Contains(result2.Projects, p2 => p2.Id == p.Id));

        // Assert - DB state after second call is identical
        var afterSecondCall = await context.Employees.AsNoTracking()
            .Include(e => e.Projects).FirstAsync(e => e.Id == 1);
        Assert.Equal("Jonathan", afterSecondCall.FirstName);
        Assert.Equal(85000, afterSecondCall.Salary);
        Assert.Equal(3, afterSecondCall.DepartmentId);
        Assert.Equal(2, afterSecondCall.Projects.Count);
        Assert.Contains(afterSecondCall.Projects, p => p.Id == 1);
        Assert.Contains(afterSecondCall.Projects, p => p.Id == 3);
    }

    [Fact]
    public async Task UpdateEmployee_ReplaceProjects_ShouldUpdateManyToMany()
    {
        // Arrange - First assign some projects
        var initialDto = new UpdateEmployeeDto
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@company.com",
            Salary = 75000,
            HireDate = new DateTime(2020, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            DepartmentId = 1,
            ProjectIds = new List<int> { 1, 2 }
        };
        await exercises.UpdateEmployee(initialDto);

        // Verify initial assignment
        var afterInitial = await context.Employees
            .AsNoTracking()
            .Include(e => e.Projects).FirstAsync(e => e.Id == 1);
        Assert.Equal(2, afterInitial.Projects.Count);
        Assert.Contains(afterInitial.Projects, p => p.Id == 1);
        Assert.Contains(afterInitial.Projects, p => p.Id == 2);

        // Replace with different projects
        var replaceDto = new UpdateEmployeeDto
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@company.com",
            Salary = 75000,
            HireDate = new DateTime(2020, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            DepartmentId = 1,
            ProjectIds = new List<int> { 2, 3 } // Remove 1, keep 2, add 3
        };

        // Act
        var result = await exercises.UpdateEmployee(replaceDto);

        // Assert - Check returned object
        Assert.Equal(2, result.Projects.Count);
        Assert.DoesNotContain(result.Projects, p => p.Id == 1);
        Assert.Contains(result.Projects, p => p.Id == 2);
        Assert.Contains(result.Projects, p => p.Id == 3);

        // Assert - Verify DB state
        var dbEmployee = await context.Employees
            .AsNoTracking()
            .Include(e => e.Projects).FirstAsync(e => e.Id == 1);
        Assert.Equal(2, dbEmployee.Projects.Count);
        Assert.DoesNotContain(dbEmployee.Projects, p => p.Id == 1);
        Assert.Contains(dbEmployee.Projects, p => p.Id == 2 && p.Name == "Project Beta");
        Assert.Contains(dbEmployee.Projects, p => p.Id == 3 && p.Name == "Project Gamma");

        // Verify Project 1 still exists and wasn't deleted
        var project1 = await context.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == 1);
        Assert.NotNull(project1);
        Assert.Equal("Project Alpha", project1.Name);
    }

    [Fact]
    public async Task UpdateEmployee_NonExistentEmployee_ShouldThrow()
    {
        // Arrange - Verify employee doesn't exist
        var employeeExists = await context.Employees.AsNoTracking().AnyAsync(e => e.Id == 999);
        Assert.False(employeeExists);

        var dto = new UpdateEmployeeDto
        {
            Id = 999,
            FirstName = "NonExistent",
            LastName = "Person",
            Email = "nonexistent@company.com",
            Salary = 50000,
            HireDate = DateTime.UtcNow,
            DepartmentId = 1,
            ProjectIds = new List<int>()
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await exercises.UpdateEmployee(dto));

        // Verify no employee was created
        var stillDoesNotExist = await context.Employees.AsNoTracking().AnyAsync(e => e.Id == 999);
        Assert.False(stillDoesNotExist);
    }

    [Fact]
    public async Task UpdateEmployee_NonExistentDepartment_ShouldThrow()
    {
        // Arrange - Verify department doesn't exist
        var deptExists = await context.Departments.AsNoTracking().AnyAsync(d => d.Id == 999);
        Assert.False(deptExists);

        var originalEmployee = await context.Employees.AsNoTracking().FirstAsync(e => e.Id == 1);
        var originalDeptId = originalEmployee.DepartmentId;

        var dto = new UpdateEmployeeDto
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@company.com",
            Salary = 75000,
            HireDate = new DateTime(2020, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            DepartmentId = 999,
            ProjectIds = new List<int>()
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await exercises.UpdateEmployee(dto));

        // Verify employee department was not changed
        var unchangedEmployee = await context.Employees.AsNoTracking().FirstAsync(e => e.Id == 1);
        Assert.Equal(originalDeptId, unchangedEmployee.DepartmentId);
    }

    [Fact]
    public async Task UpdateEmployee_NonExistentProject_ShouldThrow()
    {
        // Arrange - Verify project doesn't exist
        var projectExists = await context.Projects.AsNoTracking().AnyAsync(p => p.Id == 999);
        Assert.False(projectExists);

        var originalEmployee = await context.Employees
            .AsNoTracking()
            .Include(e => e.Projects).FirstAsync(e => e.Id == 1);
        var originalProjectCount = originalEmployee.Projects.Count;

        var dto = new UpdateEmployeeDto
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@company.com",
            Salary = 75000,
            HireDate = new DateTime(2020, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            DepartmentId = 1,
            ProjectIds = new List<int> { -123, 99239 }
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await exercises.UpdateEmployee(dto));

        // Verify employee projects were not changed
        var unchangedEmployee = await context.Employees
            .AsNoTracking()
            .Include(e => e.Projects).FirstAsync(e => e.Id == 1);
        Assert.Equal(originalProjectCount, unchangedEmployee.Projects.Count);
    }

    [Fact]
    public async Task UpdateEmployee_ComplexUpdate_ShouldUpdateAllSpecifiedRelationships()
    {
        // Arrange - Verify initial state
        var originalEmployee = await context.Employees
            .AsNoTracking()
            .Include(e => e.Projects).FirstAsync(e => e.Id == 1);
        Assert.Equal("John", originalEmployee.FirstName);
        Assert.Equal("john.doe@company.com", originalEmployee.Email);
        Assert.Equal(75000, originalEmployee.Salary);
        Assert.Equal(1, originalEmployee.DepartmentId); // Engineering
        Assert.Equal(0, originalEmployee.Projects.Count);

        // Verify target department and projects exist
        var salesDept = await context.Departments.AsNoTracking().FirstAsync(d => d.Id == 3);
        Assert.Equal("Sales", salesDept.Name);

        var projectCount = await context.Projects.AsNoTracking().CountAsync(p => p.Id >= 1 && p.Id <= 3);
        Assert.Equal(3, projectCount);

        var dto = new UpdateEmployeeDto
        {
            Id = 1,
            FirstName = "Jonathan",
            LastName = "Doe",
            Email = "j.doe@newcompany.com",
            Salary = 100000,
            HireDate = new DateTime(2020, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            DepartmentId = 3, // Sales
            ProjectIds = new List<int> { 1, 2, 3 } // All three projects
        };

        // Act
        var result = await exercises.UpdateEmployee(dto);

        // Assert - Check returned object scalar properties
        Assert.Equal("Jonathan", result.FirstName);
        Assert.Equal("j.doe@newcompany.com", result.Email);
        Assert.Equal(100000, result.Salary);

        // Assert - Check returned object one-to-many
        Assert.Equal(3, result.DepartmentId);
        Assert.NotNull(result.Department);
        Assert.Equal("Sales", result.Department.Name);

        // Assert - Check returned object many-to-many
        Assert.Equal(3, result.Projects.Count);
        Assert.Contains(result.Projects, p => p.Id == 1);
        Assert.Contains(result.Projects, p => p.Id == 2);
        Assert.Contains(result.Projects, p => p.Id == 3);

        // Assert - Verify complete DB state
        var dbEmployee = await context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Projects).FirstAsync(e => e.Id == 1);

        Assert.Equal("Jonathan", dbEmployee.FirstName);
        Assert.Equal("j.doe@newcompany.com", dbEmployee.Email);
        Assert.Equal(100000, dbEmployee.Salary);
        Assert.Equal(3, dbEmployee.DepartmentId);
        Assert.Equal("Sales", dbEmployee.Department.Name);
        Assert.Equal(3, dbEmployee.Projects.Count);
        Assert.Contains(dbEmployee.Projects, p => p.Id == 1 && p.Name == "Project Alpha");
        Assert.Contains(dbEmployee.Projects, p => p.Id == 2 && p.Name == "Project Beta");
        Assert.Contains(dbEmployee.Projects, p => p.Id == 3 && p.Name == "Project Gamma");
    }
}
