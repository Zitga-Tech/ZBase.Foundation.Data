using System.Runtime.CompilerServices;

namespace Cathei.BakingSheet.Unity.Exposed
{
    public static class BakingSheetExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetTypeInfoEx(this SheetScriptableObject obj)
        {
            return obj.typeInfo;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddEx(this SheetScriptableObject obj, SheetRowScriptableObject row)
        {
            obj.Add(row);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetTypeInfoEx(this SheetScriptableObject obj, string value)
        {
            obj.typeInfo = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClearEx(this SheetScriptableObject obj)
        {
            obj.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClearEx(this SheetContainerScriptableObject obj)
        {
            obj.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetRowEx<T>(this SheetRowScriptableObject obj, T row)
            where T : class, ISheetRow
        {
            obj.SetRow(row);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddEx(this SheetContainerScriptableObject obj, SheetScriptableObject sheet)
        {
            obj.Add(sheet);
        }
    }
}
