namespace ZBase.Foundation.Data.Authoring
{
    public static class SheetUtility
    {
        public static bool ValidateSheetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            return name.StartsWith('$') == false
                && name.StartsWith('<') == false
                && name.EndsWith('>') == false
                ;
        }

        public static string ToFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            if (name.StartsWith('<') || name.EndsWith('>'))
            {
                return $"${name.Replace("<", "").Replace(">", "")}";
            }

            return name;
        }

        public static string ToFileName(string name, int index)
        {
            if (string.IsNullOrWhiteSpace(name))
                return $"${index}";

            if (name.StartsWith('<') || name.EndsWith('>'))
            {
                return $"${name.Replace("<", "").Replace(">", "")}";
            }

            return name;
        }
    }
}
