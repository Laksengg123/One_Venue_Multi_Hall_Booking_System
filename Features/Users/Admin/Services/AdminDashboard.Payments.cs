using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VenueBookingSystem.Features.Bookings;
using VenueBookingSystem.Features.Payments;
using VenueBookingSystem.Shared;
using Xceed.Document.NET;
using Xceed.Words.NET;
using ClosedXML.Excel;

namespace VenueBookingSystem.Features.Admin
{
    public partial class AdminDashboard
    {
        private async Task ShowPaymentManagementAsync()
        {
            bool back = false;
            while (!back)
            {
                Console.Clear();
                int pendingRefunds = await _payments.GetPendingRefundCountAsync();
                ConsoleHelper.PrintHeader($"Payment Management  |  Pending Refunds: {pendingRefunds}");

                string[] options = {
                    "View Payment Details",
                    "Process Refund",
                    "Generate Invoice",
                    "Export Payments Report"
                };

                ConsoleHelper.PrintMenu("Payment Options", options);
                int choice = ValidationHelper.ReadMenuChoice(4);

                switch (choice)
                {
                    case 0:
                        back = true;
                        break;
                    case 1:
                        await ViewPaymentDetailsDirectAsync();
                        break;
                    case 2:
                        await ProcessRefundDirectAsync();
                        break;
                    case 3:
                        await GenerateInvoiceDirectAsync();
                        break;
                    case 4:
                        await ExportPaymentsAsync();
                        break;
                }
            }
        }

        private async Task ViewPaymentDetailsDirectAsync()
        {
            var payments = await _payments.GetAllPaymentsAsync();
            int selection = ConsoleHelper.ShowPaginatedTable(payments, PaymentService.TableColumns, "Select Payment to View");
            if (selection == 0) return;

            var payment = payments[selection - 1];
            ShowPaymentDetails(payment);
            ConsoleHelper.PressAnyKey();
        }

        private static void ShowPaymentDetails(Payment payment)
        {
            Console.Clear();
            ConsoleHelper.PrintHeader($"Payment Details — #{payment.PaymentId}");
            ConsoleHelper.PrintReadOnlyId("Payment ID", payment.PaymentId.ToString());
            ConsoleHelper.PrintNonEditableField("Booking ID", payment.BookingId.ToString());
            ConsoleHelper.PrintNonEditableField("Amount", ConsoleHelper.FormatCurrency(payment.Amount));
            ConsoleHelper.PrintNonEditableField("Method", payment.GetMethodDisplay());
            ConsoleHelper.PrintNonEditableField("Status", payment.GetStatusDisplay());
            ConsoleHelper.PrintNonEditableField("Transaction Ref", payment.TransactionRef ?? "—");
            ConsoleHelper.PrintNonEditableField("Paid At", payment.PaidAt.HasValue ? ConsoleHelper.FormatDateTime(payment.PaidAt.Value) : "—");
            ConsoleHelper.PrintNonEditableField("Created At", ConsoleHelper.FormatDateTime(payment.CreatedAt));
        }

