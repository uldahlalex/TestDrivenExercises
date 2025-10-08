using EfExercises.Data;
using EfExercises.DTOs;
using EfExercises.Entities;
using Microsoft.EntityFrameworkCore;

namespace EfExercises.Exercises;

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
        foreach (var project in projects)
        {
            employee.Projects.Add(project);
        }
        if(dto.Salary!=null)
            employee.Salary = (double)dto.Salary;
        if (dto.Email != null)
            employee.Email = dto.Email;
        if (dto.HireDate != null)
            employee.HireDate = (DateTime)dto.HireDate;
        if (dto.FirstName != null)
            employee.FirstName = dto.FirstName;
        if (dto.LastName != null)
            employee.LastName = dto.LastName;
        ctx.SaveChanges();
        return employee;
    }

    public Project UpdateProject(UpdateProjectDto dto)
    {
        throw new NotImplementedException();
    }

    public Department UpdateDepartment(UpdateDepartmentDto dto)
    {
        throw new NotImplementedException();
    }
}