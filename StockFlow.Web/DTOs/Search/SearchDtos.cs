using System.ComponentModel.DataAnnotations;
using StockFlow.Web.DTOs.Item;
using StockFlow.Web.DTOs.Shipment;
using StockFlow.Web.DTOs.Process;

namespace StockFlow.Web.DTOs.Search
{
    public class SearchDto
    {
        [Required, MinLength(2)]
        public string Query { get; set; } = string.Empty;

        public bool SearchItems { get; set; } = true;
        public bool SearchShipments { get; set; } = true;
        public bool SearchProcessedItems { get; set; } = true;
    }

    public class SearchResultViewModel
    {
        public string Query { get; set; } = string.Empty;
        public int TotalResults { get; set; }
        public bool SearchItems { get; set; } = true;
        public bool SearchShipments { get; set; } = true;
        public bool SearchProcessedItems { get; set; } = true;
        public IEnumerable<ItemViewModel> Items { get; set; } = Enumerable.Empty<ItemViewModel>();
        public IEnumerable<ShipmentViewModel> Shipments { get; set; } = Enumerable.Empty<ShipmentViewModel>();
        public IEnumerable<ProcessedItemViewModel> ProcessedItems { get; set; } = Enumerable.Empty<ProcessedItemViewModel>();
    }   
}