using ClosedXML.Excel;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Serilog;
using StockFlow.Web.DTOs.Process;
using StockFlow.Web.Exceptions;
using StockFlow.Web.Services.Interfaces;
using iText.Kernel.Font;
using iText.IO.Font.Constants;

namespace StockFlow.Web.Services
{
    public class ExportService : IExportService
    {
        private readonly ITreeBuilderService _treeBuilder;
        private readonly IReportService _reportService;

        public ExportService(ITreeBuilderService treeBuilder, IReportService reportService)
        {
            _treeBuilder = treeBuilder;
            _reportService = reportService;
        }

        public async Task<byte[]> ExportTreeToPdfAsync(int shipmentId, CancellationToken ct = default)
        {
            try
            {
                var tree = await _treeBuilder.BuildTreeAsync(shipmentId, ct);

                using var ms = new MemoryStream();
                using var writer = new PdfWriter(ms);
                using var pdf = new PdfDocument(writer);
                using var doc = new Document(pdf);

                var boldFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);
                var regularFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);

                doc.Add(new Paragraph($"Shipment Tree — ID: {shipmentId}")
                    .SetFont(boldFont).SetFontSize(16));
                doc.Add(new Paragraph($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC")
                    .SetFont(regularFont).SetFontSize(10));
                doc.Add(new Paragraph(" "));

                AppendTreeNodeToPdf(doc, tree, 0, regularFont, boldFont);

                doc.Close();
                return ms.ToArray();
            }
            catch (AppException) { throw; }
            catch (Exception ex)
            {
                Log.Error(ex, "Error exporting tree to PDF for shipment {ShipmentId}", shipmentId);
                throw new ExportException();
            }
        }

        public async Task<byte[]> ExportTreeToExcelAsync(int shipmentId, CancellationToken ct = default)
        {
            try
            {
                var tree = await _treeBuilder.BuildTreeAsync(shipmentId, ct);

                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Tree");

                ws.Cell(1, 1).Value = "Level";
                ws.Cell(1, 2).Value = "Item Name";
                ws.Cell(1, 3).Value = "SKU";
                ws.Cell(1, 4).Value = "Input Weight";
                ws.Cell(1, 5).Value = "Output Weight";
                ws.Cell(1, 6).Value = "Unit";
                ws.Cell(1, 7).Value = "Status";

                var headerRow = ws.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                var row = 2;
                AppendTreeNodeToExcel(ws, tree, 0, ref row);

                ws.Columns().AdjustToContents();

                using var ms = new MemoryStream();
                wb.SaveAs(ms);
                return ms.ToArray();
            }
            catch (AppException) { throw; }
            catch (Exception ex)
            {
                Log.Error(ex, "Error exporting tree to Excel for shipment {ShipmentId}", shipmentId);
                throw new ExportException();
            }
        }

        public async Task<byte[]> ExportReportToPdfAsync(DateTime from, DateTime to, CancellationToken ct = default)
        {
            try
            {
                var reports = await _reportService.GetRangeReportAsync(from, to, ct);

                using var ms = new MemoryStream();
                using var writer = new PdfWriter(ms);
                using var pdf = new PdfDocument(writer);
                using var doc = new Document(pdf);

                var boldFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);
                var regularFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);

