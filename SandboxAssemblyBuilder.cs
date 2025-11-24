using Microsoft.CodeAnalysis;

public class SandboxAssemblyBuilder {
	private List<MetadataReference> references = new();

	public void BuildSandboxAssembly() {
		// Get all runtime assemblies
		var runtimeAssemblies = AppDomain.CurrentDomain.GetAssemblies()
			.Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
			.ToList();

		// Add core assemblies for basic types
		foreach (var asm in runtimeAssemblies) {
			// Skip SYSTEM assemblies
			var isSystemAssembly = asm.FullName?.StartsWith("System.") == true ||
			                       asm.FullName?.StartsWith("Microsoft.") == true;
			
			if (isSystemAssembly && asm.GetTypes().Any(t => t.Namespace == "System.IO"))
				continue;
			
			// any further requirements go here

			references.Add(MetadataReference.CreateFromFile(asm.Location));
		}
	}

	public IEnumerable<MetadataReference> GetReferences() => references;
}