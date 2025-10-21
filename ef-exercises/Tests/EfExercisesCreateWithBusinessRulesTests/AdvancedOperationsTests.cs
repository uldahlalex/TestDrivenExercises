using Microsoft.EntityFrameworkCore;
using tests.Data;
using tests.DTOs;
using tests.Interfaces;

namespace tests.Tests.EfExercisesCreateWithBusinessRulesTests;

public class AdvancedOperationsTests(CompanyDbContext context, IEfExercisesCreateWithBusinessRules exercises)
{
    [Fact]
    public async Task PromoteEmployee_Valid_ShouldIncreaseSalary()
    {
        // Arrange - Employee hired more than 6 months ago with projects
        var employee = await context.Employees
            .Include(e => e.Projects)
            .FirstAsync(e => e.Id == 1);

        // Ensure employee has been employed for 6+ months
        employee.HireDate = DateTime.UtcNow.AddMonths(-7);

        // Ensure employee has at least one project
        if (!employee.Projects.Any())
        {
            var project = await context.Projects.FirstAsync(p => p.Id == 1);
            employee.Projects.Add(project);
        }

        await context.SaveChangesAsync();

        var originalSalary = employee.Salary;
        var newSalary = originalSalary * 1.15; // 15% increase (within 10-30%)

        // Act
        var result = await exercises.PromoteEmployee(1, newSalary);

        // Assert
        Assert.Equal(newSalary, result.Salary);

        var dbEmployee = await context.Employees.AsNoTracking().FirstAsync(e => e.Id == 1);
        Assert.Equal(newSalary, dbEmployee.Salary);
    }

