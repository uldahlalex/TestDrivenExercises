using EfExercises.Data;
using EfExercises.Entities;

namespace EfExercises.Exercises;

public class EfExercisesHard(CompanyDbContext ctx) : IEfExercisesHard
{
    public void AssignEmployeeToProject(int employeeId, int projectId)
    {   
        var project = ctx.Projects.First(p => p.Id.Equals(projectId));
        var employeesOnProject = ctx.Employees.Where(e => e.Projects.Contains(project));
        var newEmployeeOnProject = ctx.Employees.First(e => e.Id.Equals(employeeId));
        if (employeesOnProject.Contains(newEmployeeOnProject))
            return;
        project.Employees.Clear();
        foreach (var employee in employeesOnProject)
        {
            project.Employees.Add(employee);
        }
        project.Employees.Add(newEmployeeOnProject);

        ctx.SaveChanges();
    }

    public void RemoveEmployeeFromProject(int employeeId, int projectId)
    {
        throw new NotImplementedException();
    }

    public void TransferEmployeeToDepartment(int employeeId, int newDepartmentId)
    {
        throw new NotImplementedException();
    }

    public void ReplaceProjectEmployees(int projectId, List<int> newEmployeeIds)
    {
        throw new NotImplementedException();
    }

    public List<Project> GetEmployeeProjects(int employeeId)
    {
        throw new NotImplementedException();
    }

    public List<Employee> GetProjectEmployees(int projectId)
    {
        throw new NotImplementedException();
    }

    public void AssignMultipleEmployeesToProject(int projectId, List<int> employeeIds)
    {
        throw new NotImplementedException();
    }

    public List<Employee> GetEmployeesWithoutProjects()
    {
        throw new NotImplementedException();
    }

    public List<Project> GetProjectsWithoutEmployees()
    {
        throw new NotImplementedException();
    }

    public void TransferEmployeeAndAssignProject(int employeeId, int newDepartmentId, int projectId)
    {
        throw new NotImplementedException();
    }
}