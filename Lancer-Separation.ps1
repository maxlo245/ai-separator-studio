$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Windows.Forms

$root = Join-Path $env:USERPROFILE "AI-Music-Separation"
$separator = Join-Path $root "inference-env\Scripts\audio-separator.exe"
$modelDir = Join-Path $root "models"
$defaultOutput = Join-Path $root "outputs"
$torchLib = Join-Path $root "inference-env\Lib\site-packages\torch\lib"

if (-not (Test-Path $separator)) {
    [System.Windows.Forms.MessageBox]::Show(
        "L'environnement de separation est introuvable.",
        "Separation IA",
        "OK",
        "Error"
    ) | Out-Null
    exit 1
}

$fileDialog = New-Object System.Windows.Forms.OpenFileDialog
$fileDialog.Title = "Choisissez un fichier audio ou video"
$fileDialog.Filter = "Audio et video|*.wav;*.flac;*.mp3;*.m4a;*.aac;*.ogg;*.mp4;*.mkv;*.mov;*.webm|Tous les fichiers|*.*"

if ($fileDialog.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) {
    exit 0
}

$choice = [System.Windows.Forms.MessageBox]::Show(
    "OUI : voix + instrumental en tres haute qualite (MelBand-RoFormer)`n`nNON : 6 pistes (voix, batterie, basse, guitare, piano, autres)",
    "Choisissez le mode",
    [System.Windows.Forms.MessageBoxButtons]::YesNoCancel,
    [System.Windows.Forms.MessageBoxIcon]::Question
)

if ($choice -eq [System.Windows.Forms.DialogResult]::Cancel) {
    exit 0
}

$folderDialog = New-Object System.Windows.Forms.FolderBrowserDialog
$folderDialog.Description = "Choisissez le dossier de sortie"
$folderDialog.SelectedPath = $defaultOutput

if ($folderDialog.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) {
    exit 0
}

$env:PATH = "$torchLib;$env:PATH"
$arguments = @(
    $fileDialog.FileName,
    "--model_file_dir", $modelDir,
    "--output_dir", $folderDialog.SelectedPath,
    "--output_format", "WAV"
)

if ($choice -eq [System.Windows.Forms.DialogResult]::Yes) {
    $arguments += @(
        "-m", "vocals_mel_band_roformer.ckpt",
        "--use_native_fp16"
    )
} else {
    $arguments += @(
        "-m", "htdemucs_6s.yaml",
        "--demucs_shifts", "2",
        "--demucs_overlap", "0.25"
    )
}

Write-Host ""
Write-Host "Separation en cours avec la RTX 5070..." -ForegroundColor Cyan
Write-Host "Le premier traitement peut prendre plusieurs minutes." -ForegroundColor Yellow
Write-Host ""

& $separator @arguments

if ($LASTEXITCODE -ne 0) {
    [System.Windows.Forms.MessageBox]::Show(
        "La separation a echoue. Consultez les messages de cette fenetre.",
        "Separation IA",
        "OK",
        "Error"
    ) | Out-Null
    Read-Host "Appuyez sur Entree pour fermer"
    exit $LASTEXITCODE
}

Start-Process explorer.exe -ArgumentList $folderDialog.SelectedPath
[System.Windows.Forms.MessageBox]::Show(
    "Separation terminee. Le dossier de sortie a ete ouvert.",
    "Separation IA",
    "OK",
    "Information"
) | Out-Null
