# Entity Framework Exercises

This is a test-driven development (TDD) project designed to help students learn Entity Framework Core through practical exercises.

## Project Structure

- **Entities/**: Contains the database entities (Employee, Department, Project)
- **Data/**: Contains the DbContext and seed data
- **Exercises/**: Contains the exercise interface and implementation
- **Tests/**: Contains the XUnit tests that validate the exercises

## Database Schema

The project uses a simple company database with:
- **Departments**: IT departments with budget and location
- **Employees**: Staff members with salary and hire date
- **Projects**: Company projects with budgets and timelines
- **Many-to-Many**: Employees can work on multiple projects

## Prerequisites

- **.NET 9.0 SDK** (or compatible version)
- **Docker** - Required for running PostgreSQL test containers

## How to Use

1. **Clone and Setup**:
   ```bash
   cd ef-exercises
   dotnet build
   ```

2. **Run Tests**:
   ```bash
   dotnet test
   ```

   Note: Tests use XUnit.DependencyInjection for dependency injection and Testcontainers for PostgreSQL, so Docker must be running.

## Exercises

### Level 1 - Basic Queries
1. **GetEmployeesByDepartment**: Find all employees in a specific department
2. **GetTotalSalaryByDepartment**: Calculate total salary expense for a department
3. **GetEmployeesWithSalaryAbove**: Find employees earning above a threshold
4. **GetEmployeesByHireYear**: Find employees hired in a specific year
5. **GetDepartmentWithHighestBudget**: Find the department with the largest budget

### Level 2 - Advanced Queries
8. **GetEmployeesHiredBetween**: Find employees hired within a date range
9. **GetTopNHighestPaidEmployees**: Get the N highest paid employees
10. **GetDepartmentsWithAverageSalaryAbove**: Find departments with average salary above threshold

### Level 3 - Hard (Cardinality & Updates)
11. **AssignEmployeeToProject**: Add employee to project (many-to-many add)
12. **RemoveEmployeeFromProject**: Remove employee from project (many-to-many remove)
13. **TransferEmployeeToDepartment**: Move employee to different department (one-to-many update)
14. **ReplaceProjectEmployees**: Replace all employees on a project with new set
15. **GetEmployeeProjects**: Get all projects an employee is assigned to
16. **GetProjectEmployees**: Get all employees working on a specific project
17. **AssignMultipleEmployeesToProject**: Bulk assign employees to a project
18. **GetEmployeesWithoutProjects**: Find employees not assigned to any project
19. **GetProjectsWithoutEmployees**: Find projects with no employee assignments
20. **TransferEmployeeAndAssignProject**: Update department and assign project in one transaction

### Level 4 - Idempotent Updates with DTOs
21. **UpdateEmployee**: Update employee to desired state (scalar properties, department transfer, project assignments)
22. **UpdateProject**: Update project to desired state (scalar properties, nullable fields, employee assignments)
23. **UpdateDepartment**: Update department to desired state (scalar properties, employee transfers)

This level focuses on:
- **Idempotent operations**: Same input produces same result when called multiple times
- **DTO-driven updates**: Use request DTOs to specify desired end state
- **Partial updates**: Only update fields specified in DTO
- **Complex state management**: Handle one-to-many and many-to-many relationships correctly
- **Proper change tracking**: Understanding how EF Core tracks entity state changes
- **Navigation property handling**: Correctly loading and updating related entities

## Learning Objectives

Students will learn:
- Entity Framework Core query syntax
- LINQ operations (Where, Select, Include, Sum, OrderBy)
- Navigation properties and relationships
- Understanding and working with cardinalities (one-to-many, many-to-many)
- Entity state management and change tracking
- Proper handling of relationship updates
- Database querying best practices

## Features

- **PostgreSQL Test Containers**: Realistic database environment using Docker
- **XUnit with Dependency Injection**: Modern test setup with XUnit.DependencyInjection
- **Automatic Seed Data**: Consistent test data for all exercises
- **Progressive Difficulty**: Exercises build from simple to more complex
- **Comprehensive Tests**: Edge cases and validation included
- **UTC DateTime Handling**: Proper timezone handling for PostgreSQL compatibility

## Sample Data

The database is seeded with:
- 4 Departments (Engineering, Marketing, Sales, HR)
- 8 Employees across different departments
- 3 Projects with various budgets and timelines
- Realistic salary ranges and hire dates