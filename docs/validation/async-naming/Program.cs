using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;

MSBuildLocator.RegisterMSBuildPath("C:/Program Files/dotnet/sdk/10.0.401");
await Run();

static string Key(IMethodSymbol symbol) => symbol.OriginalDefinition.ContainingAssembly.Name + ":" + symbol.OriginalDefinition.GetDocumentationCommentId();
static bool RequiredByExternal(IMethodSymbol method)
{
    if (method.OverriddenMethod is { } parent && !parent.Locations.Any(location => location.IsInSource)) return true;
    return method.ContainingType.AllInterfaces.Any(contract => contract.GetMembers().OfType<IMethodSymbol>().Any(member =>
        !member.Locations.Any(location => location.IsInSource) && SymbolEqualityComparer.Default.Equals(method.ContainingType.FindImplementationForInterfaceMember(member), method)));
}
static async Task Run()
{
    string root = @"\\MYLEGION\Shared\DoubleEntryHomeBookkeeping";
    using MSBuildWorkspace workspace = MSBuildWorkspace.Create(new Dictionary<string,string> {
        ["UseArtifactsOutput"] = "true", ["ArtifactsPath"] = root + @"\docs\validation\cumulative-implementation"
    });
    workspace.WorkspaceFailed += (_, e) => Console.WriteLine("WORKSPACE: " + e.Diagnostic.Message);
    Solution solution = await workspace.OpenSolutionAsync(root + @"\backend\DoubleEntryHomeBookkeeping.slnx");
    Dictionary<string,string> names = new();
    List<(Document Doc, SyntaxNode Root, SemanticModel Model)> documents = new();
    foreach (Project project in solution.Projects)
    {
        Console.WriteLine("Inspect " + project.Name);
        foreach (Document doc in project.Documents.Where(doc => doc.FilePath != null && doc.FilePath.StartsWith(root + @"\backend\", StringComparison.OrdinalIgnoreCase)))
        {
            SyntaxNode? tree = await doc.GetSyntaxRootAsync();
            SemanticModel? model = await doc.GetSemanticModelAsync();
            if (tree == null || model == null) continue;
            documents.Add((doc,tree,model));
            foreach (MethodDeclarationSyntax node in tree.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                IMethodSymbol? symbol = model.GetDeclaredSymbol(node) as IMethodSymbol;
                if (symbol == null || !symbol.Name.EndsWith("Async") || RequiredByExternal(symbol)) continue;
                string name = symbol.Name[..^5];
                names[Key(symbol)] = name;
                foreach (IMethodSymbol sync in symbol.ContainingType.GetMembers(name).OfType<IMethodSymbol>())
                    if (sync.Locations.Any(location => location.IsInSource) && !RequiredByExternal(sync)) names[Key(sync)] = name + "Sync";
            }
        }
    }
    int files = 0, edits = 0;
    List<string> log = names.OrderBy(pair => pair.Key).Select(pair => pair.Key + " -> " + pair.Value).ToList();
    foreach ((Document doc, SyntaxNode tree, SemanticModel model) in documents)
    {
        Dictionary<TextSpan,string> changes = new();
        foreach (SyntaxNode node in tree.DescendantNodes(descendIntoTrivia:true))
        {
            IMethodSymbol? method = null;
            SyntaxToken token = default;
            if (node is MethodDeclarationSyntax declaration) { method = model.GetDeclaredSymbol(declaration) as IMethodSymbol; token = declaration.Identifier; }
            else if (node is SimpleNameSyntax name)
            {
                method = model.GetSymbolInfo(name).Symbol as IMethodSymbol;
                token = name.Identifier;
            }
            if (method != null && names.TryGetValue(Key(method.ReducedFrom ?? method), out string? replacement)) changes[token.Span] = replacement;
        }
        if (changes.Count == 0) continue;
        SourceText text = await doc.GetTextAsync();
        SourceText updated = text.WithChanges(changes.Select(pair => new TextChange(pair.Key,pair.Value)));
        File.WriteAllText(doc.FilePath!,updated.ToString(),new System.Text.UTF8Encoding(false));
        files++; edits += changes.Count;
    }
    File.WriteAllLines(root + @"\docs\validation\async-naming\renamed-methods.txt", log);
    Console.WriteLine($"Renamed {names.Count} declarations; {edits} edits in {files} files.");
}