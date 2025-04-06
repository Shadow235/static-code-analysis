using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalysis.Analyzator.Core;

public record MethodDeclaration(
    string Namespace,
    string ClassName,
    string MethodName,
    string ParameterList,
    string Body,
    MethodDeclarationSyntax MethodDeclarationSyntax,
    List<IMethodSymbol> Invocations,
    List<MethodDeclaration> Callers,
    List<MethodDeclaration> Calls
)
{
    public string Key => $"{Namespace}.{ClassName}.{MethodName}{ParameterList}";
    
    public override string ToString()
    {
        return $"{Namespace}.{ClassName}.{MethodName}{ParameterList}";
    }
};