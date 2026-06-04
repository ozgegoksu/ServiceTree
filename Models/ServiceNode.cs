namespace ServiceTreeDemo.Models;

public class ServiceNode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string Status { get; set; } = "Active"; // Active | Down
    public string Technology { get; set; } = string.Empty; // REST | SOAP | JAR | Mainframe | gRPC | MQ
    public string SwaggerUrl { get; set; } = string.Empty;
    public string WsdlUrl { get; set; } = string.Empty;
    public string HealthCheckUrl { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public List<Guid> ProjectIds { get; set; } = new();
    public DateTime? LastHealthCheck { get; set; }
}