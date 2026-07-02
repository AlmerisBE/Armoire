namespace Armoire.Features.Outfits.Presentation;

using Armoire.Features.Outfits.Models;
using Armoire.Features.PenumbraIpc;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;

public class OutfitImportPresenter : IOutfitImportPresenter {
    private readonly OutfitRepository repository;
    private readonly IPenumbraClient penumbraClient;

    public bool IsVisible { get; set; } = false;
    public string ShareCode { get; set; } = string.Empty;
    public string ErrorMessage { get; private set; } = string.Empty;

    public ArmoireOutfit? StagedOutfit { get; private set; }

    private readonly List<OutfitModRequirement> missingMods = new();
    public IReadOnlyList<OutfitModRequirement> MissingMods => this.missingMods;

    public OutfitImportPresenter(OutfitRepository repository, IPenumbraClient penumbraClient) {
        this.repository = repository;
        this.penumbraClient = penumbraClient;
    }

    public void Open() {
        this.ShareCode = string.Empty;
        this.ErrorMessage = string.Empty;
        this.StagedOutfit = null;
        this.missingMods.Clear();
        this.IsVisible = true;
    }

    public void AnalyzeCode() {
        this.ErrorMessage = string.Empty;
        this.StagedOutfit = null;
        this.missingMods.Clear();

        if (string.IsNullOrWhiteSpace(this.ShareCode) || !this.ShareCode.StartsWith("armoire_v1:")) {
            this.ErrorMessage = "Import_ErrorPrefix";
            return;
        }

        try {
            // 1. Decode Base64 and Decompress GZip
            string base64Payload = this.ShareCode.Substring("armoire_v1:".Length).Trim();
            byte[] compressedBytes = Convert.FromBase64String(base64Payload);

            using var compressedStream = new MemoryStream(compressedBytes);
            using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var resultStream = new MemoryStream();

            gzipStream.CopyTo(resultStream);
            string json = System.Text.Encoding.UTF8.GetString(resultStream.ToArray());

            // 2. Rebuild the Outfit object
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            this.StagedOutfit = JsonSerializer.Deserialize<ArmoireOutfit>(json, options);

            if (this.StagedOutfit == null) {
                this.ErrorMessage = "Import_ErrorCorrupted";
                return;
            }

            // 3. Analyze missing mods against physical Penumbra installation
            var installedMods = this.penumbraClient.GetRawModsList();
            foreach (var req in this.StagedOutfit.RequiredMods) {
                if (req.IsIgnored) {
                    continue; // Skip mods the original author decided to ignore
                }

                bool isInstalled = installedMods.ContainsKey(req.ModId) || installedMods.Values.Any(v => v.Equals(req.Name, StringComparison.OrdinalIgnoreCase));
                if (!isInstalled) {
                    this.missingMods.Add(req);
                }
            }
        } catch (InvalidDataException) {
            // CRC32 failure from GZip
            this.ErrorMessage = "Import_ErrorCRC";
        } catch {
            this.ErrorMessage = "Import_ErrorGeneric";
        }
    }

    public void ConfirmImport() {
        if (this.StagedOutfit != null) {
            // Generate a fresh GUID so we don't accidentally overwrite an existing local outfit
            this.StagedOutfit.Id = Guid.NewGuid();
            this.StagedOutfit.CreatedAt = DateTimeOffset.UtcNow; // Update timestamp to import time

            this.repository.AddOutfit(this.StagedOutfit);
        }
        CancelImport();
    }

    public void CancelImport() {
        this.IsVisible = false;
        this.StagedOutfit = null;
        this.ShareCode = string.Empty;
        this.missingMods.Clear();
    }
}