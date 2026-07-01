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
git pull origin production
git merge dev

# Pousser la branche production
git push origin production

# Créer et pousser le Tag
git tag $tagVersion
git push origin $tagVersion

# Revenir sur dev pour continuer à travailler
git checkout dev

Write-Host "Terminé ! La version $tagVersion est en cours de compilation sur GitHub Actions." -ForegroundColor Green