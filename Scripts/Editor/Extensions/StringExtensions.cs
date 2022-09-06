namespace BrunoMikoski.ServicesLocation
{
    public static partial class StringExtensions
    {
        public static string FirstToUpper(this string value)
        {
            if (value.Length == 0)
                return value;
        
            if (value.Length == 1)
                return value.ToUpper();
        
            return value.Substring(0, 1).ToUpper() + value.Substring(1);
        }
    }
}
