using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Web.Common;
using StockFlow.Web.DTOs.Item;
using StockFlow.Web.Exceptions;
using StockFlow.Web.Services.Interfaces;

namespace StockFlow.Web.Controllers
{
    [Authorize(Policy = "AllStaff")]
    public class ItemController : BaseController
    {
        private readonly IItemService _itemService;
        private readonly ISearchService _searchService;

        public ItemController(IItemService itemService, ISearchService searchService)
        {
            _itemService = itemService;
            _searchService = searchService;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] PagedQuery query, string? search, CancellationToken ct = default)
        {
            if (!string.IsNullOrWhiteSpace(search) && search.Length >= 2)
            {
                var results = await _searchService.SearchItemsAsync(search, ct);
                ViewBag.Search = search;
                ViewBag.Items = results;
                return View();
            }

            var items = await _itemService.GetAllAsync(ct);
            ViewBag.Items = items;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Detail(int id, CancellationToken ct = default)
        {
            try
            {
                var item = await _itemService.GetByIdAsync(id, ct);
                return View(item);
            }
            catch (NotFoundException)
            {
                SetErrorMessage("Item not found.");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [Authorize(Policy = "ManagerAndAbove")]
        public IActionResult Create()
            => View();

        [HttpPost]
        [Authorize(Policy = "ManagerAndAbove")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateItemDto dto, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                var item = await _itemService.CreateAsync(dto, CurrentUserId, ct);
                SetSuccessMessage($"Item '{item.ItemName}' created successfully.");
                return RedirectToAction(nameof(Index));
            }
            catch (ConflictException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(dto);
            }
        }

        [HttpGet]
        [Authorize(Policy = "ManagerAndAbove")]
        public async Task<IActionResult> Edit(int id, CancellationToken ct = default)
        {
            try
            {
                var item = await _itemService.GetByIdAsync(id, ct);
                var dto = new UpdateItemDto
                {
                    ItemName = item.ItemName,
                    Unit = item.Unit,
                    IsActive = item.IsActive
                };
                ViewBag.ItemId = id;
                return View(dto);
            }
            catch (NotFoundException)
            {
                SetErrorMessage("Item not found.");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [Authorize(Policy = "ManagerAndAbove")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UpdateItemDto dto, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ItemId = id;
                return View(dto);
            }

            try
            {
                await _itemService.UpdateAsync(id, dto, CurrentUserId, ct);
                SetSuccessMessage("Item updated successfully.");
                return RedirectToAction(nameof(Index));
            }
            catch (NotFoundException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
        {
            try
            {
                await _itemService.DeleteAsync(id, CurrentUserId, ct);
                SetSuccessMessage("Item deleted successfully.");
            }
            catch (ConflictException ex)
            {
                SetErrorMessage(ex.Message);
            }
            catch (NotFoundException ex)
            {
                SetErrorMessage(ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> CheckSKU(string sku, int? excludeId, CancellationToken ct = default)
        {
            var exists = await _itemService.SKUExistsAsync(sku, excludeId, ct);
            return JsonSuccess(new { exists });
        }
    }
}