[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
param (
    [string]$NewVersion = ""
)

# 1. Demande de la version si non fournie
if ([string]::IsNullOrWhiteSpace($NewVersion)) {
    $NewVersion = Read-Host "Entrez le nouveau numéro de version (ex: 0.1.0.4)"
}

$tagVersion = "v$NewVersion"
Write-Host "Lancement de la publication pour la version $tagVersion..." -ForegroundColor Cyan

# Définition des chemins (ajuste-les si l'arborescence est légèrement différente)
$csprojPath = "Armoire\Armoire.csproj"
$pluginJsonPath = "Armoire\Armoire.json"
$repoJsonPath = "repo.json"

# 2. Mise à jour de Armoire.csproj
if (Test-Path $csprojPath) {
    Write-Host "Mise à jour de $csprojPath..."
    (Get-Content $csprojPath) -replace '<Version>.*</Version>', "<Version>$NewVersion</Version>" | Set-Content $csprojPath
} else {
    Write-Warning "Fichier $csprojPath introuvable."
}

# 3. Mise à jour de Armoire.json
if (Test-Path $pluginJsonPath) {
    Write-Host "Mise à jour de $pluginJsonPath..."
    (Get-Content $pluginJsonPath) -replace '"AssemblyVersion":\s*".*"', "`"AssemblyVersion`": `"$NewVersion`"" | Set-Content $pluginJsonPath
} else {
    Write-Warning "Fichier $pluginJsonPath introuvable."
}

# 4. Mise à jour de repo.json (Version et liens)
if (Test-Path $repoJsonPath) {
    Write-Host "Mise à jour de $repoJsonPath..."
    $repoContent = Get-Content $repoJsonPath -Raw
    
    # Remplacement de l'AssemblyVersion normale et Testing
    $repoContent = $repoContent -replace '"AssemblyVersion":\s*".*"', "`"AssemblyVersion`": `"$NewVersion`""
    $repoContent = $repoContent -replace '"TestingAssemblyVersion":\s*".*"', "`"TestingAssemblyVersion`": `"$NewVersion`""
    
    # Remplacement du tag dans les URLs de téléchargement
    $repoContent = $repoContent -replace 'download/v[^/]+/latest.zip', "download/$tagVersion/latest.zip"
    
    Set-Content -Path $repoJsonPath -Value $repoContent -NoNewline
} else {
    Write-Warning "Fichier $repoJsonPath introuvable."
}

# 5. Opérations Git
Write-Host "Exécution des commandes Git..." -ForegroundColor Yellow

# S'assurer qu'on est sur dev
git checkout dev

# Ajouter et commiter les fichiers modifiés
git add $csprojPath $pluginJsonPath $repoJsonPath
git commit -m "ci: bump version to $NewVersion"

# Pousser dev sur GitHub
git push origin dev

# Basculer sur production, METTRE À JOUR, et fusionner dev
git checkout production
git pull origin production  # <-- LIGNE AJOUTÉE : On télécharge les nouveautés du serveur
git merge dev

# Pousser la branche production
git push origin production

# Créer et pousser le Tag
git tag $tagVersion
git push origin $tagVersion

# Revenir sur dev pour continuer à travailler
git checkout dev

Write-Host "Terminé ! La version $tagVersion est en cours de compilation sur GitHub Actions." -ForegroundColor Green