namespace TextAnimationsForUIToolkit
{
    public static class TagUtility
    {
        public static string OpeningTag(string name)
        {
            return $"<{name}>";
        }

        public static string OpeningTag(string name, string parameters)
        {
            if (parameters.Length == 0)
            {
                return OpeningTag(name);
            }

            return $"<{name} {parameters}>";
        }

        public static string ClosingTag(string name)
        {
            return $"</{name}>";
        }
    }
}
