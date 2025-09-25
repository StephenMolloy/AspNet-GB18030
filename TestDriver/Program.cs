using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Gb18030.Shared;

namespace Gb18030.TestDriver
{
    internal class Program
    {
        static int Main(string[] args)
        {
            var options = CliOptions.Parse(args);
            if (options.ShowHelp)
            {
                CliOptions.PrintHelp();
                return 1;
            }

            var allStrings = TestStringProvider.LoadAll();
            Console.WriteLine($"Loaded {allStrings.Length} test strings from file.");

            string[] activeStrings;
            bool custom = false;
            if (!string.IsNullOrEmpty(options.CustomString))
            {
                activeStrings = new[] { options.CustomString };
                custom = true;
                Console.WriteLine("Using custom string override (file set ignored for iteration).");
            }
            else if (options.SingleIndex.HasValue)
            {
                int idx = options.SingleIndex.Value;
                if (idx < 0 || idx >= allStrings.Length)
                {
                    Console.Error.WriteLine("Index out of range.");
                    return 2;
                }
                activeStrings = new[] { allStrings[idx] };
                Console.WriteLine($"Testing only index {idx}.");
            }
            else
            {
                activeStrings = allStrings;
            }

            var http = new HttpClient();
            var tester = new WebPageTester(http, options.BaseUrl, options.EncodingName, custom ? activeStrings : allStrings, custom ? activeStrings[0] : null, options.Verbose);

            var aggregate = new List<ControlTestResult>();

            if (custom)
            {
                foreach (var page in tester.PagesToTest)
                {
                    ProcessPage(tester, page, 0, activeStrings[0], options.Verbose, aggregate);
                }
            }
            else if (options.SingleIndex.HasValue)
            {
                int idx = options.SingleIndex.Value;
                foreach (var page in tester.PagesToTest)
                {
                    ProcessPage(tester, page, idx, allStrings[idx], options.Verbose, aggregate);
                }
            }
            else
            {
                for (int i = 0; i < activeStrings.Length; i++)
                {
                    var expected = activeStrings[i];
                    foreach (var page in tester.PagesToTest)
                    {
                        ProcessPage(tester, page, i, expected, options.Verbose, aggregate);
                    }
                }
            }

            Report(aggregate, options.Verbose);
            return aggregate.Any(r => !r.Passed) ? 2 : 0;
        }

        private static void ProcessPage(WebPageTester tester, string page, int index, string expected, bool verbose, List<ControlTestResult> aggregate)
        {
            var pageResults = tester.TestPage(page, index, expected).ToList();
            aggregate.AddRange(pageResults);
            if (verbose)
            {
                WriteSeparator($"PAGE {page} index={index} HTML", ConsoleColor.Cyan);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(tester.LastHtml);
                Console.ResetColor();
                var pass = pageResults.Count(r => r.Passed);
                var fail = pageResults.Count - pass;
                var color = fail == 0 ? ConsoleColor.Green : ConsoleColor.Red;
                WriteSeparator($"PAGE SUMMARY {page} index={index}: {pass}/{pageResults.Count} passed, {fail} failed", color);
            }
        }

        private static void Report(List<ControlTestResult> results, bool verbose)
        {
            var grouped = results.GroupBy(r => r.ControlId).Select(g => new
            {
                Control = g.Key,
                Total = g.Count(),
                Fail = g.Count(x => !x.Passed)
            }).OrderBy(g => g.Control).ToList();

            Console.WriteLine();
            WriteSeparator("OVERALL SUMMARY", ConsoleColor.White);
            int total = results.Count, failed = results.Count(r => !r.Passed);
            var overallColor = failed == 0 ? ConsoleColor.Green : ConsoleColor.Red;
            WriteColoredLine($"Overall: {total - failed}/{total} passed, {failed} failed", overallColor, true);
            foreach (var g in grouped)
            {
                var ctrlColor = g.Fail == 0 ? ConsoleColor.Green : (g.Fail == g.Total ? ConsoleColor.Red : ConsoleColor.Yellow);
                WriteColoredLine($"{g.Control}: {(g.Total - g.Fail)}/{g.Total} passed", ctrlColor, false);
            }

            if (failed > 0)
            {
                Console.WriteLine();
                WriteSeparator("FAILURES", ConsoleColor.Red);
                foreach (var f in results.Where(r => !r.Passed).Take(200))
                {
                    WriteFailureLine(f);
                }
            }
            else
            {
                if (verbose)
                {
                    WriteSeparator("NO FAILURES", ConsoleColor.Green);
                }
            }
        }

