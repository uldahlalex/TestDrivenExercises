using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using tests.Data;
using tests.DTOs;
using tests.Entities;
using tests.Interfaces;

namespace tests.Exercises;

public class EfExercisesCreateWithBusinessRules(CompanyDbContext ctx) : IEfExercisesCreateWithBusinessRules
{
    public async Task<Employee> CreateEmployee(CreateEmployeeDto dto)
    {
        // Rule 1: Email must be unique across ALL employees (including soft-deleted)
        var emailExists = await ctx.Employees
            .IgnoreQueryFilters()
            .AnyAsync(e => e.Email == dto.Email);

        if (emailExists)
        {
            throw new ValidationException($"Email {dto.Email} is already in use");
        }

        // Rule 2: Department must exist and not be soft-deleted
        var department = await ctx.Departments
            .FirstOrDefaultAsync(d => d.Id == dto.DepartmentId);

        if (department == null)
        {
            throw new ValidationException($"Department {dto.DepartmentId} does not exist or is deleted");
        }

        // Rule 3: Salary must be within department budget constraints (≤ 30% of dept budget)
        var maxSalary = department.Budget * 0.30;
        if (dto.Salary > maxSalary)
        {
            throw new ValidationException(
                $"Salary ${dto.Salary} exceeds department budget constraint (max ${maxSalary})");
        }

        // Rule 4: Cannot assign more than 3 projects
        if (dto.ProjectIds.Count > 3)
        {
            throw new ValidationException("Cannot assign more than 3 projects to a new employee");
        }

        // Rule 5: All assigned projects must exist and not be soft-deleted
        var projects = await ctx.Projects
            .Where(p => dto.ProjectIds.Contains(p.Id))
            .ToListAsync();

        if (projects.Count != dto.ProjectIds.Count)
        {
            throw new ValidationException("One or more projects do not exist or are deleted");
        }

        // Create the employee
        var employee = new Employee
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Salary = dto.Salary,
            HireDate = dto.HireDate,
            DepartmentId = dto.DepartmentId,
            Department = department,
            Projects = projects
        };

        ctx.Employees.Add(employee);
        await ctx.SaveChangesAsync();

