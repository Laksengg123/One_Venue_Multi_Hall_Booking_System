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
        // ─────────────────────────────────────────────────────────────────────
        // PAYMENT MANAGEMENT MENU
        // ─────────────────────────────────────────────────────────────────────
        private async Task ShowPaymentManagementAsync()
        {
            bool back = false;
            while (!back)
            {
                Console.Clear();
                int pendingRefunds = await _payments.GetPendingRefundCountAsync();
                ConsoleHelper.PrintHeader($"Payment Management  |  Pending: {pendingRefunds}");

                string[] options =
                {
                    "View Payment Details",
                    "Cancellation Requests",
                    "Export Payments Report"
                };

                ConsoleHelper.PrintMenu("Payment Options", options);
                int choice = ValidationHelper.ReadMenuChoice(3);

                switch (choice)
                {
                    case 0:
                        back = true;
                        break;
                    case 1:
                        await ViewPaymentDetailsDirectAsync();
                        break;
                    case 2:
                        await ShowCancellationRequestsAsync();
                        break;
                    case 3:
                        await ExportPaymentsAsync();
                        break;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // VIEW PAYMENT DETAILS
        // ─────────────────────────────────────────────────────────────────────
        private async Task ViewPaymentDetailsDirectAsync()
        {
            var payments = await _payments.GetAllPaymentsAsync();
            int selection = ConsoleHelper.ShowPaginatedTable(payments, PaymentService.TableColumns, "Select Payment to View");
            if (selection == 0) return;

            var payment = payments[selection - 1];
            ShowPaymentDetails(payment);

            Console.WriteLine();
            if (ConsoleHelper.Confirm("Do you want to export this payment detail as a report?"))
            {
                await ExportSinglePaymentReportAsync(payment);
            }
            else
            {
                ConsoleHelper.PressAnyKey();
            }
        }

        private async Task ExportSinglePaymentReportAsync(Payment payment)
        {
            ConsoleHelper.PrintMenu("Export Formats", new[] { "Export as TXT", "Export as DOCX", "Export as XLSX" });
            int choice = ValidationHelper.ReadMenuChoice(3);
            if (choice == 0) return;

            string baseDir   = AppDomain.CurrentDomain.BaseDirectory;
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filename  = $"Payment_{payment.PaymentId}_{timestamp}";

            try
            {
                if (choice == 1)
                {
                    string path = Path.Combine(baseDir, $"{filename}.txt");
                    using (StreamWriter sw = new StreamWriter(path))
                    {
                        sw.WriteLine("PAYMENT DETAIL REPORT");
                        sw.WriteLine($"Generated: {DateTime.Now}");
                        sw.WriteLine(new string('-', 50));
                        sw.WriteLine($"Payment ID:      {payment.PaymentId}");
                        sw.WriteLine($"Booking ID:      {payment.BookingId}");
                        sw.WriteLine($"Amount:          {ConsoleHelper.FormatCurrency(payment.Amount)}");
                        sw.WriteLine($"Method:          {payment.GetMethodDisplay()}");
                        sw.WriteLine($"Status:          {payment.GetStatusDisplay()}");
                        sw.WriteLine($"Transaction Ref: {payment.TransactionRef ?? "—"}");
                        sw.WriteLine($"Paid At:         {(payment.PaidAt.HasValue ? ConsoleHelper.FormatDateTime(payment.PaidAt.Value) : "—")}");
                        sw.WriteLine($"Created At:      {ConsoleHelper.FormatDateTime(payment.CreatedAt)}");
                        sw.WriteLine(new string('-', 50));
                    }
                    ConsoleHelper.PrintSuccess($"Exported to {path}");
                }
                else if (choice == 2)
                {
                    string path = Path.Combine(baseDir, $"{filename}.docx");
                    using (var document = DocX.Create(path))
                    {
                        document.InsertParagraph("Payment Detail Report").FontSize(20).Bold();
                        document.InsertParagraph($"Generated: {DateTime.Now}");
                        document.InsertParagraph();
                        
                        var t = document.AddTable(8, 2);
                        t.Rows[0].Cells[0].Paragraphs[0].Append("Field").Bold();
                        t.Rows[0].Cells[1].Paragraphs[0].Append("Value").Bold();

                        t.Rows[1].Cells[0].Paragraphs[0].Append("Payment ID");
                        t.Rows[1].Cells[1].Paragraphs[0].Append(payment.PaymentId.ToString());

                        t.Rows[2].Cells[0].Paragraphs[0].Append("Booking ID");
                        t.Rows[2].Cells[1].Paragraphs[0].Append(payment.BookingId.ToString());

                        t.Rows[3].Cells[0].Paragraphs[0].Append("Amount");
                        t.Rows[3].Cells[1].Paragraphs[0].Append(ConsoleHelper.FormatCurrency(payment.Amount));

                        t.Rows[4].Cells[0].Paragraphs[0].Append("Method");
                        t.Rows[4].Cells[1].Paragraphs[0].Append(payment.GetMethodDisplay());

                        t.Rows[5].Cells[0].Paragraphs[0].Append("Status");
                        t.Rows[5].Cells[1].Paragraphs[0].Append(payment.GetStatusDisplay());

                        t.Rows[6].Cells[0].Paragraphs[0].Append("Transaction Ref");
                        t.Rows[6].Cells[1].Paragraphs[0].Append(payment.TransactionRef ?? "—");

                        t.Rows[7].Cells[0].Paragraphs[0].Append("Paid At");
                        t.Rows[7].Cells[1].Paragraphs[0].Append(payment.PaidAt.HasValue ? ConsoleHelper.FormatDateTime(payment.PaidAt.Value) : "—");

                        document.InsertTable(t);
                        document.Save();
                    }
                    ConsoleHelper.PrintSuccess($"Exported to {path}");
                }
                else if (choice == 3)
                {
                    string path = Path.Combine(baseDir, $"{filename}.xlsx");
                    using (var wb = new XLWorkbook())
                    {
                        var ws = wb.Worksheets.Add("Payment Details");
                        ws.Cell(1, 1).Value = "Field";
                        ws.Cell(1, 2).Value = "Value";

                        ws.Cell(2, 1).Value = "Payment ID";
                        ws.Cell(2, 2).Value = payment.PaymentId;

                        ws.Cell(3, 1).Value = "Booking ID";
                        ws.Cell(3, 2).Value = payment.BookingId;

                        ws.Cell(4, 1).Value = "Amount";
                        ws.Cell(4, 2).Value = payment.Amount;

                        ws.Cell(5, 1).Value = "Method";
                        ws.Cell(5, 2).Value = payment.GetMethodDisplay();

                        ws.Cell(6, 1).Value = "Status";
                        ws.Cell(6, 2).Value = payment.GetStatusDisplay();

                        ws.Cell(7, 1).Value = "Transaction Ref";
                        ws.Cell(7, 2).Value = payment.TransactionRef ?? "—";

                        ws.Cell(8, 1).Value = "Paid At";
                        ws.Cell(8, 2).Value = payment.PaidAt.HasValue ? ConsoleHelper.FormatDateTime(payment.PaidAt.Value) : "—";

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

        private static void ShowPaymentDetails(Payment payment)
        {
            Console.Clear();
            ConsoleHelper.PrintHeader($"Payment Details — #{payment.PaymentId}");
            ConsoleHelper.PrintReadOnlyId("Payment ID", payment.PaymentId.ToString());
            ConsoleHelper.PrintNonEditableField("Booking ID",      payment.BookingId.ToString());
            ConsoleHelper.PrintNonEditableField("Amount",          ConsoleHelper.FormatCurrency(payment.Amount));
            ConsoleHelper.PrintNonEditableField("Method",          payment.GetMethodDisplay());
            ConsoleHelper.PrintNonEditableField("Status",          payment.GetStatusDisplay());
            ConsoleHelper.PrintNonEditableField("Transaction Ref", payment.TransactionRef ?? "—");
            ConsoleHelper.PrintNonEditableField("Paid At",         payment.PaidAt.HasValue ? ConsoleHelper.FormatDateTime(payment.PaidAt.Value) : "—");
            ConsoleHelper.PrintNonEditableField("Created At",      ConsoleHelper.FormatDateTime(payment.CreatedAt));
        }

        // ─────────────────────────────────────────────────────────────────────
        // 🚫 CANCELLATION REQUESTS — Admin view
        //    Shows ALL requests (Pending / Approved / Rejected).
        //    Pending  → Approve (process refund) or Reject
        //    Approved → read-only summary
        //    Rejected → read-only summary
        // ─────────────────────────────────────────────────────────────────────
        private async Task ShowCancellationRequestsAsync()
        {
            // ── Load ALL cancellation requests ───────────────────────────────
            var all = await _payments.GetAllCancellationRequestsAsync();

            if (all.Count == 0)
            {
                Console.Clear();
                ConsoleHelper.PrintHeader("Cancellation Requests");
                ConsoleHelper.PrintWarning("No cancellation requests have been raised yet.");
                ConsoleHelper.PressAnyKey();
                return;
            }

            int selection = ConsoleHelper.ShowPaginatedTable(
                all,
                PaymentService.RefundColumns,
                "Cancellation Requests — Select to Review");

            if (selection == 0) return;

            var req = all[selection - 1];

            Console.Clear();
            ConsoleHelper.PrintHeader($"Cancellation Request — #{req.CancellationId}");
            
            // Display cancellation and refund policies for reference
            PolicyService.DisplayCancellationPolicy();
            PolicyService.DisplayRefundPolicy();

            ShowCancellationDetailCard(req);

            // ── Already decided ───────────────────────────────────────────────
            if (req.CancellationStatus == "Approved")
            {
                ConsoleHelper.PrintSuccess("✅ This cancellation has already been APPROVED and the refund processed.");
                ConsoleHelper.PrintNonEditableField("Admin Remarks", string.IsNullOrWhiteSpace(req.AdminRemarks) ? "—" : req.AdminRemarks);
                ConsoleHelper.PressAnyKey();
                return;
            }

            if (req.CancellationStatus == "Rejected")
            {
                ConsoleHelper.PrintError("❌ This cancellation has already been REJECTED.");
                ConsoleHelper.PrintNonEditableField("Admin Remarks", string.IsNullOrWhiteSpace(req.AdminRemarks) ? "—" : req.AdminRemarks);
                ConsoleHelper.PressAnyKey();
                return;
            }

            // ── Pending: Approve / Reject / Back menu ─────────────────────────
            Console.WriteLine();
            string[] decisionOptions = { "Approve — Process Refund", "Reject — Decline Request" };
            ConsoleHelper.PrintMenu("Admin Decision", decisionOptions);
            int decision = ValidationHelper.ReadMenuChoice(2);

            if (decision == 0) return;

            // ════════════════════════════════════════════════════════════════
            // APPROVE → Show Refund Policy → collect amount → process
            // ════════════════════════════════════════════════════════════════
            if (decision == 1)
            {
                // Show Refund Policy — admin must agree before processing
                if (!PolicyService.ShowRefundPolicyOnly()) return;

                // ── Refund amount ─────────────────────────────────────────────
                decimal maxRefund  = req.RefundAmount;
                decimal refundAmount;

                if (maxRefund <= 0)
                {
                    // Zero-refund cancellation — just mark Approved with 0
                    ConsoleHelper.PrintWarning("This cancellation has no eligible refund (policy: < 3 days notice). Approving will cancel the booking only.");
                    if (!ConsoleHelper.Confirm("Approve cancellation with no refund?")) return;
                    refundAmount = 0m;
                }
                else
                {
                    refundAmount = ValidationHelper.ReadDecimal(
                        "Refund Amount",
                        0.01m,
                        maxRefund,
                        $"0.01 – {maxRefund:F2}  (max eligible)");

                    if (refundAmount == decimal.MinValue) return;
                }

                string remarks = ConsoleHelper.ReadInput("Admin Remarks", "optional notes", required: false);
                if (remarks == ConsoleHelper.BACK_COMMAND) return;

                if (!ConsoleHelper.Confirm(
                    $"Approve cancellation #{req.CancellationId} and refund " +
                    $"{ConsoleHelper.FormatCurrency(refundAmount)} to {req.CustomerName}?")) return;

                try
                {
                    var processedBy = _auth.CurrentUser?.Username ?? "admin";

                    // 1. Process the refund in the Refunds table
                    if (refundAmount > 0)
                    {
                        var result = await _payments.ProcessRefundAsync(
                            req.CancellationId, refundAmount, processedBy, remarks);

                        if (result is null)
                        {
                            ConsoleHelper.PrintError("Refund could not be processed. Please try again.");
                            ConsoleHelper.PressAnyKey();
                            return;
                        }
                    }

                    // 2. Mark the cancellation as Approved
                    await _payments.UpdateCancellationStatusAsync(
                        req.CancellationId, "Approved", remarks);

                    Console.Clear();
                    ConsoleHelper.PrintHeader("Cancellation Approved");
                    ConsoleHelper.PrintSuccess("✅ Cancellation approved and refund processed successfully!");
                    ConsoleHelper.PrintReadOnlyId("Cancellation #", req.CancellationId.ToString());
                    ConsoleHelper.PrintNonEditableField("Customer",      req.CustomerName);
                    ConsoleHelper.PrintNonEditableField("Hall",          req.HallName);
                    ConsoleHelper.PrintNonEditableField("Refund Amount", refundAmount > 0
                                                            ? ConsoleHelper.FormatCurrency(refundAmount)
                                                            : "No Refund (policy)");
                    ConsoleHelper.PrintNonEditableField("Processed By",  processedBy);
                    ConsoleHelper.PrintInfo("The hall is now free for other customers to book.");
                }
                catch (Exception ex)
                {
                    ConsoleHelper.PrintError(ex.Message);
                }
            }

            // ════════════════════════════════════════════════════════════════
            // REJECT → collect reason → mark Rejected
            // ════════════════════════════════════════════════════════════════
            else if (decision == 2)
            {
                string rejectReason = ConsoleHelper.ReadInput(
                    "Rejection Reason",
                    "explain why the request is rejected (required)",
                    required: true);
                if (rejectReason == ConsoleHelper.BACK_COMMAND) return;

                if (!ConsoleHelper.Confirm(
                    $"Reject cancellation #{req.CancellationId} for {req.CustomerName}?")) return;

                try
                {
                    await _payments.UpdateCancellationStatusAsync(
                        req.CancellationId, "Rejected", rejectReason);

                    Console.Clear();
                    ConsoleHelper.PrintHeader("Cancellation Rejected");
                    ConsoleHelper.PrintWarning("❌ Cancellation request has been rejected.");
                    ConsoleHelper.PrintReadOnlyId("Cancellation #", req.CancellationId.ToString());
                    ConsoleHelper.PrintNonEditableField("Customer", req.CustomerName);
                    ConsoleHelper.PrintNonEditableField("Hall",     req.HallName);
                    ConsoleHelper.PrintNonEditableField("Reason",   rejectReason);
                    ConsoleHelper.PrintInfo("The booking remains active. Customer has been notified via status.");
                }
                catch (Exception ex)
                {
                    ConsoleHelper.PrintError(ex.Message);
                }
            }

            ConsoleHelper.PressAnyKey();
        }

        // ── Cancellation Detail Card (Admin) ──────────────────────────────────
        private static void ShowCancellationDetailCard(RefundCandidate c)
        {
            string indent = ConsoleHelper.GetIndent();
            const int W   = 76;

            decimal      approxRefund = c.RefundAmount;
            string       tierLabel;
            ConsoleColor tierColor;

            if (approxRefund > 0 && c.RefundStatus == "Pending")
            {
                tierLabel = $"Pending  ({ConsoleHelper.FormatCurrency(approxRefund)} eligible)";
                tierColor = ConsoleColor.Yellow;
            }
            else if (c.RefundStatus == "Processed")
            {
                tierLabel = "Already Processed";
                tierColor = ConsoleColor.Green;
            }
            else
            {
                tierLabel = "No Refund Eligible";
                tierColor = ConsoleColor.Red;
            }

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(indent + "╔" + new string('═', W) + "╗");

            Console.Write(indent + "║");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(ConsoleHelper.CenterPad($"CANCELLATION #{c.CancellationId} — REFUND REVIEW", W));
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("║");
            Console.WriteLine(indent + "╠" + new string('═', W) + "╣");

            PrintAdminCancelRow("Cancellation #", c.CancellationId.ToString(),                           ConsoleColor.DarkGray, W);
            PrintAdminCancelRow("Booking #",      c.BookingId.ToString(),                                ConsoleColor.White,    W);
            PrintAdminCancelRow("Customer",        c.CustomerName,                                        ConsoleColor.White,    W);
            PrintAdminCancelRow("Hall",            c.HallName,                                           ConsoleColor.Cyan,     W);
            PrintAdminCancelRow("Cancelled On",    ConsoleHelper.FormatDateTime(c.CancellationDate),     ConsoleColor.Yellow,   W);
            PrintAdminCancelRow("Reason",          c.Reason,                                             ConsoleColor.Gray,     W);

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(indent + "╟" + new string('─', W) + "╢");

            PrintAdminCancelRow("Refund Eligible", ConsoleHelper.FormatCurrency(approxRefund), ConsoleColor.Green,  W);
            PrintAdminCancelRow("Refund Status",   tierLabel,                                  tierColor,            W);

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(indent + "╚" + new string('═', W) + "╝");
            Console.ResetColor();
            Console.WriteLine();
        }

        private static void PrintAdminCancelRow(string label, string value, ConsoleColor valColor, int width)
        {
            string indent    = ConsoleHelper.GetIndent();
            string labelPart = $"  {label,-16} : ";
            int remaining    = width - labelPart.Length - 2;

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write(indent + "║");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(labelPart);
            Console.ForegroundColor = valColor;
            string valPart = value.Length > remaining ? value[..remaining] : value.PadRight(remaining);
            Console.Write(valPart);
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("║");
        }

        // ─────────────────────────────────────────────────────────────────────
        // PROCESS REFUND (legacy standalone path)
        // ─────────────────────────────────────────────────────────────────────
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

            // ── Show Refund Policy before processing ──────────────────────────
            Console.Clear();
            ConsoleHelper.PrintHeader($"Process Refund — Cancellation #{candidate.CancellationId}");
            ShowCancellationDetailCard(candidate);
            if (!PolicyService.ShowRefundPolicyOnly()) return;

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
                    candidate.CancellationId, refundAmount, processedBy, remarks);

                if (result is null)
                {
                    ConsoleHelper.PrintError("Refund could not be processed.");
                }
                else
                {
                    ConsoleHelper.PrintSuccess("Refund processed successfully.");
                    ConsoleHelper.PrintReadOnlyId("Refund ID", result.RefundId.ToString());
                    ConsoleHelper.PrintNonEditableField("Booking ID",    result.BookingId.ToString());
                    ConsoleHelper.PrintNonEditableField("Refund Amount", ConsoleHelper.FormatCurrency(result.RefundAmount));
                    ConsoleHelper.PrintNonEditableField("Status",        result.RefundStatus);
                    ConsoleHelper.PrintNonEditableField("Processed By",  result.ProcessedBy);
                    ConsoleHelper.PrintInfo("This cancellation has been removed from the pending refund queue.");
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.PrintError(ex.Message);
            }

            ConsoleHelper.PressAnyKey();
        }

        // ─────────────────────────────────────────────────────────────────────
        // GENERATE INVOICE
        // ─────────────────────────────────────────────────────────────────────
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
                    ConsoleHelper.PrintReadOnlyId("Invoice ID",    invoice.InvoiceId.ToString());
                    ConsoleHelper.PrintNonEditableField("Invoice Number", invoice.InvoiceNumber);
                    ConsoleHelper.PrintNonEditableField("Booking ID",     invoice.BookingId.ToString());
                    ConsoleHelper.PrintNonEditableField("Amount",         ConsoleHelper.FormatCurrency(invoice.TotalAmount));
                    ConsoleHelper.PrintNonEditableField("Issued To",      invoice.IssuedTo);
                    ConsoleHelper.PrintNonEditableField("Generated",      ConsoleHelper.FormatDateTime(invoice.GeneratedDate));
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.PrintError(ex.Message);
            }

            ConsoleHelper.PressAnyKey();
        }

        // ─────────────────────────────────────────────────────────────────────
        // EXPORT PAYMENTS
        // ─────────────────────────────────────────────────────────────────────
        private async Task ExportPaymentsAsync()
        {
            var payments = await _payments.GetAllPaymentsAsync();
            if (payments.Count == 0)
            {
                Console.Clear();
                ConsoleHelper.PrintHeader("Export Payments Report");
                ConsoleHelper.PrintWarning("No payments available to export.");
                ConsoleHelper.PressAnyKey();
                return;
            }

            Console.Clear();
            ConsoleHelper.PrintHeader("Payments Report");
            ConsoleHelper.PrintTable(payments, PaymentService.TableColumns, "Payments Database");

            Console.WriteLine();
            if (!ConsoleHelper.Confirm("Do you want to export this payments report?"))
            {
                return;
            }

            Console.WriteLine();
            ConsoleHelper.PrintMenu("Export Formats", new[] { "Export as TXT", "Export as DOCX", "Export as XLSX" });

            int choice = ValidationHelper.ReadMenuChoice(3);
            if (choice == 0) return;

            string baseDir   = AppDomain.CurrentDomain.BaseDirectory;
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
                            sw.WriteLine($"ID: {p.PaymentId} | Booking: {p.BookingId} | Amount: {p.Amount:F2} | Status: {p.Status}");
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
