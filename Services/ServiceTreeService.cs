using Microsoft.EntityFrameworkCore;
using ServiceTreeDemo.Data;
using ServiceTreeDemo.Models;

namespace ServiceTreeDemo.Services;

/// <summary>
/// Keeps an in-memory cache of Nodes / Connections / Projects and persists
/// every mutation to SQLite via EF Core.  The public API is identical to the
/// old file-based version so existing Blazor pages need no changes.
/// </summary>
public class ServiceTreeService
{
    // ── In-memory cache (the source-of-truth for the UI) ─────────────────
    public List<ServiceNode> Nodes { get; private set; } = new();
    public List<ServiceConnection> Connections { get; private set; } = new();
    public List<ServiceProject> Projects { get; private set; } = new();

    public event Action? OnChange;

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private bool _loaded = false;

    public ServiceTreeService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    // ── Initialisation ────────────────────────────────────────────────────
    public async Task InitAsync()
    {
        if (_loaded) return;
        _loaded = true;

        await using var db = await _dbFactory.CreateDbContextAsync();

        bool hasData = await db.Projects.AnyAsync()
                    || await db.Nodes.AnyAsync()
                    || await db.Connections.AnyAsync();

        if (hasData)
        {
            // Load everything into memory
            Nodes = await db.Nodes.ToListAsync();
            Connections = await db.Connections.ToListAsync();
            Projects = await db.Projects.ToListAsync();
        }
        // First run → no seed, just empty. Users add their own data.
    }

    // ── Persistence helpers ───────────────────────────────────────────────

    /// <summary>Replaces every row in the DB with the current in-memory state.</summary>
    public async Task SaveAsync()
    {
        await PersistAllAsync();
    }

    private async Task PersistAllAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // Nodes
        var dbNodeIds = await db.Nodes.Select(n => n.Id).ToListAsync();
        var memNodeIds = Nodes.Select(n => n.Id).ToHashSet();

        db.Nodes.RemoveRange(db.Nodes.Where(n => !memNodeIds.Contains(n.Id)));
        foreach (var node in Nodes)
        {
            if (dbNodeIds.Contains(node.Id))
                db.Nodes.Update(node);
            else
                db.Nodes.Add(node);
        }

        // Connections
        var dbConnIds = await db.Connections.Select(c => c.Id).ToListAsync();
        var memConnIds = Connections.Select(c => c.Id).ToHashSet();

        db.Connections.RemoveRange(db.Connections.Where(c => !memConnIds.Contains(c.Id)));
        foreach (var conn in Connections)
        {
            if (dbConnIds.Contains(conn.Id))
                db.Connections.Update(conn);
            else
                db.Connections.Add(conn);
        }

        // Projects
        var dbProjIds = await db.Projects.Select(p => p.Id).ToListAsync();
        var memProjIds = Projects.Select(p => p.Id).ToHashSet();

        db.Projects.RemoveRange(db.Projects.Where(p => !memProjIds.Contains(p.Id)));
        foreach (var proj in Projects)
        {
            if (dbProjIds.Contains(proj.Id))
                db.Projects.Update(proj);
            else
                db.Projects.Add(proj);
        }

