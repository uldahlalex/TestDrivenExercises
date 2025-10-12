namespace tests.Entities;

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public double Budget { get; set; }
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}