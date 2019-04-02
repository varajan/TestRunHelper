using System.Linq;

namespace TestRunHelper.Helpers
{
    public static class ListHelper<T>
    {
        public static bool ExistsIn(T expected, params T[] list) => list.Any(line => line.Equals(expected));
    }
}