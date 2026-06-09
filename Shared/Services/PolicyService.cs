using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using VenueBookingSystem.Storage;

namespace VenueBookingSystem.Shared
{
    public class PolicyModel
    {
        public int PolicyId { get; set; }
        public string PolicyType { get; set; } = "";
        public string PolicyRule { get; set; } = "";
        public int SequenceOrder { get; set; }
    }

    public static class PolicyService
    {
        public static readonly Dictionary<string, Func<PolicyModel, string>> TableColumns = new()
        {
            ["ID"] = p => p.PolicyId.ToString(),
            ["Type"] = p => p.PolicyType,
            ["Rule"] = p => p.PolicyRule.Length > 45 ? p.PolicyRule[..42] + "..." : p.PolicyRule,
            ["Seq"] = p => p.SequenceOrder.ToString()
        };

        private static List<PolicyModel> GetPoliciesFromDbSync(string type)
        {
            var list = new List<PolicyModel>();
            try
            {
                var connStr = DatabaseContext.Instance?.ConnectionString ?? "Server=LAKSHAYA;Database=VenueBookingDB;Trusted_Connection=True;TrustServerCertificate=True;";
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(
                        "SELECT PolicyId, PolicyType, PolicyRule, SequenceOrder FROM Policies WHERE PolicyType = @Type ORDER BY SequenceOrder", conn))
                    {
                        cmd.Parameters.AddWithValue("@Type", type);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(new PolicyModel
                                {
                                    PolicyId = reader.GetInt32(0),
                                    PolicyType = reader.GetString(1),
                                    PolicyRule = reader.GetString(2),
                                    SequenceOrder = reader.GetInt32(3)
                                });
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fallback will be handled by caller
            }
            return list;
        }

        public static async Task<List<PolicyModel>> GetPoliciesFromDbAsync()
        {
            var list = new List<PolicyModel>();
            try
            {
                var dbContext = DatabaseContext.Instance;
                await using var conn = await dbContext.CreateConnectionAsync();
                await using var cmd = new SqlCommand(
                    "SELECT PolicyId, PolicyType, PolicyRule, SequenceOrder FROM Policies ORDER BY PolicyType, SequenceOrder", conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new PolicyModel
                    {
                        PolicyId = reader.GetInt32(0),
                        PolicyType = reader.GetString(1),
                        PolicyRule = reader.GetString(2),
                        SequenceOrder = reader.GetInt32(3)
                    });
                }
            }
            catch
            {
                // Ignore
            }
            return list;
        }

