# AI Separator Studio

Application Windows locale pour séparer un morceau audio ou une vidéo en pistes à l'aide de modèles d'intelligence artificielle. Elle propose un lanceur graphique `.exe` pour choisir le fichier, le modèle et les réglages, tout en exécutant l'inférence avec `audio-separator` sur la machine locale.

> Les fichiers audio ne sont pas envoyés à un service distant : les modèles, les traitements et les résultats restent sur votre PC.

## Fonctionnalités

- Interface Windows pour lancer la séparation sans écrire de commande.
- Sélection d'un fichier audio ou vidéo et d'un dossier de sortie.
- Sélection des modèles installés avec une indication claire de niveau, vitesse, VRAM conseillée et usage.
- Réglages FP16 et Demucs (`demucs_shifts`, `demucs_overlap`) lorsque le modèle les prend en charge.
- Journaux de traitement et erreurs `audio-separator` visibles dans l'application.
- Surveillance continue d'un dossier : les nouveaux médias sont traités un par un.
- Redémarrage automatique à l'ouverture de session Windows pour un poste dédié.

## Sommaire

- [Configuration requise](#configuration-requise)
- [Installation rapide](#installation-rapide)
- [Installation détaillée](#installation-détaillée)
- [Utiliser l'application](#utiliser-lapplication)
- [Comprendre les niveaux de modèles](#comprendre-les-niveaux-de-modèles)
- [Traitement continu 24/7](#traitement-continu-247)
- [Lanceurs PowerShell existants](#lanceurs-powershell-existants)
- [Sources et licences des modèles](#sources-et-licences-des-modèles)
- [Entraînement personnalisé](#entraînement-personnalisé)
- [Dépannage](#dépannage)
- [Créer l'exécutable depuis les sources](#créer-lexécutable-depuis-les-sources)

## Configuration requise

| Élément | Requis | Détails |
| --- | --- | --- |
| Système | Oui | Windows 10 ou Windows 11 64 bits |
| Espace disque | Oui | Au moins 12 Go libres recommandés : environ 7,6 Go pour les modèles, auxquels s'ajoutent Python et les résultats |
| Git + Git LFS | Oui | Git LFS est indispensable pour télécharger les poids des modèles |
| Python | Oui | Python **3.11** pour l'environnement d'inférence |
| FFmpeg | Oui | Nécessaire pour lire de nombreux formats audio et vidéo |
| GPU NVIDIA | Recommandé | Accélère fortement l'inférence ; une carte avec 8 Go de VRAM ou plus est recommandée |
| .NET | Non pour l'utilisateur final | L'exécutable de release est autonome |

L'application peut fonctionner sans GPU si les dépendances installées le permettent, mais le traitement sera beaucoup plus lent. Les conseils de VRAM indiqués dans l'interface sont des repères ; la consommation réelle dépend du modèle et de la durée du média.

## Installation rapide

Ces étapes installent l'application recommandée sur un nouveau PC.

1. Installez Git, Git LFS, Python 3.11 et FFmpeg.
2. Clonez ce dépôt avec Git LFS dans un dossier local.
3. Créez l'environnement Python `inference-env` et installez `audio-separator`.
4. Téléchargez `SeparatorStudio.App.exe` depuis la [dernière release](https://github.com/maxlo245/ai-separator-studio/releases).
5. Lancez l'exécutable, choisissez le dossier du dépôt, puis sélectionnez votre fichier et votre modèle.

Les étapes suivantes détaillent chaque action.

## Installation détaillée

### 1. Installer les outils système

Installez les composants suivants depuis leurs sites officiels, puis fermez et rouvrez PowerShell :

- [Git for Windows](https://git-scm.com/download/win)
- [Git LFS](https://git-lfs.com/)
- [Python 3.11](https://www.python.org/downloads/windows/) — cochez l'option qui ajoute Python au `PATH`
- [FFmpeg](https://ffmpeg.org/download.html) — le dossier contenant `ffmpeg.exe` doit être ajouté au `PATH`
- Le pilote NVIDIA le plus récent si vous utilisez un GPU NVIDIA

Vérifiez les installations dans PowerShell :

```powershell
git --version
git lfs --version
py -3.11 --version
ffmpeg -version
```

Chaque commande doit afficher sa version. Si `ffmpeg` n'est pas reconnu, son dossier n'est pas encore présent dans le `PATH`.

### 2. Cloner le dépôt et télécharger les modèles

Choisissez un dossier qui dispose de suffisamment d'espace libre. L'emplacement suivant est recommandé pour rester compatible avec les scripts PowerShell historiques :

```powershell
git lfs install
git clone https://github.com/maxlo245/ai-separator-studio.git "$env:USERPROFILE\AI-Music-Separation"
Set-Location "$env:USERPROFILE\AI-Music-Separation"
git lfs pull
```

Le téléchargement des modèles peut être long. Vérifiez qu'ils sont bien récupérés :

```powershell
git lfs ls-files
Get-ChildItem .\models\*.ckpt, .\models\*.th | Select-Object Name, Length
```

Les fichiers de modèles ne doivent pas avoir une taille de quelques octets ou kilo-octets. Dans ce cas, relancez `git lfs pull`.

> L'application graphique peut utiliser un dépôt installé dans un autre dossier : sélectionnez simplement sa racine dans le champ **Dépôt de l'IA**. Seuls les anciens scripts `Lancer-*.ps1` exigent le chemin `%USERPROFILE%\AI-Music-Separation`.

### 3. Créer l'environnement d'inférence

Dans PowerShell, toujours à la racine du dépôt :

```powershell
py -3.11 -m venv inference-env
.\inference-env\Scripts\Activate.ps1
python -m pip install --upgrade pip
python -m pip install "audio-separator[gpu]==0.47.0"
audio-separator --env_info
```

La dernière commande doit détecter FFmpeg et, si votre machine est compatible, l'accélération NVIDIA/CUDA. Conservez la sortie de cette commande si vous devez demander de l'aide.

Si PowerShell bloque l'activation de l'environnement, utilisez une session PowerShell normale et exécutez :

```powershell
Set-ExecutionPolicy -Scope CurrentUser RemoteSigned
```

Fermez PowerShell, ouvrez-en une nouvelle, puis reprenez la commande d'activation.

### 4. Télécharger l'application Windows

1. Ouvrez la page des [releases](https://github.com/maxlo245/ai-separator-studio/releases).
2. Sous la dernière release, téléchargez `SeparatorStudio.App.exe`.
3. Téléchargez aussi `SeparatorStudio.App.exe.sha256` si vous souhaitez vérifier l'intégrité du fichier.
4. Placez l'exécutable dans un dossier stable, par exemple `%LOCALAPPDATA%\AISeparatorStudio`.

Pour vérifier le téléchargement, placez les deux fichiers dans le même dossier et exécutez :

```powershell
Get-FileHash .\SeparatorStudio.App.exe -Algorithm SHA256
Get-Content .\SeparatorStudio.App.exe.sha256
```

Les deux empreintes SHA-256 doivent être identiques.

> Windows peut afficher un avertissement pour un exécutable non signé. Vérifiez que le fichier provient de la release GitHub officielle et que son empreinte correspond avant de l'ouvrir.

## Utiliser l'application

### Premier lancement

1. Ouvrez `SeparatorStudio.App.exe`.
2. Dans **Dépôt de l'IA**, choisissez le dossier qui contient les sous-dossiers `models` et `inference-env`.
3. Dans **Fichier source**, choisissez un média. Les formats courants pris en charge incluent WAV, FLAC, MP3, M4A, AAC, OGG, MP4, MKV, MOV et WebM.
4. Dans **Dossier de sortie**, choisissez ou conservez le dossier proposé.
5. Choisissez un modèle.
6. Cliquez sur **Lancer la séparation**.

Les pistes générées au format WAV sont placées dans le dossier de sortie. Le journal noir en bas de l'interface affiche la progression et les erreurs du moteur.

### Réglages d'inférence

| Réglage | Quand l'utiliser | Effet |
| --- | --- | --- |
| Précision FP16 native | GPU NVIDIA compatible et modèles RoFormer | Réduit généralement l'utilisation mémoire et accélère le calcul |
| Décalages Demucs | Modèles Demucs | Plus de décalages peut améliorer la qualité, mais ralentit le traitement |
| Chevauchement Demucs | Modèles Demucs | Un chevauchement supérieur peut réduire certains artefacts, au prix de mémoire et de temps |

Commencez avec les valeurs proposées. Ne modifiez les réglages Demucs que si vous constatez des artefacts ou si vous cherchez le meilleur compromis sur votre matériel.

## Comprendre les niveaux de modèles

L'interface classe les modèles connus en quatre niveaux. Ils servent à choisir un compromis de traitement ; ils ne garantissent pas un résultat identique sur chaque chanson.

| Niveau | Priorité | Vitesse | VRAM GPU conseillée | Usage |
| --- | --- | --- | --- | --- |
| `[1/4] Faible` | Rapidité | Rapide | 4 Go ou plus | Modèles de base ou détectés sans profil détaillé |
| `[2/4] Moyen` | Équilibre | Équilibrée | 6 Go ou plus | Séparation standard à quatre pistes |
| `[3/4] Élevé` | Qualité | Plus lente | 8 Go ou plus | Séparation détaillée ou variantes affinées |
| `[4/4] Meilleur` | Qualité maximale | Le plus exigeant | 10 Go ou plus | Modèles recommandés pour voix/instrumental ou réduction des fuites |

### Choix rapide

- Pour **voix + instrumental** et un bon résultat général, commencez par `vocals_mel_band_roformer.ckpt` (`[4/4] Meilleur`).
- Pour obtenir **six pistes** séparées, utilisez `htdemucs_6s.yaml` (`[3/4] Élevé`).
- Pour un essai plus rapide avec quatre pistes, utilisez `htdemucs.yaml` ou `hdemucs_mmi.yaml`.
- Pour réduire les fuites voix/instrumental, testez `mel_band_roformer_kim_ft2_bleedless_unwa.ckpt` si votre GPU est suffisamment puissant.

Le meilleur choix dépend du mixage d'origine. Comparez les résultats sur un même extrait avant de traiter une grande bibliothèque.

## Traitement continu 24/7

Le mode de surveillance transforme le poste en station de traitement locale.

1. Créez un dossier d'entrée, par exemple `D:\Audio-A-Traiter`.
2. Dans l'application, cochez **Surveiller un dossier en continu**.
3. Sélectionnez ce dossier dans **Fichier source**.
4. Vérifiez le modèle et le dossier de sortie, puis cliquez sur **Lancer la séparation**.
5. Copiez les nouveaux médias dans le dossier surveillé.

Les fichiers sont traités **un à la fois** pour éviter de lancer plusieurs calculs GPU simultanément. Fermer la fenêtre réduit l'application dans la zone de notification. Pour arrêter définitivement la surveillance, utilisez **Arrêter** dans l'application ou dans le menu de l'icône de notification.

### Démarrage automatique

Après avoir placé l'exécutable dans son emplacement définitif, ouvrez PowerShell à la racine du dépôt et exécutez :

```powershell
.\Install-AlwaysOn.ps1 -ExecutablePath "C:\chemin\vers\SeparatorStudio.App.exe"
```

Le script crée la tâche planifiée **AI Separator Studio** au démarrage de votre session et configure jusqu'à trois tentatives de redémarrage en cas d'échec.

Pour une disponibilité réelle 24/7 :

- utilisez une machine Windows dédiée ou maintenue sous tension ;
- désactivez la mise en veille et l'hibernation ;
- gardez une session Windows ouverte ;
- vérifiez périodiquement l'espace disque du dossier de sortie ;
- prévoyez une alimentation et une connexion stables si la machine est administrée à distance.

L'interface est une application graphique : elle ne peut pas traiter de fichiers lorsqu'aucune session Windows n'existe.

## Lanceurs PowerShell existants

Le dépôt conserve deux scripts pour les utilisateurs qui préfèrent PowerShell :

| Script | Rôle |
| --- | --- |
| `Lancer-Separation.ps1` | Demande un fichier, un dossier de sortie et le mode voix/instrumental ou six pistes |
| `Lancer-Entrainement.ps1` | Démarre un entraînement LoRA personnalisé lorsque les données sont prêtes |

Pour lancer une séparation depuis PowerShell :

```powershell
Set-Location "$env:USERPROFILE\AI-Music-Separation"
.\Lancer-Separation.ps1
```

## Sources et licences des modèles

Le fichier [`LICENSE`](LICENSE) couvre uniquement le code original de l'application et les scripts de ce dépôt. Il ne donne aucun droit sur les poids pré-entraînés ni sur les éléments tiers listés dans [`THIRD_PARTY_NOTICES`](THIRD_PARTY_NOTICES). Les licences des poids ne sont pas nécessairement celles du code qui les charge.

Les modèles Demucs sont associés aux sources officielles et à leurs fichiers de téléchargement publics. Le projet Demucs indique une licence MIT pour son code ; aucune licence distincte n'est fournie avec chaque poids. Les modèles RoFormer ci-dessous sont référencés par le registre d'[`audio-separator`](https://github.com/nomadkaraoke/python-audio-separator/blob/main/audio_separator/models.json), mais ce registre ne précise pas la licence des poids. Ne supposez pas qu'ils sont libres de redistribution : vérifiez les conditions auprès de l'auteur avant de les copier, publier ou utiliser commercialement.

| Fichier | Source / téléchargement | Licence connue |
| --- | --- | --- |
| `models/04573f0d-f3cf25b2.th` | Demucs `htdemucs_ft` (voix) — [poids officiels](https://dl.fbaipublicfiles.com/demucs/hybrid_transformer/04573f0d-f3cf25b2.th) | Projet Demucs MIT ; licence propre à ce poids non précisée |
| `models/5c90dfd2-34c22ccb.th` | Demucs `htdemucs_6s` — [poids officiels](https://dl.fbaipublicfiles.com/demucs/hybrid_transformer/5c90dfd2-34c22ccb.th) | Projet Demucs MIT ; licence propre à ce poids non précisée |
| `models/75fc33f5-1941ce65.th` | Demucs `hdemucs_mmi` — [poids officiels](https://dl.fbaipublicfiles.com/demucs/hybrid_transformer/75fc33f5-1941ce65.th) | Projet Demucs MIT ; licence propre à ce poids non précisée |
| `models/92cfc3b6-ef3bcb9c.th` | Demucs `htdemucs_ft` (other) — [poids officiels](https://dl.fbaipublicfiles.com/demucs/hybrid_transformer/92cfc3b6-ef3bcb9c.th) | Projet Demucs MIT ; licence propre à ce poids non précisée |
| `models/955717e8-8726e21a.th` | Demucs `htdemucs` — [poids officiels](https://dl.fbaipublicfiles.com/demucs/hybrid_transformer/955717e8-8726e21a.th) | Projet Demucs MIT ; licence propre à ce poids non précisée |
| `models/d12395a8-e57c48e6.th` | Demucs `htdemucs_ft` (basse) — [poids officiels](https://dl.fbaipublicfiles.com/demucs/hybrid_transformer/d12395a8-e57c48e6.th) | Projet Demucs MIT ; licence propre à ce poids non précisée |
| `models/f7e0c4bc-ba3fe64a.th` | Demucs `htdemucs_ft` (batterie) — [poids officiels](https://dl.fbaipublicfiles.com/demucs/hybrid_transformer/f7e0c4bc-ba3fe64a.th) | Projet Demucs MIT ; licence propre à ce poids non précisée |
| `models/bs_roformer_instrumental_resurrection_unwa.ckpt` | Modèle « Instrumental Resurrection » d'Unwa ; rechercher ce nom dans le [registre audio-separator](https://github.com/nomadkaraoke/python-audio-separator/blob/main/audio_separator/models.json) | Non indiquée pour le poids |
| `models/bs_roformer_vocals_revive_v2_unwa.ckpt` | Modèle « Vocals Revive V2 » d'Unwa ; [registre audio-separator](https://github.com/nomadkaraoke/python-audio-separator/blob/main/audio_separator/models.json) | Non indiquée pour le poids |
| `models/bs_roformer_vocals_revive_v3e_unwa.ckpt` | Modèle « Vocals Revive V3e » d'Unwa ; [registre audio-separator](https://github.com/nomadkaraoke/python-audio-separator/blob/main/audio_separator/models.json) | Non indiquée pour le poids |
| `models/mel_band_roformer_instrumental_becruily.ckpt` | Modèle instrumental de becruily ; [registre audio-separator](https://github.com/nomadkaraoke/python-audio-separator/blob/main/audio_separator/models.json) | Non indiquée pour le poids |
| `models/mel_band_roformer_instrumental_fv7z_gabox.ckpt` | Modèle « Instrumental FV7z » de Gabox ; [registre audio-separator](https://github.com/nomadkaraoke/python-audio-separator/blob/main/audio_separator/models.json) | Non indiquée pour le poids |
| `models/mel_band_roformer_kim_ft2_bleedless_unwa.ckpt` | Modèle « Kim FT 2 Bleedless » d'Unwa ; [registre audio-separator](https://github.com/nomadkaraoke/python-audio-separator/blob/main/audio_separator/models.json) | Non indiquée pour le poids |
| `models/mel_band_roformer_vocals_becruily.ckpt` | Modèle vocal de becruily ; [registre audio-separator](https://github.com/nomadkaraoke/python-audio-separator/blob/main/audio_separator/models.json) | Non indiquée pour le poids |
| `models/melband_roformer_inst_v1e_plus.ckpt` | Modèle « Inst V1 (E) Plus » d'Unwa ; [registre audio-separator](https://github.com/nomadkaraoke/python-audio-separator/blob/main/audio_separator/models.json) | Non indiquée pour le poids |
| `models/vocals_mel_band_roformer.ckpt` | Modèle vocal de Kimberley Jensen ; [source du modèle](https://huggingface.co/KimberleyJSN/melbandroformer) et [registre audio-separator](https://github.com/nomadkaraoke/python-audio-separator/blob/main/audio_separator/models.json) | Non indiquée pour le poids |
| `models/config_bs_roformer_instrumental_resurrection_unwa.yaml` | Configuration du modèle Unwa ; [registre audio-separator](https://github.com/nomadkaraoke/python-audio-separator/blob/main/audio_separator/models.json) | Licence distincte non indiquée |
| `models/config_bs_roformer_vocals_revive_unwa.yaml` | Configuration des modèles Vocals Revive d'Unwa ; [registre audio-separator](https://github.com/nomadkaraoke/python-audio-separator/blob/main/audio_separator/models.json) | Licence distincte non indiquée |
| `models/config_mel_band_roformer_instrumental_becruily.yaml` | Configuration du modèle de becruily ; [registre audio-separator](https://github.com/nomadkaraoke/python-audio-separator/blob/main/audio_separator/models.json) | Licence distincte non indiquée |
| `models/config_mel_band_roformer_instrumental_gabox.yaml` | Configuration des modèles instrumentaux de Gabox ; [registre audio-separator](https://github.com/nomadkaraoke/python-audio-separator/blob/main/audio_separator/models.json) | Licence distincte non indiquée |
| `models/config_mel_band_roformer_kim_ft_unwa.yaml` | Configuration des modèles Kim FT d'Unwa ; [registre audio-separator](https://github.com/nomadkaraoke/python-audio-separator/blob/main/audio_separator/models.json) | Licence distincte non indiquée |
| `models/config_mel_band_roformer_vocals_becruily.yaml` | Configuration du modèle vocal de becruily ; [registre audio-separator](https://github.com/nomadkaraoke/python-audio-separator/blob/main/audio_separator/models.json) | Licence distincte non indiquée |
| `models/config_melbandroformer_inst.yaml` | Configuration des modèles MelBand-RoFormer d'Unwa ; [registre audio-separator](https://github.com/nomadkaraoke/python-audio-separator/blob/main/audio_separator/models.json) | Licence distincte non indiquée |
| `models/hdemucs_mmi.yaml` | Configuration Demucs ; [fichier publié par UVR](https://github.com/TRvlvr/model_repo/releases/download/all_public_uvr_models/hdemucs_mmi.yaml) | Projet Demucs MIT ; licence propre à ce fichier non précisée |
| `models/htdemucs.yaml` | Configuration Demucs ; [fichier publié par UVR](https://github.com/TRvlvr/model_repo/releases/download/all_public_uvr_models/htdemucs.yaml) | Projet Demucs MIT ; licence propre à ce fichier non précisée |
| `models/htdemucs_6s.yaml` | Configuration Demucs ; [fichier publié par UVR](https://github.com/TRvlvr/model_repo/releases/download/all_public_uvr_models/htdemucs_6s.yaml) | Projet Demucs MIT ; licence propre à ce fichier non précisée |
| `models/htdemucs_ft.yaml` | Configuration Demucs ; [fichier publié par UVR](https://github.com/TRvlvr/model_repo/releases/download/all_public_uvr_models/htdemucs_ft.yaml) | Projet Demucs MIT ; licence propre à ce fichier non précisée |
| `models/vocals_mel_band_roformer.yaml` | Configuration du modèle de Kimberley Jensen ; [registre audio-separator](https://github.com/nomadkaraoke/python-audio-separator/blob/main/audio_separator/models.json) | Licence distincte non indiquée |
| `models/download_checks.json` | Registre de téléchargement UVR ; il référence notamment les URL Demucs ci-dessus et les sources de modèles | Licence du fichier de registre non identifiée ; ses URL ne confèrent aucun droit sur les modèles |
| `weights/model_bs_roformer_ep_17_sdr_9.6568.ckpt` | Checkpoint de départ pour l'entraînement LoRA selon le guide ZFTurbo ; [téléchargement officiel](https://github.com/ZFTurbo/Music-Source-Separation-Training/releases/download/v1.0.12/model_bs_roformer_ep_17_sdr_9.6568.ckpt) | Aucune licence distincte du poids identifiée ; la licence MIT du code ne s'étend pas automatiquement au checkpoint |

Pour récupérer un modèle RoFormer référencé, consultez le registre et demandez à `audio-separator` de le charger par son nom, par exemple `audio-separator input.wav --model_filename vocals_mel_band_roformer.ckpt`. Le téléchargement automatique dépend de la disponibilité du modèle dans la version installée. L'interface graphique de ce dépôt ne télécharge pas elle-même les modèles : elle ne propose que les poids présents dans `models/`. Pour le checkpoint de `weights/`, aucune source de téléchargement vérifiée n'est connue.

Les fichiers de poids sont actuellement suivis par Git LFS. Avant de republier ce dépôt, ses releases ou un miroir, vérifiez les droits de redistribution de chaque poids dont la licence est inconnue.

## Entraînement personnalisé

L'entraînement est optionnel et distinct de l'inférence. Il nécessite un environnement Python séparé, des données annotées et beaucoup de temps de calcul.

Le script attend la structure suivante :

```text
datasets\
  train\
    nom-du-morceau\
      vocals.wav
      drums.wav
      bass.wav
      other.wav
  valid\
    nom-du-morceau\
      mixture.wav
      vocals.wav
      drums.wav
      bass.wav
      other.wav
```

Le script `Lancer-Entrainement.ps1` vérifie cette structure avant de démarrer. Consultez `config-personnel-bs-roformer-lora.yaml` pour les paramètres LoRA et `Music-Source-Separation-Training\docs\LoRA.md` pour les informations du projet d'entraînement.

## Dépannage

### `audio-separator.exe est introuvable`

L'environnement `inference-env` n'est pas présent à la racine du dépôt sélectionné. Reprenez l'étape [Créer l'environnement d'inférence](#3-créer-lenvironnement-dinférence).

### Les modèles sont absents ou très petits

Git LFS n'a probablement pas téléchargé les poids :

```powershell
Set-Location "C:\chemin\vers\AI-Music-Separation"
git lfs install
git lfs pull
```

### Le GPU n'est pas détecté

1. Mettez à jour le pilote NVIDIA.
2. Activez l'environnement `inference-env`.
3. Exécutez `audio-separator --env_info`.
4. Consultez le guide d'installation officiel d'[`audio-separator`](https://github.com/nomadkaraoke/python-audio-separator#installation) pour la compatibilité CUDA/ONNX Runtime.

Vous pouvez désactiver FP16 dans l'application pour tester un problème spécifique au calcul demi-précision.

### FFmpeg n'est pas détecté

Installez FFmpeg et ajoutez son répertoire `bin` au `PATH` système ou utilisateur. Ouvrez ensuite une **nouvelle** session PowerShell et relancez `audio-separator --env_info`.

### Le traitement échoue ou se ferme

- Consultez les dernières lignes du journal de l'application.
- Vérifiez que le dossier de sortie est accessible en écriture.
- Vérifiez l'espace disque disponible.
- Essayez un fichier WAV court pour isoler un problème de codec.
- Essayez un niveau de modèle inférieur si la mémoire GPU est insuffisante.

### Le dossier surveillé ne traite pas de nouveau fichier

Attendez la fin de la copie du fichier avant de le déposer dans le dossier surveillé. L'application attend trois secondes après l'arrivée d'un fichier, puis lance le traitement s'il est accessible.

## Créer l'exécutable depuis les sources

Cette étape est nécessaire uniquement si vous modifiez `SeparatorStudio.App` ou souhaitez construire votre propre exécutable.

1. Installez le [.NET SDK 9](https://dotnet.microsoft.com/download/dotnet/9.0).
2. Ouvrez PowerShell à la racine du dépôt.
3. Publiez l'application :

```powershell
.\Publish-SeparatorStudio.ps1
```

L'exécutable autonome Windows x64 est créé dans :

```text
artifacts\SeparatorStudio\SeparatorStudio.App.exe
```

Pour vérifier la compilation sans publier :

```powershell
dotnet build .\SeparatorStudio.App\SeparatorStudio.App.csproj --configuration Release
```

Les répertoires de sortie `artifacts`, `SeparatorStudio.App\bin` et `SeparatorStudio.App\obj` sont volontairement exclus de Git.

## Structure du dépôt

```text
SeparatorStudio.App\                 Application Windows WinForms
  Program.cs                          Interface, réglages et lancement du moteur
  SeparatorStudio.App.csproj          Projet .NET Windows
models\                               Modèles d'inférence suivis par Git LFS
weights\                              Poids utilisés pour l'entraînement
Music-Source-Separation-Training\     Projet et outils d'entraînement
Lancer-Separation.ps1                 Lanceur PowerShell de séparation
Lancer-Entrainement.ps1               Lanceur PowerShell d'entraînement
Publish-SeparatorStudio.ps1           Publication de l'exécutable autonome
Install-AlwaysOn.ps1                  Tâche planifiée de démarrage automatique
```

## Références

- [audio-separator : installation et configuration GPU](https://github.com/nomadkaraoke/python-audio-separator)
- [Git LFS](https://git-lfs.com/)
- [Documentation FFmpeg](https://ffmpeg.org/documentation.html)
