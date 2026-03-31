using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Web.DTOs.Search;
using StockFlow.Web.Exceptions;
using StockFlow.Web.Services.Interfaces;

namespace StockFlow.Web.Controllers
{
    [Authorize(Policy = "AllStaff")]
    public class SearchController : BaseController
    {
        private readonly ISearchService _searchService;

        public SearchController(ISearchService searchService)
        {
            _searchService = searchService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(SearchDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Query) || dto.Query.Length < 2)
                return View();

            try
            {
                var results = await _searchService.SearchAsync(dto, ct);
                return View(results);
            }
            catch (ValidationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Quick(string q, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return JsonFail("Query too short.", 400);

            try
            {
                var dto = new SearchDto { Query = q };
                var results = await _searchService.SearchAsync(dto, ct);
                return JsonSuccess(results);
            }
            catch (ValidationException ex)
            {
                return JsonFail(ex.Message);
            }
        }
    }
}