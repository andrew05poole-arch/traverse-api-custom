namespace TRAVERSE.Web.API
{
    public sealed class ApiUserSkipped
    {
        private ApiUserSkipped() { }

        static ApiUserSkipped() { Value = new ApiUserSkipped(); }

        public static bool IsApiUserSkipped(object item)
        {
            return (item == Value);
        }

        public readonly static ApiUserSkipped Value;
    }
}