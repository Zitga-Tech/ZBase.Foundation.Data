using Microsoft.CodeAnalysis;
using ZBase.Foundation.SourceGen;

namespace ZBase.Foundation.Data.DatabaseSourceGen
{
    public class ConverterRef
    {
        public ConverterKind Kind { get; set; }

        public ITypeSymbol ConverterType { get; set; }

        public ITypeSymbol TargetType { get; set; }

        public TypeRef SourceTypeRef { get; } = new();

        public string Convert(string expression)
        {
            if (ConverterType == null)
            {
                return expression;
            }

            if (Kind == ConverterKind.Instance)
            {
                return $"new {ConverterType.ToFullName()}().Convert({expression})";
            }

            if (Kind == ConverterKind.Static)
            {
                return $"{ConverterType.ToFullName()}.Convert({expression})";
            }

            return expression;
        }
    }
}
