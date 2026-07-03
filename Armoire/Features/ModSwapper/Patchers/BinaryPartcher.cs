namespace Armoire.Features.ModSwapper.Patchers;

using Dalamud.Plugin.Services;
using System;
using System.IO;

public class BinaryPatcher : IBinaryPatcher {
    private readonly IPluginLog pluginLog;

    public BinaryPatcher(IPluginLog pluginLog) {
        this.pluginLog = pluginLog;
    }

    public string PatchBinaryFile(string fullModPath, string localFilePath, string oldModelId, string targetModelId) {
        string absoluteOriginalPath = Path.Combine(fullModPath, localFilePath);
        if (!File.Exists(absoluteOriginalPath)) {
            return localFilePath;
        }

        string extension = Path.GetExtension(localFilePath);
        string newLocalPath = localFilePath.Replace(extension, $"_{targetModelId}_armoire{extension}", StringComparison.OrdinalIgnoreCase);
        string absolutePatchPath = Path.Combine(fullModPath, newLocalPath);

        try {
            byte[] fileBytes = File.ReadAllBytes(absoluteOriginalPath);
            byte[] searchBytes = System.Text.Encoding.ASCII.GetBytes(oldModelId.ToLowerInvariant());
            byte[] replaceBytes = System.Text.Encoding.ASCII.GetBytes(targetModelId.ToLowerInvariant());

            // Iterate through the binary array to find and replace the ASCII model ID
            for (int i = 0; i <= fileBytes.Length - searchBytes.Length; i++) {
                bool isMatch = true;
                for (int j = 0; j < searchBytes.Length; j++) {
                    if (fileBytes[i + j] != searchBytes[j]) {
                        isMatch = false;
                        break;
                    }
                }

                if (isMatch) {
                    for (int j = 0; j < replaceBytes.Length; j++) {
                        fileBytes[i + j] = replaceBytes[j];
                    }
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(absolutePatchPath)!);
            File.WriteAllBytes(absolutePatchPath, fileBytes);

            return newLocalPath;
        } catch (Exception ex) {
            this.pluginLog.Error(ex, $"[BinaryPatcher] Failed to binary patch {localFilePath}");
            return localFilePath;
        }
    }
}