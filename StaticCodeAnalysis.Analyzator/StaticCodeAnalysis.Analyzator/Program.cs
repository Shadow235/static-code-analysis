using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using StaticCodeAnalysis.Analyzator;

using var workspace = MSBuildWorkspace.Create();
var solution = await workspace.OpenSolutionAsync("C:\\Users\\Tekken\\Desktop\\Development\\StaticCodeAnalysis\\StaticCodeAnalysis.Target\\StaticCodeAnalysis.Target.sln"); // Nastav správnou cestu k řešení

//var methodDeclarationList = new HashSet<MethodDeclaration>();
var methodDeclarationDictionary = new Dictionary<string, MethodDeclaration>();

foreach (var project in solution.Projects)
{
    foreach (var document in project.Documents)
    {
        var syntaxTree = await document.GetSyntaxTreeAsync();
        if (syntaxTree == null) continue;

        var semanticModel = await document.GetSemanticModelAsync();
        if (semanticModel == null) continue;

        // var descendents = syntaxTree.GetRoot().DescendantNodes().Select(x => x.GetType()).ToList();
        //
        // foreach (var descendent in descendents)
        // {
        //     Console.WriteLine(descendent);
        // }

        // Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax
        
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
                        MethodDeclarationSyntax: methodDeclaration,
                        Invocations: invocationMethodSymbols,
                        Callers: new List<MethodDeclaration>(),
                        Calls: new List<MethodDeclaration>());

                    methodDeclarationDictionary.Add(method.ToString(), method);

                    // var invocationList = new List<MethodDeclaration>();
                    //
                    // var invocationDeclarations = methodDeclaration.DescendantNodes().OfType<InvocationExpressionSyntax>();
                    // foreach (var invocationDeclaration in invocationDeclarations)
                    // {
                    //     var methodSymbolInfo = semanticModel.GetSymbolInfo(invocationDeclaration.Expression);
                    //     var methodSymbol = methodSymbolInfo.Symbol as IMethodSymbol;
                    //
                    //     // Console.WriteLine($"({string.Join(", ", methodSymbol.Parameters.Select(x => $"{x.Type.ToDisplayString()} {x.Name}"))})");
                    //     //
                    //     invocationList.Add(new MethodDeclaration(
                    //         Namespace: methodSymbol.ContainingType.ContainingNamespace.Name,
                    //         ClassName: methodSymbol.ContainingType.Name,
                    //         MethodName: methodSymbol.Name,
                    //         ParameterList: $"({string.Join(", ", methodSymbol.Parameters.Select(x => $"{x.Type.ToDisplayString()} {x.Name}"))})"
                    //         MethodDeclarationSyntax: methodDeclaration,
                    //             
                    //         Callers: new List<MethodDeclaration>(),
                    //         Calls: new List<MethodDeclaration>()));
                    // }
                    //
                    //
                    //
                    // foreach (var invocation in invocationList)
                    // {
                    //     
                    // }
                }
            }
        }

    }
}

// Calles and callers
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

var anchorMethod = methodDeclarationDictionary["StaticCodeAnalysis.Target.C.Level3.Method3(int a, int b, int c)"];

// var stack = new Stack<MethodDeclaration>();
// var callStacks = new List<Stack<MethodDeclaration>>();
// var top = new List<MethodDeclaration>();
//
// stack.Push(anchorMethod);
//
// var callStack = new Stack<MethodDeclaration>();
//
// while (stack.Count > 0)
// {
//     var item = stack.Pop();
//     
//     callStack.Push(item);
//
//     if (item.Callers.Any())
//     {
//         foreach (var Caller in item.Callers)
//         {
//             stack.Push(Caller);
//         }
//     }
//     else
//     {
//         top.Add(item);
//         callStacks.Add(callStack);
//         callStack = new Stack<MethodDeclaration>();
//     }
// }

var allPaths = new List<List<MethodDeclaration>>();
var visited = new HashSet<string>();

DfsRecursive(anchorMethod, new List<MethodDeclaration>(), allPaths, visited);

Console.WriteLine(string.Empty);

static void DfsRecursive(
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