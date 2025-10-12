using Microsoft.EntityFrameworkCore;
using tests.Data;
using tests.DTOs;
using tests.Interfaces;

namespace tests.Tests.EfExercisesIdempotentUpdatesTests;

public class UpdateEmployeeTests(CompanyDbContext context, IEfExercisesIdempotentUpdates exercises)
{
    [Fact]
    public void UpdateEmployee_ScalarPropertiesOnly_ShouldUpdateCorrectly()
    {
        // Arrange - Verify John exists with original values
        var originalEmployee = context.Employees.AsNoTracking().First(e => e.Id == 1);
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
            Salary = 80000
        };

        // Act
        var result = exercises.UpdateEmployee(dto);

        // Assert - Check returned object
        Assert.Equal("Jonathan", result.FirstName);
        Assert.Equal("Doe-Smith", result.LastName);
        Assert.Equal("jonathan.doesmith@company.com", result.Email);
        Assert.Equal(80000, result.Salary);
        Assert.Equal(1, result.DepartmentId); // Should remain unchanged

        // Assert - Verify DB state
        var dbEmployee = context.Employees.AsNoTracking().First(e => e.Id == 1);
        Assert.Equal("Jonathan", dbEmployee.FirstName);
        Assert.Equal("Doe-Smith", dbEmployee.LastName);
        Assert.Equal("jonathan.doesmith@company.com", dbEmployee.Email);
        Assert.Equal(80000, dbEmployee.Salary);
        Assert.Equal(1, dbEmployee.DepartmentId);

