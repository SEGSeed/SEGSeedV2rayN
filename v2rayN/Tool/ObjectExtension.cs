namespace v2rayN.Tool
{
    public static class ObjectExtension
    {

        public static T To<T>(this object source)
        {
            try
            {
                return (T)Convert.ChangeType(source, typeof(T));
            }
            catch
            {
                return default(T);
            }
        }

    }
}
