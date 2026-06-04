using ServiceTreeDemo.Models;

namespace ServiceTreeDemo.Services;

/// <summary>
/// Background service: automatically checks all node health endpoints every 30 s.
/// Also exposes CheckAllNowAsync() so the UI button can trigger an immediate check.
/// </summary>
public class HealthCheckService : BackgroundService
{
    private readonly ServiceTreeService _treeService;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<HealthCheckService> _logger;

    public HealthCheckService(
        ServiceTreeService treeService,
        IHttpClientFactory httpFactory,
        ILogger<HealthCheckService> logger)
    {
        _treeService = treeService;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    // ── BackgroundService loop ─────────────────────────────────
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a bit for the app to fully start before the first sweep
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckAllNowAsync();
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    // ── Manual trigger (called from UI button) ─────────────────
    public async Task CheckAllNowAsync()
    {
        var nodes = _treeService.Nodes
            .Where(n => !string.IsNullOrWhiteSpace(n.HealthCheckUrl))
            .ToList();

        if (!nodes.Any()) return;

        var client = _httpFactory.CreateClient("healthcheck");
        client.Timeout = TimeSpan.FromSeconds(5);

        var tasks = nodes.Select(node => CheckNodeAsync(client, node));
        await Task.WhenAll(tasks);

        // Persist the updated statuses and timestamps
        await _treeService.SaveAsync();
        _treeService.NotifyChange();
    }

    private async Task CheckNodeAsync(HttpClient client, ServiceNode node)
    {
        try
        {
            var response = await client.GetAsync(node.HealthCheckUrl);
            node.Status = response.IsSuccessStatusCode ? "Active" : "Down";
            node.LastHealthCheck = DateTime.Now;
            _logger.LogInformation("Health check {Name}: {Status}", node.Name, node.Status);
        }
        catch (Exception ex)
        {
            node.Status = "Down";
            node.LastHealthCheck = DateTime.Now;
            _logger.LogWarning("Health check failed for {Name}: {Message}", node.Name, ex.Message);
        }
    }
}