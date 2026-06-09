using System;
using System.Collections.Generic;
using System.Text;

namespace VenueBookingSystem.Shared
{
    public static class ConsoleHelper
    {
        public const int PAGE_SIZE = 7;

        public const string BACK_COMMAND = "0";
        private static int _tableRenderCount = 0;

        // ─────────────────────────── LAYOUT ───────────────────────────

        static ConsoleHelper()
        {
            Console.OutputEncoding = Encoding.UTF8;
        }

        public static string GetIndent()
        {
            return "  ";
        }

        private static int SafeWidth()
        {
            try { return Math.Max(Console.WindowWidth, 120); }
            catch { return 120; }
        }

        // ─────────────────────────── SPLASH SCREEN ───────────────────────────

        /// <summary>
        /// Displays the animated project splash / intro screen.
        /// Called once at application startup (from Program.cs before login loop).
        /// </summary>
        public static void ShowSplashScreen()
        {
            try { Console.Clear(); } catch { }
            Console.CursorVisible = false;

            // ── ASCII art arrays (defined locally) ──────────────────────────
            // "HALL" in block letters
            string[] art = new[]
            {
                @" █  █   █   █     █     ",
                @" █  █  █ █  █     █     ",
                @" ████ █████ █     █     ",
                @" █  █ █   █ █     █     ",
                @" █  █ █   █ ████  ████  ",
            };

            // "BOOKING" in block letters
            string[] art2 = new[]
            {
                @" ████   ████   ████  █  █  ████  █  █   ████  ",
                @" █   █ █    █ █    █ █ █      █  █  █  █      ",
                @" ████  █    █ █    █ ███    ██   ████  █  ███ ",
                @" █   █ █    █ █    █ █ █      █  █  █  █    █ ",
                @" ████   ████   ████  █  █ ████   █  █   ████  ",
            };

            // "SYSTEM" in block letters
            string[] art3 = new[]
            {
                @"  ████  ██  █  ████  ████  ████  █   █ ",
                @" █      █ █ █    █  █      █      ██ ██ ",
                @"  ███   █  ██    █   ███   ███   █ █ █ ",
                @"     █  █   █    █      █  █      █   █ ",
                @" ████   █   █    █  ████   ████   █   █ ",
            };

            const int W = 76;
            string ind = "  ";
            try
            {
                int width = Console.WindowWidth;
                if (width > (W + 2))
                    ind = new string(' ', (width - (W + 2)) / 2);
            }
            catch { }

            Console.WriteLine();

            // ── Decorative top accent ──────────────────────────────────────
            SetColor(ConsoleColor.DarkGray);
            Console.WriteLine(ind + "  " + new string('·', W - 2));

            // Top border
            SetColor(ConsoleColor.Cyan);
            Console.WriteLine(ind + "╔" + new string('═', W) + "╗");
            Console.WriteLine(ind + "║" + new string(' ', W) + "║");

            // Print HALL
            foreach (string line in art)
            {
                string padded = CenterPad(line, W);
                Console.Write(ind + "║");
                SetColor(ConsoleColor.Cyan);
                Console.Write(padded);
                SetColor(ConsoleColor.Cyan);
                Console.WriteLine("║");
            }

            // Spacer
            SetColor(ConsoleColor.Cyan);
            Console.WriteLine(ind + "║" + new string(' ', W) + "║");

            // Divider between HALL and BOOKING
            SetColor(ConsoleColor.DarkGray);
            Console.Write(ind + "║");
            Console.Write(new string('─', W));
            Console.WriteLine("║");
            SetColor(ConsoleColor.Cyan);
            Console.WriteLine(ind + "║" + new string(' ', W) + "║");

            // Print BOOKING
            foreach (string line in art2)
            {
                string padded = CenterPad(line, W);
                Console.Write(ind + "║");
                SetColor(ConsoleColor.White);
                Console.Write(padded);
                SetColor(ConsoleColor.Cyan);
                Console.WriteLine("║");
            }

            // Spacer
            Console.WriteLine(ind + "║" + new string(' ', W) + "║");

            // Divider between BOOKING and SYSTEM
            SetColor(ConsoleColor.DarkGray);
            Console.Write(ind + "║");
            Console.Write(new string('─', W));
            Console.WriteLine("║");
            SetColor(ConsoleColor.Cyan);
            Console.WriteLine(ind + "║" + new string(' ', W) + "║");

            // Print SYSTEM
            foreach (string line in art3)
            {
                string padded = CenterPad(line, W);
                Console.Write(ind + "║");
                SetColor(ConsoleColor.Yellow);
                Console.Write(padded);
                SetColor(ConsoleColor.Cyan);
                Console.WriteLine("║");
            }

            Console.WriteLine(ind + "║" + new string(' ', W) + "║");

            // Accent divider before tagline
            SetColor(ConsoleColor.DarkCyan);
            Console.Write(ind + "╠");
            Console.Write(new string('═', W));
            Console.WriteLine("╣");

            // Tagline
            string tag = "✦   One Venue  ·  Multiple Halls  ·  Seamless Booking Experience   ✦";
            string tagPad = CenterPad(tag, W);
            Console.Write(ind + "║");
            SetColor(ConsoleColor.Yellow);
            Console.Write(tagPad);
            SetColor(ConsoleColor.Cyan);
            Console.WriteLine("║");

            Console.WriteLine(ind + "║" + new string(' ', W) + "║");

            // Version strip
            string ver = "v1.0.0   ·   .NET 10   ·   SQL Server   ·   Authenticated Access Only";
            string verPad = CenterPad(ver, W);
            Console.Write(ind + "║");
            SetColor(ConsoleColor.DarkGray);
            Console.Write(verPad);
            SetColor(ConsoleColor.Cyan);
            Console.WriteLine("║");

            Console.WriteLine(ind + "║" + new string(' ', W) + "║");
            SetColor(ConsoleColor.Cyan);
            Console.WriteLine(ind + "╚" + new string('═', W) + "╝");

            // Decorative bottom accent
            SetColor(ConsoleColor.DarkGray);
            Console.WriteLine(ind + "  " + new string('·', W - 2));

            ResetColor();
            Console.WriteLine();

            // Loading bar animation
            SetColor(ConsoleColor.DarkGray);
            Console.Write(ind + "  Initialising  [");
            SetColor(ConsoleColor.Cyan);
            for (int i = 0; i < 20; i++)
            {
                System.Threading.Thread.Sleep(60);
                Console.Write("█");
            }
            SetColor(ConsoleColor.DarkGray);
            Console.WriteLine("]  Ready!");
            ResetColor();
            Console.CursorVisible = true;
        }

