namespace Armoire.Features.ModDetails;

using Armoire.Core.Localization;
using Armoire.Features.ConflictEngine.Models;
using Armoire.Features.GameData;
using Armoire.Features.LocalScanner;
using Armoire.Features.ModDetails.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public interface IModDetailsResolver {
    DetailedModState? ResolveModDetails(string modId, EffectiveCollectionState currentState);
}

public class ModDetailsResolver : IModDetailsResolver {
    private readonly IModScannerManager scannerManager;
    private readonly IGameDataService gameDataService;
    private readonly ILocalizationService loc;
    private readonly ArmoireConfiguration config;

    private readonly Regex modelIdRegex = new Regex(@"([ewa]\d{4})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ModDetailsResolver(IModScannerManager scannerManager, IGameDataService gameDataService, ILocalizationService loc, ArmoireConfiguration config) {
        this.scannerManager = scannerManager;
        this.gameDataService = gameDataService;
        this.loc = loc;
        this.config = config;
    }

    public DetailedModState? ResolveModDetails(string modId, EffectiveCollectionState currentState) {
        if (!this.scannerManager.ModCache.TryGetValue(modId, out var cachedMod)) {
            return null;
        }

        if (!currentState.EffectiveMods.TryGetValue(modId, out var activeModSettings)) {
            return null;
        }

        var result = new DetailedModState {
            ModId = modId,
            ModName = cachedMod.ModName,
            IsEnabled = activeModSettings.IsEnabled,
            Priority = activeModSettings.Priority
        };

        var targetPaths = GetActivePathsForMod(cachedMod, activeModSettings.Settings);

        // --- GLOBAL TEXTURE PRESENCE DETECTION ---
        // We evaluate if the mod as a whole contains materials/textures for its models
        var allModelIdsWithMdl = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allModelIdsWithTex = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var p in targetPaths) {
            var match = this.modelIdRegex.Match(p);
            if (match.Success) {
                if (p.EndsWith(".mdl", StringComparison.OrdinalIgnoreCase)) {
                    allModelIdsWithMdl.Add(match.Value);
                } else if (p.EndsWith(".mtrl", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".tex", StringComparison.OrdinalIgnoreCase)) {
                    allModelIdsWithTex.Add(match.Value);
                }
            }
        }
        // -----------------------------------------

        var higherPriorityMods = currentState.EffectiveMods.Values
            .Where(m => m.IsEnabled && m.Priority > activeModSettings.Priority)
            .OrderByDescending(m => m.Priority)
            .ToList();

        var rawSlots = new List<DetailedSlotState>();

        foreach (var path in targetPaths) {
            var resolvedData = this.gameDataService.ResolveItem(path);
            var slotState = new DetailedSlotState {
                AffectedPaths = new List<string> { path },
                LocalizedItemName = resolvedData.Name,
                IconId = resolvedData.IconId,
                ItemId = resolvedData.ItemId,
                SlotCategory = resolvedData.SlotKey,
                EquipLevel = resolvedData.EquipLevel,
                ItemLevel = resolvedData.ItemLevel
            };

            foreach (var enemy in higherPriorityMods) {
                if (this.scannerManager.ModCache.TryGetValue(enemy.Id, out var enemyCache)) {
                    var enemyPaths = GetActivePathsForMod(enemyCache, enemy.Settings);

                    if (enemyPaths.Contains(path, StringComparer.OrdinalIgnoreCase)) {
                        slotState.IsConflicting = true;
                        string priorityStr = string.Format(this.loc.GetString("ModDetails_PriorityLabel"), enemy.Priority);
                        slotState.OverwrittenByMods.Add($"{enemy.Name}{priorityStr}");
                        break;
                    }
                }
            }

            rawSlots.Add(slotState);
        }

        result.ReplacedSlots = rawSlots
            .GroupBy(s => new { s.LocalizedItemName, s.IsConflicting, Winners = string.Join(",", s.OverwrittenByMods), s.IconId, s.SlotCategory, s.EquipLevel, s.ItemLevel })
            .Select(g => {
                var paths = g.SelectMany(x => x.AffectedPaths).Distinct().ToList();

                var modelIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in paths) {
                    var match = this.modelIdRegex.Match(p);
                    if (match.Success) {
                        modelIds.Add(match.Value);
                    }
                }

                // An item group is flagged as missing textures ONLY IF its base model ID 
                // has a 3D model (.mdl) but absolutely NO materials or textures anywhere in the entire mod.
                bool isMissingTextures = modelIds.Any(mId => allModelIdsWithMdl.Contains(mId) && !allModelIdsWithTex.Contains(mId));

                var slotState = new DetailedSlotState {
                    LocalizedItemName = g.Key.LocalizedItemName,
                    IconId = g.Key.IconId,
                    ItemId = g.First().ItemId,
                    SlotCategory = g.Key.SlotCategory,
                    IsConflicting = g.Key.IsConflicting,
                    OverwrittenByMods = g.First().OverwrittenByMods,
                    AffectedPaths = paths,
                    IsMissingTextures = isMissingTextures,
                    EquipLevel = g.Key.EquipLevel,
                    ItemLevel = g.Key.ItemLevel
                };

                // Load persistent texture provider selection from configuration if it exists
                if (this.config.ModifiedMods.TryGetValue(modId, out var modEntry) &&
                    modEntry.TextureProviders.TryGetValue(g.Key.SlotCategory, out var savedProviderId)) {
                    slotState.SelectedTextureProviderId = savedProviderId;
                }

                // Search for potential providers only if genuinely missing textures
                if (isMissingTextures) {
                    foreach (var otherMod in currentState.EffectiveMods.Values) {
                        if (!otherMod.IsEnabled || otherMod.Id == modId) {
                            continue;
                        }

                        if (this.scannerManager.ModCache.TryGetValue(otherMod.Id, out var otherCache)) {
                            var otherPaths = GetActivePathsForMod(otherCache, otherMod.Settings);

                            bool providesTexture = otherPaths.Any(p =>
                                (p.EndsWith(".mtrl", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".tex", StringComparison.OrdinalIgnoreCase)) &&
                                modelIds.Any(mId => p.Contains(mId, StringComparison.OrdinalIgnoreCase))
                            );

                            if (providesTexture) {
                                slotState.AvailableTextureProviders[otherMod.Id] = otherMod.Name;
                            }
                        }
                    }
                }

                return slotState;
            })
            .OrderByDescending(s => s.IsConflicting)
            .ThenBy(s => s.LocalizedItemName)
            .ToList();

        return result;
    }

    private HashSet<string> GetActivePathsForMod(Armoire.Features.LocalScanner.Models.ArmoireModCacheEntry cache, Dictionary<string, uint> userSettings) {
        var paths = new HashSet<string>(cache.ModifiedGamePaths, StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in cache.OptionGroups) {
            uint setting = userSettings.TryGetValue(kvp.Key, out var val) ? val : 0;

            if (kvp.Value.Type.Equals("Multi", StringComparison.OrdinalIgnoreCase)) {
                for (int i = 0; i < kvp.Value.OptionPaths.Count; i++) {
                    if ((setting & (1u << i)) != 0) {
                        foreach (var p in kvp.Value.OptionPaths[i]) {
                            paths.Add(p);
                        }
                    }
                }
            } else {
                int index = (int)setting;
                if (index >= 0 && index < kvp.Value.OptionPaths.Count) {
                    foreach (var p in kvp.Value.OptionPaths[index]) {
                        paths.Add(p);
                    }
                }
            }
        }
        return paths;
    }
}