        await db.SaveChangesAsync();
    }

    // ── Auto-layout (Sugiyama-lite, overlap-free) ─────────────────────────
    public void AutoLayout()
    {
        if (!Nodes.Any()) return;

        const double nodeW = 155, nodeH = 95;
        const double layerGapX = 240, nodeGapY = 30;
        const double startX = 60, startY = 60;

        var inDegree = Nodes.ToDictionary(n => n.Id, _ => 0);
        foreach (var c in Connections)
            if (inDegree.ContainsKey(c.TargetId)) inDegree[c.TargetId]++;

        var layer = Nodes.ToDictionary(n => n.Id, _ => 0);

        var queue = new Queue<Guid>(Nodes.Where(n => inDegree[n.Id] == 0).Select(n => n.Id));
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            var l = layer.GetValueOrDefault(id, 0);
            foreach (var c in Connections.Where(c => c.SourceId == id))
            {
                if (!layer.ContainsKey(c.TargetId) || layer[c.TargetId] < l + 1)
                {
                    layer[c.TargetId] = l + 1;
                    queue.Enqueue(c.TargetId);
                }
            }
        }

        foreach (var group in Nodes.GroupBy(n => layer.GetValueOrDefault(n.Id, 0)).OrderBy(g => g.Key))
        {
            double xPos = startX + group.Key * layerGapX;
            double yPos = startY;
            foreach (var node in group)
            {
                node.PositionX = xPos;
                node.PositionY = yPos;
                yPos += nodeH + nodeGapY;
            }
        }

        FixOverlaps();
        _ = SaveAsync();
        NotifyChange();
    }

    public void FixOverlaps()
    {
        if (Nodes.Count < 2) return;

        const double nodeW = 155, nodeH = 95, pad = 20;
        bool anyOverlap = true;
        int maxPasses = 30;

        while (anyOverlap && maxPasses-- > 0)
        {
            anyOverlap = false;
            for (int i = 0; i < Nodes.Count; i++)
            {
                for (int j = i + 1; j < Nodes.Count; j++)
                {
                    var a = Nodes[i];
                    var b = Nodes[j];

                    double overlapX = (nodeW + pad) - Math.Abs(a.PositionX - b.PositionX);
                    double overlapY = (nodeH + pad) - Math.Abs(a.PositionY - b.PositionY);

                    if (overlapX > 0 && overlapY > 0)
                    {
                        anyOverlap = true;
                        if (overlapY <= overlapX)
                        {
                            double push = overlapY / 2.0 + 1;
                            if (a.PositionY <= b.PositionY) { a.PositionY -= push; b.PositionY += push; }
                            else { a.PositionY += push; b.PositionY -= push; }
                        }
                        else
                        {
                            double push = overlapX / 2.0 + 1;
                            if (a.PositionX <= b.PositionX) { a.PositionX -= push; b.PositionX += push; }
                            else { a.PositionX += push; b.PositionX -= push; }
                        }
                        a.PositionX = Math.Max(10, a.PositionX);
                        a.PositionY = Math.Max(10, a.PositionY);
                        b.PositionX = Math.Max(10, b.PositionX);
                        b.PositionY = Math.Max(10, b.PositionY);
                    }
                }
            }
        }
    }

    // ── CRUD — identical signatures to the original ───────────────────────

    public void AddNode(ServiceNode node)
    {
        Nodes.Add(node);
        _ = SaveAsync();
        Notify();
    }

    public void UpdateNode(ServiceNode node)
    {
        // The caller mutates the object in-place; the in-memory list already
        // holds a reference to it, so we just persist.
        _ = SaveAsync();
        Notify();
    }

    public void RemoveNode(Guid id)
    {
        Nodes.RemoveAll(n => n.Id == id);
        Connections.RemoveAll(c => c.SourceId == id || c.TargetId == id);
        _ = SaveAsync();
        Notify();
    }

    public void UpdateNodePosition(Guid id, double x, double y)
    {
        var node = GetNode(id);
        if (node is null) return;
        node.PositionX = x;
        node.PositionY = y;
        _ = SaveAsync();
        Notify();
    }

    /// <summary>
    /// Efficient single-row position update called from drag-drop.
    /// Updates only the one node column — does NOT re-persist the entire dataset.
    /// Does NOT call Notify() to avoid re-initialising drag handlers mid-session.
    /// </summary>
    public async Task UpdateNodePositionAsync(Guid id, double safeX, double safeY)
    {
        var node = GetNode(id);
        if (node is null) return;

        node.PositionX = safeX;
        node.PositionY = safeY;

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var dbNode = await db.Nodes.FindAsync(id);
            if (dbNode is not null)
            {
                dbNode.PositionX = safeX;
                dbNode.PositionY = safeY;
                await db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[UpdateNodePositionAsync] DB error: {ex.Message}");
        }
    }

    public void AddConnection(ServiceConnection conn)
    {
        Connections.Add(conn);
        _ = SaveAsync();
        Notify();
    }

    public void RemoveConnection(Guid id)
    {
        Connections.RemoveAll(c => c.Id == id);
        _ = SaveAsync();
        Notify();
    }

    public void AddProject(ServiceProject project)
    {
        Projects.Add(project);
        _ = SaveAsync();
        Notify();
    }

    public void UpdateProject(ServiceProject project)
    {
        _ = SaveAsync();
        Notify();
    }

    public void RemoveProject(Guid id)
    {
        Projects.RemoveAll(p => p.Id == id);
        foreach (var n in Nodes) n.ProjectIds.Remove(id);
        _ = SaveAsync();
        Notify();
    }

    // ── Queries ───────────────────────────────────────────────────────────

    public ServiceNode? GetNode(Guid id) => Nodes.FirstOrDefault(n => n.Id == id);
    public ServiceProject? GetProject(Guid id) => Projects.FirstOrDefault(p => p.Id == id);

    public List<ServiceNode> GetNodesForProject(Guid projectId) =>
        Nodes.Where(n => n.ProjectIds.Contains(projectId)).ToList();

    public List<ServiceConnection> GetConnectionsForProject(Guid projectId)
    {
        var nodeIds = GetNodesForProject(projectId).Select(n => n.Id).ToHashSet();
        return Connections
            .Where(c => nodeIds.Contains(c.SourceId) && nodeIds.Contains(c.TargetId))
            .ToList();
    }

    public List<ServiceNode?> GetUpstream(Guid nodeId) =>
        Connections.Where(c => c.TargetId == nodeId)
                   .Select(c => GetNode(c.SourceId))
                   .Where(n => n is not null)
                   .ToList();

    public List<ServiceNode?> GetDownstream(Guid nodeId) =>
        Connections.Where(c => c.SourceId == nodeId)
                   .Select(c => GetNode(c.TargetId))
                   .Where(n => n is not null)
                   .ToList();

    public List<(ServiceNode Node, ServiceConnection Connection, bool IsUpstream)>
        GetCrossProjectNeighbours(Guid nodeId)
    {
        var node = GetNode(nodeId);
        if (node is null) return new();

        var result = new List<(ServiceNode, ServiceConnection, bool)>();

        foreach (var conn in Connections.Where(c => c.SourceId == nodeId || c.TargetId == nodeId))
        {
            bool isUpstream = conn.TargetId == nodeId;
            var neighbourId = isUpstream ? conn.SourceId : conn.TargetId;
            var neighbour = GetNode(neighbourId);
            if (neighbour is null) continue;

            bool isCross = !neighbour.ProjectIds.Any(pid => node.ProjectIds.Contains(pid));
            if (isCross)
                result.Add((neighbour, conn, isUpstream));
        }

        return result;
    }

    public void NotifyChange() => Notify();
    private void Notify() => OnChange?.Invoke();
}