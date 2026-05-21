namespace ServiceTreeDemo.Models;

public class ServiceProject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Color { get; set; } = "#5b9bd5";
    public string Icon { get; set; } = "🗂️";
    public string Owner { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
}