        // ─────────────────────────── WELCOME SCREEN (per-login) ───────────────────────────

        public static void ShowWelcomeScreen()
        {
            try { Console.Clear(); } catch (System.IO.IOException) { }

            const int W = 74;
            string ind = "  ";
            try
            {
                int width = Console.WindowWidth;
                if (width > (W + 2))
                    ind = new string(' ', (width - (W + 2)) / 2);
            }
            catch { }

            Console.WriteLine();

            // ── Decorative top dots ──
            SetColor(ConsoleColor.DarkGray);
            Console.WriteLine(ind + "  " + new string('·', W - 2));

            // ── Top border ──
            SetColor(ConsoleColor.Cyan);
            Console.WriteLine(ind + "╔" + new string('═', W) + "╗");
            Console.WriteLine(ind + "║" + new string(' ', W) + "║");

            // ── System title ──
            string sysLabel = "◈  ONE VENUE  MULTI-HALL  BOOKING  SYSTEM  ◈";
            Console.Write(ind + "║");
            SetColor(ConsoleColor.Cyan);
            Console.Write(CenterPad(sysLabel, W));
            SetColor(ConsoleColor.Cyan);
            Console.WriteLine("║");
            Console.WriteLine(ind + "║" + new string(' ', W) + "║");

            // ── Double divider ──
            SetColor(ConsoleColor.DarkCyan);
            Console.Write(ind + "╠");
            Console.Write(new string('═', W));
            Console.WriteLine("╣");

            // ── Login prompt ──
            Console.WriteLine(ind + "║" + new string(' ', W) + "║");
            Console.Write(ind + "║");
            SetColor(ConsoleColor.White);
            Console.Write(CenterPad("Please log in to continue", W));
            SetColor(ConsoleColor.Cyan);
            Console.WriteLine("║");
            Console.WriteLine(ind + "║" + new string(' ', W) + "║");

            // ── Tagline ──
            SetColor(ConsoleColor.DarkGray);
            Console.Write(ind + "╠"); Console.Write(new string('─', W)); Console.WriteLine("╣");
            Console.WriteLine(ind + "║" + new string(' ', W) + "║");
            Console.Write(ind + "║");
            SetColor(ConsoleColor.Yellow);
            Console.Write(CenterPad("✦   One Venue  ·  Multiple Halls  ·  Seamless Experience   ✦", W));
            SetColor(ConsoleColor.Cyan);
            Console.WriteLine("║");
            Console.WriteLine(ind + "║" + new string(' ', W) + "║");

            // ── Version strip ──
            SetColor(ConsoleColor.DarkGray);
            Console.Write(ind + "║");
            Console.Write(CenterPad("v1.0.0  ·  .NET 10  ·  SQL Server  ·  Authenticated Access Only", W));
            SetColor(ConsoleColor.Cyan);
            Console.WriteLine("║");
            Console.WriteLine(ind + "║" + new string(' ', W) + "║");
            Console.WriteLine(ind + "╚" + new string('═', W) + "╝");

            // ── Feature / Usage color-coded table ──
            Console.WriteLine();
            SetColor(ConsoleColor.DarkGray);
            Console.WriteLine(ind + "  " + new string('·', W - 2));
            Console.WriteLine();

            // Table header
            string col1H = " Role          "; // 15
            string col2H = " Feature                        "; // 32
            string col3H = " Access "; // 8
            int t1 = col1H.Length, t2 = col2H.Length, t3 = col3H.Length;
            string tborder = "+" + new string('-', t1) + "+" + new string('-', t2) + "+" + new string('-', t3) + "+";

            SetColor(ConsoleColor.DarkCyan);
            Console.WriteLine(ind + tborder);
            Console.Write(ind + "|");
            SetColor(ConsoleColor.Yellow); Console.Write(col1H);
            SetColor(ConsoleColor.DarkCyan); Console.Write("|");
            SetColor(ConsoleColor.Yellow); Console.Write(col2H);
            SetColor(ConsoleColor.DarkCyan); Console.Write("|");
            SetColor(ConsoleColor.Yellow); Console.Write(col3H);
            SetColor(ConsoleColor.DarkCyan); Console.WriteLine("|");
            Console.WriteLine(ind + tborder);

            // Table rows: (role, feature, access-color)
            var rows = new (string Role, string Feature, string Access, ConsoleColor Color)[]
            {
                (" Admin         ", " Manage Halls & Venues          ", " ✔ Full  ", ConsoleColor.Green),
                (" Admin         ", " Manage All Bookings            ", " ✔ Full  ", ConsoleColor.Green),
                (" Admin         ", " Process Payments               ", " ✔ Full  ", ConsoleColor.Green),
                (" Admin         ", " Generate Reports               ", " ✔ Full  ", ConsoleColor.Green),
                (" Admin         ", " Manage Users & Roles           ", " ✔ Full  ", ConsoleColor.Green),
                (" Customer      ", " Browse Available Halls         ", " ✔ Yes   ", ConsoleColor.Cyan),
                (" Customer      ", " Create & Cancel Bookings       ", " ✔ Yes   ", ConsoleColor.Cyan),
                (" Customer      ", " View Booking History           ", " ✔ Yes   ", ConsoleColor.Cyan),
                (" Customer      ", " Make Payments                  ", " ✔ Yes   ", ConsoleColor.Cyan),
                (" Customer      ", " Manage Reports                 ", " ✖ No    ", ConsoleColor.Red),
            };

            bool alt = false;
            for (int ri = 0; ri < rows.Length; ri++)
            {
                var (role, feature, access, color) = rows[ri];
                SetColor(ConsoleColor.DarkCyan);
                Console.Write(ind + "|");
                SetColor(alt ? ConsoleColor.Gray : ConsoleColor.White);
                Console.Write(role);
                SetColor(ConsoleColor.DarkCyan);
                Console.Write("|");
                SetColor(alt ? ConsoleColor.Gray : ConsoleColor.White);
                Console.Write(feature);
                SetColor(ConsoleColor.DarkCyan);
                Console.Write("|");
                SetColor(color);
                Console.Write(access);
                SetColor(ConsoleColor.DarkCyan);
                Console.WriteLine("|");
                if (ri < rows.Length - 1)
                {
                    SetColor(ConsoleColor.DarkGray);
                    Console.WriteLine(ind + tborder);
                }
                alt = !alt;
            }
            SetColor(ConsoleColor.DarkCyan);
            Console.WriteLine(ind + tborder);

            // ── Color legend ──
            Console.WriteLine();
            Console.Write(ind + "  Color Guide:  ");
            SetColor(ConsoleColor.Green);  Console.Write("● Full Access  ");
            SetColor(ConsoleColor.Cyan);   Console.Write("● Customer Access  ");
            SetColor(ConsoleColor.Yellow); Console.Write("✎ Editable Fields  ");
            SetColor(ConsoleColor.Red);    Console.Write("✖ Errors / Denied");
            Console.WriteLine();

            // ── Decorative bottom dots ──
            SetColor(ConsoleColor.DarkGray);
            Console.WriteLine();
            Console.WriteLine(ind + "  " + new string('·', W - 2));

            Console.WriteLine();
            ResetColor();
        }