    [Fact]
    public async Task PromoteEmployee_EmployedLessThan6Months_ShouldThrow()
    {
        // Arrange - New employee
        var employee = await context.Employees.FirstAsync(e => e.Id == 1);
        employee.HireDate = DateTime.UtcNow.AddMonths(-3); // Only 3 months
        await context.SaveChangesAsync();

        var newSalary = employee.Salary * 1.2;

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.PromoteEmployee(1, newSalary));
    }

    [Fact]
    public async Task PromoteEmployee_IncreaseLessThan10Percent_ShouldThrow()
    {
        // Arrange
        var employee = await context.Employees.FirstAsync(e => e.Id == 1);
        employee.HireDate = DateTime.UtcNow.AddMonths(-7);
        await context.SaveChangesAsync();

        var newSalary = employee.Salary * 1.05; // Only 5% increase

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.PromoteEmployee(1, newSalary));
    }

    [Fact]
    public async Task PromoteEmployee_IncreaseMoreThan30Percent_ShouldThrow()
    {
        // Arrange
        var employee = await context.Employees.FirstAsync(e => e.Id == 1);
        employee.HireDate = DateTime.UtcNow.AddMonths(-7);
        await context.SaveChangesAsync();

        var newSalary = employee.Salary * 1.35; // 35% increase

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.PromoteEmployee(1, newSalary));
    }

    [Fact]
    public async Task PromoteEmployee_NoProjects_ShouldThrow()
    {
        // Arrange
        var employee = await context.Employees
            .Include(e => e.Projects)
            .FirstAsync(e => e.Id == 1);

        employee.HireDate = DateTime.UtcNow.AddMonths(-7);
        employee.Projects.Clear();
        await context.SaveChangesAsync();

        var newSalary = employee.Salary * 1.2;

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.PromoteEmployee(1, newSalary));
    }

    [Fact]
    public async Task CloseProject_Valid_ShouldSoftDeleteAndUnassignEmployees()
    {
        // Arrange - Create project that started > 30 days ago
        var project = await context.Projects
            .Include(p => p.Employees)
            .FirstAsync(p => p.Id == 1);

        project.StartDate = DateTime.UtcNow.AddDays(-35);

        var employee1 = await context.Employees.FirstAsync(e => e.Id == 1);
        var employee2 = await context.Employees.FirstAsync(e => e.Id == 2);
        project.Employees.Add(employee1);
        project.Employees.Add(employee2);
        await context.SaveChangesAsync();

        var employeeCount = project.Employees.Count;
        Assert.True(employeeCount >= 2);

        // Act
        var result = await exercises.CloseProject(1);

        // Assert
        Assert.True(result.IsDeleted);
        Assert.NotNull(result.EndDate);
        Assert.Empty(result.Employees);

        var dbProject = await context.Projects
            .IgnoreQueryFilters()
            .Include(p => p.Employees)
            .AsNoTracking()
            .FirstAsync(p => p.Id == 1);
        Assert.True(dbProject.IsDeleted);
        Assert.Empty(dbProject.Employees);
    }

    [Fact]
    public async Task CloseProject_StartedLessThan30DaysAgo_ShouldThrow()
    {
        // Arrange
        var project = await context.Projects.FirstAsync(p => p.Id == 1);
        project.StartDate = DateTime.UtcNow.AddDays(-20); // Only 20 days
        await context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.CloseProject(1));
    }

    [Fact]
    public async Task TransferEmployeeToDepartment_Valid_ShouldTransfer()
    {
        // Arrange
        var employee = await context.Employees
            .Include(e => e.Department)
            .FirstAsync(e => e.Id == 1);

        var originalDeptId = employee.DepartmentId;

        // Target department with capacity
        var targetDept = await context.Departments
            .Include(d => d.Employees)
            .FirstAsync(d => d.Id == 2);

        // Ensure target has capacity (< 5 employees)
        while (targetDept.Employees.Count >= 5)
        {
            var empToRemove = targetDept.Employees.First();
            targetDept.Employees.Remove(empToRemove);
            await context.SaveChangesAsync();
        }

        // Ensure salary fits budget constraint (< 30% of dept budget)
        employee.Salary = targetDept.Budget * 0.2; // 20%
        await context.SaveChangesAsync();

        // Act
        var result = await exercises.TransferEmployeeToDepartment(1, 2);

        // Assert
        Assert.Equal(2, result.DepartmentId);

        var dbEmployee = await context.Employees.AsNoTracking().FirstAsync(e => e.Id == 1);
        Assert.Equal(2, dbEmployee.DepartmentId);
    }

    [Fact]
    public async Task TransferEmployeeToDepartment_TargetAtCapacity_ShouldThrow()
    {
        // Arrange - Fill target department to capacity
        var targetDept = await context.Departments
            .Include(d => d.Employees)
            .FirstAsync(d => d.Id == 2);

        // Add employees until capacity
        while (targetDept.Employees.Count < 5)
        {
            var newEmp = new Entities.Employee
            {
                FirstName = "Filler",
                LastName = $"Employee{targetDept.Employees.Count}",
                Email = $"filler{Guid.NewGuid()}@company.com",
                Salary = 50000,
                HireDate = DateTime.UtcNow,
                DepartmentId = targetDept.Id
            };
            context.Employees.Add(newEmp);
            await context.SaveChangesAsync();
        }

        // Refresh
        targetDept = await context.Departments
            .Include(d => d.Employees)
            .FirstAsync(d => d.Id == 2);
        Assert.Equal(5, targetDept.Employees.Count);

        // Act & Assert - Try to transfer employee to full department
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.TransferEmployeeToDepartment(1, 2));
    }

    [Fact]
    public async Task CreateDepartmentWithEmployees_Valid_ShouldCreateBoth()
    {
        // Arrange
        var dto = new CreateDepartmentWithEmployeesDto
        {
            Name = "New Startup Department",
            Location = "Building Y",
            Budget = 500000,
            Employees = new List<CreateEmployeeDto>
            {
                new CreateEmployeeDto
                {
                    FirstName = "Manager",
                    LastName = "Person",
                    Email = "manager@company.com",
                    Salary = 100000, // >= $75,000 manager requirement
                    HireDate = DateTime.UtcNow,
                    ProjectIds = new List<int>()
                },
                new CreateEmployeeDto
                {
                    FirstName = "Worker",
                    LastName = "Bee",
                    Email = "worker@company.com",
                    Salary = 60000,
                    HireDate = DateTime.UtcNow,
                    ProjectIds = new List<int>()
                }
            }
        };

        var initialDeptCount = await context.Departments.CountAsync();
        var initialEmpCount = await context.Employees.CountAsync();

        // Act
        var result = await exercises.CreateDepartmentWithEmployees(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Employees.Count);

        var newDeptCount = await context.Departments.CountAsync();
        var newEmpCount = await context.Employees.CountAsync();

        Assert.Equal(initialDeptCount + 1, newDeptCount);
        Assert.Equal(initialEmpCount + 2, newEmpCount);
    }

    [Fact]
    public async Task CreateDepartmentWithEmployees_NoManager_ShouldThrow()
    {
        // Arrange - No employee with salary >= $75,000
        var dto = new CreateDepartmentWithEmployeesDto
        {
            Name = "No Manager Department",
            Location = "Building Y",
            Budget = 500000,
            Employees = new List<CreateEmployeeDto>
            {
                new CreateEmployeeDto
                {
                    FirstName = "Worker1",
                    LastName = "Bee",
                    Email = "worker1@company.com",
                    Salary = 60000, // Less than $75,000
                    HireDate = DateTime.UtcNow,
                    ProjectIds = new List<int>()
                },
                new CreateEmployeeDto
                {
                    FirstName = "Worker2",
                    LastName = "Bee",
                    Email = "worker2@company.com",
                    Salary = 65000, // Less than $75,000
                    HireDate = DateTime.UtcNow,
                    ProjectIds = new List<int>()
                }
            }
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.CreateDepartmentWithEmployees(dto));
    }

    [Fact]
    public async Task CreateDepartmentWithEmployees_SalariesExceed80PercentBudget_ShouldThrow()
    {
        // Arrange
        var dto = new CreateDepartmentWithEmployeesDto
        {
            Name = "Overpaid Department",
            Location = "Building Y",
            Budget = 200000,
            Employees = new List<CreateEmployeeDto>
            {
                new CreateEmployeeDto
                {
                    FirstName = "Expensive",
                    LastName = "Employee1",
                    Email = "expensive1@company.com",
                    Salary = 100000,
                    HireDate = DateTime.UtcNow,
                    ProjectIds = new List<int>()
                },
                new CreateEmployeeDto
                {
                    FirstName = "Expensive",
                    LastName = "Employee2",
                    Email = "expensive2@company.com",
                    Salary = 70000, // Total = 170,000 = 85% of 200,000
                    HireDate = DateTime.UtcNow,
                    ProjectIds = new List<int>()
                }
            }
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await exercises.CreateDepartmentWithEmployees(dto));
    }
}
