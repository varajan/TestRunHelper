using System.Linq;

namespace TestRunHelper.Helpers
{
    public static class StringHelper
    {
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
    }
}