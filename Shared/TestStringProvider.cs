using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Gb18030.Shared
{
    public static class TestStringProvider
    {
        private static readonly object _lock = new object();
    private static string[] _cached;
        private const string FileName = "GB18030-strings.txt";

        public static string[] LoadAll()
        {
            if (_cached != null) return _cached;
            lock (_lock)
            {
                if (_cached != null) return _cached;
                var path = ResolveFilePath();
                var list = new List<string>();
                foreach (var raw in File.ReadAllLines(path, Encoding.UTF8))
                {
                    if (string.IsNullOrWhiteSpace(raw)) continue; // skip blank
                    if (raw.TrimStart().StartsWith("#")) continue; // skip comment
                    // Treat the entire raw line (minus trailing CR/LF already removed) as the test string.
                    // Preserve leading/trailing spaces (except newline) to allow explicit space tests.
                    list.Add(raw);
                }
                _cached = list.ToArray();
                return _cached;
            }
        }

        public static string GetByIndex(int index)
        {
            var all = LoadAll();
            if (index < 0 || index >= all.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return all[index];
        }

        // Parsing helper removed; lines are now treated as raw test strings.

        private static string ResolveFilePath()
        {
            // 1. Current base directory
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var direct = Path.Combine(baseDir, FileName);
            if (File.Exists(direct)) return direct;

            // 2. Look upward a few levels for Shared folder
            for (int i = 0; i < 6; i++)
            {
                var probe = Path.Combine(baseDir, Enumerable.Repeat("..", i + 1).Aggregate(Path.Combine), "Shared", FileName);
                if (File.Exists(probe)) return Path.GetFullPath(probe);
            }

            // 3. App_Data (web)
            var appData = Path.Combine(baseDir, "App_Data", FileName);
            if (File.Exists(appData)) return appData;

            throw new FileNotFoundException("Could not locate test strings file", FileName);
        }
    }
}