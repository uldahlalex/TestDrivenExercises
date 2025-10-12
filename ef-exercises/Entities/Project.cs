namespace tests.Entities;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public double Budget { get; set; }
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}