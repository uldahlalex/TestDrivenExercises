# Create Operations with Business Rules - Level 6

## Overview
This advanced exercise module teaches students about **complex business rule validation** before database operations - a critical skill for production CRUD applications where data integrity and business logic enforcement are paramount.

## What Students Learn

### Core Concepts
1. **Pre-validation Patterns**: Check database state before making changes
2. **Business Rule Enforcement**: Implement complex multi-step validations
3. **Cross-entity Validation**: Rules that span multiple tables/entities
4. **Constraint Management**: Budget limits, capacity limits, uniqueness checks
5. **Transaction Management**: Atomic operations with rollback on failure
6. **Defensive Programming**: Validate assumptions at every step

### Key Skills
- Querying database state to validate business rules
- Throwing meaningful `ValidationException` with clear messages
- Combining multiple validation rules in sequence
- Managing one-to-many and many-to-many relationships during creation
- Using EF transactions for atomic multi-step operations
- Handling soft-deleted entities in validation logic

## Exercise Structure

### Files Created
```
ef-exercises/
├── Interfaces/
│   └── IEfExercisesCreateWithBusinessRules.cs  # 10 methods with complex rules
├── DTOs/
│   ├── CreateEmployeeDto.cs
│   ├── CreateDepartmentDto.cs
│   ├── CreateProjectDto.cs
│   ├── CreateProjectWithAutoAssignmentDto.cs
│   └── CreateDepartmentWithEmployeesDto.cs
├── Exercises/
│   └── EfExercisesCreateWithBusinessRules.cs   # Solution implementation
└── Tests/EfExercisesCreateWithBusinessRulesTests/
    ├── CreateEmployeeTests.cs                  # 11 tests
    ├── CreateDepartmentTests.cs                # 8 tests
    ├── CreateProjectTests.cs                   # 12 tests
    └── AdvancedOperationsTests.cs              # 10 tests
```

## Business Rules by Method

### 1. CreateEmployee (5 Rules)
```csharp
Task<Employee> CreateEmployee(CreateEmployeeDto dto);
```

**Rules:**
1. ✅ Email must be unique across ALL employees (even soft-deleted)
2. ✅ Department must exist and not be soft-deleted
3. ✅ Salary ≤ 30% of department budget
4. ✅ Cannot assign more than 3 projects to new employee
5. ✅ All assigned projects must exist and not be soft-deleted

**Teaching Points:**
- `IgnoreQueryFilters()` to check soft-deleted records
- Cross-entity validation (department budget vs salary)
- Collection count validation
- Existence checks with proper error messages

### 2. CreateDepartment (4 Rules)
```csharp
Task<Department> CreateDepartment(CreateDepartmentDto dto);
```

**Rules:**
1. ✅ Name must be unique (case-insensitive) across active departments
2. ✅ Budget ≥ $50,000 minimum
3. ✅ Location cannot have 3+ departments (facility constraint)
4. ✅ Cannot create more than 10 total active departments

**Teaching Points:**
- Case-insensitive string comparison (`ToLower()`)
- Range validation with meaningful minimums
- Aggregate constraints (count per location)
- Global system limits

### 3. CreateProject (7 Rules)
```csharp
Task<Project> CreateProject(CreateProjectDto dto);
```

**Rules:**
1. ✅ Name must be unique across active projects
2. ✅ StartDate must be future or today
3. ✅ EndDate (if provided) must be after StartDate
4. ✅ Budget ≥ $10,000
5. ✅ Total active project budgets ≤ $1,000,000
6. ✅ Cannot assign employees from soft-deleted departments
7. ✅ Max employees = floor(budget / $50,000) - "1 employee per $50k"

**Teaching Points:**
- Date validation logic
- Sum aggregation for global limits
- Navigation property traversal for validation
- Mathematical business rules (budget per employee)

