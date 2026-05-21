using System.Text.Json;
using ServiceTreeDemo.Models;

namespace ServiceTreeDemo.Services;

public class PersistenceService
{
    private readonly string _dataPath;
    private readonly JsonSerializerOptions _opts = new() { WriteIndented = true };

    public PersistenceService(IWebHostEnvironment env)
    {
        _dataPath = Path.Combine(env.ContentRootPath, "data", "servicemap.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_dataPath)!);
    }

    public async Task SaveAsync(List<ServiceNode> nodes, List<ServiceConnection> connections, List<ServiceProject> projects)
    {
        var data = new ServiceMapData { Nodes = nodes, Connections = connections, Projects = projects };
        var json = JsonSerializer.Serialize(data, _opts);
        await File.WriteAllTextAsync(_dataPath, json);
    }

    public async Task<ServiceMapData?> LoadAsync()
    {
        if (!File.Exists(_dataPath)) return null;
        var json = await File.ReadAllTextAsync(_dataPath);
        return JsonSerializer.Deserialize<ServiceMapData>(json);
    }

    public bool HasSavedData() => File.Exists(_dataPath);
}

public class ServiceMapData
{
    public List<ServiceNode> Nodes { get; set; } = new();
    public List<ServiceConnection> Connections { get; set; } = new();
    public List<ServiceProject> Projects { get; set; } = new();
}