        // ─────────────────────────── HEADERS ───────────────────────────

        public static void PrintHeader(string title)
        {
            int inner = Math.Max(SafeWidth() - 4, 74);
            string ind = "  ";

            Console.WriteLine();

            // Top double border
            SetColor(ConsoleColor.DarkCyan);
            Console.WriteLine(ind + "╔" + new string('═', inner) + "╗");

            // Title row
            Console.Write(ind + "║");
            SetColor(ConsoleColor.White);
            string titleText = $" ◈  {title}  ◈ ";
            Console.Write(CenterPad(titleText, inner));
            SetColor(ConsoleColor.DarkCyan);
            Console.WriteLine("║");

            // Accent separator row (filled with ─)
            SetColor(ConsoleColor.Cyan);
            Console.Write(ind + "╠");
            Console.Write(new string('─', inner));
            Console.WriteLine("╣");

            // Timestamp / context row
            string ts = DateTime.Now.ToString("dd-MMM-yyyy   HH:mm");
            Console.Write(ind + "║");
            SetColor(ConsoleColor.DarkGray);
            Console.Write(CenterPad(ts, inner));
            SetColor(ConsoleColor.Cyan);
            Console.WriteLine("║");

            // Bottom border
            SetColor(ConsoleColor.DarkCyan);
            Console.WriteLine(ind + "╚" + new string('═', inner) + "╝");
            ResetColor();
            Console.WriteLine();
        }

        public static void PrintSubHeader(string title)
        {
            SetColor(ConsoleColor.Cyan);
            Console.WriteLine($"  \u25b8\u25b8  {title}");
            SetColor(ConsoleColor.DarkGray);
            Console.WriteLine("  " + new string('\u2500', Math.Min(title.Length + 8, 60)));
            ResetColor();
        }

        public static void PrintSeparator()
        {
            int width = Math.Max(SafeWidth() - 4, 74);
            SetColor(ConsoleColor.DarkGray);
            Console.WriteLine(GetIndent() + new string('─', width));
            ResetColor();
        }

        // ─────────────────────────── STATUS MESSAGES ───────────────────────────

        public static void PrintSuccess(string message)
        {
            string ind = GetIndent();
            int len = Math.Min(message.Length + 10, 74);
            SetColor(ConsoleColor.Green);
            Console.WriteLine(ind + "╔" + new string('═', len) + "╗");
            Console.Write(ind + "║");
            Console.ForegroundColor = ConsoleColor.White;
            string content = $"  ✔  {message}  ";
            if (content.Length < len) content = content.PadRight(len);
            else if (content.Length > len) content = content[..len];
            Console.Write(content);
            SetColor(ConsoleColor.Green);
            Console.WriteLine("║");
            Console.WriteLine(ind + "╚" + new string('═', len) + "╝");
            ResetColor();
        }

        public static void PrintError(string message)
        {
            string ind = GetIndent();
            int len = Math.Min(message.Length + 10, 74);
            SetColor(ConsoleColor.Red);
            Console.WriteLine(ind + "╔" + new string('═', len) + "╗");
            Console.Write(ind + "║");
            Console.ForegroundColor = ConsoleColor.White;
            string content = $"  ✖  {message}  ";
            if (content.Length < len) content = content.PadRight(len);
            else if (content.Length > len) content = content[..len];
            Console.Write(content);
            SetColor(ConsoleColor.Red);
            Console.WriteLine("║");
            Console.WriteLine(ind + "╚" + new string('═', len) + "╝");
            ResetColor();
        }