### 4. HireEmployeeWithOnboarding (5 Rules + all CreateEmployee rules)
```csharp
Task<Employee> HireEmployeeWithOnboarding(CreateEmployeeDto dto);
```

**Additional Rules:**
1. ✅ Department capacity < 5 employees
2. ✅ Must assign to at least one project
3. ✅ HireDate must be weekday (Monday-Friday)
4. ✅ Max 2 hires per day (HR bandwidth constraint)

**Teaching Points:**
- Method composition (calling CreateEmployee)
- `DayOfWeek` enum usage
- Date-based aggregation queries
- Resource constraint modeling

### 5. CreateProjectWithAutoAssignment (Complex)
```csharp
Task<Project> CreateProjectWithAutoAssignment(CreateProjectWithAutoAssignmentDto dto);
```

**Rules:**
- All CreateProject rules apply
- Auto-assign "least busy" employees from department
- Least busy = fewest current project assignments
- Number to assign = floor(budget / $50,000)

**Teaching Points:**
- Algorithmic logic in business rules
- `OrderBy()` with `ThenInclude()` for complex queries
- Calculated field logic
- Method orchestration

### 6. TransferEmployeeToDepartment (5 Rules)
```csharp
Task<Employee> TransferEmployeeToDepartment(int employeeId, int targetDepartmentId);
```

**Rules:**
1. ✅ Employee must exist and not be soft-deleted
2. ✅ Target department must exist and not be soft-deleted
3. ✅ Target department capacity < 5
4. ✅ Salary must fit target department budget constraints
5. ✅ Projects must allow transfer (max 2 departments per project)

**Teaching Points:**
- Complex navigation property validation
- Distinct() operations on related data
- Impact analysis before making changes
- Relationship constraint checking

### 7. PromoteEmployee (5 Rules)
```csharp
Task<Employee> PromoteEmployee(int employeeId, double newSalary);
```

**Rules:**
1. ✅ Employed for ≥ 6 months
2. ✅ New salary 10-30% higher than current
3. ✅ New salary fits department budget constraints
4. ✅ Max 1 promotion per department per month
5. ✅ Must be assigned to ≥ 1 project

**Teaching Points:**
- DateTime arithmetic (`TotalDays`)
- Percentage-based range validation
- Time-window queries (last 30 days)
- Eligibility checking

### 8. CloseProject (5 Rules)
```csharp
Task<Project> CloseProject(int projectId);
```

**Rules:**
1. ✅ Project must exist and not be soft-deleted
2. ✅ Set EndDate to today if not set
3. ✅ Unassign all employees
4. ✅ Soft delete the project
5. ✅ Minimum duration: 30 days since start

**Teaching Points:**
- Duration validation
- Cleanup operations (clearing collections)
- Multi-step state changes
- Combining update and soft delete

### 9. RestructureDepartment (5 Rules)
```csharp
Task<Department> RestructureDepartment(int departmentId, List<int> newEmployeeIds);
```

**Rules:**
1. ✅ Department must exist and not be soft-deleted
2. ✅ All new employees must exist and not be soft-deleted
3. ✅ Cannot remove employee who is only one on critical project (budget > $100k)
4. ✅ New department size must be 1-5 employees
5. ✅ Total salary must fit department budget

**Teaching Points:**
- Impact analysis loops
- Protecting critical resources
- Collection replacement patterns
- Aggregate calculations

### 10. CreateDepartmentWithEmployees (Transaction - 6 Rules)
```csharp
Task<Department> CreateDepartmentWithEmployees(CreateDepartmentWithEmployeesDto dto);
```

**Rules:**
1. ✅ All CreateDepartment rules apply
2. ✅ Must hire ≥ 2 employees
3. ✅ All CreateEmployee rules apply to each
4. ✅ Total salaries ≤ 80% of budget
5. ✅ At least one employee salary ≥ $75,000 (manager)
6. ✅ Transaction: rollback if any employee fails

