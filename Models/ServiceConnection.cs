namespace ServiceTreeDemo.Models;

public class ServiceConnection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SourceId { get; set; }
    public Guid TargetId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string ConnectionType { get; set; } = "REST"; // REST | SOAP | JAR | gRPC | MQ
}