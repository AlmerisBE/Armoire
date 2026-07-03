namespace Armoire.Features.ModSwapper.Engines;

using Armoire.Features.ModSwapper.Patchers;
using Dalamud.Plugin.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class JsonMutationEngine : IJsonMutationEngine {
    private readonly IBinaryPatcher binaryPatcher;
    private readonly IPluginLog pluginLog;

    private readonly Regex modelIdRegex = new Regex(@"([ewa]\d{4})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public JsonMutationEngine(IBinaryPatcher binaryPatcher, IPluginLog pluginLog) {
        this.binaryPatcher = binaryPatcher;
        this.pluginLog = pluginLog;
    }

    public bool MutateModConfig(string originalJson, string fullModPath, string slotKey, string targetModelId, out string modifiedJson) {
        modifiedJson = string.Empty;

        try {
            var root = JToken.Parse(originalJson);
            bool hasChanged = RecurseAndReplace(root, slotKey, targetModelId, fullModPath);

            if (hasChanged) {
                modifiedJson = root.ToString();
                return true;
            }
            return false;

        } catch (Exception ex) {
            this.pluginLog.Error(ex, "[JsonMutationEngine] Failed to parse and mutate JSON config.");
            return false;
        }
    }

    private bool RecurseAndReplace(JToken token, string slotKey, string targetModelId, string fullModPath) {
        bool changed = false;

        if (token is JObject obj) {
            // 1. Processing the "Files" dictionary
            if (obj["Files"] is JObject files) {
                var properties = files.Properties().ToList();

                // Pass 1: Intercept cross-slot swaps (e.g., Top to Down) and extract them into FileSwaps
                foreach (var prop in properties) {
                    string originalGamePath = prop.Name;

                    if (IsPathMatchingSlot(originalGamePath, slotKey)) {
                        // Special case: Converting long-torso files into virtual FileSwaps
                        if (slotKey == "dwn" && originalGamePath.Contains("_top")) {

                            // Safely retrieve or create the FileSwaps object using pattern matching
                            if (obj["FileSwaps"] is not JObject fileSwaps) {
                                fileSwaps = new JObject();
                                obj["FileSwaps"] = fileSwaps;
                            }

                            string targetPath = this.modelIdRegex.Replace(originalGamePath, targetModelId).Replace("_top", "_dwn");

                            // Apply the universal clone (c0101) for virtual redirections
                            var raceRegex = new Regex(@"c\d{4}");
                            string universalTargetPath = raceRegex.Replace(targetPath, "c0101");

                            // Create deep clones BEFORE removing the property to avoid Newtonsoft parent-graph exceptions
                            JToken? clonedValue1 = prop.Value?.DeepClone();
                            JToken? clonedValue2 = prop.Value?.DeepClone();

                            fileSwaps[targetPath] = clonedValue1;
                            if (targetPath != universalTargetPath) {
                                fileSwaps[universalTargetPath] = clonedValue2;
                            }

                            prop.Remove();
                            changed = true;
                        }
                    }
                }

                // Pass 2: Safe standard processing on the remaining intact files
                changed |= ProcessJObjectPaths(files, slotKey, targetModelId, fullModPath);
            }

            // 2. Processing the "FileSwaps" dictionary
            if (obj["FileSwaps"] is JObject fileSwapsObj) {
                changed |= ProcessJObjectPaths(fileSwapsObj, slotKey, targetModelId, fullModPath);
            }

            // 3. Recursive traversal for option sub-groups
            foreach (var property in obj.Properties().ToList()) {
                if (property.Name != "Files" && property.Name != "FileSwaps" && (property.Value is JObject || property.Value is JArray)) {
                    changed |= RecurseAndReplace(property.Value, slotKey, targetModelId, fullModPath);
                }
            }
        } else if (token is JArray arr) {
            foreach (var item in arr) {
                changed |= RecurseAndReplace(item, slotKey, targetModelId, fullModPath);
            }
        }

        return changed;
    }

    private bool ProcessJObjectPaths(JObject dict, string slotKey, string targetModelId, string fullModPath) {
        var propertiesToReplace = new List<JProperty>();
        bool hasChanged = false;

        foreach (var prop in dict.Properties()) {
            string originalGamePath = prop.Name;

            if (IsPathMatchingSlot(originalGamePath, slotKey)) {
                // ACCESSORY SAFETY: Skip mismatched internal accessory IDs
                if (targetModelId.StartsWith("a")) {
                    bool isMismatchedAccessory =
                        (slotKey == "wrs" && !originalGamePath.Contains("_wrs")) ||
                        (slotKey == "rir" && !originalGamePath.Contains("_rir")) ||
                        (slotKey == "ear" && !originalGamePath.Contains("_ear"));

                    if (isMismatchedAccessory) {
                        continue;
                    }
                }

                string newGamePath = this.modelIdRegex.Replace(originalGamePath, targetModelId);
                if (newGamePath != originalGamePath) {
                    propertiesToReplace.Add(prop);
                }
            }
        }

        foreach (var prop in propertiesToReplace) {
            string originalGamePath = prop.Name;
            JToken? localFilePath = prop.Value;

            var match = this.modelIdRegex.Match(originalGamePath);
            string oldModelId = match.Success ? match.Value : string.Empty;
            string newGamePath = this.modelIdRegex.Replace(originalGamePath, targetModelId);

            // --- 1. UNIVERSAL INTERCEPTION NET (The "Folder Illusion") ---
            var pathsToRegister = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            pathsToRegister.Add(newGamePath);

            var raceRegex = new Regex(@"c\d{4}");
            string unisexPath = raceRegex.Replace(newGamePath, "c0101");
            pathsToRegister.Add(unisexPath);

            var expandedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var variantFolderRegex = new Regex(@"/v\d{4}/");

            // We flood the variant folders (/v0001/, /v0002/, /v0003/) to ensure Penumbra intercepts 
            // the game's request. We intentionally DO NOT flood the _a.mtrl / _b.mtrl suffixes anymore,
            // because items that use multiple materials (like skin + leather) would overwrite each other!
            foreach (var path in pathsToRegister) {
                expandedPaths.Add(path);

                if (path.Contains("/material/") || path.Contains("/texture/")) {
                    for (int v = 1; v <= 3; v++) {
                        string vFolder = $"/v{v:D4}/";
                        string pathWithFolder = variantFolderRegex.IsMatch(path)
                            ? variantFolderRegex.Replace(path, vFolder)
                            : path;

                        expandedPaths.Add(pathWithFolder);
                    }
                }
            }
            pathsToRegister = expandedPaths;

            prop.Remove();

            // Extract the string value safely
            string localStr = (localFilePath?.Type == JTokenType.String)
                ? (localFilePath.ToString() ?? string.Empty)
                : string.Empty;

            // --- 2. DUAL BINARY PATCHING ---
            if (!string.IsNullOrEmpty(localStr) &&
                (localStr.EndsWith(".mdl", StringComparison.OrdinalIgnoreCase) ||
                 localStr.EndsWith(".mtrl", StringComparison.OrdinalIgnoreCase) ||
                 localStr.EndsWith(".tex", StringComparison.OrdinalIgnoreCase)) &&
                 !string.IsNullOrEmpty(oldModelId)) {

                string patchedPath = this.binaryPatcher.PatchBinaryFile(fullModPath, localStr, oldModelId, targetModelId);

                foreach (var path in pathsToRegister) {
                    dict[path] = patchedPath;
                }
            } else {
                foreach (var path in pathsToRegister) {
                    dict[path] = localFilePath?.DeepClone();
                }
            }

            hasChanged = true;
        }

        return hasChanged;
    }

    private bool IsPathMatchingSlot(string path, string slotKey) {
        string lowerPath = path.ToLowerInvariant();
        if (slotKey != "wpn" && slotKey != "sub" && slotKey != "custom" && slotKey != "unknown") {
            return lowerPath.Contains($"_{slotKey}.") || lowerPath.Contains($"_{slotKey}_");
        }
        if (slotKey == "wpn" || slotKey == "sub") {
            return lowerPath.Contains("chara/weapon/");
        }

        return false;
    }
}