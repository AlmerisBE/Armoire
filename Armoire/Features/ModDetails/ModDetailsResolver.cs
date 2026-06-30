namespace Armoire.Features.ModDetails;

using Armoire.Core.Localization;
using Armoire.Features.ConflictEngine.Models;
using Armoire.Features.GameData;
using Armoire.Features.LocalScanner;
using Armoire.Features.ModDetails.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions; // Ajout nécessaire pour la Regex

public interface IModDetailsResolver {
    DetailedModState? ResolveModDetails(string modId, EffectiveCollectionState currentState);
}

public class ModDetailsResolver : IModDetailsResolver {
    private readonly IModScannerManager scannerManager;
    private readonly IGameDataService gameDataService;
    private readonly ILocalizationService loc;

    // Expression régulière pour extraire l'ID du modèle (ex: e0521, w0101)
    private readonly Regex modelIdRegex = new Regex(@"([ew]\d{4})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ModDetailsResolver(IModScannerManager scannerManager, IGameDataService gameDataService, ILocalizationService loc) {
        this.scannerManager = scannerManager;
        this.gameDataService = gameDataService;
        this.loc = loc;
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

        var higherPriorityMods = currentState.EffectiveMods.Values
            .Where(m => m.IsEnabled && m.Priority > activeModSettings.Priority)
            .OrderByDescending(m => m.Priority)
            .ToList();

        // 1. Collect all raw slots first
        var rawSlots = new List<DetailedSlotState>();

        foreach (var path in targetPaths) {
            var resolvedData = this.gameDataService.ResolveItem(path);

            var slotState = new DetailedSlotState {
                AffectedPaths = new List<string> { path },
                LocalizedItemName = resolvedData.Name,
                IconId = resolvedData.IconId,
                ItemId = resolvedData.ItemId,
                SlotCategory = resolvedData.SlotKey
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

        // 2. Group slots by LocalizedItemName, IsConflicting, and the exact winners
        result.ReplacedSlots = rawSlots
            .GroupBy(s => new { s.LocalizedItemName, s.IsConflicting, Winners = string.Join(",", s.OverwrittenByMods), s.IconId, s.SlotCategory })
            .Select(g => {
                var paths = g.SelectMany(x => x.AffectedPaths).Distinct().ToList();

                // --- LOGIQUE DE DÉTECTION D'HÉRITAGE DE TEXTURES ---
                bool hasMdl = paths.Any(p => p.EndsWith(".mdl", StringComparison.OrdinalIgnoreCase));
                bool hasTex = paths.Any(p => p.EndsWith(".mtrl", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".tex", StringComparison.OrdinalIgnoreCase));
                bool isMissingTextures = hasMdl && !hasTex;

                var slotState = new DetailedSlotState {
                    LocalizedItemName = g.Key.LocalizedItemName,
                    IconId = g.Key.IconId,
                    ItemId = g.First().ItemId,
                    SlotCategory = g.Key.SlotCategory,
                    IsConflicting = g.Key.IsConflicting,
                    OverwrittenByMods = g.First().OverwrittenByMods,
                    AffectedPaths = paths,
                    IsMissingTextures = isMissingTextures
                };

                // Si le mod est incomplet, on cherche les fournisseurs potentiels
                if (isMissingTextures) {
                    var modelIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var p in paths) {
                        var match = this.modelIdRegex.Match(p);
                        if (match.Success) {
                            modelIds.Add(match.Value);
                        }
                    }

                    // Parcourir tous les autres mods actifs de la collection
                    foreach (var otherMod in currentState.EffectiveMods.Values) {
                        if (!otherMod.IsEnabled || otherMod.Id == modId) {
                            continue;
                        }

                        if (this.scannerManager.ModCache.TryGetValue(otherMod.Id, out var otherCache)) {
                            var otherPaths = GetActivePathsForMod(otherCache, otherMod.Settings);

                            // Un mod est un fournisseur s'il contient un .mtrl ou .tex qui mentionne le même Model ID (ex: e0521)
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