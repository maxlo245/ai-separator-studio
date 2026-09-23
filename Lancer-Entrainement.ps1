$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Windows.Forms

$root = Join-Path $env:USERPROFILE "AI-Music-Separation"
$repo = Join-Path $root "Music-Source-Separation-Training"
$python = Join-Path $root "training-env\Scripts\python.exe"
$trainData = Join-Path $root "datasets\train"
$validData = Join-Path $root "datasets\valid"
$results = Join-Path $root "training-results"
$config = Join-Path $root "config-personnel-bs-roformer-lora.yaml"
$checkpoint = Join-Path $root "weights\model_bs_roformer_ep_17_sdr_9.6568.ckpt"

$requiredTrain = @("vocals.wav", "drums.wav", "bass.wav", "other.wav")
$requiredValid = @("vocals.wav", "drums.wav", "bass.wav", "other.wav", "mixture.wav")

function Test-SongFolder {
    param(
        [string]$Path,
        [string[]]$RequiredFiles
    )

    $songFolders = @(Get-ChildItem $Path -Directory -ErrorAction SilentlyContinue)
    if ($songFolders.Count -eq 0) {
        return $false
    }

    foreach ($folder in $songFolders) {
        foreach ($file in $RequiredFiles) {
            if (-not (Test-Path (Join-Path $folder.FullName $file))) {
                return $false
            }
        }
    }

    return $true
}

if (-not (Test-SongFolder -Path $trainData -RequiredFiles $requiredTrain) -or
    -not (Test-SongFolder -Path $validData -RequiredFiles $requiredValid)) {
    [System.Windows.Forms.MessageBox]::Show(
        "Les donnees ne sont pas encore pretes.`n`nPlacez les morceaux et leurs pistes isolees dans :`n$trainData`n$validData`n`nLe dossier GUIDE.txt explique la structure.",
        "Entrainement personnel",
        "OK",
        "Warning"
    ) | Out-Null
    Start-Process explorer.exe -ArgumentList (Join-Path $root "datasets")
    exit 2
}

if (-not (Test-Path $checkpoint)) {
    [System.Windows.Forms.MessageBox]::Show(
        "Le checkpoint de depart est introuvable :`n$checkpoint",
        "Entrainement personnel",
        "OK",
        "Error"
    ) | Out-Null
    exit 3
}

$confirmation = [System.Windows.Forms.MessageBox]::Show(
    "Demarrer le fine-tuning LoRA sur la RTX 5070 ?`n`nL'operation peut durer plusieurs heures ou plusieurs jours.",
    "Entrainement personnel",
    [System.Windows.Forms.MessageBoxButtons]::YesNo,
    [System.Windows.Forms.MessageBoxIcon]::Question
)

if ($confirmation -ne [System.Windows.Forms.DialogResult]::Yes) {
    exit 0
}

Set-Location $repo
& $python train.py `
    --model_type bs_roformer `
    --config_path $config `
    --start_check_point $checkpoint `
    --results_path $results `
    --data_path $trainData `
    --valid_path $validData `
    --device_ids 0 `
    --metrics sdr `
    --train_lora

if ($LASTEXITCODE -ne 0) {
    Read-Host "L'entrainement a echoue. Appuyez sur Entree pour fermer"
    exit $LASTEXITCODE
}
