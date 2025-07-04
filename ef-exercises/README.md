# Entity Framework Exercises

This is a test-driven development (TDD) project designed to help students learn Entity Framework Core through practical exercises.

## Project Structure

- **Entities/**: Contains the database entities (Employee, Department, Project)
- **Data/**: Contains the DbContext and seed data
- **Exercises/**: Contains the exercise interface, stub implementation, and solution
- **Tests/**: Contains the TUnit tests that validate the exercises

## Database Schema

The project uses a simple company database with:
- **Departments**: IT departments with budget and location
- **Employees**: Staff members with salary and hire date
- **Projects**: Company projects with budgets and timelines
- **Many-to-Many**: Employees can work on multiple projects

## How to Use

### For Students (Learning Mode)

1. **Clone and Setup**:
   ```bash
   cd server/ef-exercises
   dotnet restore
   ```

2. **Run Tests** (they should fail initially):
   ```bash
   dotnet run -c Release
   ```

3. **Implement Exercises**:
   - Open `Exercises/EfExercisesStub.cs`
   - Implement each method to make the tests pass
   - Use the hints in the comments

4. **Verify Your Work**:
   - Run tests after each implementation
   - All tests should pass when correctly implemented
   - Tests with `NotImplementedException` will fail until you implement them

### For Instructors (Solution Mode)

1. **Switch to Solutions**:
   - In `Tests/EfExercisesTests.cs`, comment out the stub line and uncomment the solution line:
   ```csharp
   // _exercises = new EfExercisesStub(_context);
   _exercises = new EfExercisesSolution(_context);
   ```

2. **Verify Tests Pass**:
   ```bash
   dotnet run -c Release
   ```

## The 5 Exercises

1. **GetEmployeesByDepartmentAsync**: Find all employees in a specific department
2. **GetTotalSalaryByDepartmentAsync**: Calculate total salary expense for a department
3. **GetEmployeesWithSalaryAboveAsync**: Find employees earning above a threshold
4. **GetEmployeesByHireYearAsync**: Find employees hired in a specific year
5. **GetDepartmentWithHighestBudgetAsync**: Find the department with the largest budget

## Learning Objectives

Students will learn:
- Entity Framework Core query syntax
- LINQ operations (Where, Select, Include, Sum, OrderBy)
- Navigation properties and relationships
- Async/await patterns
- Database querying best practices

## Features

- **SQLite In-Memory Database**: Fast setup, no external dependencies
- **Automatic Seed Data**: Consistent test data for all exercises
- **Progressive Difficulty**: Exercises build from simple to more complex
- **Comprehensive Tests**: Edge cases and validation included
- **Switch Between Modes**: Easy toggle between student and solution implementations
- **Standard TUnit Testing**: Use familiar test runner commands and IDE integration

## Sample Data

The database is seeded with:
- 4 Departments (Engineering, Marketing, Sales, HR)
- 8 Employees across different departments
- 3 Projects with various budgets and timelines
- Realistic salary ranges and hire dates