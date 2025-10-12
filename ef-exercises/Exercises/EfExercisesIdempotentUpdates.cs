using Microsoft.EntityFrameworkCore;
using tests.Data;
using tests.DTOs;
using tests.Entities;
using tests.Interfaces;

namespace tests.Exercises;

public class EfExercisesIdempotentUpdates(CompanyDbContext ctx) : IEfExercisesIdempotentUpdates
{
    public Employee UpdateEmployee(UpdateEmployeeDto dto)
    {
        var employee = ctx.Employees
            .Include(e => e.Department)
            .Include(e => e.Projects)
            .ThenInclude(p => p.Employees)
            .First(e => e.Id == dto.Id);


            employee.Department = ctx.Departments.First(d => d.Id == dto.DepartmentId);

   
            employee.Projects.Clear();
            var projects = ctx.Projects.Where(p => dto.ProjectIds.Contains(p.Id));
            if(dto.ProjectIds.Count != projects.Count())
                throw new Exception("One or more projects not found");
            foreach (var project in projects)
            {
                if(project == null)
                    throw new Exception("Project not found");
            
                employee.Projects.Add(project);
            }
        

    
            employee.Salary = (double)dto.Salary;
            employee.Email = dto.Email;
            employee.HireDate = (DateTime)dto.HireDate;
            employee.FirstName = dto.FirstName;
            employee.LastName = dto.LastName;
        ctx.SaveChanges();
        return employee;
    }

    public Project UpdateProject(UpdateProjectDto dto)
    {
        var project = ctx.Projects
            .Include(p => p.Employees)
            .ThenInclude(e => e.Department)
            .First(p => p.Id == dto.Id);

       
                    project.Employees.Clear();
                 foreach (var employee in ctx.Employees.Where(e => dto.EmployeeIds.Contains(e.Id)))
                 {
                     project.Employees.Add(employee);
                 }

        

        project.Budget = (double)dto.Budget;

         project.Description = dto.Description;
            project.Name = dto.Name;
        project.EndDate = dto.EndDate;

            project.StartDate = dto.StartDate;
            project.EndDate = dto.EndDate;
        
 
        ctx.SaveChanges();
     
     return project;
    }   

    public Department UpdateDepartment(UpdateDepartmentDto dto)
    {
        var department = ctx.Departments
            .Include(d => d.Employees)
            .ThenInclude(e => e.Projects)
            .First(d => d.Id == dto.Id);

     
                    var desiredEmployees = ctx.Employees.Where(e => dto.EmployeeIds.Contains(e.Id));
                       
                    
                    if (desiredEmployees.Count() != dto.EmployeeIds.Count)
                        throw new Exception("One ID does not exist");
                    department.Employees.Clear();
                    foreach (var desiredEmployee in desiredEmployees)
                    {
                        if (desiredEmployee == null)
                            throw new Exception("Employee is null");
                        department.Employees.Add(desiredEmployee);
                    }
        


            department.Budget = (double)dto.Budget;
            department.Name = dto.Name;
            department.Location = dto.Location;
        ctx.SaveChanges();
        return department;
    }
}