using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TestRunHelper.Helpers
{
    public static class StringHelper
    {
        public static bool IsVersionLessThen(this string version1, string version2)
        {
            var v1 = version1.Split('.');
            var v2 = version2.Split('.');

            var vAsInt1 = 1;
            var vAsInt2 = 1;

            for (int i = 1; i <= Math.Max(v1.Length, v2.Length); i++)
            {
                vAsInt1 *= 10;
                vAsInt2 *= 10;

                if (v1.Length >= i) vAsInt1 += int.Parse(v1[i - 1]);
                if (v2.Length >= i) vAsInt2 += int.Parse(v2[i - 1]);
            }

            return vAsInt1 < vAsInt2;
        }

        public static string HashString(this string line, int length = 20)
        {
            var hash = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(line));
            var result = hash.Aggregate("", (current, b) => current + b.ToString("X2"));

            return result.Substring(0, length);
        }

        public static int ToInt(this string value)
        {
            try
            {
                value = new string(value.Where((t, i) => i == 0 && t == '-' || char.IsDigit(t)).ToArray());
                return int.Parse(value);
            }
            catch
            {
                return 0;
            }
        }

        public static List<string> Split(this string line, int separator, int skipFirst = 0, int skipLast = 0)
        {
            return line.Split(separator.ToString(), skipFirst, skipLast);
        }

        public static List<string> Split(this string line, string separator, int skipFirst = 0, int skipLast = 0)
        {
            return Split(line, new[] { separator }, skipFirst, skipLast);
        }

        public static List<string> Split(this string line, string[] separators, int skipFirst = 0, int skipLast = 0)
        {
            if (string.IsNullOrEmpty(line)) return new List<string>();

            var result = new List<string>();
            var list = line.Split(separators, StringSplitOptions.None);

            for (var i = skipFirst; i < list.Length - skipLast; i++)
            {
                result.Add(list[i]);
            }

            return result;
        }

        public static string SubString(this string line, string from, string to)
        {
            var start = line.IndexOf(from, StringComparison.OrdinalIgnoreCase) + from.Length;
            var count = line.IndexOf(to, Math.Min(start, line.Length), StringComparison.OrdinalIgnoreCase) - start;

            return line.IndexOf(from, StringComparison.OrdinalIgnoreCase) >= 0 ? line.Substring(start, count) : string.Empty;
        }

        public static string SubString(this string line, string from)
        {
            var start = line.ContainsIgnoreCase(from)
                ? line.IndexOf(from, StringComparison.OrdinalIgnoreCase) + from.Length
                : line.Length;

            return line.Substring(start);
        }

        public static bool ContainsIgnoreCase(this string line, string word)
        {
            return line.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static string SubStringTo(this string line, string to)
        {
            var count = line.ContainsIgnoreCase(to) ? line.IndexOf(to, StringComparison.OrdinalIgnoreCase) : line.Length;

            return line.Substring(0, count);
        }

        public static string SubStringToLast(this string line, string to)
        {
            var count = line.ContainsIgnoreCase(to) ? line.LastIndexOf(to, StringComparison.OrdinalIgnoreCase) : line.Length;

            return line.Substring(0, count);
        }
    }
}