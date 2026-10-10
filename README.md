# AI Separator Studio

Application Windows pour séparer une chanson en pistes audio avec des modèles d’intelligence artificielle. Le lanceur permet de choisir un fichier audio ou vidéo, un modèle, puis un dossier où enregistrer les pistes WAV.

## Application Windows (.exe)

Le dépôt contient aussi `SeparatorStudio.App`, une application Windows avec interface graphique qui utilise directement l'installation locale de `audio-separator`. Elle ne téléverse aucun fichier : le traitement et les modèles restent sur votre machine.

L'application permet de :

- sélectionner le dépôt, un fichier à traiter et le dossier de sortie ;
- choisir parmi les modèles réellement présents dans `models`, avec une indication de niveau (**Faible**, **Moyen**, **Élevé** ou **Meilleur**), vitesse, VRAM GPU conseillée et usage ;
- régler FP16, `demucs_shifts` et `demucs_overlap` lorsque le modèle Demucs est utilisé ;
- surveiller un dossier continuellement : les nouveaux fichiers audio ou vidéo sont ajoutés à la file de traitement ;
- afficher les journaux et les erreurs émis par `audio-separator`.

Après avoir installé l'environnement d'inférence décrit ci-dessous, créez l'exécutable Windows autonome :

```powershell
.\Publish-SeparatorStudio.ps1
```

L'exécutable est produit dans `artifacts\SeparatorStudio\SeparatorStudio.App.exe`. Il est autonome : l'utilisateur final n'a pas besoin d'installer .NET, mais le dépôt AI, l'environnement `inference-env`, les modèles, FFmpeg et les pilotes GPU restent nécessaires.

Les niveaux de modèles sont des repères pour choisir le compromis : **Faible** privilégie la rapidité, **Moyen** l'équilibre, **Élevé** la qualité, et **Meilleur** les modèles recommandés les plus exigeants. La séparation obtenue dépend aussi de la qualité et du mixage du fichier source.

### Fonctionnement 24/7

Dans l'application, sélectionnez **Surveiller un dossier en continu**, choisissez un dossier d'entrée et lancez la surveillance. Fermer la fenêtre réduit l'application dans la zone de notification ; utilisez **Arrêter la surveillance** avant de quitter réellement.

Pour la relancer automatiquement à chaque ouverture de session Windows, après publication, exécutez :

```powershell
.\Install-AlwaysOn.ps1
```

Ce script installe une tâche planifiée qui redémarre l'application jusqu'à trois fois en cas d'échec. Pour un fonctionnement réellement continu, utilisez un PC ou serveur Windows maintenu sous tension, désactivez la mise en veille et conservez une session utilisateur ouverte ; une application graphique ne peut pas traiter des fichiers dans une session Windows inexistante.

## Ce que contient le dépôt

- Les scripts PowerShell pour lancer la séparation et l’entraînement.
- L'application source WinForms et les scripts de publication de l'exécutable.
- Les modèles de séparation, versionnés avec [Git LFS](https://git-lfs.com/).
- Les poids d’un modèle d’entraînement.

Les environnements Python, les jeux de données et les fichiers audio de sortie ne sont pas inclus. Le téléchargement des modèles demande environ **7,6 Go** et nécessite Git LFS. Pour cloner ce dépôt privé, votre compte GitHub doit aussi y avoir accès.

## Prérequis

- Windows 10 ou 11.
- Python 3.11 et Git.
- Git LFS installé et activé.
- Pour l’accélération prévue par le lanceur : GPU NVIDIA avec pilotes et bibliothèques CUDA compatibles. Un lancement sans accélération GPU peut être beaucoup plus lent; le fonctionnement dépend alors de la configuration installée.
- FFmpeg disponible. Vérifiez sa détection avec `audio-separator --env_info`.

## Installation sur un nouvel ordinateur

Le script `Lancer-Separation.ps1` recherche automatiquement le projet à l’emplacement `%USERPROFILE%\AI-Music-Separation`. Clonez donc le dépôt directement dans ce dossier (et non dans un sous-dossier `ai-separator-studio`). Dans PowerShell :

```powershell
git lfs install
git clone https://github.com/maxlo245/ai-separator-studio.git "$env:USERPROFILE\AI-Music-Separation"
Set-Location "$env:USERPROFILE\AI-Music-Separation"
git lfs pull
```

Créez ensuite l’environnement Python de séparation et installez `audio-separator` avec les dépendances GPU :

```powershell
py -3.11 -m venv inference-env
.\inference-env\Scripts\Activate.ps1
python -m pip install --upgrade pip
python -m pip install "audio-separator[gpu]==0.47.0"
audio-separator --env_info
```

La dernière commande affiche les capacités détectées. Pour l’accélération NVIDIA, vérifiez que CUDA est disponible pour ONNX Runtime; FFmpeg doit également être détecté. La configuration CUDA/PyTorch peut dépendre de la carte graphique et des pilotes du PC. Consultez le guide d’installation officiel d’[`audio-separator`](https://github.com/nomadkaraoke/python-audio-separator#installation) si la carte ou FFmpeg n’est pas détecté.

> Si votre dépôt local est déjà dans un autre dossier, le lanceur actuel ne le trouvera pas automatiquement : il attend `%USERPROFILE%\AI-Music-Separation`.

## Lancer une séparation

Dans PowerShell, depuis `AI-Music-Separation` :

```powershell
.\Lancer-Separation.ps1
```

Choisissez un fichier audio ou vidéo, puis le dossier de sortie et le mode :

- **Oui** : séparation voix / instrumental avec MelBand-RoFormer.
- **Non** : séparation en six pistes avec HTDemucs 6-stems (voix, batterie, basse, guitare, piano et autres).

Les résultats sont enregistrés en WAV dans le dossier choisi. Le dossier `outputs` est proposé par défaut.

## Est-ce que la séparation fonctionne juste après le téléchargement ?

**Pas immédiatement.** Le dépôt fournit les scripts et les fichiers des modèles, mais pas l’environnement Python, les pilotes GPU ni FFmpeg. Après un clone complet avec Git LFS et l’installation ci-dessus, la séparation peut fonctionner si le PC dispose des dépendances compatibles. Le premier lancement peut prendre plusieurs minutes.

Si les gros modèles semblent absents ou ont une taille minuscule, vérifiez que Git LFS est installé, puis lancez `git lfs pull` dans le dépôt.

## Entraînement

Le script `Lancer-Entrainement.ps1` est également fourni, mais entraîner un modèle demande un environnement séparé, des jeux de données dans le format attendu par le script et beaucoup de temps de calcul. Ces jeux de données et l’environnement d’entraînement ne sont pas inclus dans le dépôt.

## Dépannage

- **« L’environnement de séparation est introuvable »** : créez `inference-env` à la racine du dépôt avec les commandes d’installation ci-dessus.
- **Modèle introuvable ou fichier trop petit** : exécutez `git lfs pull` et vérifiez que le téléchargement des modèles s’est terminé.
- **GPU non détecté** : lancez `audio-separator --env_info`, puis vérifiez les pilotes NVIDIA et la compatibilité CUDA/PyTorch/ONNX Runtime.
- **FFmpeg non détecté** : installez FFmpeg pour Windows et assurez-vous qu’il est accessible dans `PATH`, puis relancez `audio-separator --env_info`.

## Références

- [audio-separator : installation et configuration GPU](https://github.com/nomadkaraoke/python-audio-separator)
- [Git LFS](https://git-lfs.com/)
