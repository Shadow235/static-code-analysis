using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace StaticCodeAnalysis.Analyzator.Core;

public class CodeAnalysis
{
    private readonly string _solutionFilePath;
    private Dictionary<string, MethodDeclaration> _methods = new();

    private CodeAnalysis(string solutionFilePath)
    {
        _solutionFilePath = solutionFilePath;
    }

    public Dictionary<string, MethodDeclaration> MethodDeclarations => _methods ??= new Dictionary<string, MethodDeclaration>();

    public static async Task<CodeAnalysis> Create(string solutionFilePath)
    {
        var codeAnalysis = new CodeAnalysis(solutionFilePath);
        await codeAnalysis.GetMethodDeclarations();

        return codeAnalysis;
    }

    public List<List<MethodDeclaration>> GetAllCallPaths(string methodDeclaration)
    {
        var method = _methods[methodDeclaration];
        var allPaths = new List<List<MethodDeclaration>>();
        var visited = new HashSet<string>();

        DfsRecursive(method, new List<MethodDeclaration>(), allPaths, visited);
        
        return allPaths;
    }
    
   private static void DfsRecursive(
        MethodDeclaration currentNode,
        List<MethodDeclaration> currentPath,
        List<List<MethodDeclaration>> allPaths,
        HashSet<string> visited
    )
    {
        currentPath.Add(currentNode);
        visited.Add(currentNode.Key);

        if (currentNode.Callers == null || !currentNode.Callers.Any())
        {
            // Aktuální uzel nemá další volání (je to "list" v tomto směru)
            allPaths.Add(new List<MethodDeclaration>(currentPath)); // Přidejte kopii cesty
        }
        else
        {
            foreach (var nextNode in currentNode.Callers)
            {
                if (!visited.Contains(nextNode.Key))
                {
                    DfsRecursive(nextNode, currentPath, allPaths, visited);
                }
                // Pokud chcete procházet i cykly, tuto podmínku vynechejte,
                // ale dejte pozor na potenciálně nekonečnou rekurzi.
            }
        }

        // Backtracking: Odstraníme aktuální uzel z cesty a ze seznamu navštívených
        // prozkoumání ostatních větví.
        currentPath.RemoveAt(currentPath.Count - 1);
        visited.Remove(currentNode.Key);
    }

    private async Task GetMethodDeclarations()
    {
        using var workspace = MSBuildWorkspace.Create();
        var solution = await workspace.OpenSolutionAsync(_solutionFilePath);
        
        var methodDeclarationDictionary = new Dictionary<string, MethodDeclaration>();

        foreach (var project in solution.Projects)
        {
            foreach (var document in project.Documents)
            {
                var syntaxTree = await document.GetSyntaxTreeAsync();
                if (syntaxTree == null) continue;

                var semanticModel = await document.GetSemanticModelAsync();
                if (semanticModel == null) continue;

                var namespaceDeclarations = syntaxTree.GetRoot().DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>();

                foreach (var namespaceDeclaration in namespaceDeclarations)
                {
                    var classDeclarations = namespaceDeclaration.DescendantNodes().OfType<ClassDeclarationSyntax>();

                    foreach (var classDeclaration in classDeclarations)
                    {
                        var methodDeclarations = classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>();

                        foreach (var methodDeclaration in methodDeclarations)
                        {
                            //Console.WriteLine($"{namespaceDeclaration.Name}.{classDeclaration.Identifier}.{methodDeclaration.Identifier}{methodDeclaration.ParameterList}");

                            var invocationDeclarations = methodDeclaration.DescendantNodes().OfType<InvocationExpressionSyntax>();
                            var invocationMethodSymbols = new List<IMethodSymbol>();
                            foreach (var invocationDeclaration in invocationDeclarations)
                            {
                                var methodSymbolInfo = semanticModel.GetSymbolInfo(invocationDeclaration.Expression);
                                var methodSymbol = methodSymbolInfo.Symbol as IMethodSymbol;

                                invocationMethodSymbols.Add(methodSymbol);
                            }

                            var method = new MethodDeclaration(
                                Namespace: namespaceDeclaration.Name.ToString(),
                                ClassName: classDeclaration.Identifier.ToString(),
                                MethodName: methodDeclaration.Identifier.ToString(),
                                ParameterList: methodDeclaration.ParameterList.ToString(),
                                Body: methodDeclaration.Body.ToString(),
                                MethodDeclarationSyntax: methodDeclaration,
                                Invocations: invocationMethodSymbols,
                                Callers: new List<MethodDeclaration>(),
                                Calls: new List<MethodDeclaration>());

                            methodDeclarationDictionary.Add(method.ToString(), method);
                        }
                    }
                }
            }
        }
        
        foreach (var item in methodDeclarationDictionary)
        {
            foreach (var invocation in item.Value.Invocations)
            {
                var key = $"{invocation.ContainingType.ContainingNamespace}.{invocation.ContainingType.Name}.{invocation.Name}({string.Join(", ", invocation.Parameters.Select(x => $"{x.Type.ToDisplayString()} {x.Name}"))})";

                if (methodDeclarationDictionary.ContainsKey(key))
                {
                    var invocationDeclaration = methodDeclarationDictionary[key];

                    item.Value.Calls.Add(invocationDeclaration); // Call
                    invocationDeclaration.Callers.Add(item.Value); // Called
                }
            }
        }
        
        _methods = methodDeclarationDictionary;
    }
}