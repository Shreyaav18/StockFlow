using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Web.Common;
using StockFlow.Web.DTOs.Shipment;
using StockFlow.Web.Exceptions;
using StockFlow.Web.Services.Interfaces;

namespace StockFlow.Web.Controllers
{
    [Authorize(Policy = "AllStaff")]
    public class ShipmentController : BaseController
    {
        private readonly IShipmentService _shipmentService;
        private readonly IItemService _itemService;
        private readonly ISearchService _searchService;

        public ShipmentController(
            IShipmentService shipmentService,
            IItemService itemService,
            ISearchService searchService)
        {
            _shipmentService = shipmentService;
            _itemService = itemService;
            _searchService = searchService;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] PagedQuery query, string? search, CancellationToken ct = default)
        {
            if (!string.IsNullOrWhiteSpace(search) && search.Length >= 2)
            {
                var results = await _searchService.SearchShipmentsAsync(search, ct);
                ViewBag.Search = search;
                ViewBag.Shipments = results;
                return View();
            }

            var shipments = await _shipmentService.GetAllAsync(ct);
            ViewBag.Shipments = shipments;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Detail(int id, CancellationToken ct = default)
        {
            try
            {
                var shipment = await _shipmentService.GetByIdAsync(id, ct);
                return View(shipment);
            }
            catch (NotFoundException)
            {
                SetErrorMessage("Shipment not found.");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Receive(CancellationToken ct = default)
        {
            ViewBag.Items = await _itemService.GetAllAsync(ct);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Receive(CreateShipmentDto dto, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Items = await _itemService.GetAllAsync(ct);
                return View(dto);
            }

            try
            {
                var shipment = await _shipmentService.ReceiveAsync(dto, CurrentUserId, ct);
                SetSuccessMessage($"Shipment #{shipment.ShipmentId} received successfully.");
                return RedirectToAction(nameof(Detail), new { id = shipment.ShipmentId });
            }
            catch (NotFoundException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                ViewBag.Items = await _itemService.GetAllAsync(ct);
                return View(dto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Pending(CancellationToken ct = default)
        {
            var pending = await _shipmentService.GetPendingAsync(ct);
            return View(pending);
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
        {
            try
            {
                await _shipmentService.DeleteAsync(id, CurrentUserId, ct);
                SetSuccessMessage("Shipment deleted successfully.");
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
    }
}