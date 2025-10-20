# Soft Delete Exercises - Level 5

## Overview
This exercise module teaches students about **soft deletes** and **global query filters** in Entity Framework Core - essential patterns for production CRUD applications.

## What Students Learn

### Core Concepts
1. **Soft Delete Pattern**: Mark records as deleted instead of removing them from the database
2. **Global Query Filters**: Automatically exclude soft-deleted records from all queries
3. **Audit Trails**: Track when records were deleted via `DeletedAt` timestamp
4. **Referential Integrity**: Handle relationships correctly when soft deleting
5. **IgnoreQueryFilters()**: Bypass filters when you need to work with deleted records

### Key Skills
- Implementing soft delete on entities (Employee, Department, Project)
- Using `HasQueryFilter()` in `OnModelCreating` to configure global filters
- Managing many-to-many relationships during soft delete (clearing project assignments)
- Enforcing business rules (can't delete department with active employees)
- Implementing hard delete (permanent deletion after soft delete)
- Writing idempotent delete operations

## Exercise Structure

### Files Created
```
ef-exercises/
├── Entities/                           # Updated with IsDeleted, DeletedAt
│   ├── Employee.cs
│   ├── Department.cs
│   └── Project.cs
├── Interfaces/
│   └── IEfExercisesSoftDeletes.cs     # Interface with 10 methods
├── Exercises/
│   └── EfExercisesSoftDeletes.cs      # Solution implementation
├── Tests/EfExercisesSoftDeletesTests/
│   ├── SoftDeleteEmployeeTests.cs     # 16 tests
│   ├── SoftDeleteDepartmentTests.cs   # 8 tests
│   └── SoftDeleteProjectTests.cs      # 7 tests
└── Data/
    └── CompanyDbContext.cs            # Updated with query filters
```

### Test Coverage (31 tests total)

#### Employee Tests (16 tests)
- ✅ Soft delete marks IsDeleted=true and sets DeletedAt
- ✅ Soft delete is idempotent (can delete already deleted)
- ✅ Restore employee clears IsDeleted and DeletedAt
- ✅ Restore is idempotent (can restore already active)
- ✅ Soft delete clears project assignments
- ✅ Hard delete only works on soft-deleted employees
- ✅ Query filtering: GetActiveEmployees, GetDeletedEmployees, GetAllEmployeesIncludingDeleted
- ✅ Error handling for non-existent employees

#### Department Tests (8 tests)
- ✅ Can soft delete empty department
- ✅ Cannot soft delete department with active employees (throws)
- ✅ Can soft delete department with only deleted employees
- ✅ Restore department works correctly
- ✅ Idempotent operations
- ✅ Error handling

#### Project Tests (7 tests)
- ✅ Soft delete project clears employee assignments
- ✅ Restore project does NOT restore old assignments
- ✅ Delete/restore cycles work correctly
- ✅ Idempotent operations
- ✅ Error handling

## How to Use with Students

### Step 1: Show Them the Interface
```csharp
public interface IEfExercisesSoftDeletes
{
    Task<Employee> SoftDeleteEmployee(int id);
    Task<Employee> RestoreEmployee(int id);
    // ... more methods
}
```

### Step 2: Students Implement Empty Methods
Students start with:
```csharp
public class EfExercisesSoftDeletes(CompanyDbContext ctx) : IEfExercisesSoftDeletes
{
    public async Task<Employee> SoftDeleteEmployee(int id)
    {
        // TODO: Students implement this
        throw new NotImplementedException();
    }
}
```

### Step 3: Run Tests to Guide Implementation
```bash
dotnet test --filter "SoftDeleteEmployee_ValidEmployee"
```

### Step 4: Key Learning Moments

#### Query Filters
Students discover that after adding `IsDeleted` properties, they need to add query filters:
```csharp
modelBuilder.Entity<Employee>(entity =>
{
    entity.HasQueryFilter(e => !e.IsDeleted);
});
```

#### IgnoreQueryFilters
Students learn when to bypass filters:
```csharp
// To work with soft-deleted records
var employee = await ctx.Employees
    .IgnoreQueryFilters()
    .FirstAsync(e => e.Id == id);
```

#### Relationship Handling
Students learn to clear relationships before soft delete:
```csharp
employee.Projects.Clear(); // Clear many-to-many before soft delete
```

#### Business Rules
Students implement business logic:
```csharp
if (department.Employees.Any(e => !e.IsDeleted))
{
    throw new ValidationException("Cannot delete department with active employees");
}
```

## Progression Path

Students should complete in this order:
1. **EfExercises** - Basic filtering, LINQ (Level 1-3)
2. **IdempotentUpdates** - Update operations, DTOs (Level 4)
3. **SoftDeletes** ← Current - Soft deletes, query filters (Level 5)
4. **Next suggestions**:
   - Optimistic Concurrency (RowVersion)
   - Eager/Explicit Loading & N+1 queries
   - Batch Operations (ExecuteUpdate/ExecuteDelete)

## Solution Highlights

The provided solution demonstrates:

### Idempotent Soft Delete
```csharp
public async Task<Employee> SoftDeleteEmployee(int id)
{
    var employee = await ctx.Employees
        .IgnoreQueryFilters() // Can find already-deleted
        .Include(e => e.Projects)
        .FirstAsync(e => e.Id == id);

    employee.Projects.Clear(); // Clean up relationships
    employee.IsDeleted = true;
    employee.DeletedAt = DateTime.UtcNow;

    await ctx.SaveChangesAsync();
    return employee;
}
```

### Referential Integrity
```csharp
public async Task<Department> SoftDeleteDepartment(int id)
{
    var department = await ctx.Departments
        .IgnoreQueryFilters()
        .Include(d => d.Employees)
        .FirstAsync(d => d.Id == id);

    // Business rule: protect referential integrity
    if (department.Employees.Any(e => !e.IsDeleted))
    {
        throw new ValidationException(
            "Cannot soft delete department with active employees");
    }

    department.IsDeleted = true;
    department.DeletedAt = DateTime.UtcNow;
    await ctx.SaveChangesAsync();
    return department;
}
```

### Safe Hard Delete
```csharp
public async Task HardDeleteEmployee(int id)
{
    var employee = await ctx.Employees
        .IgnoreQueryFilters()
        .FirstAsync(e => e.Id == id);

    // Safety: only allow hard delete of soft-deleted records
    if (!employee.IsDeleted)
    {
        throw new ValidationException(
            "Cannot hard delete an active employee. Soft delete first.");
    }

    ctx.Employees.Remove(employee); // Actual EF delete
    await ctx.SaveChangesAsync();
}
```

## Testing Approach

All tests follow the TDD pattern:
1. **Arrange** - Set up initial state, verify assumptions
2. **Act** - Call the method under test
3. **Assert** - Check returned object AND database state

Example:
```csharp
[Fact]
public async Task SoftDeleteEmployee_ValidEmployee_ShouldMarkAsDeleted()
{
    // Arrange
    var employee = await context.Employees.FirstAsync(e => e.Id == 1);
    Assert.False(employee.IsDeleted); // Verify assumption

    // Act
    var result = await exercises.SoftDeleteEmployee(1);

    // Assert returned object
    Assert.True(result.IsDeleted);

    // Assert database state
    var dbEmployee = await context.Employees
        .IgnoreQueryFilters()
        .FirstAsync(e => e.Id == 1);
    Assert.True(dbEmployee.IsDeleted);
}
```

## Running the Exercises

```bash
# Run all soft delete tests
dotnet test --filter "SoftDeletes"

# Run specific entity tests
dotnet test --filter "SoftDeleteEmployee"
dotnet test --filter "SoftDeleteDepartment"
dotnet test --filter "SoftDeleteProject"

# Run all tests (including previous exercises)
dotnet test
```

## Common Student Mistakes to Watch For

1. **Forgetting IgnoreQueryFilters()** when working with deleted entities
2. **Not clearing relationships** before soft delete (causes FK violations on hard delete)
3. **Not checking business rules** (e.g., deleting department with active employees)
4. **Forgetting to add query filters** to OnModelCreating
5. **Not making operations idempotent** (throwing when deleting already-deleted)

## Real-World Applications

This pattern is used in production for:
- **Audit trails** - Keep history of deleted records
- **Compliance** - Legal requirements to retain data
- **Undo functionality** - Allow users to restore deleted items
- **Data recovery** - Protect against accidental deletions
- **Soft cascades** - Delete hierarchies without losing data

## Entity Changes

All entities now have:
```csharp
public bool IsDeleted { get; set; }
public DateTime? DeletedAt { get; set; }
```

These properties are automatically respected by EF Core query filters, making soft deletes transparent to most application code.
