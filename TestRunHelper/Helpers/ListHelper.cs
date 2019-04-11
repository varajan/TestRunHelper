using System.Collections.Generic;
using System.Linq;

namespace TestRunHelper.Helpers
{
    public static class ListHelper<T>
    {
        public static bool ExistsIn(T expected, params T[] list) => list.Any(line => line.Equals(expected));
    }

    public static class ListExtensions
    {
        private static string Get(this List<string> list, int i) => list.Count >= i ? list[i - 1] : null;
        public static string Second(this List<string> list) => list.Get(2);
        public static string Third(this List<string> list) => list.Get(3);
        public static string Fourth(this List<string> list) => list.Get(4);

        public static string Second(this string[] list) => list.ToList().Second();
        public static string Third(this string[] list) => list.ToList().Third();
        public static string Fourth(this string[] list) => list.ToList().Fourth();
    }
}