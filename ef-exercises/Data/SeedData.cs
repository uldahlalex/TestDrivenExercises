using tests.Entities;

namespace tests.Data;

public static class SeedData
{
    public static async Task SeedAsync(CompanyDbContext context)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Clear existing data
        if (context.Employees.Any())
        {
            context.Employees.RemoveRange(context.Employees);
            context.Departments.RemoveRange(context.Departments);
            context.Projects.RemoveRange(context.Projects);
            await context.SaveChangesAsync();
        }

        // Seed Departments
        var departments = new List<Department>
        {
            new Department { Id = 1, Name = "Engineering", Location = "Building A", Budget = 500000 },
            new Department { Id = 2, Name = "Marketing", Location = "Building B", Budget = 200000 },
            new Department { Id = 3, Name = "Sales", Location = "Building C", Budget = 300000 },
            new Department { Id = 4, Name = "HR", Location = "Building D", Budget = 150000 }
        };

        context.Departments.AddRange(departments);
        await context.SaveChangesAsync();

        // Seed Employees
        var employees = new List<Employee>
        {
            new Employee { Id = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@company.com", Salary = 75000, HireDate = new DateTime(2020, 1, 15, 0, 0, 0, DateTimeKind.Utc), DepartmentId = 1 },
            new Employee { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane.smith@company.com", Salary = 85000, HireDate = new DateTime(2019, 3, 20, 0, 0, 0, DateTimeKind.Utc), DepartmentId = 1 },
            new Employee { Id = 3, FirstName = "Bob", LastName = "Johnson", Email = "bob.johnson@company.com", Salary = 65000, HireDate = new DateTime(2021, 5, 10, 0, 0, 0, DateTimeKind.Utc), DepartmentId = 2 },
            new Employee { Id = 4, FirstName = "Alice", LastName = "Brown", Email = "alice.brown@company.com", Salary = 70000, HireDate = new DateTime(2020, 8, 25, 0, 0, 0, DateTimeKind.Utc), DepartmentId = 2 },
            new Employee { Id = 5, FirstName = "Charlie", LastName = "Davis", Email = "charlie.davis@company.com", Salary = 60000, HireDate = new DateTime(2022, 2, 14, 0, 0, 0, DateTimeKind.Utc), DepartmentId = 3 },
            new Employee { Id = 6, FirstName = "Diana", LastName = "Wilson", Email = "diana.wilson@company.com", Salary = 80000, HireDate = new DateTime(2019, 11, 30, 0, 0, 0, DateTimeKind.Utc), DepartmentId = 3 },
            new Employee { Id = 7, FirstName = "Eve", LastName = "Miller", Email = "eve.miller@company.com", Salary = 55000, HireDate = new DateTime(2021, 7, 18, 0, 0, 0, DateTimeKind.Utc), DepartmentId = 4 },
            new Employee { Id = 8, FirstName = "Frank", LastName = "Garcia", Email = "frank.garcia@company.com", Salary = 90000, HireDate = new DateTime(2018, 6, 12, 0, 0, 0, DateTimeKind.Utc), DepartmentId = 1 }
        };

        context.Employees.AddRange(employees);
        await context.SaveChangesAsync();

        // Seed Projects
        var projects = new List<Project>
        {
            new Project { Id = 1, Name = "Project Alpha", Description = "First major project", StartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc), Budget = 100000 },
            new Project { Id = 2, Name = "Project Beta", Description = "Second major project", StartDate = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc), Budget = 150000 },
            new Project { Id = 3, Name = "Project Gamma", Description = "Third major project", StartDate = new DateTime(2023, 9, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2023, 12, 31, 0, 0, 0, DateTimeKind.Utc), Budget = 75000 }
        };

        context.Projects.AddRange(projects);
        await context.SaveChangesAsync();
    }
}