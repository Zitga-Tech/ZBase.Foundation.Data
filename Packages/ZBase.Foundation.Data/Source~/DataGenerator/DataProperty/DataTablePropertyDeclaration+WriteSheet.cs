using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using ZBase.Foundation.SourceGen;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ZBase.Foundation.Data.DataSourceGen
{
    partial class DataTablePropertyDeclaration
    {
        public const string GENERATOR_NAME = nameof(DataTableGenerator);
        public void GenerateCodeForSheets(
                SourceProductionContext context
              , Compilation compilation
              , bool outputSourceGenFiles
              , DiagnosticDescriptor errorDescriptor
          )
        {
            var syntax = CompilationUnit().NormalizeWhitespace(eol: "\n");

            foreach (var typeRef in TypeRefs)
            {
                try
                {
                    var typeSyntax = typeRef.Syntax;
                    var source = WriteSheet(compilation.Assembly.Name, typeRef);
                    var sourceFilePath = syntax.SyntaxTree.GetGeneratedSourceFilePath(compilation.Assembly.Name, GENERATOR_NAME);

                    var outputSource = TypeCreationHelpers.GenerateSourceTextForRootNodes(
                          sourceFilePath
                        , syntax
                        , source
                        , context.CancellationToken
                    );

                    context.AddSource(
                          typeSyntax.SyntaxTree.GetGeneratedSourceFileName(GENERATOR_NAME, typeSyntax, typeRef.Symbol.ToValidIdentifier())
                        , outputSource
                    );

                    if (outputSourceGenFiles)
                    {
                        SourceGenHelpers.OutputSourceToFile(
                              context
                            , typeSyntax.GetLocation()
                            , sourceFilePath
                            , outputSource
                        );
                    }
                }
                catch (Exception e)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                          errorDescriptor
                        , typeRef.Syntax.GetLocation()
                        , e.ToUnityPrintableString()
                    ));
                }
            }
        }

        public void GenerateCodeForContainer(
               SourceProductionContext context
             , Compilation compilation
             , bool outputSourceGenFiles
             , DiagnosticDescriptor errorDescriptor
         )
        {
            var syntax = CompilationUnit().NormalizeWhitespace(eol: "\n");

            try
            {
                var syntaxTree = syntax.SyntaxTree;
                var source = WriteCodeContainer(compilation.Assembly.Name, TypeRefs);
                var sourceFilePath = syntaxTree.GetGeneratedSourceFilePath(compilation.Assembly.Name, GENERATOR_NAME);

                var outputSource = TypeCreationHelpers.GenerateSourceTextForRootNodes(
                              sourceFilePath
                            , syntax
                            , source
                            , context.CancellationToken
                        );

                var fileName = $"BakingSheetContainer_{compilation.Assembly.Name}";

                context.AddSource(
                      syntaxTree.GetGeneratedSourceFileName(GENERATOR_NAME, fileName, syntax)
                    , outputSource
                );

                if (outputSourceGenFiles)
                {
                    SourceGenHelpers.OutputSourceToFile(
                          context
                        , syntax.GetLocation()
                        , sourceFilePath
                        , outputSource
                    );
                }
            }
            catch (Exception e)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                      errorDescriptor
                    , syntax.GetLocation()
                    , e.ToUnityPrintableString()
                ));
            }

        }


        private static string WriteSheet(string assemblyName, TypeRef typeRef)
        {
            var className = $"{typeRef.Syntax.Identifier.Text}Sheet";
            var typeName = typeRef.TypeArgument.Name;

            var p = Printer.DefaultLarge;

            p.PrintEndLine();
            p.PrintLine("#pragma warning disable");
            p.PrintEndLine();

            p.PrintLine($"namespace ZBase.Foundation.Data.Authoring.__Editor.{assemblyName.ToValidIdentifier()}");
            p.OpenScope();
            {
                p.PrintBeginLine()
                    .Print($"public class {className}")
                    .Print($": global::Cathei.BakingSheet.Sheet<{typeRef.IdType.ToFullName()}, {className}.{typeName}>")
                    .PrintEndLine();
                p.OpenScope();
                {
                    p.PrintBeginLine()
                   .Print($"public class {typeName}")
                   .Print($": global::Cathei.BakingSheet.SheetRow<{typeRef.IdType.ToFullName()}>")
                   .PrintEndLine();

                    p.OpenScope();
                    {
                        foreach (var field in typeRef.Fields)
                        {
                            if (field.Type is not IArrayTypeSymbol arrayType)
                            {
                                p.PrintLine($"public {field.Type} {field.ToPropertyName()} {{get; private set;}}");
                            }
                            else
                            {
                                string elemTypeName;

                                if (typeRef.ElementTypes.Contains(arrayType.ElementType))
                                {
                                    elemTypeName = arrayType.ElementType.Name;
                                }
                                else
                                {
                                    elemTypeName = arrayType.ElementType.ToFullName();
                                }

                                if (field.HasAttribute(VERTICAL_LIST_ATTRIBUTE))
                                {
                                    p.PrintLine($"public {VERTICAL_LIST_TYPE}<{elemTypeName}> {field.ToPropertyName()} {{get; private set;}}");
                                }
                                else
                                {
                                    p.PrintLine($"public {LIST_TYPE}<{elemTypeName}> {field.ToPropertyName()} {{get; private set;}}");
                                }
                            }
                        }
                    }
                    p.CloseScope();

                    foreach (var elementType in typeRef.ElementTypes)
                    {
                        var elemMembers = elementType.GetMembers();
                        p.PrintLine($"public class {elementType.Name}");
                        p.OpenScope();
                        {
                            foreach (var member in elemMembers)
                            {
                                if (member is IFieldSymbol memberField)
                                {
                                    p.PrintLine($"public {memberField.Type} {memberField.ToPropertyName()} {{get; private set;}}");
                                }
                            }
                            p.CloseScope();

                        }
                    }
                    p.CloseScope();
                }
                p.CloseScope();

                return p.Result;
            }
        }

        private static string WriteCodeContainer(string assemblyName, ImmutableArray<TypeRef> typeRefs)
        {
            var p = Printer.DefaultLarge;


            p.PrintEndLine();
            p.PrintLine("#pragma warning disable");
            p.PrintEndLine();

            p.PrintLine($"namespace ZBase.Foundation.Data.Authoring.__Editor.{assemblyName.ToValidIdentifier()}");
            p.OpenScope();
            {
                p.PrintBeginLine()
                  .Print($"public class SheetContainer : global::Cathei.BakingSheet.SheetContainerBase")
                  .PrintEndLine();
                p.OpenScope();
                {
                    p.PrintLine("public SheetContainer(global::Microsoft.Extensions.Logging.ILogger logger) : base(logger) { }");
                    
                    foreach (var typeRef in typeRefs)
                    {
                        var className = $"{typeRef.Syntax.Identifier.Text}Sheet";
                        p.PrintLine($"public {className} {className} {{ get; set;}} ");
                    }

                }
                p.CloseScope();
            }
            p.CloseScope();

            return p.Result;
        }

        

    }
}
