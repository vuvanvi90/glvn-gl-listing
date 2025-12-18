using ClosedXML.Excel;
using DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WpfApp.Services
{
    public interface IExcelExportService
    {
        void ExportGLTransactions(List<GLTransaction> transactions, string filePath, ExcelExportOptions options = null);

        void ExportVASTransactions(List<GLTransaction> transactions, string filePath, ExcelExportOptions options = null);
    }

    public class ExcelExportOptions
    {
        /// <summary>
        /// Enable AutoFilter on header row (default: true)
        /// </summary>
        public bool EnableAutoFilter { get; set; } = true;

        /// <summary>
        /// Remove table formatting/styling (default: true)
        /// </summary>
        public bool RemoveTableFormat { get; set; } = true;

        /// <summary>
        /// Freeze header row (default: true)
        /// </summary>
        public bool FreezeHeaderRow { get; set; } = true;

        /// <summary>
        /// Make header row bold (default: true)
        /// </summary>
        public bool BoldHeader { get; set; } = true;

        /// <summary>
        /// Add header background color (default: LightGray)
        /// </summary>
        public XLColor? HeaderBackgroundColor { get; set; } = XLColor.LightGray;

        /// <summary>
        /// Auto-fit column widths (default: true)
        /// </summary>
        public bool AutoFitColumns { get; set; } = true;

        /// <summary>
        /// Minimum column width (default: 8)
        /// </summary>
        public double MinColumnWidth { get; set; } = 8;

        /// <summary>
        /// Maximum column width (default: 60)
        /// </summary>
        public double MaxColumnWidth { get; set; } = 60;

        /// <summary>
        /// Apply number format to numeric columns (default: true)
        /// </summary>
        public bool FormatNumbers { get; set; } = true;

        /// <summary>
        /// Number format for Debit/Credit/Total columns (default: "#,##0.00")
        /// </summary>
        public string NumberFormat { get; set; } = "#,##0.00";
        //NumberFormat = "#,##0.00"                 // 1,234.56 (✓)
        //NumberFormat = "#,##0"                    // 1,235 (no decimal)
        //NumberFormat = "0.00"                     // 1234.56 (no comma)
        //NumberFormat = "#,##0.00;[Red]-#,##0.00"  // Negative in red
        //NumberFormat = "$#,##0.00"                // $1,234.56 (with currency)
        //NumberFormat = "#,##0.00_);(#,##0.00)"    // Accounting format
    }

    public class ExcelExportService : IExcelExportService
    {
        public void ExportGLTransactions(List<GLTransaction> transactions, string filePath, ExcelExportOptions options = null)
        {
            // Use default options if not provided
            options ??= new ExcelExportOptions();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Sheet1");

            // Define column headers manually
            var headers = new[]
            {
                "CompanyCode", "PostMonth", "Date", "JournalNumber", "AccountCode", "AccountName",
                "VASAccount", "Description", "Debit", "Credit", "Total", "Ref1", "Ref2", 
                "Allocation1", "Allocation2", "Allocation3", "Allocation4", "Allocation5",
                "SupplierCode", "SupplierName", "UserLastModified", "DateLastModified"
            };

            // Write headers (Row 1)
            for (int col = 0; col < headers.Length; col++)
            {
                worksheet.Cell(1, col + 1).Value = headers[col];
            }

            // Write data rows (starting from Row 2)
            int currentRow = 2;
            foreach (var transaction in transactions)
            {
                worksheet.Cell(currentRow, 1).Value = transaction.CompanyCode ?? "";
                worksheet.Cell(currentRow, 2).Value = transaction.PostMonth ?? "";
                worksheet.Cell(currentRow, 3).Value = transaction.Date ?? "";
                worksheet.Cell(currentRow, 4).Value = transaction.JournalNumber ?? "";
                worksheet.Cell(currentRow, 5).Value = transaction.AccountCode ?? "";
                worksheet.Cell(currentRow, 6).Value = transaction.AccountName ?? "";
                worksheet.Cell(currentRow, 7).Value = transaction.VASAccount ?? "";
                worksheet.Cell(currentRow, 8).Value = transaction.Description ?? "";
                worksheet.Cell(currentRow, 9).Value = transaction.Debit;
                worksheet.Cell(currentRow, 10).Value = transaction.Credit;
                worksheet.Cell(currentRow, 11).Value = transaction.Total;
                worksheet.Cell(currentRow, 12).Value = transaction.Ref1 ?? "";
                worksheet.Cell(currentRow, 13).Value = transaction.Ref2 ?? "";
                worksheet.Cell(currentRow, 14).Value = transaction.Allocation1 ?? "";
                worksheet.Cell(currentRow, 15).Value = transaction.Allocation2 ?? "";
                worksheet.Cell(currentRow, 16).Value = transaction.Allocation3 ?? "";
                worksheet.Cell(currentRow, 17).Value = transaction.Allocation4 ?? "";
                worksheet.Cell(currentRow, 18).Value = transaction.Allocation5 ?? "";
                worksheet.Cell(currentRow, 19).Value = transaction.SupplierCode ?? "";
                worksheet.Cell(currentRow, 20).Value = transaction.SupplierName ?? "";
                worksheet.Cell(currentRow, 21).Value = transaction.UserLastModified ?? "";
                worksheet.Cell(currentRow, 22).Value = transaction.DateLastModified ?? "";

                currentRow++;
            }

            // Get the data range
            var lastRow = transactions.Count + 1; // +1 for header
            var lastColumn = headers.Length;
            var dataRange = worksheet.Range(1, 1, lastRow, lastColumn);

            // Enable AutoFilter
            if (options.EnableAutoFilter)
            {
                worksheet.Range(1, 1, 1, lastColumn).SetAutoFilter();
            }

            // Style header row
            var headerRow = worksheet.Row(1);

            if (options.BoldHeader)
            {
                headerRow.Style.Font.Bold = true;
            }

            if (options.HeaderBackgroundColor is XLColor bg)
            {
                headerRow.Style.Fill.BackgroundColor = bg;
                headerRow.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }

            headerRow.Height = 20;

            // Freeze header row
            if (options.FreezeHeaderRow)
            {
                worksheet.SheetView.FreezeRows(1);
            }

            // Auto-fit columns
            if (options.AutoFitColumns)
            {
                worksheet.Columns().AdjustToContents(options.MinColumnWidth, options.MaxColumnWidth);
            }

            // Apply number formatting to numeric columns
            if (options.FormatNumbers)
            {
                // Debit column (column 9)
                worksheet.Column(9).Style.NumberFormat.Format = options.NumberFormat;
                worksheet.Column(9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                // Credit column (column 10)
                worksheet.Column(10).Style.NumberFormat.Format = options.NumberFormat;
                worksheet.Column(10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                // Total column (column 11)
                worksheet.Column(11).Style.NumberFormat.Format = options.NumberFormat;
                worksheet.Column(11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                //worksheet.Column(11).Style.Font.Bold = true;
            }

            // Set specific column widths for better readability
            worksheet.Column(1).Width = 5;
            worksheet.Column(8).Width = 30; // Description - wider
            worksheet.Column(5).Width = 10;
            worksheet.Column(6).Width = 20;  // Account Name
            worksheet.Column(7).Width = 10;
            worksheet.Column(12).Width = 5;
            worksheet.Column(13).Width = 5;
            worksheet.Column(14).Width = 5;
            worksheet.Column(15).Width = 5;
            worksheet.Column(16).Width = 5;
            worksheet.Column(17).Width = 5;
            worksheet.Column(18).Width = 5;
            worksheet.Column(21).Width = 20; // Supplier Name

            // Add worksheet properties
            worksheet.Author = "Vu Van Vi";
            worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            worksheet.PageSetup.FitToPages(1, 0); // Fit to 1 page wide

            // Save workbook
            workbook.SaveAs(filePath);
        }

        public void ExportVASTransactions(List<GLTransaction> transactions, string filePath, ExcelExportOptions options = null)
        {
            // Use default options if not provided
            options ??= new ExcelExportOptions();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Sheet1");

            // Define column headers manually
            var headers = new[]
            {
                "CompanyCode", "PostMonth", "Date", "JournalNumber", "AccountCode", "AccountName",
                "VASAccount", "CorrespondingAccount", "Description", "Debit", "Credit", "Total", 
                "Ref1", "Ref2", 
                "Allocation1", "Allocation2", "Allocation3", "Allocation4", "Allocation5",
                "SupplierCode", "SupplierName", "UserLastModified", "DateLastModified"
            };

            // Write headers (Row 1)
            for (int col = 0; col < headers.Length; col++)
            {
                worksheet.Cell(1, col + 1).Value = headers[col];
            }

            // Write data rows (starting from Row 2)
            int currentRow = 2;
            foreach (var transaction in transactions)
            {
                worksheet.Cell(currentRow, 1).Value = transaction.CompanyCode ?? "";
                worksheet.Cell(currentRow, 2).Value = transaction.PostMonth ?? "";
                worksheet.Cell(currentRow, 3).Value = transaction.Date ?? "";
                worksheet.Cell(currentRow, 4).Value = transaction.JournalNumber ?? "";
                worksheet.Cell(currentRow, 5).Value = transaction.AccountCode ?? "";
                worksheet.Cell(currentRow, 6).Value = transaction.AccountName ?? "";
                worksheet.Cell(currentRow, 7).Value = transaction.VASAccount ?? "";
                worksheet.Cell(currentRow, 8).Value = transaction.CorrespondingAccount ?? "";
                worksheet.Cell(currentRow, 9).Value = transaction.Description ?? "";
                worksheet.Cell(currentRow, 10).Value = transaction.Debit;
                worksheet.Cell(currentRow, 11).Value = transaction.Credit;
                worksheet.Cell(currentRow, 12).Value = transaction.Total;
                worksheet.Cell(currentRow, 13).Value = transaction.Ref1 ?? "";
                worksheet.Cell(currentRow, 14).Value = transaction.Ref2 ?? "";
                worksheet.Cell(currentRow, 15).Value = transaction.Allocation1 ?? "";
                worksheet.Cell(currentRow, 16).Value = transaction.Allocation2 ?? "";
                worksheet.Cell(currentRow, 17).Value = transaction.Allocation3 ?? "";
                worksheet.Cell(currentRow, 18).Value = transaction.Allocation4 ?? "";
                worksheet.Cell(currentRow, 19).Value = transaction.Allocation5 ?? "";
                worksheet.Cell(currentRow, 20).Value = transaction.SupplierCode ?? "";
                worksheet.Cell(currentRow, 21).Value = transaction.SupplierName ?? "";
                worksheet.Cell(currentRow, 22).Value = transaction.UserLastModified ?? "";
                worksheet.Cell(currentRow, 23).Value = transaction.DateLastModified ?? "";

                currentRow++;
            }

            // Get the data range
            var lastRow = transactions.Count + 1; // +1 for header
            var lastColumn = headers.Length;
            var dataRange = worksheet.Range(1, 1, lastRow, lastColumn);

            // Enable AutoFilter
            if (options.EnableAutoFilter)
            {
                worksheet.Range(1, 1, 1, lastColumn).SetAutoFilter();
            }

            // Style header row
            var headerRow = worksheet.Row(1);

            if (options.BoldHeader)
            {
                headerRow.Style.Font.Bold = true;
            }

            if (options.HeaderBackgroundColor is XLColor bg)
            {
                headerRow.Style.Fill.BackgroundColor = bg;
                headerRow.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }

            headerRow.Height = 20;

            // Freeze header row
            if (options.FreezeHeaderRow)
            {
                worksheet.SheetView.FreezeRows(1);
            }

            // Auto-fit columns
            if (options.AutoFitColumns)
            {
                worksheet.Columns().AdjustToContents(options.MinColumnWidth, options.MaxColumnWidth);
            }

            // Apply number formatting to numeric columns
            if (options.FormatNumbers)
            {
                // Debit column (column 9)
                worksheet.Column(10).Style.NumberFormat.Format = options.NumberFormat;
                worksheet.Column(10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                // Credit column (column 10)
                worksheet.Column(11).Style.NumberFormat.Format = options.NumberFormat;
                worksheet.Column(11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                // Total column (column 11)
                worksheet.Column(12).Style.NumberFormat.Format = options.NumberFormat;
                worksheet.Column(12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                //worksheet.Column(11).Style.Font.Bold = true;
            }

            // Set specific column widths for better readability
            worksheet.Column(1).Width = 5;
            worksheet.Column(9).Width = 30; // Description - wider
            worksheet.Column(5).Width = 10;
            worksheet.Column(6).Width = 20;  // Account Name
            worksheet.Column(7).Width = 10;
            worksheet.Column(8).Width = 10;
            worksheet.Column(13).Width = 5;
            worksheet.Column(14).Width = 5;
            worksheet.Column(15).Width = 5;
            worksheet.Column(16).Width = 5;
            worksheet.Column(17).Width = 5;
            worksheet.Column(18).Width = 5;
            worksheet.Column(19).Width = 5;
            worksheet.Column(21).Width = 30; // Supplier Name

            // Add worksheet properties
            worksheet.Author = "Vu Van Vi";
            worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            worksheet.PageSetup.FitToPages(1, 0); // Fit to 1 page wide

            // Save workbook
            workbook.SaveAs(filePath);
        }
    }
}