        private static void WriteSeparator(string title, ConsoleColor color)
        {
            var line = new string('─', Math.Max(10, Math.Min(Console.WindowWidth - 1, 80)));
            Console.ForegroundColor = color;
            Console.WriteLine(line);
            Console.WriteLine(title);
            Console.WriteLine(line);
            Console.ResetColor();
        }

        private static void WriteColoredLine(string text, ConsoleColor color, bool padBlankBefore)
        {
            if (padBlankBefore) { /* reserved for spacing if needed later */ }
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = prev;
        }

        private static void WriteFailureLine(ControlTestResult f)
        {
            string exp = f.Expected ?? string.Empty;
            string act = f.Actual ?? string.Empty;
            // Compute per-character match using LCS so inserts/deletes highlighted.
            bool[] expMatch, actMatch;
            ComputeDiffMatchMasks(exp, act, out expMatch, out actMatch);

            Console.Write("[");
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(f.ControlId);
            Console.ForegroundColor = prev;
            Console.WriteLine($"] Page={f.Page} Index={f.StringIndex} Reason={f.Message}");

            Console.Write("    Expected='");
            WriteDiffString(exp, expMatch, ConsoleColor.Yellow, ConsoleColor.Red);
            Console.WriteLine("'");

            Console.Write("      Actual='");
            WriteDiffString(act, actMatch, ConsoleColor.Yellow, ConsoleColor.Red);
            Console.WriteLine("'");
        }

        private static string FormatInline(string s)
        {
            if (s == null) return string.Empty;
            return s.Replace("\r", "\\r").Replace("\n", "\\n");
        }

        private static void WriteDiffString(string text, bool[] matchMask, ConsoleColor matchColor, ConsoleColor diffColor)
        {
            var prev = Console.ForegroundColor;
            for (int i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                string display = ch == '\r' ? "\\r" : ch == '\n' ? "\\n" : ch.ToString();
                Console.ForegroundColor = matchMask.Length > i && matchMask[i] ? matchColor : diffColor;
                Console.Write(display);
            }
            Console.ForegroundColor = prev;
        }

        private static void ComputeDiffMatchMasks(string expected, string actual, out bool[] expMatch, out bool[] actMatch)
        {
            int m = expected.Length, n = actual.Length;
            expMatch = new bool[m];
            actMatch = new bool[n];
            if (m == 0 || n == 0) return;
            // DP LCS length table
            int[,] dp = new int[m + 1, n + 1];
            for (int i = m - 1; i >= 0; i--)
                for (int j = n - 1; j >= 0; j--)
                    dp[i, j] = expected[i] == actual[j] ? dp[i + 1, j + 1] + 1 : (dp[i + 1, j] >= dp[i, j + 1] ? dp[i + 1, j] : dp[i, j + 1]);

            int x = 0, y = 0;
            while (x < m && y < n)
            {
                if (expected[x] == actual[y])
                {
                    expMatch[x] = true;
                    actMatch[y] = true;
                    x++; y++;
                }
                else if (dp[x + 1, y] >= dp[x, y + 1]) x++; else y++;
            }
        }
    }

    internal class CliOptions
    {
        public string BaseUrl { get; private set; }
        public string EncodingName { get; private set; }
        public bool ShowHelp { get; private set; }
        public string CustomString { get; private set; }
        public int? SingleIndex { get; private set; }
        public bool Verbose { get; private set; }

        public static CliOptions Parse(string[] args)
        {
            var opt = new CliOptions();
            for (int i = 0; i < args.Length; i++)
            {
                var a = args[i];
                if (a == "--help" || a == "-h" || a == "/?") opt.ShowHelp = true;
                else if (a == "--baseUrl" && i + 1 < args.Length) opt.BaseUrl = args[++i];
                else if (a == "--encoding" && i + 1 < args.Length) opt.EncodingName = args[++i];
                else if (a == "--string" && i + 1 < args.Length) opt.CustomString = args[++i];
                else if (a == "--index" && i + 1 < args.Length)
                {
                    int parsed;
                    if (int.TryParse(args[++i], out parsed)) opt.SingleIndex = parsed;
                }
                else if (a == "--verbose" || a == "-v") opt.Verbose = true;
            }
            if (string.IsNullOrEmpty(opt.BaseUrl)) opt.ShowHelp = true;
            return opt;
        }

        public static void PrintHelp()
        {
            Console.WriteLine("Usage: TestDriver --baseUrl http://localhost:12345 [--encoding utf-8] [--index N] [--string \"custom text\"] [--verbose]");
            Console.WriteLine("  --string takes precedence over --index if both are supplied.");
        }
    }
}