        public static async Task<bool> AddPolicyAsync(string type, string rule, int sequence)
        {
            try
            {
                var dbContext = DatabaseContext.Instance;
                await using var conn = await dbContext.CreateConnectionAsync();
                await using var cmd = new SqlCommand(
                    "INSERT INTO Policies (PolicyType, PolicyRule, SequenceOrder) VALUES (@Type, @Rule, @Sequence)", conn);
                cmd.Parameters.AddWithValue("@Type", type);
                cmd.Parameters.AddWithValue("@Rule", rule);
                cmd.Parameters.AddWithValue("@Sequence", sequence);
                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> EditPolicyAsync(int id, string rule, int sequence)
        {
            try
            {
                var dbContext = DatabaseContext.Instance;
                await using var conn = await dbContext.CreateConnectionAsync();
                await using var cmd = new SqlCommand(
                    "UPDATE Policies SET PolicyRule = @Rule, SequenceOrder = @Sequence WHERE PolicyId = @Id", conn);
                cmd.Parameters.AddWithValue("@Rule", rule);
                cmd.Parameters.AddWithValue("@Sequence", sequence);
                cmd.Parameters.AddWithValue("@Id", id);
                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> DeletePolicyAsync(int id)
        {
            try
            {
                var dbContext = DatabaseContext.Instance;
                await using var conn = await dbContext.CreateConnectionAsync();
                await using var cmd = new SqlCommand(
                    "DELETE FROM Policies WHERE PolicyId = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        public static void DisplayBookingPolicy()
        {
            string indent = ConsoleHelper.GetIndent();
            const int W = 76;

            Console.WriteLine();

            // ── Header ──────────────────────────────────────────────────────────
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(indent + "╔" + new string('═', W) + "╗");

            Console.Write(indent + "║");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(ConsoleHelper.CenterPad("📋  BOOKING POLICY", W));
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("║");

            Console.WriteLine(indent + "╠" + new string('═', W) + "╣");

            // ── Policy Items ────────────────────────────────────────────────────
            var dbPolicies = GetPoliciesFromDbSync("Booking");
            if (dbPolicies.Count > 0)
            {
                for (int i = 0; i < dbPolicies.Count; i++)
                {
                    PrintPolicyLine($"{i + 1}.", dbPolicies[i].PolicyRule, W);
                }
            }
            else
            {
                PrintPolicyLine("1.", "Booking is confirmed only after successful payment.", W);
                PrintPolicyLine("2.", "Hall availability is subject to real-time status.", W);
                PrintPolicyLine("3.", "Booking once confirmed cannot be modified.", W);
                PrintPolicyLine("4.", "Users must provide valid details during booking.", W);
                PrintPolicyLine("5.", "Management reserves the right to cancel bookings", W);
                PrintPolicyLine("  ", "  in special circumstances.", W);
            }

            // ── Footer ───────────────────────────────────────────────────────────
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(indent + "╚" + new string('═', W) + "╝");
            Console.ResetColor();
        }

        /// <summary>
        /// Renders the Refund Policy panel to the console, detailing the
        /// percentage refund available for different cancellation windows.
        /// </summary>
        public static void DisplayRefundPolicy()
        {
            string indent = ConsoleHelper.GetIndent();
            const int W = 76;

            Console.WriteLine();

            // ── Header ──────────────────────────────────────────────────────────
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(indent + "╔" + new string('═', W) + "╗");

            Console.Write(indent + "║");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(ConsoleHelper.CenterPad("💰  REFUND POLICY", W));
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("║");

            Console.WriteLine(indent + "╠" + new string('═', W) + "╣");

            // ── Refund Tiers ─────────────────────────────────────────────────────
            var dbPolicies = GetPoliciesFromDbSync("Refund");
            if (dbPolicies.Count > 0)
            {
                for (int i = 0; i < dbPolicies.Count; i++)
                {
                    var rule = dbPolicies[i].PolicyRule;
                    if (rule.Contains("->"))
                    {
                        var parts = rule.Split(new[] { "->" }, StringSplitOptions.None);
                        PrintRefundLine($"{i + 1}.", parts[0].Trim(), "→ " + parts[1].Trim(), ConsoleColor.Green, W);
                    }
                    else
                    {
                        PrintRefundLine($"{i + 1}.", rule, "", ConsoleColor.Green, W);
                    }
                }
            }
            else
            {
                PrintRefundLine("1.", "Cancellation before 7 days",   "→  80% refund",   ConsoleColor.Green,  W);
                PrintRefundLine("2.", "Cancellation 3–7 days before", "→  50% refund",   ConsoleColor.Yellow, W);
                PrintRefundLine("3.", "Cancellation within 3 days",   "→  No refund",    ConsoleColor.Red,    W);
                PrintRefundLine("4.", "Refund processed within",      "5–10 working days", ConsoleColor.Cyan, W);
                PrintRefundLine("5.", "Service charges",              "Non-refundable",  ConsoleColor.DarkYellow, W);
            }

            // ── Footer ───────────────────────────────────────────────────────────
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(indent + "╚" + new string('═', W) + "╝");
            Console.ResetColor();
            Console.WriteLine();
        }

        /// <summary>
        /// Renders the Cancellation Policy panel to the console.
        /// </summary>
        public static void DisplayCancellationPolicy()
        {
            string indent = ConsoleHelper.GetIndent();
            const int W = 76;

            Console.WriteLine();

            // ── Header ──────────────────────────────────────────────────────────
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(indent + "╔" + new string('═', W) + "╗");

            Console.Write(indent + "║");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(ConsoleHelper.CenterPad("🚫  CANCELLATION POLICY", W));
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("║");

            Console.WriteLine(indent + "╠" + new string('═', W) + "╣");

            // ── Colour key sub-header ────────────────────────────────────────────
            Console.Write(indent + "║");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("  Colour key: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("● Green = High Refund   ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("● Yellow = Partial Refund   ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("● Red = No Refund");
            int keyLen = "  Colour key: ".Length
                       + "● Green = High Refund   ".Length
                       + "● Yellow = Partial Refund   ".Length
                       + "● Red = No Refund".Length;
            int keyPad = W - keyLen;
            if (keyPad > 0) Console.Write(new string(' ', keyPad));
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("║");

            Console.WriteLine(indent + "╟" + new string('─', W) + "╢");

            var dbPolicies = GetPoliciesFromDbSync("Cancellation");
            if (dbPolicies.Count > 0)
            {
                int ruleIndex = 1;
                for (int i = 0; i < dbPolicies.Count; i++)
                {
                    var rule = dbPolicies[i].PolicyRule;
                    if (rule.Contains("->"))
                    {
                        var parts = rule.Split(new[] { "->" }, StringSplitOptions.None);
                        ConsoleColor col = ConsoleColor.Green;
                        if (rule.Contains("Yellow")) col = ConsoleColor.Yellow;
                        if (rule.Contains("Red")) col = ConsoleColor.Red;
                        PrintRefundLine($"{ruleIndex}.", parts[0].Trim(), "→ " + parts[1].Trim(), col, W);
                        ruleIndex++;
                    }
                }
                
                Console.WriteLine(indent + "╟" + new string('─', W) + "╢");

                for (int i = 0; i < dbPolicies.Count; i++)
                {
                    var rule = dbPolicies[i].PolicyRule;
                    if (!rule.Contains("->"))
                    {
                        PrintPolicyLine("•", rule, W);
                    }
                }
            }
            else
            {
                PrintRefundLine("1.", "Cancel ≥ 7 days before event date",   "→  80% refund (Green tier)",  ConsoleColor.Green,  W);
                PrintRefundLine("2.", "Cancel 3–6 days before event date",   "→  50% refund (Yellow tier)", ConsoleColor.Yellow, W);
                PrintRefundLine("3.", "Cancel within 3 days of event date",  "→  No refund  (Red tier)",    ConsoleColor.Red,    W);

                Console.WriteLine(indent + "╟" + new string('─', W) + "╢");

                PrintPolicyLine("•", "Cancellations must be submitted via the booking system.", W);
                PrintPolicyLine("•", "No-show on event day = full forfeiture (Red tier applies).", W);
                PrintPolicyLine("•", "Service charges (5%) are non-refundable in all tiers.", W);
                PrintPolicyLine("•", "Refunds are processed within 5–10 working days.", W);
                PrintPolicyLine("•", "Force majeure / emergencies reviewed on a case-by-case basis.", W);
            }

            // ── Footer ───────────────────────────────────────────────────────────
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(indent + "╚" + new string('═', W) + "╝");
            Console.ResetColor();
            Console.WriteLine();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // ❓ PROMPT FOR POLICY ACCEPTANCE
        //    Loops until the user enters a valid "yes" or "no".
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        public static bool PromptPolicyAcceptance(
            string question    = "Do you agree to the Booking, Refund & Cancellation Policies?",
            string rejectedMsg = "Action cancelled. You must accept the policies to proceed.",
            bool   loopOnNo    = false)
        {
            string indent = ConsoleHelper.GetIndent();

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(indent + $"⚠️  {question} ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("(Yes/No): ");
                Console.ResetColor();

                string? input = Console.ReadLine()?.Trim().ToLowerInvariant();

                switch (input)
                {
                    case "yes":
                    case "y":
                        DateTime acceptedTime = DateTime.Now;
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(indent + $"✅ Accepted at {acceptedTime:dd-MMM-yyyy HH:mm:ss}. Proceeding...");
                        Console.ResetColor();
                        Console.WriteLine();
                        return true;

                    case "no":
                    case "n":
                        if (loopOnNo)
                        {
                            // Force the user to agree — "No" just re-prompts
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(indent + "❌ You must agree to this policy to continue.");
                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            Console.WriteLine(indent + "    Please review the policy above and type 'Yes' to proceed.");
                            Console.ResetColor();
                            Console.WriteLine();
                            break; // back to top of while loop
                        }
                        // loopOnNo = false: abort as normal
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(indent + $"❌ {rejectedMsg}");
                        Console.ResetColor();
                        Console.WriteLine();
                        return false;

                    default:
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine(indent + "⚠️  Invalid input — please type 'Yes' or 'No'.");
                        Console.ResetColor();
                        break;
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 🚀 CONTEXT-SPECIFIC WRAPPERS
        //    Each caller gets exactly the policy box(es) relevant to
        //    their flow — nothing more, nothing less.
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>
        /// 📋 BOOKING FLOW — shows only the Booking Policy box, then asks
        /// the customer to agree before the hall-selection table appears.
        /// </summary>
        public static bool ShowBookingPolicyOnly()
        {
            DisplayBookingPolicy();
            return PromptPolicyAcceptance(
                question    : "Do you agree to the Booking Policy?",
                rejectedMsg : "You must accept the Booking Policy to proceed.",
                loopOnNo    : true);   // ← 'No' re-prompts; cannot skip policy
        }

        /// <summary>
        /// 🚫 CUSTOMER CANCEL FLOW — shows ONLY the Cancellation Policy box,
        /// then asks the customer to acknowledge before confirming the cancellation.
        /// </summary>
        public static bool ShowCancellationPolicyOnly()
        {
            DisplayCancellationPolicy();
            return PromptPolicyAcceptance(
                question    : "Do you agree to the Cancellation Policy?",
                rejectedMsg : "Cancellation aborted. You must acknowledge the Cancellation Policy.",
                loopOnNo    : true);
        }

        /// <summary>
        /// Kept for backward compatibility — delegates to ShowCancellationPolicyOnly().
        /// </summary>
        public static bool ShowRefundCancellationPolicies()
            => ShowCancellationPolicyOnly();

        /// <summary>
        /// 💰 ADMIN REFUND FLOW — shows only the Refund Policy box, then asks
        /// the admin to confirm before the refund is processed.
        /// </summary>
        public static bool ShowRefundPolicyOnly()
        {
            DisplayRefundPolicy();
            return PromptPolicyAcceptance(
                question    : "Do you agree to process this refund per the Refund Policy?",
                rejectedMsg : "Refund processing cancelled.",
                loopOnNo    : true);
        }

        /// <summary>
        /// Shows all three policy boxes (Booking + Refund + Cancellation) and
        /// prompts for acceptance. Kept for backward compatibility.
        /// </summary>
        public static bool ShowAndAcceptPolicies()
        {
            DisplayBookingPolicy();
            DisplayRefundPolicy();
            DisplayCancellationPolicy();
            return PromptPolicyAcceptance();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 🖨️  PRIVATE RENDERING HELPERS
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private static void PrintPolicyLine(string number, string text, int width)
        {
            string indent = ConsoleHelper.GetIndent();

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write(indent + "║");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"  {number,-4}");

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(text.PadRight(width - 6)); // 6 = 2 indent + 4 number

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("║");
        }

        /// <summary>
        /// Renders a single refund-tier line with a colour-coded outcome column.
        /// </summary>
        private static void PrintRefundLine(
            string number,
            string label,
            string outcome,
            ConsoleColor outcomeColor,
            int width)
        {
            string indent = ConsoleHelper.GetIndent();

            // Fixed column widths: number(4) + label(34) + outcome fills rest
            int numberW  = 4;
            int labelW   = 34;
            int outcomeW = width - numberW - labelW - 2; // 2 border chars
            if (outcomeW < 0) outcomeW = 0;

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write(indent + "║");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"  {number,-3}");   // "1." padded to 3 chars + 2 leading spaces → numberW

            Console.ForegroundColor = ConsoleColor.White;
            string labelPart = label.Length > labelW ? label[..labelW] : label.PadRight(labelW);
            Console.Write(labelPart);

            Console.ForegroundColor = outcomeColor;
            string outcomePart = outcome.Length > outcomeW ? outcome[..outcomeW] : outcome.PadRight(outcomeW);
            Console.Write(outcomePart);

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("║");
        }
    }
}