        public static void PrintWarning(string message)
        {
            string ind = GetIndent();
            int len = Math.Min(message.Length + 10, 74);
            SetColor(ConsoleColor.Yellow);
            Console.WriteLine(ind + "┌" + new string('─', len) + "┐");
            Console.Write(ind + "│");
            Console.ForegroundColor = ConsoleColor.Yellow;
            string content = $"  ▲  {message}  ";
            if (content.Length < len) content = content.PadRight(len);
            else if (content.Length > len) content = content[..len];
            Console.Write(content);
            SetColor(ConsoleColor.Yellow);
            Console.WriteLine("│");
            Console.WriteLine(ind + "└" + new string('─', len) + "┘");
            ResetColor();
        }

        public static void PrintInfo(string message)
        {
            SetColor(ConsoleColor.Cyan);
            Console.Write(GetIndent() + "  ●  ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
            ResetColor();
        }

        // ─────────────────────────── FIELD DISPLAY ───────────────────────────

        /// <summary>Editable field — highlighted in Yellow with pencil icon.</summary>
        public static void PrintEditableField(string label, string value)
        {
            string paddedLabel = label.PadLeft(22);
            SetColor(ConsoleColor.DarkCyan);
            Console.Write($"  {paddedLabel}");
            SetColor(ConsoleColor.DarkGray);
            Console.Write(" │ ");
            SetColor(ConsoleColor.Yellow);
            Console.Write("✎ ");
            SetColor(ConsoleColor.Yellow);
            Console.Write("[ ");
            SetColor(ConsoleColor.White);
            Console.Write(value);
            SetColor(ConsoleColor.Yellow);
            Console.WriteLine(" ]");
            ResetColor();
        }

        /// <summary>Read-only display field — shown in White.</summary>
        public static void PrintNonEditableField(string label, string value)
        {
            string paddedLabel = label.PadLeft(22);
            SetColor(ConsoleColor.DarkCyan);
            Console.Write($"  {paddedLabel}");
            SetColor(ConsoleColor.DarkGray);
            Console.Write(" │ ");
            SetColor(ConsoleColor.White);
            Console.WriteLine(value);
            ResetColor();
        }

        /// <summary>Auto-generated ID field — shown in Dark Gray.</summary>
        public static void PrintReadOnlyId(string label, string value)
        {
            string paddedLabel = label.PadLeft(22);
            SetColor(ConsoleColor.DarkCyan);
            Console.Write($"  {paddedLabel}");
            SetColor(ConsoleColor.DarkGray);
            Console.Write(" │ ");
            SetColor(ConsoleColor.DarkGray);
            Console.WriteLine($"{value}  (auto)");
            ResetColor();
        }

        /// <summary>Status field — Green for active/confirmed, Red for cancelled/inactive, Yellow for pending.</summary>
        public static void PrintStatusField(string label, string status)
        {
            string paddedLabel = label.PadLeft(22);
            SetColor(ConsoleColor.DarkCyan);
            Console.Write($"  {paddedLabel}");
            SetColor(ConsoleColor.DarkGray);
            Console.Write(" │ ");
            string statusUpper = (status ?? "").ToUpper();
            ConsoleColor statusColor = statusUpper switch
            {
                var s when s.Contains("ACTIVE") || s.Contains("CONFIRM") || s.Contains("PAID") || s.Contains("COMPLETE") => ConsoleColor.Green,
                var s when s.Contains("CANCEL") || s.Contains("REJECT") || s.Contains("FAIL") || s.Contains("INACTIVE") => ConsoleColor.Red,
                var s when s.Contains("PENDING") || s.Contains("PARTIAL") || s.Contains("HOLD") => ConsoleColor.Yellow,
                _ => ConsoleColor.White
            };
            string dot = statusColor == ConsoleColor.Green ? "● " : statusColor == ConsoleColor.Red ? "● " : "◌ ";
            SetColor(statusColor);
            Console.WriteLine(dot + status);
            ResetColor();
        }

        // ─────────────────────────── MENUS ───────────────────────────

        public static void PrintMenu(string title, string[] options, bool showBack = true)
        {
            PrintHeader(title);

            string ind = GetIndent();

            // Find the longest option text — loop through every option
            int longestOption = 0;
            foreach (string option in options)
            {
                if (option.Length > longestOption)
                    longestOption = option.Length;
            }

            int rowWidth = Math.Min(Math.Max(42, longestOption + 14), Math.Max(42, SafeWidth() - ind.Length - 8));

            SetColor(ConsoleColor.DarkCyan);
            Console.WriteLine($"{ind}╭{new string('─', rowWidth)}╮");

            for (int i = 0; i < options.Length; i++)
            {
                bool accent = i % 2 == 0;
                string shortcutNum = $"{i + 1}";

                Console.Write(ind);
                SetColor(ConsoleColor.DarkCyan);
                Console.Write("│");

                // Number badge — alternating Cyan / Yellow
                Console.Write(" ");
                SetColor(accent ? ConsoleColor.Cyan : ConsoleColor.Yellow);
                Console.Write($"[{shortcutNum}]");

                // Option text
                SetColor(accent ? ConsoleColor.White : ConsoleColor.Gray);
                string textPart = $"  {options[i]}";
                int remaining = rowWidth - shortcutNum.Length - 4; // 4 = " [" + "] "
                if (textPart.Length > remaining) textPart = textPart[..remaining];
                else textPart = textPart.PadRight(remaining);
                Console.Write(textPart);

                SetColor(ConsoleColor.DarkCyan);
                Console.WriteLine("│");

                if (i < options.Length - 1)
                    Console.WriteLine($"{ind}├{new string('─', rowWidth)}┤");
            }

            if (showBack)
            {
                Console.WriteLine($"{ind}├{new string('─', rowWidth)}┤");
                Console.Write(ind);
                SetColor(ConsoleColor.DarkCyan);
                Console.Write("│");
                SetColor(ConsoleColor.DarkGray);
                Console.Write(" [0]");
                SetColor(ConsoleColor.DarkGray);
                string backText = "  ← Back / Exit";
                Console.Write(backText.PadRight(rowWidth - 4));
                SetColor(ConsoleColor.DarkCyan);
                Console.WriteLine("│");
            }

            SetColor(ConsoleColor.DarkCyan);
            Console.WriteLine($"{ind}╰{new string('─', rowWidth)}╯");
            ResetColor();
            Console.WriteLine();
        }

        // ─────────────────────────── INPUT ───────────────────────────

        public static string ReadInput(
            string prompt,
            string formatHint = "",
            bool required = true,
            bool allowBack = true,
            Func<string, string?>? validator = null)
        {
            while (true)
            {
                string ind = GetIndent();

                // ── Prompt label ──────────────────────────────────────────────
                SetColor(ConsoleColor.DarkYellow);
                Console.Write(ind + "➤ ");
                SetColor(ConsoleColor.White);
                Console.Write(prompt);

                // ── Required / Optional badge ─────────────────────────────────
                if (required)
                {
                    SetColor(ConsoleColor.Red);
                    Console.Write(" *");          // red asterisk = required
                }
                else
                {
                    SetColor(ConsoleColor.DarkGray);
                    Console.Write(" (optional)");
                }

                // ── Format hint ───────────────────────────────────────────────
                if (!string.IsNullOrWhiteSpace(formatHint))
                {
                    SetColor(ConsoleColor.DarkGray);
                    Console.Write($" [{formatHint}]");
                }

                // ── Back hint ─────────────────────────────────────────────────
                if (allowBack)
                {
                    SetColor(ConsoleColor.DarkGray);
                    Console.Write("  0=Back");
                }

                SetColor(ConsoleColor.DarkGray);
                Console.Write(" : ");
                SetColor(ConsoleColor.Cyan);
                string? raw = Console.ReadLine();
                ResetColor();

                string input = raw?.Trim() ?? string.Empty;

                // Back command
                if (input == BACK_COMMAND && allowBack)
                    return BACK_COMMAND;

                // ── Empty check for required fields ───────────────────────────
                if (required && string.IsNullOrWhiteSpace(input))
                {
                    PrintError($"'{prompt}' is required — this field cannot be left empty.");
                    continue;
                }

                // Optional field — empty is fine
                if (!required && string.IsNullOrWhiteSpace(input))
                    return string.Empty;

                // ── Custom validator ──────────────────────────────────────────
                if (validator != null)
                {
                    string? error = validator(input);
                    if (error != null)
                    {
                        PrintError(error);
                        continue;
                    }
                }

                return input;
            }
        }

        public static string ReadPassword(string prompt = "Enter Password", bool allowBack = true, Func<string, string?>? validator = null)
        {
            while (true)
            {
                // Pure append-only: always print on current cursor line
                SetColor(ConsoleColor.DarkYellow);
                Console.Write(GetIndent() + "➤ ");
                SetColor(ConsoleColor.White);
                Console.Write($"{prompt}");
                SetColor(ConsoleColor.DarkGray);
                if (allowBack)
                    Console.Write(" (0=Back)");
                Console.Write(" [Tab=Show/Hide]");
                SetColor(ConsoleColor.White);
                Console.Write(" : ");

                var sb = new StringBuilder();
                bool showPassword = false;

                while (true)
                {
                    ConsoleKeyInfo key = Console.ReadKey(intercept: true);

                    if (key.Key == ConsoleKey.Enter)
                        break;

                    if (key.Key == ConsoleKey.Tab)
                    {
                        showPassword = !showPassword;
                        for (int i = 0; i < sb.Length; i++)
                            Console.Write("\b \b");

                        if (showPassword)
                        {
                            SetColor(ConsoleColor.Cyan);
                            Console.Write(sb.ToString());
                        }
                        else
                        {
                            SetColor(ConsoleColor.DarkGray);
                            Console.Write(new string('*', sb.Length));
                        }
                    }
                    else if (key.Key == ConsoleKey.Backspace)
                    {
                        if (sb.Length > 0)
                        {
                            sb.Remove(sb.Length - 1, 1);
                            Console.Write("\b \b");
                        }
                    }
                    else if (key.KeyChar != '\0')
                    {
                        sb.Append(key.KeyChar);
                        if (showPassword)
                        {
                            SetColor(ConsoleColor.Cyan);
                            Console.Write(key.KeyChar);
                        }
                        else
                        {
                            SetColor(ConsoleColor.DarkGray);
                            Console.Write('*');
                        }
                    }
                }

                Console.WriteLine();
                ResetColor();

                string password = sb.ToString();

                // Back command
                if (allowBack && password == BACK_COMMAND)
                    return BACK_COMMAND;

                // Empty check
                if (string.IsNullOrWhiteSpace(password))
                {
                    SetColor(ConsoleColor.Red);
                    Console.WriteLine(GetIndent() + "  ✖  Password cannot be empty.");
                    ResetColor();
                    continue;
                }

                // Custom validator
                if (validator != null)
                {
                    string? error = validator(password);
                    if (error != null)
                    {
                        SetColor(ConsoleColor.Red);
                        Console.WriteLine(GetIndent() + $"  ✖  {error}");
                        ResetColor();
                        continue;
                    }
                }

                // Valid
                return password;
            }
        }

        public static int ReadMenuChoice(int min, int max)
        {
            while (true)
            {
                SetColor(ConsoleColor.DarkYellow);
                Console.Write(GetIndent() + "➤ ");
                SetColor(ConsoleColor.White);
                Console.Write("Select an option by number or 0 to go back : ");

                SetColor(ConsoleColor.Cyan);
                string? raw = Console.ReadLine();
                ResetColor();

                if (int.TryParse(raw?.Trim(), out int choice))
                {
                    if (choice == 0) return 0;
                    if (choice >= min && choice <= max) return choice;
                }

                SetColor(ConsoleColor.Red);
                Console.WriteLine(GetIndent() + "  ✖  Invalid choice. Please enter a number between " + min + " and " + max + " (or 0).");
                ResetColor();
            }
        }

        private static void ClearLine(int row)
        {
            try
            {
                Console.SetCursorPosition(0, row);
                Console.Write(new string(' ', Console.WindowWidth - 1));
                Console.SetCursorPosition(0, row);
            }
            catch { }
        }

        private static void PrintInlineError(string message, int row)
        {
            try
            {
                Console.SetCursorPosition(0, row);
                SetColor(ConsoleColor.Red);
                Console.WriteLine($"  ✖  {message}");
                ResetColor();
            }
            catch { }
        }

        public static int ReadInt(string prompt, int min, int max, string formatHint = "")
        {
            string? result = ReadInput(prompt, formatHint.Length > 0 ? formatHint : $"{min}-{max}", required: true, allowBack: true, validator: input =>
            {
                if (input == BACK_COMMAND) return null;
                if (!int.TryParse(input, out int value)) return $"Please enter a valid integer between {min} and {max}.";
                if (value < min || value > max) return $"Please enter an integer between {min} and {max}.";
                return null;
            });

            if (result == BACK_COMMAND) return int.MinValue;
            return int.Parse(result!);
        }

        public static decimal ReadDecimal(string prompt, decimal min, string formatHint = "")
        {
            string? result = ReadInput(prompt, formatHint.Length > 0 ? formatHint : $"≥ {min}", required: true, allowBack: true, validator: input =>
            {
                if (input == BACK_COMMAND) return null;
                if (!decimal.TryParse(input, out decimal value)) return $"Please enter a valid decimal number ≥ {min}.";
                if (value < min) return $"Please enter a decimal number ≥ {min}.";
                return null;
            });

            if (result == BACK_COMMAND) return decimal.MinValue;
            return decimal.Parse(result!);
        }

        public static DateTime ReadDate(string prompt, string formatHint = "dd-MM-yyyy")
        {
            string? result = ReadInput(prompt, formatHint, required: true, allowBack: true, validator: input =>
            {
                if (input == BACK_COMMAND) return null;
                if (!DateTime.TryParseExact(input, "dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime _))
                    return $"Invalid date. Use format: {formatHint}";
                return null;
            });

            if (result == BACK_COMMAND) return DateTime.MinValue;
            return DateTime.ParseExact(result!, "dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture);
        }

        public static DateTime ReadDateTime(string prompt, string formatHint = "dd-MM-yyyy HH:mm")
        {
            string? result = ReadInput(prompt, formatHint, required: true, allowBack: true, validator: input =>
            {
                if (input == BACK_COMMAND) return null;
                if (!DateTime.TryParseExact(input, "dd-MM-yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime _))
                    return $"Invalid date/time. Use format: {formatHint}";
                return null;
            });

            if (result == BACK_COMMAND) return DateTime.MinValue;
            return DateTime.ParseExact(result!, "dd-MM-yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
        }

        // ─────────────────────────── CONFIRM / KEY ───────────────────────────

        public static bool Confirm(string message)
        {
            while (true)
            {
                SetColor(ConsoleColor.Yellow);
                Console.Write($"  ◆  {message} [Y/N] : ");
                SetColor(ConsoleColor.Cyan);
                string? answer = Console.ReadLine()?.Trim().ToUpper();
                ResetColor();

                if (answer == "Y") return true;
                if (answer == "N") return false;

                SetColor(ConsoleColor.Red);
                Console.WriteLine(GetIndent() + "  ✖  Please enter Y or N.");
                ResetColor();
            }
        }

        public static void PressAnyKey(string message = "Press any key to continue...")
        {
            Console.WriteLine();
            SetColor(ConsoleColor.DarkGray);
            Console.Write($"  ►  {message}");
            ResetColor();
            Console.ReadKey(intercept: true);
            Console.WriteLine();
        }

        // ─────────────────────────── TABLES ───────────────────────────

        public static void PrintTable<T>(
            IEnumerable<T> items,
            Dictionary<string, Func<T, string>> columns,
            string? title = null)
        {
            _tableRenderCount++;

            var list = new List<T>();
            foreach (var item in items) list.Add(item);

            if (!string.IsNullOrWhiteSpace(title))
                PrintSubHeader(title);

            if (list.Count == 0)
            {
                PrintWarning("No records found.");
                return;
            }

            var headers = new List<string>();
            foreach (var k in columns.Keys) headers.Add(k);

            var extractors = new List<Func<T, string>>();
            foreach (var v in columns.Values) extractors.Add(v);

            string indent = GetIndent();
            int maxTableWidth = Math.Max(headers.Count * 6 + 1, SafeWidth() - indent.Length - 2);
            var widths = new int[headers.Count];

            for (int i = 0; i < headers.Count; i++)
            {
                int maxData = 0;
                foreach (var row in list)
                {
                    string dataVal = extractors[i](row) ?? string.Empty;
                    if (dataVal.Length > maxData) maxData = dataVal.Length;
                }

                int desiredWidth = Math.Max(headers[i].Length, maxData) + 4;
                widths[i] = Math.Min(desiredWidth, GetPreferredColumnWidth(headers[i]));
            }

            FitTableWidths(widths, headers, maxTableWidth);
            string border    = BuildBorderRow(widths, "╔", "╦", "╗", '═');
            string midBorder = BuildBorderRow(widths, "╠", "╬", "╣", '═');
            string rowBorder = BuildBorderRow(widths, "╟", "╫", "╢", '─');
            string botBorder = BuildBorderRow(widths, "╚", "╩", "╝", '═');

            Console.WriteLine();

            // ── Header row ──
            SetColor(ConsoleColor.DarkCyan);
            Console.WriteLine(indent + border);
            Console.Write(indent + "║");
            for (int i = 0; i < headers.Count; i++)
            {
                SetColor(ConsoleColor.Yellow);
                Console.Write(FormatTableCell(headers[i], widths[i], alignRight: false));
                SetColor(ConsoleColor.DarkCyan);
                Console.Write("║");
            }
            Console.WriteLine();
            SetColor(ConsoleColor.DarkCyan);
            Console.WriteLine(indent + midBorder);

            // ── Data rows ──
            bool alternate = false;
            for (int r = 0; r < list.Count; r++)
            {
                Console.Write(indent + "║");
                for (int i = 0; i < extractors.Count; i++)
                {
                    string raw = extractors[i](list[r]) ?? string.Empty;
                    // Status-aware coloring
                    ConsoleColor cellColor = GetStatusColor(headers[i], raw, alternate);
                    SetColor(cellColor);
                    Console.Write(FormatTableCell(raw, widths[i], alignRight: ShouldAlignRight(headers[i])));
                    SetColor(ConsoleColor.DarkCyan);
                    Console.Write("║");
                }
                Console.WriteLine();
                alternate = !alternate;

                if (r < list.Count - 1)
                {
                    SetColor(ConsoleColor.DarkGray);
                    Console.WriteLine(indent + rowBorder);
                }
            }

            SetColor(ConsoleColor.DarkCyan);
            Console.WriteLine(indent + botBorder);
            ResetColor();
            Console.WriteLine();
        }

        /// <summary>Returns a cell color based on header name and cell value for status-aware coloring.</summary>
        private static ConsoleColor GetStatusColor(string header, string value, bool alternate)
        {
            string h = header.ToLower().Replace(" ", "").Replace("_", "");
            string v = value.ToLower();
            // Status column coloring
            if (h == "status")
            {
                if (v.Contains("confirm") || v.Contains("active") || v.Contains("paid") || v.Contains("complete")) return ConsoleColor.Green;
                if (v.Contains("cancel") || v.Contains("reject") || v.Contains("fail") || v.Contains("inactive"))  return ConsoleColor.Red;
                if (v.Contains("pending") || v.Contains("partial") || v.Contains("hold"))                         return ConsoleColor.Yellow;
            }
            // Amount / Price in Cyan
            if (h == "amount" || h == "price" || h == "priceperhour") return ConsoleColor.Cyan;
            // ID column in DarkGray
            if (h == "id" || h == "#" || h == "bookingid") return ConsoleColor.DarkGray;
            // Default alternating
            return alternate ? ConsoleColor.Gray : ConsoleColor.White;
        }
        public static int ShowPaginatedTable<T>(
            List<T> items,
            Dictionary<string, Func<T, string>> columns,
            string title,
            int pageSize = PAGE_SIZE)
        {
            if (items.Count == 0)
            {
                PrintWarning("No records found.");
                return 0;
            }

            int totalPages = (int)Math.Ceiling(items.Count / (double)pageSize);
            int currentPage = 1;

            while (true)
            {
                try { Console.Clear(); } catch (System.IO.IOException) { }
                PrintHeader(title);

                int startIdx = (currentPage - 1) * pageSize;
                int endIdx   = Math.Min(startIdx + pageSize, items.Count);

                var pageItems = new List<T>();
                for (int i = startIdx; i < endIdx; i++) pageItems.Add(items[i]);

                var displayColumns = new Dictionary<string, Func<T, string>>();
                displayColumns["#"] = item => (pageItems.IndexOf(item) + 1 + startIdx).ToString();
                foreach (var kvp in columns)
                {
                    if (kvp.Key != "#")
                    {
                        displayColumns[kvp.Key] = kvp.Value;
                    }
                }

                PrintTable(pageItems, displayColumns);

                SetColor(ConsoleColor.DarkGray);
                Console.WriteLine($"  Page {currentPage} of {totalPages}  |  Total: {items.Count} record(s)");
                Console.WriteLine();
                ResetColor();

                // Navigation bar
                SetColor(ConsoleColor.DarkCyan);
                Console.Write(GetIndent() + "  Navigation: ");
                if (currentPage > 1) { SetColor(ConsoleColor.White); Console.Write("[P] Prev  "); }
                if (currentPage < totalPages) { SetColor(ConsoleColor.White); Console.Write("[N] Next  "); }
                SetColor(ConsoleColor.White); Console.Write("[S] Search  ");
                SetColor(ConsoleColor.DarkGray); Console.Write("[0] Back");
                Console.WriteLine();
                ResetColor();

                while (true)
                {
                    SetColor(ConsoleColor.DarkYellow);
                    int firstChoice = startIdx + 1;
                    int lastChoice = endIdx;
                    Console.Write($"  ➤ Choice ({firstChoice}-{lastChoice}, P/N/S, or 0=Back) : ");
                    SetColor(ConsoleColor.Cyan);
                    string? nav = Console.ReadLine()?.Trim().ToUpper();
                    ResetColor();

                    if (nav == "0") return 0;

                    if (nav == "N" && currentPage < totalPages) { currentPage++; break; }
                    if (nav == "P" && currentPage > 1) { currentPage--; break; }

                    if (nav == "S")
                    {
                        string keyword = ReadInput("Search Keyword", "", false, true).ToLower();
                        if (keyword != BACK_COMMAND && !string.IsNullOrWhiteSpace(keyword))
                        {
                            var filtered = new List<T>();
                            var filteredIdx = new List<int>();
                            for (int i = 0; i < items.Count; i++)
                            {
                                foreach (var ext in columns.Values)
                                {
                                    string val = ext(items[i]) ?? "";
                                    if (val.ToLower().Contains(keyword))
                                    {
                                        filtered.Add(items[i]);
                                        filteredIdx.Add(i);
                                        break;
                                    }
                                }
                            }
                            int sel = ShowPaginatedTable(filtered, columns, $"{title} (Search: \"{keyword}\")", pageSize);
                            if (sel > 0) return filteredIdx[sel - 1] + 1;
                        }
                        break;
                    }

                    if (int.TryParse(nav, out int choice) && choice >= firstChoice && choice <= lastChoice)
                        return choice;

                    SetColor(ConsoleColor.Red);
                    Console.WriteLine(GetIndent() + "  ✖  Invalid choice. Please try again.");
                    ResetColor();
                }
            }
        }

        // ─────────────────────────── FORMATTERS ───────────────────────────

        public static string FormatCurrency(decimal amount) => $"Rs.{amount:N2}";
        public static string FormatDate(DateTime dt)        => dt.ToString("dd-MMM-yyyy");
        public static string FormatDateTime(DateTime dt)    => dt.ToString("dd-MMM-yyyy HH:mm");
        public static string FormatBool(bool value)         => value ? "Yes" : "No";

        public static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s ?? string.Empty;
            return s[..(max - 3)] + "...";
        }

        // ─────────────────────────── GOODBYE SCREEN ───────────────────────────

        public static void ShowGoodbyeScreen()
        {
            Console.Clear();
            const int W = 44;
            string ind = "  ";
            try
            {
                int width = Console.WindowWidth;
                if (width > (W + 2))
                    ind = new string(' ', (width - (W + 2)) / 2);
            }
            catch { }

            Console.WriteLine();
            SetColor(ConsoleColor.Cyan);
            Console.WriteLine(ind + "╔" + new string('═', W) + "╗");
            Console.WriteLine(ind + "║" + new string(' ', W) + "║");

            PrintCenteredLine("Thank you for using", W, ConsoleColor.White, ind);
            PrintCenteredLine("Hall  Booking  System", W, ConsoleColor.Yellow, ind);
            Console.Write(ind + "║"); SetColor(ConsoleColor.DarkCyan);
            Console.Write(new string(' ', W)); SetColor(ConsoleColor.Cyan); Console.WriteLine("║");
            PrintCenteredLine("Goodbye!  👋", W, ConsoleColor.Cyan, ind);

            Console.WriteLine(ind + "║" + new string(' ', W) + "║");
            Console.WriteLine(ind + "╚" + new string('═', W) + "╝");
            Console.WriteLine();
            ResetColor();
        }

        // ─────────────────────────── PRIVATE HELPERS ───────────────────────────

        private static void PrintCenteredLine(string text, int width, ConsoleColor color, string ind)
        {
            string padded = CenterPad(text, width);
            SetColor(ConsoleColor.Cyan);
            Console.Write(ind + "║");
            SetColor(color);
            Console.Write(padded);
            SetColor(ConsoleColor.Cyan);
            Console.WriteLine("║");
        }

        public static string CenterPad(string text, int width)
        {
            if (text.Length >= width) return text[..width];
            int totalPad = width - text.Length;
            int leftPad  = totalPad / 2;
            int rightPad = totalPad - leftPad;
            return new string(' ', leftPad) + text + new string(' ', rightPad);
        }

        private static void SetColor(ConsoleColor c)  => Console.ForegroundColor = c;
        private static void ResetColor()               => Console.ResetColor();

        private static string FormatTableCell(string value, int width, bool alignRight = false)
        {
            value = (value ?? string.Empty).ReplaceLineEndings(" ").Trim();
            int contentWidth = Math.Max(1, width - 2);

            if (value.Length > contentWidth)
                value = value[..contentWidth];

            string aligned = alignRight
                ? value.PadLeft(contentWidth)
                : value.PadRight(contentWidth);

            return " " + aligned + " ";
        }

        private static string BuildBorderRow(int[] widths, string left, string mid, string right, char fill)
        {
            var sb = new StringBuilder();
            sb.Append(left);
            for (int i = 0; i < widths.Length; i++)
            {
                sb.Append(new string(fill, widths[i]));
                sb.Append(i < widths.Length - 1 ? mid : right);
            }
            return sb.ToString();
        }

        private static int GetPreferredColumnWidth(string header)
        {
            switch (header.ToLower().Replace(" ", "").Replace("_", ""))
            {
                case "id": case "#": return 6;
                case "bookingid": return 12;
                case "name": case "customer": case "customername": case "hall": case "hallname": return 34;
                case "purpose": return 40;
                case "date": return 15;
                case "start": case "end": return 18;
                case "amount": return 16;
                case "price": case "priceperhour": return 16;
                case "duration": return 15;
                case "status": return 12;
                case "email": return 36;
                case "reason": return 44;
                default: return 36;
            }
        }

        private static bool ShouldAlignRight(string header)
        {
            string key = header.ToLower().Replace(" ", "").Replace("_", "");
            return key is "amount" or "price" or "priceperhour" or "duration" or "bookingid" or "id" or "#";
        }

        private static void FitTableWidths(int[] widths, List<string> headers, int maxWidth)
        {
            int separatorWidth = widths.Length + 1;
            int availableCellWidth = Math.Max(widths.Length * 5, maxWidth - separatorWidth);

            int total = 0;
            foreach (var w in widths) total += w;

            if (total > availableCellWidth && total > 0)
            {
                for (int i = 0; i < widths.Length; i++)
                {
                    int minWidth = Math.Min(Math.Max(headers[i].Length + 2, 5), 20);
                    widths[i] = Math.Max(minWidth, (int)Math.Floor((double)widths[i] / total * availableCellWidth));
                }
            }

            while (CurrentTableWidth(widths) > maxWidth)
            {
                int widestIndex = 0;
                for (int i = 1; i < widths.Length; i++)
                {
                    if (widths[i] > widths[widestIndex])
                        widestIndex = i;
                }

                int minWidth = Math.Min(Math.Max(headers[widestIndex].Length + 2, 5), 20);
                if (widths[widestIndex] <= minWidth)
                    break;

                widths[widestIndex]--;
            }
        }

        private static int CurrentTableWidth(int[] widths)
        {
            int total = widths.Length + 1;
            foreach (var w in widths) total += w;
            return total;
        }
    }
}
