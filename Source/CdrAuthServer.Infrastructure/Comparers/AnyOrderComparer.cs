namespace CdrAuthServer.Infrastructure.Comparers
{
    public class AnyOrderComparer : IEqualityComparer<string>
    {
        public bool Equals(string? x, string? y)
        {
            // Basic checks.
            if (x == y)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            if (x.Length != y.Length)
            {
                return false;
            }

            // Split the strings based on space delimiter, and sort into order.
            string[] arrayX = x.Split(' ');
            string[] arrayY = y.Split(' ');
            if (arrayX.Length != arrayY.Length)
            {
                return false;
            }

            Array.Sort(arrayX);
            Array.Sort(arrayY);

            // Join the strings back together from the sorted arrays.
            var sortedX = string.Join(" ", arrayX);
            var sortedY = string.Join(" ", arrayY);

            return sortedX.Equals(sortedY);
        }

        public int GetHashCode(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            var array = value.Split(' ');
            Array.Sort(array);
            var sortedString = string.Join(" ", array);
            return sortedString.GetHashCode();
        }
    }
}