        // Verify HireDate was NOT changed (not in DTO)
        Assert.Equal(originalEmployee.HireDate, dbEmployee.HireDate);
    }

    [Fact]
    public void UpdateEmployee_TransferDepartment_ShouldUpdateOneToMany()
    {
        // Arrange - Verify initial state
        var originalEmployee = context.Employees.AsNoTracking().First(e => e.Id == 1);
        Assert.Equal(1, originalEmployee.DepartmentId); // Engineering

        var marketingDept = context.Departments.AsNoTracking().First(d => d.Id == 2);
        Assert.Equal("Marketing", marketingDept.Name);

        var originalMarketingCount = context.Employees.AsNoTracking().Count(e => e.DepartmentId == 2);
        var originalEngineeringCount = context.Employees.AsNoTracking().Count(e => e.DepartmentId == 1);

        var dto = new UpdateEmployeeDto
        {
            Id = 1,
            DepartmentId = 2
        };

        // Act
        var result = exercises.UpdateEmployee(dto);

        // Assert - Check returned object
        Assert.Equal(2, result.DepartmentId);
        Assert.NotNull(result.Department);
        Assert.Equal("Marketing", result.Department.Name);

        // Assert - Verify DB state
        var dbEmployee = context.Employees.AsNoTracking().First(e => e.Id == 1);
        Assert.Equal(2, dbEmployee.DepartmentId);

        // Verify department employee counts changed correctly
        var newMarketingCount = context.Employees.AsNoTracking().Count(e => e.DepartmentId == 2);
        var newEngineeringCount = context.Employees.AsNoTracking().Count(e => e.DepartmentId == 1);
        Assert.Equal(originalMarketingCount + 1, newMarketingCount);
        Assert.Equal(originalEngineeringCount - 1, newEngineeringCount);
    }

    [Fact]
    public void UpdateEmployee_AssignProjects_ShouldUpdateManyToMany()
    {
        // Arrange - Verify employee and projects exist
        var employeeExists = context.Employees.AsNoTracking().Any(e => e.Id == 1);
        Assert.True(employeeExists);

        var project1Exists = context.Projects.AsNoTracking().Any(p => p.Id == 1);
        var project2Exists = context.Projects.AsNoTracking().Any(p => p.Id == 2);
        Assert.True(project1Exists);
        Assert.True(project2Exists);

        // Verify John has no projects initially
        var initialProjectCount = context.Employees
            .AsNoTracking()
            .Include(e => e.Projects)
            .First(e => e.Id == 1)
            .Projects.Count;
        Assert.Equal(0, initialProjectCount);

        var dto = new UpdateEmployeeDto
        {
            Id = 1,
            ProjectIds = new List<int> { 1, 2 }
        };

        // Act
        var result = exercises.UpdateEmployee(dto);

        // Assert - Check returned object
        Assert.Equal(2, result.Projects.Count);
        Assert.Contains(result.Projects, p => p.Id == 1);
        Assert.Contains(result.Projects, p => p.Id == 2);
        Assert.All(result.Projects, p => Assert.NotNull(p.Name)); // Verify navigation loaded

        // Assert - Verify DB state via fresh query
        var dbEmployee = context.Employees
            .AsNoTracking()
            .Include(e => e.Projects)
            .First(e => e.Id == 1);
        Assert.Equal(2, dbEmployee.Projects.Count);
        Assert.Contains(dbEmployee.Projects, p => p.Id == 1 && p.Name == "Project Alpha");
        Assert.Contains(dbEmployee.Projects, p => p.Id == 2 && p.Name == "Project Beta");
    }

    [Fact]
    public void UpdateEmployee_RemoveAllProjects_ShouldClearManyToMany()
    {
        // Arrange - First assign projects
        var assignDto = new UpdateEmployeeDto
        {
            Id = 1,
            ProjectIds = new List<int> { 1, 2 }
        };
        exercises.UpdateEmployee(assignDto);

        // Verify projects were assigned
        var employeeWithProjects = context.Employees
            .AsNoTracking()
            .Include(e => e.Projects)
            .First(e => e.Id == 1);
        Assert.Equal(2, employeeWithProjects.Projects.Count);

        var removeDto = new UpdateEmployeeDto
        {
            Id = 1,
            ProjectIds = new List<int>() // Empty list = remove all
        };

        // Act
        var result = exercises.UpdateEmployee(removeDto);

        // Assert - Check returned object
        Assert.Empty(result.Projects);

        // Assert - Verify DB state
        var dbEmployee = context.Employees
            .AsNoTracking()
            .Include(e => e.Projects)
            .First(e => e.Id == 1);
        Assert.Empty(dbEmployee.Projects);

        // Verify projects still exist (not deleted, just unassigned)
        var project1StillExists = context.Projects.AsNoTracking().Any(p => p.Id == 1);
        var project2StillExists = context.Projects.AsNoTracking().Any(p => p.Id == 2);
        Assert.True(project1StillExists);
        Assert.True(project2StillExists);
    }

    [Fact]
    public void UpdateEmployee_PartialUpdate_ShouldOnlyUpdateSpecifiedFields()
    {
        // Arrange - Get original state
        var originalEmployee = context.Employees.AsNoTracking().First(e => e.Id == 1);
        Assert.Equal("John", originalEmployee.FirstName);
        Assert.Equal("Doe", originalEmployee.LastName);
        Assert.Equal(75000, originalEmployee.Salary);
        Assert.Equal("john.doe@company.com", originalEmployee.Email);

        var dto = new UpdateEmployeeDto
        {
            Id = 1,
            Salary = 95000 // Only update salary
        };

        // Act
        var result = exercises.UpdateEmployee(dto);

        // Assert - Salary changed
        Assert.Equal(95000, result.Salary);

        // Assert - Everything else unchanged
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Doe", result.LastName);
        Assert.Equal(originalEmployee.Email, result.Email);
        Assert.Equal(originalEmployee.HireDate, result.HireDate);

        // Assert - Verify DB state
        var dbEmployee = context.Employees.AsNoTracking().First(e => e.Id == 1);
        Assert.Equal(95000, dbEmployee.Salary);
        Assert.Equal("John", dbEmployee.FirstName);
        Assert.Equal("Doe", dbEmployee.LastName);
        Assert.Equal(originalEmployee.Email, dbEmployee.Email);
        Assert.Equal(originalEmployee.HireDate, dbEmployee.HireDate);
    }

    [Fact]
    public void UpdateEmployee_Idempotent_ShouldProduceSameResultWhenCalledTwice()
    {
        // Arrange - Verify initial state
        var initialEmployee = context.Employees.AsNoTracking().First(e => e.Id == 1);
        Assert.Equal("John", initialEmployee.FirstName);
        Assert.Equal(75000, initialEmployee.Salary);
        Assert.Equal(1, initialEmployee.DepartmentId);

        var dto = new UpdateEmployeeDto
        {
            Id = 1,
            FirstName = "Jonathan",
            Salary = 85000,
            DepartmentId = 3,
            ProjectIds = new List<int> { 1, 3 }
        };

        // Act - Call twice
        var result1 = exercises.UpdateEmployee(dto);

        // Verify first call worked
        var afterFirstCall = context.Employees.AsNoTracking()
            .Include(e => e.Projects)
            .First(e => e.Id == 1);
        Assert.Equal("Jonathan", afterFirstCall.FirstName);
        Assert.Equal(85000, afterFirstCall.Salary);
        Assert.Equal(3, afterFirstCall.DepartmentId);
        Assert.Equal(2, afterFirstCall.Projects.Count);

        var result2 = exercises.UpdateEmployee(dto);

        // Assert - Both calls should produce identical results
        Assert.Equal(result1.FirstName, result2.FirstName);
        Assert.Equal(result1.Salary, result2.Salary);
        Assert.Equal(result1.DepartmentId, result2.DepartmentId);
        Assert.Equal(result1.Projects.Count, result2.Projects.Count);
        Assert.All(result1.Projects, p => Assert.Contains(result2.Projects, p2 => p2.Id == p.Id));

        // Assert - DB state after second call is identical
        var afterSecondCall = context.Employees.AsNoTracking()
            .Include(e => e.Projects)
            .First(e => e.Id == 1);
        Assert.Equal("Jonathan", afterSecondCall.FirstName);
        Assert.Equal(85000, afterSecondCall.Salary);
        Assert.Equal(3, afterSecondCall.DepartmentId);
        Assert.Equal(2, afterSecondCall.Projects.Count);
        Assert.Contains(afterSecondCall.Projects, p => p.Id == 1);
        Assert.Contains(afterSecondCall.Projects, p => p.Id == 3);
    }

    [Fact]
    public void UpdateEmployee_ReplaceProjects_ShouldUpdateManyToMany()
    {
        // Arrange - First assign some projects
        var initialDto = new UpdateEmployeeDto
        {
            Id = 1,
            ProjectIds = new List<int> { 1, 2 }
        };
        exercises.UpdateEmployee(initialDto);

        // Verify initial assignment
        var afterInitial = context.Employees
            .AsNoTracking()
            .Include(e => e.Projects)
            .First(e => e.Id == 1);
        Assert.Equal(2, afterInitial.Projects.Count);
        Assert.Contains(afterInitial.Projects, p => p.Id == 1);
        Assert.Contains(afterInitial.Projects, p => p.Id == 2);

        // Replace with different projects
        var replaceDto = new UpdateEmployeeDto
        {
            Id = 1,
            ProjectIds = new List<int> { 2, 3 } // Remove 1, keep 2, add 3
        };

        // Act
        var result = exercises.UpdateEmployee(replaceDto);

        // Assert - Check returned object
        Assert.Equal(2, result.Projects.Count);
        Assert.DoesNotContain(result.Projects, p => p.Id == 1);
        Assert.Contains(result.Projects, p => p.Id == 2);
        Assert.Contains(result.Projects, p => p.Id == 3);

        // Assert - Verify DB state
        var dbEmployee = context.Employees
            .AsNoTracking()
            .Include(e => e.Projects)
            .First(e => e.Id == 1);
        Assert.Equal(2, dbEmployee.Projects.Count);
        Assert.DoesNotContain(dbEmployee.Projects, p => p.Id == 1);
        Assert.Contains(dbEmployee.Projects, p => p.Id == 2 && p.Name == "Project Beta");
        Assert.Contains(dbEmployee.Projects, p => p.Id == 3 && p.Name == "Project Gamma");

        // Verify Project 1 still exists and wasn't deleted
        var project1 = context.Projects.AsNoTracking().FirstOrDefault(p => p.Id == 1);
        Assert.NotNull(project1);
        Assert.Equal("Project Alpha", project1.Name);
    }

    [Fact]
    public void UpdateEmployee_NonExistentEmployee_ShouldThrow()
    {
        // Arrange - Verify employee doesn't exist
        var employeeExists = context.Employees.AsNoTracking().Any(e => e.Id == 999);
        Assert.False(employeeExists);

        var dto = new UpdateEmployeeDto
        {
            Id = 999,
            FirstName = "NonExistent"
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => exercises.UpdateEmployee(dto));

        // Verify no employee was created
        var stillDoesNotExist = context.Employees.AsNoTracking().Any(e => e.Id == 999);
        Assert.False(stillDoesNotExist);
    }

    [Fact]
    public void UpdateEmployee_NonExistentDepartment_ShouldThrow()
    {
        // Arrange - Verify department doesn't exist
        var deptExists = context.Departments.AsNoTracking().Any(d => d.Id == 999);
        Assert.False(deptExists);

        var originalEmployee = context.Employees.AsNoTracking().First(e => e.Id == 1);
        var originalDeptId = originalEmployee.DepartmentId;

        var dto = new UpdateEmployeeDto
        {
            Id = 1,
            DepartmentId = 999
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => exercises.UpdateEmployee(dto));

        // Verify employee department was not changed
        var unchangedEmployee = context.Employees.AsNoTracking().First(e => e.Id == 1);
        Assert.Equal(originalDeptId, unchangedEmployee.DepartmentId);
    }

    [Fact]
    public void UpdateEmployee_NonExistentProject_ShouldThrow()
    {
        // Arrange - Verify project doesn't exist
        var projectExists = context.Projects.AsNoTracking().Any(p => p.Id == 999);
        Assert.False(projectExists);

        var originalEmployee = context.Employees
            .AsNoTracking()
            .Include(e => e.Projects)
            .First(e => e.Id == 1);
        var originalProjectCount = originalEmployee.Projects.Count;

        var dto = new UpdateEmployeeDto
        {
            Id = 1,
            ProjectIds = new List<int> { -123, 99239 }
        };

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => exercises.UpdateEmployee(dto));

        // Verify employee projects were not changed
        var unchangedEmployee = context.Employees
            .AsNoTracking()
            .Include(e => e.Projects)
            .First(e => e.Id == 1);
        Assert.Equal(originalProjectCount, unchangedEmployee.Projects.Count);
    }

    [Fact]
    public void UpdateEmployee_ComplexUpdate_ShouldUpdateAllSpecifiedRelationships()
    {
        // Arrange - Verify initial state
        var originalEmployee = context.Employees
            .AsNoTracking()
            .Include(e => e.Projects)
            .First(e => e.Id == 1);
        Assert.Equal("John", originalEmployee.FirstName);
        Assert.Equal("john.doe@company.com", originalEmployee.Email);
        Assert.Equal(75000, originalEmployee.Salary);
        Assert.Equal(1, originalEmployee.DepartmentId); // Engineering
        Assert.Equal(0, originalEmployee.Projects.Count);

        // Verify target department and projects exist
        var salesDept = context.Departments.AsNoTracking().First(d => d.Id == 3);
        Assert.Equal("Sales", salesDept.Name);

        var projectCount = context.Projects.AsNoTracking().Count(p => p.Id >= 1 && p.Id <= 3);
        Assert.Equal(3, projectCount);

        var dto = new UpdateEmployeeDto
        {
            Id = 1,
            FirstName = "Jonathan",
            Email = "j.doe@newcompany.com",
            Salary = 100000,
            DepartmentId = 3, // Sales
            ProjectIds = new List<int> { 1, 2, 3 } // All three projects
        };

        // Act
        var result = exercises.UpdateEmployee(dto);

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
        var dbEmployee = context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Projects)
            .First(e => e.Id == 1);

        Assert.Equal("Jonathan", dbEmployee.FirstName);
        Assert.Equal("j.doe@newcompany.com", dbEmployee.Email);
        Assert.Equal(100000, dbEmployee.Salary);
        Assert.Equal(3, dbEmployee.DepartmentId);
        Assert.Equal("Sales", dbEmployee.Department.Name);
        Assert.Equal(3, dbEmployee.Projects.Count);
        Assert.Contains(dbEmployee.Projects, p => p.Id == 1 && p.Name == "Project Alpha");
        Assert.Contains(dbEmployee.Projects, p => p.Id == 2 && p.Name == "Project Beta");
        Assert.Contains(dbEmployee.Projects, p => p.Id == 3 && p.Name == "Project Gamma");

        // Verify LastName was NOT changed (not in DTO)
        Assert.Equal(originalEmployee.LastName, dbEmployee.LastName);
        Assert.Equal(originalEmployee.HireDate, dbEmployee.HireDate);
    }
}
