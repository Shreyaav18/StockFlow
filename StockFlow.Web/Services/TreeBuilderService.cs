using Microsoft.EntityFrameworkCore;
using Serilog;
using StockFlow.Web.Data;
using StockFlow.Web.DTOs.Process;
using StockFlow.Web.Exceptions;
using StockFlow.Web.Services.Interfaces;

namespace StockFlow.Web.Services
{
    public class TreeBuilderService : ITreeBuilderService
    {
        private readonly AppDbContext _db;

        public TreeBuilderService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<TreeNodeViewModel> BuildTreeAsync(int shipmentId, CancellationToken ct = default)
        {
            try
            {
                var allNodes = await _db.ProcessedItems
                    .AsNoTracking()
                    .Include(p => p.Item)
                    .Where(p => p.ShipmentId == shipmentId)
                    .ToListAsync(ct);

                if (!allNodes.Any())
                    throw new NotFoundException(ErrorMessages.Process.NotFound);

                var shipment = await _db.Shipments
                    .AsNoTracking()
                    .Include(s => s.Item)
                    .FirstOrDefaultAsync(s => s.ShipmentId == shipmentId, ct)
                    ?? throw new NotFoundException(ErrorMessages.Shipment.NotFound);

                var root = new TreeNodeViewModel
                {
                    ProcessedItemId = 0,
                    ParentId = null,
                    ItemName = shipment.Item?.ItemName ?? string.Empty,
                    SKU = shipment.Item?.SKU ?? string.Empty,
                    InputWeight = shipment.TotalWeight,
                    OutputWeight = shipment.TotalWeight,
                    Unit = shipment.Item?.Unit ?? string.Empty,
                    Status = shipment.Status,
                    Depth = 0,
                    Children = BuildChildren(allNodes, null, 1)
                };

                return root;
            }
            catch (AppException) { throw; }
            catch (Exception ex)
            {
                Log.Error(ex, "Error building tree for shipment {ShipmentId}", shipmentId);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task<TreeNodeViewModel> BuildSubTreeAsync(int processedItemId, CancellationToken ct = default)
        {
            try
            {
                var allDescendants = await GetAllDescendantsAsync(processedItemId, ct);
                var root = await _db.ProcessedItems
                    .AsNoTracking()
                    .Include(p => p.Item)
                    .FirstOrDefaultAsync(p => p.ProcessedItemId == processedItemId, ct)
                    ?? throw new NotFoundException(ErrorMessages.Process.NotFound);

                return new TreeNodeViewModel
                {
                    ProcessedItemId = root.ProcessedItemId,
                    ParentId = root.ParentId,
                    ItemName = root.Item?.ItemName ?? string.Empty,
                    SKU = root.Item?.SKU ?? string.Empty,
                    InputWeight = root.InputWeight,
                    OutputWeight = root.OutputWeight,
                    Unit = root.Item?.Unit ?? string.Empty,
                    Status = root.Status,
                    Depth = 0,
                    Children = BuildChildren(allDescendants, processedItemId, 1)
                };
            }
            catch (AppException) { throw; }
            catch (Exception ex)
            {
                Log.Error(ex, "Error building subtree for processed item {ProcessedItemId}", processedItemId);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task<int> GetTreeDepthAsync(int processedItemId, CancellationToken ct = default)
        {
            try
            {
                var allDescendants = await GetAllDescendantsAsync(processedItemId, ct);
                return CalculateDepth(allDescendants, processedItemId, 0);
            }
            catch (AppException) { throw; }
            catch (Exception ex)
            {
                Log.Error(ex, "Error calculating depth for processed item {ProcessedItemId}", processedItemId);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task<IEnumerable<TreeNodeViewModel>> GetAncestorsAsync(int processedItemId, CancellationToken ct = default)
        {
            try
            {
                var ancestors = new List<TreeNodeViewModel>();
                var currentId = (int?)processedItemId;

                while (currentId.HasValue)
                {
                    var node = await _db.ProcessedItems
                        .AsNoTracking()
                        .Include(p => p.Item)
                        .FirstOrDefaultAsync(p => p.ProcessedItemId == currentId.Value, ct);

                    if (node == null) break;

                    ancestors.Insert(0, new TreeNodeViewModel
                    {
                        ProcessedItemId = node.ProcessedItemId,
                        ParentId = node.ParentId,
                        ItemName = node.Item?.ItemName ?? string.Empty,
                        SKU = node.Item?.SKU ?? string.Empty,
                        InputWeight = node.InputWeight,
                        OutputWeight = node.OutputWeight,
                        Unit = node.Item?.Unit ?? string.Empty,
                        Status = node.Status,
                        Depth = ancestors.Count
                    });

                    currentId = node.ParentId;
                }

                return ancestors;
            }
            catch (AppException) { throw; }
            catch (Exception ex)
            {
                Log.Error(ex, "Error fetching ancestors for processed item {ProcessedItemId}", processedItemId);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        private static List<TreeNodeViewModel> BuildChildren(
            IEnumerable<Models.ProcessedItem> allNodes,
            int? parentId,
            int depth)
        {
            return allNodes
                .Where(n => n.ParentId == parentId)
                .Select(n => new TreeNodeViewModel
                {
                    ProcessedItemId = n.ProcessedItemId,
                    ParentId = n.ParentId,
                    ItemName = n.Item?.ItemName ?? string.Empty,
                    SKU = n.Item?.SKU ?? string.Empty,
                    InputWeight = n.InputWeight,
                    OutputWeight = n.OutputWeight,
                    Unit = n.Item?.Unit ?? string.Empty,
                    Status = n.Status,
                    Depth = depth,
                    Children = BuildChildren(allNodes, n.ProcessedItemId, depth + 1)
                })
                .ToList();
        }

        private static int CalculateDepth(IEnumerable<Models.ProcessedItem> nodes, int parentId, int currentDepth)
        {
            var children = nodes.Where(n => n.ParentId == parentId).ToList();
            if (!children.Any()) return currentDepth;
            return children.Max(c => CalculateDepth(nodes, c.ProcessedItemId, currentDepth + 1));
        }

        private async Task<List<Models.ProcessedItem>> GetAllDescendantsAsync(int rootId, CancellationToken ct)
        {
            var result = new List<Models.ProcessedItem>();
            var queue = new Queue<int>();
            queue.Enqueue(rootId);

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();
                var children = await _db.ProcessedItems
                    .AsNoTracking()
                    .Include(p => p.Item)
                    .Where(p => p.ParentId == currentId)
                    .ToListAsync(ct);

                foreach (var child in children)
                {
                    result.Add(child);
                    queue.Enqueue(child.ProcessedItemId);
                }
            }

            return result;
        }
    }
}