**Teaching Points:**
- `BeginTransactionAsync()` / `CommitAsync()` / `RollbackAsync()`
- Atomicity in multi-step operations
- Loop validation with early exit
- Aggregate pre-validation

## Test Coverage (41 tests total)

### CreateEmployeeTests (11 tests)
- ✅ Valid data creates successfully
- ✅ Duplicate email throws
- ✅ Non-existent department throws
- ✅ Salary exceeds budget constraint throws
- ✅ Salary within constraint succeeds
- ✅ More than 3 projects throws
- ✅ Exactly 3 projects succeeds
- ✅ Non-existent project throws
- ✅ Soft-deleted department throws
- ✅ Soft-deleted project throws

### CreateDepartmentTests (8 tests)
- ✅ Valid data creates successfully
- ✅ Duplicate name throws
- ✅ Case-insensitive duplicate throws
- ✅ Budget < $50,000 throws
- ✅ Budget exactly $50,000 succeeds
- ✅ Location with 3 departments throws
- ✅ More than 10 active departments throws
- ✅ 10 active after soft deletes allows more

### CreateProjectTests (12 tests)
- ✅ Valid data creates successfully
- ✅ Duplicate name throws
- ✅ Start date in past throws
- ✅ Start date today succeeds
- ✅ End date before start throws
- ✅ Budget < $10,000 throws
- ✅ Exceeds total budget limit throws
- ✅ Within total budget succeeds
- ✅ Employee from deleted department throws
- ✅ Too many employees for budget throws
- ✅ Employees match budget constraint succeeds

### AdvancedOperationsTests (10 tests)
- ✅ Promote employee valid increases salary
- ✅ Employed < 6 months throws
- ✅ Increase < 10% throws
- ✅ Increase > 30% throws
- ✅ No projects throws
- ✅ Close project valid soft deletes and unassigns
- ✅ Started < 30 days ago throws
- ✅ Transfer valid succeeds
- ✅ Target at capacity throws
- ✅ Create department with employees (transaction)

## How to Use with Students

### Step 1: Show Complexity Level
Show students the interface with all the business rules documented:

```csharp
/// <summary>
/// Create a new employee with business rules:
/// 1. Email must be unique across all employees (including soft-deleted)
/// 2. Department must exist and not be soft-deleted
/// 3. Salary must be within department budget constraints
/// ...
/// </summary>
Task<Employee> CreateEmployee(CreateEmployeeDto dto);
```

### Step 2: Students Implement Step-by-Step
Start with simplest method (CreateEmployee) and work up to transactions:

```csharp
public async Task<Employee> CreateEmployee(CreateEmployeeDto dto)
{
    // TODO: Rule 1 - Check email uniqueness

    // TODO: Rule 2 - Validate department exists

    // TODO: Rule 3 - Check salary constraint

    // TODO: Rule 4 - Validate project count

    // TODO: Rule 5 - Validate projects exist

    // TODO: Create the employee

    throw new NotImplementedException();
}
```

### Step 3: Run Tests One at a Time
```bash
# Start with valid data test
dotnet test --filter "CreateEmployee_ValidData"

# Then test each validation rule
dotnet test --filter "CreateEmployee_DuplicateEmail"
dotnet test --filter "CreateEmployee_SalaryExceedsBudget"
```

### Step 4: Progress to Complex Operations
After mastering basic CRUD with validation:
1. **HireEmployeeWithOnboarding** - Learn method composition
2. **TransferEmployeeToDepartment** - Learn impact analysis
3. **PromoteEmployee** - Learn time-based rules
4. **CreateProjectWithAutoAssignment** - Learn algorithms in business logic
5. **CreateDepartmentWithEmployees** - Learn transactions

## Key Learning Moments

### Validation Before Action
Students learn to ALWAYS validate before mutating:

```csharp
// ❌ BAD - Create then check
var employee = new Employee { ... };
ctx.Employees.Add(employee);
if (emailExists) throw; // Too late!

// ✅ GOOD - Check then create
if (emailExists) throw new ValidationException("...");
var employee = new Employee { ... };
ctx.Employees.Add(employee);
```

### Meaningful Error Messages
Students learn error messages are for users:

```csharp
// ❌ BAD
throw new Exception("Invalid");

// ✅ GOOD
throw new ValidationException(
    $"Salary ${dto.Salary} exceeds department budget constraint (max ${maxSalary})");
```

### Query Filters Awareness
Students learn when to ignore filters:

```csharp
// Check including soft-deleted (emails must be globally unique)
var emailExists = await ctx.Employees
    .IgnoreQueryFilters()
    .AnyAsync(e => e.Email == dto.Email);

// Check only active (departments must be active)
var department = await ctx.Departments
    .FirstOrDefaultAsync(d => d.Id == dto.DepartmentId);
```

### Transaction Management
Students learn atomic operations:

```csharp
await using var transaction = await ctx.Database.BeginTransactionAsync();

try
{
    // Multiple operations
    var dept = await CreateDepartment(...);
    foreach (var emp in employees)
    {
        await CreateEmployee(...);
    }

    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

## Real-World Applications

These patterns are used in production for:
- **E-commerce**: Stock validation, order limits, pricing rules
- **HR Systems**: Headcount limits, salary bands, hiring workflows
- **Project Management**: Resource allocation, budget tracking
- **Financial Systems**: Transaction limits, compliance rules
- **Healthcare**: Patient capacity, appointment rules
- **Education**: Enrollment limits, prerequisite checking

## Progression Path

Students should complete exercises in this order:
1. **EfExercises** - Basic LINQ filtering (Level 1-3)
2. **IdempotentUpdates** - Update operations (Level 4)
3. **SoftDeletes** - Soft delete patterns (Level 5)
4. **CreateWithBusinessRules** ← Current (Level 6)
5. **Next suggestions**:
   - Optimistic Concurrency with conflict resolution
   - Batch Operations with ExecuteUpdate/ExecuteDelete
   - Complex Query Optimization (N+1 prevention)

## Solution Highlights

### Example: Complex Validation Chain
```csharp
public async Task<Employee> CreateEmployee(CreateEmployeeDto dto)
{
    // Rule 1: Email uniqueness (including soft-deleted)
    var emailExists = await ctx.Employees
        .IgnoreQueryFilters()
        .AnyAsync(e => e.Email == dto.Email);
    if (emailExists)
        throw new ValidationException($"Email {dto.Email} is already in use");

    // Rule 2: Department exists and active
    var department = await ctx.Departments
        .FirstOrDefaultAsync(d => d.Id == dto.DepartmentId);
    if (department == null)
        throw new ValidationException("Department does not exist or is deleted");

    // Rule 3: Salary within budget constraint
    var maxSalary = department.Budget * 0.30;
    if (dto.Salary > maxSalary)
        throw new ValidationException(
            $"Salary ${dto.Salary} exceeds constraint (max ${maxSalary})");

    // ... more rules ...

    // Finally create the employee
    var employee = new Employee { ... };
    ctx.Employees.Add(employee);
    await ctx.SaveChangesAsync();
    return employee;
}
```

### Example: Transaction Pattern
```csharp
public async Task<Department> CreateDepartmentWithEmployees(CreateDepartmentWithEmployeesDto dto)
{
    await using var transaction = await ctx.Database.BeginTransactionAsync();

    try
    {
        // Validate department rules
        var department = await CreateDepartment(new CreateDepartmentDto { ... });

        // Validate aggregate rules BEFORE creating employees
        var totalSalaries = dto.Employees.Sum(e => e.Salary);
        if (totalSalaries > department.Budget * 0.80)
            throw new ValidationException("Total salaries exceed 80% of budget");

        if (!dto.Employees.Any(e => e.Salary >= 75000))
            throw new ValidationException("Need at least one manager (≥$75k)");

        // Create each employee (each with full validation)
        foreach (var empDto in dto.Employees)
        {
            empDto.DepartmentId = department.Id;
            await CreateEmployee(empDto); // Re-uses validation logic!
        }

        await transaction.CommitAsync();
        return department;
    }
    catch
    {
        await transaction.RollbackAsync();
        throw; // Re-throw for caller
    }
}
```

### Example: Impact Analysis
```csharp
public async Task<Department> RestructureDepartment(int departmentId, List<int> newEmployeeIds)
{
    var department = await ctx.Departments
        .Include(d => d.Employees)
            .ThenInclude(e => e.Projects)
                .ThenInclude(p => p.Employees)
        .FirstOrDefaultAsync(d => d.Id == departmentId);

    // Find employees being removed
    var employeesToRemove = department.Employees
        .Where(e => !newEmployeeIds.Contains(e.Id))
        .ToList();

    // Check impact of removing each employee
    foreach (var emp in employeesToRemove)
    {
        foreach (var project in emp.Projects)
        {
            // Critical projects need protection
            if (project.Budget > 100000 && project.Employees.Count == 1)
            {
                throw new ValidationException(
                    $"Cannot remove {emp.FirstName} - only employee on critical project '{project.Name}'");
            }
        }
    }

    // Safe to proceed with restructure
    // ...
}
```

## Common Student Mistakes

1. **Not checking soft-deleted entities** when uniqueness matters globally
2. **Forgetting to validate existence** before accessing navigation properties
3. **Poor error messages** that don't help users understand what went wrong
4. **Not using transactions** for multi-step atomic operations
5. **Validating after mutating** instead of before
6. **Not considering edge cases** (exactly at limit, empty collections, etc.)
7. **Forgetting to load navigation properties** needed for validation
8. **Not testing negative cases** (only testing happy path)

## Running the Exercises

```bash
# Run all business rules tests
dotnet test --filter "CreateWithBusinessRules"