                doc.Add(new Paragraph($"Processing Report: {from:yyyy-MM-dd} to {to:yyyy-MM-dd}")
                    .SetFont(boldFont).SetFontSize(16));
                doc.Add(new Paragraph($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC")
                    .SetFont(regularFont).SetFontSize(10));
                doc.Add(new Paragraph(" "));

                foreach (var report in reports)
                {
                    doc.Add(new Paragraph($"Date: {report.Date:yyyy-MM-dd}").SetFont(boldFont).SetFontSize(12));
                    doc.Add(new Paragraph($"Shipments Received: {report.TotalShipmentsReceived}").SetFont(regularFont).SetFontSize(11));
                    doc.Add(new Paragraph($"Items Processed: {report.TotalItemsProcessed}").SetFont(regularFont).SetFontSize(11));
                    doc.Add(new Paragraph($"Approved: {report.TotalApproved} | Rejected: {report.TotalRejected} | Pending: {report.TotalPending}").SetFont(regularFont).SetFontSize(11));
                    doc.Add(new Paragraph($"Total Output Weight: {report.TotalOutputWeight:F2} | Weight Loss: {report.WeightLoss:F2}").SetFont(regularFont).SetFontSize(11));
                    doc.Add(new Paragraph(" "));
                }

                doc.Close();
                return ms.ToArray();
            }
            catch (AppException) { throw; }
            catch (Exception ex)
            {
                Log.Error(ex, "Error exporting report to PDF from {From} to {To}", from, to);
                throw new ExportException();
            }
        }

        public async Task<byte[]> ExportReportToExcelAsync(DateTime from, DateTime to, CancellationToken ct = default)
        {
            try
            {
                var reports = await _reportService.GetRangeReportAsync(from, to, ct);

                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Report");

                ws.Cell(1, 1).Value = "Date";
                ws.Cell(1, 2).Value = "Shipments Received";
                ws.Cell(1, 3).Value = "Items Processed";
                ws.Cell(1, 4).Value = "Approved";
                ws.Cell(1, 5).Value = "Rejected";
                ws.Cell(1, 6).Value = "Pending";
                ws.Cell(1, 7).Value = "Total Output Weight";
                ws.Cell(1, 8).Value = "Weight Loss";

                var headerRow = ws.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                var row = 2;
                foreach (var report in reports)
                {
                    ws.Cell(row, 1).Value = report.Date.ToString("yyyy-MM-dd");
                    ws.Cell(row, 2).Value = report.TotalShipmentsReceived;
                    ws.Cell(row, 3).Value = report.TotalItemsProcessed;
                    ws.Cell(row, 4).Value = report.TotalApproved;
                    ws.Cell(row, 5).Value = report.TotalRejected;
                    ws.Cell(row, 6).Value = report.TotalPending;
                    ws.Cell(row, 7).Value = report.TotalOutputWeight;
                    ws.Cell(row, 8).Value = report.WeightLoss;
                    row++;
                }

                ws.Columns().AdjustToContents();

                using var ms = new MemoryStream();
                wb.SaveAs(ms);
                return ms.ToArray();
            }
            catch (AppException) { throw; }
            catch (Exception ex)
            {
                Log.Error(ex, "Error exporting report to Excel from {From} to {To}", from, to);
                throw new ExportException();
            }
        }

        private static void AppendTreeNodeToPdf(Document doc, TreeNodeViewModel node, int depth, PdfFont regular, PdfFont bold)
        {
            var indent = new string(' ', depth * 4);
            doc.Add(new Paragraph($"{indent}[{node.Status}] {node.ItemName} ({node.SKU}) — In: {node.InputWeight:F2} | Out: {node.OutputWeight:F2} {node.Unit}")
                .SetFont(regular).SetFontSize(11));

            foreach (var child in node.Children)
                AppendTreeNodeToPdf(doc, child, depth + 1, regular, bold);
        }

        private static void AppendTreeNodeToExcel(IXLWorksheet ws, TreeNodeViewModel node, int depth, ref int row)
        {
            ws.Cell(row, 1).Value = depth;
            ws.Cell(row, 2).Value = node.ItemName;
            ws.Cell(row, 3).Value = node.SKU;
            ws.Cell(row, 4).Value = node.InputWeight;
            ws.Cell(row, 5).Value = node.OutputWeight;
            ws.Cell(row, 6).Value = node.Unit;
            ws.Cell(row, 7).Value = node.Status;
            row++;

            foreach (var child in node.Children)
                AppendTreeNodeToExcel(ws, child, depth + 1, ref row);
        }
    }
}