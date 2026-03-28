using StockFlow.Web.DTOs.Auth;
using StockFlow.Web.DTOs.Item;
using StockFlow.Web.DTOs.Shipment;
using StockFlow.Web.DTOs.Process;
using StockFlow.Web.DTOs.Report;
using StockFlow.Web.DTOs.Search;

namespace StockFlow.Web.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResultViewModel> LoginAsync(LoginDto dto, CancellationToken ct = default);
        Task LogoutAsync(int userId, CancellationToken ct = default);
        Task<UserViewModel> RegisterAsync(RegisterDto dto, CancellationToken ct = default);
        Task ChangePasswordAsync(int userId, ChangePasswordDto dto, CancellationToken ct = default);
        Task<UserViewModel> GetCurrentUserAsync(int userId, CancellationToken ct = default);
    }

    public interface IRoleService
    {
        Task<bool> HasPermissionAsync(int userId, string permission, CancellationToken ct = default);
        Task<string> GetRoleAsync(int userId, CancellationToken ct = default);
        Task AssignRoleAsync(int targetUserId, string role, int requestedByUserId, CancellationToken ct = default);
        Task<IEnumerable<string>> GetAllPermissionsForRoleAsync(string role, CancellationToken ct = default);
        bool IsRoleHigherOrEqual(string requesterRole, string targetRole);
    }

    public interface IItemService
    {
        Task<ItemViewModel> GetByIdAsync(int itemId, CancellationToken ct = default);
        Task<IEnumerable<ItemViewModel>> GetAllAsync(CancellationToken ct = default);
        Task<ItemViewModel> CreateAsync(CreateItemDto dto, int createdBy, CancellationToken ct = default);
        Task<ItemViewModel> UpdateAsync(int itemId, UpdateItemDto dto, int updatedBy, CancellationToken ct = default);
        Task DeleteAsync(int itemId, int deletedBy, CancellationToken ct = default);
        Task<bool> SKUExistsAsync(string sku, int? excludeItemId = null, CancellationToken ct = default);
    }

    public interface IShipmentService
    {
        Task<ShipmentViewModel> GetByIdAsync(int shipmentId, CancellationToken ct = default);
        Task<IEnumerable<ShipmentViewModel>> GetAllAsync(CancellationToken ct = default);
        Task<IEnumerable<ShipmentViewModel>> GetPendingAsync(CancellationToken ct = default);
        Task<IEnumerable<ShipmentViewModel>> GetStaleAsync(int olderThanHours, CancellationToken ct = default);
        Task<ShipmentViewModel> ReceiveAsync(CreateShipmentDto dto, int receivedBy, CancellationToken ct = default);
        Task UpdateStatusAsync(int shipmentId, string status, int updatedBy, CancellationToken ct = default);
        Task DeleteAsync(int shipmentId, int deletedBy, CancellationToken ct = default);
    }

    public interface IProcessService
    {
        Task<ProcessedItemViewModel> GetByIdAsync(int processedItemId, CancellationToken ct = default);
        Task<IEnumerable<ProcessedItemViewModel>> GetByShipmentAsync(int shipmentId, CancellationToken ct = default);
        Task<ProcessedItemViewModel> ProcessAsync(CreateProcessDto dto, int processedBy, CancellationToken ct = default);
        Task<ProcessedItemViewModel> ApproveAsync(int processedItemId, int approvedBy, CancellationToken ct = default);
        Task<ProcessedItemViewModel> RejectAsync(int processedItemId, string reason, int rejectedBy, CancellationToken ct = default);
        Task<IEnumerable<ProcessedItemViewModel>> GetChildrenAsync(int parentId, CancellationToken ct = default);
        Task<IEnumerable<ProcessedItemViewModel>> GetPendingApprovalsAsync(CancellationToken ct = default);
    }

    public interface IWeightValidatorService
    {
        Task ValidateChildWeightsAsync(int parentId, IEnumerable<double> childWeights, CancellationToken ct = default);
        Task ValidateSingleWeightAsync(double weight, string fieldName, CancellationToken ct = default);
        double GetRemainingWeight(double parentWeight, IEnumerable<double> existingChildWeights);
    }

    public interface ITreeBuilderService
    {
        Task<TreeNodeViewModel> BuildTreeAsync(int shipmentId, CancellationToken ct = default);
        Task<TreeNodeViewModel> BuildSubTreeAsync(int processedItemId, CancellationToken ct = default);
        Task<int> GetTreeDepthAsync(int processedItemId, CancellationToken ct = default);
        Task<IEnumerable<TreeNodeViewModel>> GetAncestorsAsync(int processedItemId, CancellationToken ct = default);
    }

    public interface IAuditLogService
    {
        Task LogAsync(AuditLogDto dto, CancellationToken ct = default);
        Task<IEnumerable<AuditLogViewModel>> GetByEntityAsync(string entityName, int entityId, CancellationToken ct = default);
        Task<IEnumerable<AuditLogViewModel>> GetByUserAsync(int userId, CancellationToken ct = default);
        Task<IEnumerable<AuditLogViewModel>> GetRecentAsync(int count = 50, CancellationToken ct = default);
    }

    public interface ISearchService
    {
        Task<SearchResultViewModel> SearchAsync(SearchDto dto, CancellationToken ct = default);
        Task<IEnumerable<ItemViewModel>> SearchItemsAsync(string query, CancellationToken ct = default);
        Task<IEnumerable<ShipmentViewModel>> SearchShipmentsAsync(string query, CancellationToken ct = default);
        Task<IEnumerable<ProcessedItemViewModel>> SearchProcessedItemsAsync(string query, CancellationToken ct = default);
    }

    public interface IReportService
    {
        Task<DailyReportViewModel> GetDailyReportAsync(DateTime date, CancellationToken ct = default);
        Task<IEnumerable<DailyReportViewModel>> GetRangeReportAsync(DateTime from, DateTime to, CancellationToken ct = default);
        Task<ItemBreakdownViewModel> GetItemBreakdownAsync(int itemId, DateTime from, DateTime to, CancellationToken ct = default);
        Task<ProcessingStatsViewModel> GetProcessingStatsAsync(CancellationToken ct = default);
    }

    public interface IExportService
    {
        Task<byte[]> ExportTreeToPdfAsync(int shipmentId, CancellationToken ct = default);
        Task<byte[]> ExportTreeToExcelAsync(int shipmentId, CancellationToken ct = default);
        Task<byte[]> ExportReportToPdfAsync(DateTime from, DateTime to, CancellationToken ct = default);
        Task<byte[]> ExportReportToExcelAsync(DateTime from, DateTime to, CancellationToken ct = default);
    }

    public interface INotificationService
    {
        Task NotifyProcessingCompleteAsync(int shipmentId, int processedItemId, CancellationToken ct = default);
        Task NotifyApprovalRequiredAsync(int processedItemId, CancellationToken ct = default);
        Task NotifyApprovalDecisionAsync(int processedItemId, string decision, CancellationToken ct = default);
        Task NotifyStaleShipmentAsync(int shipmentId, CancellationToken ct = default);
        Task NotifyUserAsync(int userId, string message, CancellationToken ct = default);
    }
}