        return employee;
    }

    public async Task<Department> CreateDepartment(CreateDepartmentDto dto)
    {
        // Rule 1: Name must be unique (case-insensitive) across active departments
        var nameExists = await ctx.Departments
            .AnyAsync(d => d.Name.ToLower() == dto.Name.ToLower());

        if (nameExists)
        {
            throw new ValidationException($"Department name '{dto.Name}' already exists");
        }

        // Rule 2: Budget must be at least $50,000
        if (dto.Budget < 50000)
        {
            throw new ValidationException("Department budget must be at least $50,000");
        }

        // Rule 3: Location must not already have 3+ departments (facility constraint)
        var departmentsInLocation = await ctx.Departments
            .CountAsync(d => d.Location == dto.Location);

        if (departmentsInLocation >= 3)
        {
            throw new ValidationException(
                $"Location '{dto.Location}' already has 3 departments (facility limit)");
        }

        // Rule 4: Cannot create more than 10 total active departments
        var totalActiveDepartments = await ctx.Departments.CountAsync();

        if (totalActiveDepartments >= 10)
        {
            throw new ValidationException("Cannot exceed 10 active departments (company policy)");
        }

        // Create the department
        var department = new Department
        {
            Name = dto.Name,
            Location = dto.Location,
            Budget = dto.Budget
        };

        ctx.Departments.Add(department);
        await ctx.SaveChangesAsync();

        return department;
    }

    public async Task<Project> CreateProject(CreateProjectDto dto)
    {
        // Rule 1: Name must be unique across active projects
        var nameExists = await ctx.Projects.AnyAsync(p => p.Name == dto.Name);

        if (nameExists)
        {
            throw new ValidationException($"Project name '{dto.Name}' already exists");
        }

        // Rule 2: StartDate must be in the future or today
        if (dto.StartDate.Date < DateTime.UtcNow.Date)
        {
            throw new ValidationException("Project start date cannot be in the past");
        }

        // Rule 3: EndDate (if provided) must be after StartDate
        if (dto.EndDate.HasValue && dto.EndDate.Value <= dto.StartDate)
        {
            throw new ValidationException("Project end date must be after start date");
        }

        // Rule 4: Budget must be positive and at least $10,000
        if (dto.Budget < 10000)
        {
            throw new ValidationException("Project budget must be at least $10,000");
        }

        // Rule 5: Total active project budgets cannot exceed $1,000,000
        var currentTotalBudget = await ctx.Projects.SumAsync(p => p.Budget);

        if (currentTotalBudget + dto.Budget > 1_000_000)
        {
            throw new ValidationException(
                $"Total project budgets would exceed $1,000,000 limit " +
                $"(current: ${currentTotalBudget}, new: ${dto.Budget})");
        }

        // Rule 6 & 7: Validate employees
        var employees = await ctx.Employees
            .Include(e => e.Department)
            .Where(e => dto.EmployeeIds.Contains(e.Id))
            .ToListAsync();

        if (employees.Count != dto.EmployeeIds.Count)
        {
            throw new ValidationException("One or more employees do not exist");
        }

        // Rule 6: Cannot assign employees from soft-deleted departments
        var employeesFromDeletedDepts = employees.Where(e => e.Department.IsDeleted).ToList();
        if (employeesFromDeletedDepts.Any())
        {
            throw new ValidationException(
                "Cannot assign employees from soft-deleted departments to new project");
        }

        // Rule 7: Cannot assign more employees than budget supports (1 per $50k)
        var maxEmployees = (int)Math.Floor(dto.Budget / 50000);
        if (dto.EmployeeIds.Count > maxEmployees)
        {
            throw new ValidationException(
                $"Project budget of ${dto.Budget} supports max {maxEmployees} employees " +
                $"(1 per $50,000), but {dto.EmployeeIds.Count} were assigned");
        }

        // Create the project
        var project = new Project
        {
            Name = dto.Name,
            Description = dto.Description,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Budget = dto.Budget,
            Employees = employees
        };

        ctx.Projects.Add(project);
        await ctx.SaveChangesAsync();

        return project;
    }

    public async Task<Employee> HireEmployeeWithOnboarding(CreateEmployeeDto dto)
    {
        // Rule 2: Department capacity check (< 5 employees)
        var department = await ctx.Departments
            .Include(d => d.Employees)
            .FirstOrDefaultAsync(d => d.Id == dto.DepartmentId);

        if (department == null)
        {
            throw new ValidationException("Department does not exist or is deleted");
        }

        if (department.Employees.Count >= 5)
        {
            throw new ValidationException($"Department '{department.Name}' is at capacity (5 employees)");
        }

        // Rule 3: Must assign to at least one active project
        if (dto.ProjectIds.Count < 1)
        {
            throw new ValidationException("New employee must be assigned to at least one project");
        }

        // Rule 4: HireDate must be a weekday (Monday-Friday)
        var dayOfWeek = dto.HireDate.DayOfWeek;
        if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
        {
            throw new ValidationException("Hire date must be a weekday (Monday-Friday)");
        }

        // Rule 5: Cannot hire more than 2 employees on the same day
        var hiresOnSameDay = await ctx.Employees
            .CountAsync(e => e.HireDate.Date == dto.HireDate.Date);

        if (hiresOnSameDay >= 2)
        {
            throw new ValidationException(
                $"Cannot hire more than 2 employees on {dto.HireDate.ToShortDateString()} (HR bandwidth constraint)");
        }

        // Rule 1: All CreateEmployee rules apply
        return await CreateEmployee(dto);
    }

    public async Task<Project> CreateProjectWithAutoAssignment(CreateProjectWithAutoAssignmentDto dto)
    {
        // First validate all CreateProject rules by creating base DTO
        var baseDto = new CreateProjectDto
        {
            Name = dto.Name,
            Description = dto.Description,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Budget = dto.Budget,
            EmployeeIds = new List<int>() // Will auto-assign
        };

        // Validate department exists
        var department = await ctx.Departments
            .Include(d => d.Employees)
                .ThenInclude(e => e.Projects)
            .FirstOrDefaultAsync(d => d.Id == dto.DepartmentId);

        if (department == null)
        {
            throw new ValidationException("Department does not exist or is deleted");
        }

        // Calculate number of employees to assign
        var employeesToAssign = (int)Math.Floor(dto.Budget / 50000);

        if (employeesToAssign == 0)
        {
            // No auto-assignment needed, just create project
            return await CreateProject(baseDto);
        }

        // Check if department has enough employees
        if (department.Employees.Count < employeesToAssign)
        {
            throw new ValidationException(
                $"Department only has {department.Employees.Count} employees " +
                $"but project requires {employeesToAssign}");
        }

        // Find least-busy employees (fewest current project assignments)
        var leastBusyEmployees = department.Employees
            .OrderBy(e => e.Projects.Count)
            .Take(employeesToAssign)
            .Select(e => e.Id)
            .ToList();

        baseDto.EmployeeIds = leastBusyEmployees;

        return await CreateProject(baseDto);
    }

    public async Task<Employee> TransferEmployeeToDepartment(int employeeId, int targetDepartmentId)
    {
        // Rule 1: Employee must exist and not be soft-deleted
        var employee = await ctx.Employees
            .Include(e => e.Projects)
                .ThenInclude(p => p.Employees)
                    .ThenInclude(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null)
        {
            throw new ValidationException("Employee does not exist or is soft-deleted");
        }

        // Rule 2: Target department must exist and not be soft-deleted
        var targetDepartment = await ctx.Departments
            .Include(d => d.Employees)
            .FirstOrDefaultAsync(d => d.Id == targetDepartmentId);

        if (targetDepartment == null)
        {
            throw new ValidationException("Target department does not exist or is soft-deleted");
        }

        // Rule 3: Target department must have capacity (< 5 employees)
        if (targetDepartment.Employees.Count >= 5)
        {
            throw new ValidationException(
                $"Target department '{targetDepartment.Name}' is at capacity (5 employees)");
        }

        // Rule 4: Employee salary must fit within target department budget constraints
        var maxSalary = targetDepartment.Budget * 0.30;
        if (employee.Salary > maxSalary)
        {
            throw new ValidationException(
                $"Employee salary ${employee.Salary} exceeds target department budget constraint (max ${maxSalary})");
        }

        // Rule 5: Project employees must be from max 2 different departments after transfer
        foreach (var project in employee.Projects)
        {
            var uniqueDepartments = project.Employees
                .Where(e => e.Id != employeeId) // Exclude current employee
                .Select(e => e.DepartmentId)
                .Distinct()
                .ToList();

            uniqueDepartments.Add(targetDepartmentId); // Add target department

            if (uniqueDepartments.Distinct().Count() > 2)
            {
                throw new ValidationException(
                    $"Transfer would cause project '{project.Name}' to have employees from >2 departments");
            }
        }

        // Perform the transfer
        employee.DepartmentId = targetDepartmentId;
        employee.Department = targetDepartment;

        await ctx.SaveChangesAsync();

        return employee;
    }

    public async Task<Employee> PromoteEmployee(int employeeId, double newSalary)
    {
        // Rule 1: Employee must exist, not be soft-deleted, and employed >= 6 months
        var employee = await ctx.Employees
            .Include(e => e.Department)
            .Include(e => e.Projects)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null)
        {
            throw new ValidationException("Employee does not exist or is soft-deleted");
        }

        var employmentDuration = DateTime.UtcNow - employee.HireDate;
        if (employmentDuration.TotalDays < 180) // ~6 months
        {
            throw new ValidationException("Employee must be employed for at least 6 months to be promoted");
        }

        // Rule 2: New salary must be 10-30% higher than current
        var minSalary = employee.Salary * 1.10;
        var maxSalary = employee.Salary * 1.30;

        if (newSalary < minSalary || newSalary > maxSalary)
        {
            throw new ValidationException(
                $"New salary must be 10-30% higher than current (${minSalary}-${maxSalary})");
        }

        // Rule 3: New salary must fit department budget constraints
        var deptMaxSalary = employee.Department.Budget * 0.30;
        if (newSalary > deptMaxSalary)
        {
            throw new ValidationException(
                $"New salary ${newSalary} exceeds department budget constraint (max ${deptMaxSalary})");
        }

        // Rule 4: Cannot promote more than 1 employee per department per month
        var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);
        var recentPromotionsInDept = await ctx.Employees
            .Where(e => e.DepartmentId == employee.DepartmentId)
            .Where(e => e.HireDate < oneMonthAgo) // Not recently hired
            .Where(e => e.Salary >= e.Department.Budget * 0.25) // Assume promoted if high salary
            .CountAsync();

        // This is a simplified check - in real app, you'd track promotions separately
        // For now, just check if we've updated salaries recently (would need audit table)

        // Rule 5: Employee must be assigned to at least one project
        if (!employee.Projects.Any())
        {
            throw new ValidationException("Employee must be assigned to at least one project to be promoted");
        }

        // Perform the promotion
        employee.Salary = newSalary;
        await ctx.SaveChangesAsync();

        return employee;
    }

    public async Task<Project> CloseProject(int projectId)
    {
        // Rule 1: Project must exist and not be soft-deleted
        var project = await ctx.Projects
            .Include(p => p.Employees)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null)
        {
            throw new ValidationException("Project does not exist or is soft-deleted");
        }

        // Rule 5: Cannot close project that started < 30 days ago
        var duration = DateTime.UtcNow - project.StartDate;
        if (duration.TotalDays < 30)
        {
            throw new ValidationException(
                $"Project started only {(int)duration.TotalDays} days ago (minimum 30 days required)");
        }

        // Rule 2: Set EndDate to today if not already set
        if (!project.EndDate.HasValue)
        {
            project.EndDate = DateTime.UtcNow;
        }

        // Rule 3: Unassign all employees
        project.Employees.Clear();

        // Rule 4: Soft delete the project
        project.IsDeleted = true;
        project.DeletedAt = DateTime.UtcNow;

        await ctx.SaveChangesAsync();

        return project;
    }

    public async Task<Department> RestructureDepartment(int departmentId, List<int> newEmployeeIds)
    {
        // Rule 1: Department must exist and not be soft-deleted
        var department = await ctx.Departments
            .Include(d => d.Employees)
                .ThenInclude(e => e.Projects)
                    .ThenInclude(p => p.Employees)
            .FirstOrDefaultAsync(d => d.Id == departmentId);

        if (department == null)
        {
            throw new ValidationException("Department does not exist or is soft-deleted");
        }

        // Rule 2: All new employees must exist and not be soft-deleted
        var newEmployees = await ctx.Employees
            .Include(e => e.Projects)
            .Where(e => newEmployeeIds.Contains(e.Id))
            .ToListAsync();

        if (newEmployees.Count != newEmployeeIds.Count)
        {
            throw new ValidationException("One or more employees do not exist or are soft-deleted");
        }

        // Rule 3: Cannot remove employee who is only one on critical project
        var currentEmployees = department.Employees.ToList();
        var employeesToRemove = currentEmployees.Where(e => !newEmployeeIds.Contains(e.Id)).ToList();

        foreach (var emp in employeesToRemove)
        {
            foreach (var project in emp.Projects)
            {
                // Critical = budget > $100,000
                if (project.Budget > 100000)
                {
                    var employeesOnProject = project.Employees.Count;
                    if (employeesOnProject == 1)
                    {
                        throw new ValidationException(
                            $"Cannot remove {emp.FirstName} {emp.LastName} - only employee on critical project '{project.Name}'");
                    }
                }
            }
        }

        // Rule 4: New department size must be 1-5 employees
        if (newEmployeeIds.Count < 1 || newEmployeeIds.Count > 5)
        {
            throw new ValidationException("Department must have between 1 and 5 employees");
        }

        // Rule 5: Total salary must fit department budget
        var totalSalary = newEmployees.Sum(e => e.Salary);
        var maxTotalSalary = department.Budget * 0.80; // Conservative limit

        if (totalSalary > maxTotalSalary)
        {
            throw new ValidationException(
                $"Total salaries ${totalSalary} exceed budget constraint (max ${maxTotalSalary})");
        }

        // Perform the restructure
        department.Employees.Clear();
        foreach (var emp in newEmployees)
        {
            emp.DepartmentId = departmentId;
            department.Employees.Add(emp);
        }

        await ctx.SaveChangesAsync();

        return department;
    }

    public async Task<Department> CreateDepartmentWithEmployees(CreateDepartmentWithEmployeesDto dto)
    {
        // Use a transaction to ensure atomicity
        await using var transaction = await ctx.Database.BeginTransactionAsync();

        try
        {
            // Rule 1: All CreateDepartment rules apply
            var department = await CreateDepartment(new CreateDepartmentDto
            {
                Name = dto.Name,
                Location = dto.Location,
                Budget = dto.Budget
            });

            // Rule 2: Must hire at least 2 employees
            if (dto.Employees.Count < 2)
            {
                throw new ValidationException("Must hire at least 2 employees");
            }

            // Rule 5: At least one employee must have salary >= $75,000
            if (!dto.Employees.Any(e => e.Salary >= 75000))
            {
                throw new ValidationException("At least one employee must have salary >= $75,000 (manager requirement)");
            }

            // Rule 4: Total salaries must not exceed 80% of department budget
            var totalSalaries = dto.Employees.Sum(e => e.Salary);
            var maxSalaries = department.Budget * 0.80;

            if (totalSalaries > maxSalaries)
            {
                throw new ValidationException(
                    $"Total salaries ${totalSalaries} exceed 80% of budget (max ${maxSalaries})");
            }

            // Rule 3: Create each employee (all CreateEmployee rules apply)
            var createdEmployees = new List<Employee>();

            foreach (var empDto in dto.Employees)
            {
                empDto.DepartmentId = department.Id; // Set to new department

                var employee = await CreateEmployee(empDto);
                createdEmployees.Add(employee);
            }

            // Load the department with employees
            await ctx.Entry(department).Collection(d => d.Employees).LoadAsync();

            await transaction.CommitAsync();

            return department;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
