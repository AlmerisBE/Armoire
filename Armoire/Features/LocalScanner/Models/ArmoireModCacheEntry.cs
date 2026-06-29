namespace Armoire.Features.LocalScanner.Models;

using System.Collections.Generic;

public class ArmoireOptionGroup {
    public string Type { get; set; } = "Single";
    // The index of this list corresponds to the option index or the bit position
    public List<HashSet<string>> OptionPaths { get; set; } = new();
}

public class ArmoireModCacheEntry {
    public string DirectoryName { get; set; } = string.Empty;
    public string ModName { get; set; } = string.Empty;

    // Default modified files (without any options checked)
    public HashSet<string> ModifiedGamePaths { get; set; } = new(System.StringComparer.OrdinalIgnoreCase);

    // NEW: Conditionally modified files, grouped by Group Name
    public Dictionary<string, ArmoireOptionGroup> OptionGroups { get; set; } = new(System.StringComparer.OrdinalIgnoreCase);
}