# Run specific method tests
dotnet test --filter "CreateEmployee"
dotnet test --filter "CreateDepartment"
dotnet test --filter "PromoteEmployee"

# Run all tests including previous exercises
dotnet test
```

## Notes for Instructors

### Test Isolation
The tests in this module sometimes affect each other because they share database state. This is **intentional** - it teaches students about:
- Database state management
- Test isolation importance
- Why transactions and rollback matter
- Real-world constraints that persist

Students can run tests individually to avoid state issues, or you can discuss test database strategies.

### Difficulty Progression
Recommend this order:
1. **CreateEmployee** - Foundation with multiple simple rules
2. **CreateDepartment** - Similar complexity, reinforces pattern
3. **CreateProject** - More complex with aggregations
4. **HireEmployeeWithOnboarding** - Method composition
5. **PromoteEmployee** - Time-based logic
6. **TransferEmployeeToDepartment** - Impact analysis
7. **CreateProjectWithAutoAssignment** - Algorithms
8. **CloseProject** - Multi-step state changes
9. **RestructureDepartment** - Complex validation loops
10. **CreateDepartmentWithEmployees** - Transactions

### Extension Ideas
After students complete these exercises, challenge them to:
- Add logging to validation failures
- Create a validation result pattern (instead of exceptions)
- Implement validation rules as separate classes (Strategy pattern)
- Add more complex cross-entity rules
- Implement audit trails for all creates
- Add rate limiting (max operations per time period)

## Summary

This exercise teaches students that **validation is the heart of business logic** in CRUD applications. Production systems don't just store data - they enforce complex business rules, protect data integrity, and ensure operations make business sense.

By the end of this module, students will understand:
- How to translate business requirements into code
- When and how to query database state for validation
- How to write defensive, robust data operations
- How to provide meaningful feedback when rules are violated
- How to manage complex multi-step operations atomically

These skills are essential for building production-ready applications.
