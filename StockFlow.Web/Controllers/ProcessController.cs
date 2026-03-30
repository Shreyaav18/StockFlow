using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Web.DTOs.Process;
using StockFlow.Web.Exceptions;
using StockFlow.Web.Services.Interfaces;

namespace StockFlow.Web.Controllers
{
    [Authorize(Policy = "AllStaff")]
    public class ProcessController : BaseController
    {
        private readonly IProcessService _processService;
        private readonly IShipmentService _shipmentService;
        private readonly IItemService _itemService;
        private readonly ITreeBuilderService _treeBuilderService;
        private readonly ISearchService _searchService;

        public ProcessController(
            IProcessService processService,
            IShipmentService shipmentService,
            IItemService itemService,
            ITreeBuilderService treeBuilderService,
            ISearchService searchService)
        {
            _processService = processService;
            _shipmentService = shipmentService;
            _itemService = itemService;
            _treeBuilderService = treeBuilderService;
            _searchService = searchService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, CancellationToken ct = default)
        {
            if (!string.IsNullOrWhiteSpace(search) && search.Length >= 2)
            {
                var results = await _searchService.SearchProcessedItemsAsync(search, ct);
                ViewBag.Search = search;
                ViewBag.ProcessedItems = results;
                return View();
            }

            var items = await _processService.GetPendingApprovalsAsync(ct);
            ViewBag.ProcessedItems = items;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Create(int shipmentId, int? parentId, CancellationToken ct = default)
        {
            try
            {
                var shipment = await _shipmentService.GetByIdAsync(shipmentId, ct);
                var allItems = await _itemService.GetAllAsync(ct);

                ViewBag.Shipment = shipment;
                ViewBag.Items = allItems;
                ViewBag.ParentId = parentId;

                if (parentId.HasValue)
                {
                    var parent = await _processService.GetByIdAsync(parentId.Value, ct);
                    ViewBag.Parent = parent;
                }

                return View(new CreateProcessDto
                {
                    ShipmentId = shipmentId,
                    ParentId = parentId
                });
            }
            catch (NotFoundException)
            {
                SetErrorMessage("Shipment not found.");
                return RedirectToAction("Index", "Shipment");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProcessDto dto, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Shipment = await _shipmentService.GetByIdAsync(dto.ShipmentId, ct);
                ViewBag.Items = await _itemService.GetAllAsync(ct);
                return View(dto);
            }

            try
            {
                var result = await _processService.ProcessAsync(dto, CurrentUserId, ct);
                SetSuccessMessage("Items processed successfully and sent for approval.");
                return RedirectToAction(nameof(Tree), new { shipmentId = dto.ShipmentId });
            }
            catch (WeightValidationException ex)
            {
                ModelState.AddModelError(string.Empty,
                    $"Weight exceeded. Parent: {ex.ParentWeight}, Children total: {ex.ChildrenTotalWeight}");
                ViewBag.Shipment = await _shipmentService.GetByIdAsync(dto.ShipmentId, ct);
                ViewBag.Items = await _itemService.GetAllAsync(ct);
                return View(dto);
            }
            catch (ValidationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                ViewBag.Shipment = await _shipmentService.GetByIdAsync(dto.ShipmentId, ct);
                ViewBag.Items = await _itemService.GetAllAsync(ct);
                return View(dto);
            }
            catch (ConflictException ex)
            {
                SetErrorMessage(ex.Message);
                return RedirectToAction("Index", "Shipment");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Detail(int id, CancellationToken ct = default)
        {
            try
            {
                var item = await _processService.GetByIdAsync(id, ct);
                var children = await _processService.GetChildrenAsync(id, ct);
                var ancestors = await _treeBuilderService.GetAncestorsAsync(id, ct);

                ViewBag.Children = children;
                ViewBag.Ancestors = ancestors;
                return View(item);
            }
            catch (NotFoundException)
            {
                SetErrorMessage("Processed item not found.");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Tree(int shipmentId, CancellationToken ct = default)
        {
            try
            {
                var tree = await _treeBuilderService.BuildTreeAsync(shipmentId, ct);
                var shipment = await _shipmentService.GetByIdAsync(shipmentId, ct);
                ViewBag.Shipment = shipment;
                return View(tree);
            }
            catch (NotFoundException)
            {
                SetErrorMessage("No processed items found for this shipment.");
                return RedirectToAction("Detail", "Shipment", new { id = shipmentId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> TreeJson(int shipmentId, CancellationToken ct = default)
        {
            try
            {
                var tree = await _treeBuilderService.BuildTreeAsync(shipmentId, ct);
                return JsonSuccess(tree);
            }
            catch (NotFoundException ex)
            {
                return JsonFail(ex.Message, 404);
            }
        }

        [HttpPost]
        [Authorize(Policy = "ManagerAndAbove")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, int shipmentId, CancellationToken ct = default)
        {
            try
            {
                await _processService.ApproveAsync(id, CurrentUserId, ct);
                SetSuccessMessage("Item approved successfully.");
            }
            catch (ForbiddenException ex)
            {
                SetErrorMessage(ex.Message);
            }
            catch (ConflictException ex)
            {
                SetErrorMessage(ex.Message);
            }

            return RedirectToAction(nameof(Tree), new { shipmentId });
        }

        [HttpPost]
        [Authorize(Policy = "ManagerAndAbove")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, int shipmentId, string reason, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                SetErrorMessage("A rejection reason is required.");
                return RedirectToAction(nameof(Tree), new { shipmentId });
            }

            try
            {
                await _processService.RejectAsync(id, reason, CurrentUserId, ct);
                SetSuccessMessage("Item rejected.");
            }
            catch (ConflictException ex)
            {
                SetErrorMessage(ex.Message);
            }

            return RedirectToAction(nameof(Tree), new { shipmentId });
        }

        [HttpGet]
        [Authorize(Policy = "ManagerAndAbove")]
        public async Task<IActionResult> PendingApprovals(CancellationToken ct = default)
        {
            var items = await _processService.GetPendingApprovalsAsync(ct);
            return View(items);
        }
    }
}