        private async Task ProcessRefundDirectAsync()
        {
            var refundCandidates = await _payments.GetPendingRefundsAsync();
            if (refundCandidates.Count == 0)
            {
                Console.Clear();
                ConsoleHelper.PrintHeader("Process Refund");
                ConsoleHelper.PrintWarning("No pending refunds are available. Customer cancellations will appear here once recorded.");
                ConsoleHelper.PressAnyKey();
                return;
            }

            int selection = ConsoleHelper.ShowPaginatedTable(
                refundCandidates,
                PaymentService.RefundColumns,
                "Select Cancellation to Refund");

            if (selection == 0) return;

            var candidate = refundCandidates[selection - 1];

            Console.Clear();
            ConsoleHelper.PrintHeader($"Process Refund - Cancellation #{candidate.CancellationId}");
            ConsoleHelper.PrintNonEditableField("Booking ID", candidate.BookingId.ToString());
            ConsoleHelper.PrintNonEditableField("Customer", candidate.CustomerName);
            ConsoleHelper.PrintNonEditableField("Hall", candidate.HallName);
            ConsoleHelper.PrintNonEditableField("Approved Refund", ConsoleHelper.FormatCurrency(candidate.RefundAmount));
            ConsoleHelper.PrintNonEditableField("Cancelled At", ConsoleHelper.FormatDateTime(candidate.CancellationDate));
            ConsoleHelper.PrintNonEditableField("Refund Status", candidate.RefundStatus);
            ConsoleHelper.PrintNonEditableField("Reason", candidate.Reason);
            Console.WriteLine();

            decimal refundAmount = ValidationHelper.ReadDecimal(
                "Refund Amount",
                0.01m,
                candidate.RefundAmount,
                $"0.01 - {candidate.RefundAmount:F2}");

            if (refundAmount == decimal.MinValue) return;

            string remarks = ConsoleHelper.ReadInput("Refund Remarks", "optional", required: false);
            if (remarks == ConsoleHelper.BACK_COMMAND) return;

            if (!ConsoleHelper.Confirm($"Process refund of {ConsoleHelper.FormatCurrency(refundAmount)}?")) return;

            try
            {
                var processedBy = _auth.CurrentUser?.Username ?? "admin";
                var result = await _payments.ProcessRefundAsync(
                    candidate.CancellationId,
                    refundAmount,
                    processedBy,
                    remarks);

                if (result is null)
                {
                    ConsoleHelper.PrintError("Refund could not be processed.");
                }
                else
                {
                    ConsoleHelper.PrintSuccess("Refund processed successfully.");
                    ConsoleHelper.PrintReadOnlyId("Refund ID", result.RefundId.ToString());
                    ConsoleHelper.PrintNonEditableField("Booking ID", result.BookingId.ToString());
                    ConsoleHelper.PrintNonEditableField("Refund Amount", ConsoleHelper.FormatCurrency(result.RefundAmount));
                    ConsoleHelper.PrintNonEditableField("Status", result.RefundStatus);
                    ConsoleHelper.PrintNonEditableField("Processed By", result.ProcessedBy);
                    ConsoleHelper.PrintInfo("This cancellation has been removed from the pending refund queue.");
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.PrintError(ex.Message);
            }

            ConsoleHelper.PressAnyKey();
        }

        private async Task GenerateInvoiceDirectAsync()
        {
            Console.Clear();
            ConsoleHelper.PrintHeader("Generate Invoice");

            int bookingId = ValidationHelper.ReadInt("Booking ID", 1, int.MaxValue, "numeric booking id");
            if (bookingId == int.MinValue) return;

            try
            {
                var invoice = await _payments.GenerateInvoiceAsync(bookingId);
                if (invoice is null)
                {
                    ConsoleHelper.PrintError("Invoice could not be generated.");
                }
                else
                {
                    ConsoleHelper.PrintSuccess("Invoice ready.");
                    ConsoleHelper.PrintReadOnlyId("Invoice ID", invoice.InvoiceId.ToString());
                    ConsoleHelper.PrintNonEditableField("Invoice Number", invoice.InvoiceNumber);
                    ConsoleHelper.PrintNonEditableField("Booking ID", invoice.BookingId.ToString());
                    ConsoleHelper.PrintNonEditableField("Amount", ConsoleHelper.FormatCurrency(invoice.TotalAmount));
                    ConsoleHelper.PrintNonEditableField("Issued To", invoice.IssuedTo);
                    ConsoleHelper.PrintNonEditableField("Generated", ConsoleHelper.FormatDateTime(invoice.GeneratedDate));
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.PrintError(ex.Message);
            }

            ConsoleHelper.PressAnyKey();
        }

        private async Task ExportPaymentsAsync()
        {
            Console.Clear();
            ConsoleHelper.PrintHeader("Export Payments Report");
            string[] options = { "Export as TXT", "Export as DOCX", "Export as XLSX" };
            ConsoleHelper.PrintMenu("Export Formats", options);
            
            int choice = ValidationHelper.ReadMenuChoice(3);
            if (choice == 0) return;

            var payments = await _payments.GetAllPaymentsAsync();
            if (payments.Count == 0)
            {
                ConsoleHelper.PrintWarning("No payments available to export.");
                ConsoleHelper.PressAnyKey();
                return;
            }

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            try
            {
                if (choice == 1)
                {
                    string path = Path.Combine(baseDir, $"Payments_{timestamp}.txt");
                    using (StreamWriter sw = new StreamWriter(path))
                    {
                        sw.WriteLine("PAYMENTS REPORT");
                        sw.WriteLine($"Generated: {DateTime.Now}");
                        sw.WriteLine(new string('-', 50));
                        foreach (var p in payments)
                        {
                            sw.WriteLine($"ID: {p.PaymentId} | Booking: {p.BookingId} | Amount: {p.Amount:F2} | Status: {p.Status}");
                        }
                    }
                    ConsoleHelper.PrintSuccess($"Exported to {path}");
                }
                else if (choice == 2)
                {
                    string path = Path.Combine(baseDir, $"Payments_{timestamp}.docx");
                    using (var document = DocX.Create(path))
                    {
                        document.InsertParagraph("Payments Report").FontSize(20).Bold();
                        document.InsertParagraph($"Generated: {DateTime.Now}");
                        var t = document.AddTable(payments.Count + 1, 4);
                        t.Rows[0].Cells[0].Paragraphs[0].Append("Payment ID").Bold();
                        t.Rows[0].Cells[1].Paragraphs[0].Append("Booking ID").Bold();
                        t.Rows[0].Cells[2].Paragraphs[0].Append("Amount").Bold();
                        t.Rows[0].Cells[3].Paragraphs[0].Append("Status").Bold();
                        for (int i = 0; i < payments.Count; i++)
                        {
                            t.Rows[i+1].Cells[0].Paragraphs[0].Append(payments[i].PaymentId.ToString());
                            t.Rows[i+1].Cells[1].Paragraphs[0].Append(payments[i].BookingId.ToString());
                            t.Rows[i+1].Cells[2].Paragraphs[0].Append(payments[i].Amount.ToString("F2"));
                            t.Rows[i+1].Cells[3].Paragraphs[0].Append(payments[i].Status.ToString());
                        }
                        document.InsertTable(t);
                        document.Save();
                    }
                    ConsoleHelper.PrintSuccess($"Exported to {path}");
                }
                else if (choice == 3)
                {
                    string path = Path.Combine(baseDir, $"Payments_{timestamp}.xlsx");
                    using (var wb = new XLWorkbook())
                    {
                        var ws = wb.Worksheets.Add("Payments");
                        ws.Cell(1,1).Value = "Payment ID";
                        ws.Cell(1,2).Value = "Booking ID";
                        ws.Cell(1,3).Value = "Amount";
                        ws.Cell(1,4).Value = "Status";
                        for (int i = 0; i < payments.Count; i++)
                        {
                            ws.Cell(i+2, 1).Value = payments[i].PaymentId;
                            ws.Cell(i+2, 2).Value = payments[i].BookingId;
                            ws.Cell(i+2, 3).Value = payments[i].Amount;
                            ws.Cell(i+2, 4).Value = payments[i].Status.ToString();
                        }
                        wb.SaveAs(path);
                    }
                    ConsoleHelper.PrintSuccess($"Exported to {path}");
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.PrintError($"Failed to export: {ex.Message}");
            }
            
            ConsoleHelper.PressAnyKey();
        }
    }
}
