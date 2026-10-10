using System.Diagnostics;
using System.Text.Json;

namespace SeparatorStudio.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class MainForm : Form
{
    private static readonly string[] MediaExtensions =
    [
        ".wav", ".flac", ".mp3", ".m4a", ".aac", ".ogg", ".mp4", ".mkv", ".mov", ".webm"
    ];

    private readonly TextBox repositoryPath = new() { Dock = DockStyle.Fill };
    private readonly TextBox sourcePath = new() { Dock = DockStyle.Fill };
    private readonly TextBox outputPath = new() { Dock = DockStyle.Fill };
    private readonly ComboBox modelSelector = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label modelDescription = new() { AutoSize = false, Dock = DockStyle.Fill, ForeColor = Color.DimGray };
    private readonly CheckBox nativeFp16 = new() { Text = "Utiliser la précision FP16 native (GPU NVIDIA compatible)", AutoSize = true, Checked = true };
    private readonly NumericUpDown demucsShifts = new() { Minimum = 0, Maximum = 10, Value = 2, Width = 70 };
    private readonly NumericUpDown demucsOverlap = new() { Minimum = 0, Maximum = 0.99M, DecimalPlaces = 2, Increment = 0.05M, Value = 0.25M, Width = 70 };
    private readonly CheckBox watchFolder = new() { Text = "Surveiller un dossier en continu (mode 24/7)", AutoSize = true };
    private readonly Button runButton = new() { Text = "Lancer la séparation", AutoSize = true };
    private readonly Button cancelButton = new() { Text = "Arrêter", AutoSize = true, Enabled = false };
    private readonly RichTextBox log = new() { Dock = DockStyle.Fill, ReadOnly = true, BackColor = Color.FromArgb(25, 25, 25), ForeColor = Color.Gainsboro, Font = new Font(FontFamily.GenericMonospace, 9) };
    private readonly NotifyIcon trayIcon = new() { Text = "AI Separator Studio", Visible = false };
    private readonly List<ModelProfile> profiles = [];
    private readonly HashSet<string> queuedPaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim separationGate = new(1, 1);
    private AppSettings settings = new();
    private Process? activeProcess;
    private FileSystemWatcher? watcher;
    private bool isStopping;

    public MainForm()
    {
        Text = "AI Separator Studio";
        MinimumSize = new Size(840, 680);
        StartPosition = FormStartPosition.CenterScreen;
        Icon = SystemIcons.Application;

        settings = AppSettings.Load();
        BuildLayout();
        LoadSettings();
        ConfigureTrayIcon();
        DiscoverModels();

        FormClosing += OnFormClosing;
        modelSelector.SelectedIndexChanged += (_, _) => ShowSelectedModelDetails();
        watchFolder.CheckedChanged += (_, _) => UpdateSourceMode();
        runButton.Click += async (_, _) => await StartAsync();
        cancelButton.Click += (_, _) => StopCurrentWork();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 3,
            RowCount = 9,
            AutoScroll = true
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));

        AddPathRow(root, 0, "Dépôt de l'IA", repositoryPath, "Choisir", BrowseRepository);
        AddPathRow(root, 1, "Fichier source", sourcePath, "Choisir", BrowseSource);
        AddPathRow(root, 2, "Dossier de sortie", outputPath, "Choisir", BrowseOutput);

        root.Controls.Add(new Label { Text = "Modèle", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 3);
        root.Controls.Add(modelSelector, 1, 3);
        root.SetColumnSpan(modelSelector, 2);

        root.Controls.Add(new Label { Text = "Indication", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 4);
        root.Controls.Add(modelDescription, 1, 4);
        root.SetColumnSpan(modelDescription, 2);
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        var inferenceOptions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
        inferenceOptions.Controls.Add(nativeFp16);
        inferenceOptions.Controls.Add(new Label { Text = "Décalages Demucs :", AutoSize = true, Margin = new Padding(20, 6, 3, 3) });
        inferenceOptions.Controls.Add(demucsShifts);
        inferenceOptions.Controls.Add(new Label { Text = "Chevauchement :", AutoSize = true, Margin = new Padding(14, 6, 3, 3) });
        inferenceOptions.Controls.Add(demucsOverlap);
        root.Controls.Add(new Label { Text = "Réglages", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 5);
        root.Controls.Add(inferenceOptions, 1, 5);
        root.SetColumnSpan(inferenceOptions, 2);

        root.Controls.Add(new Label { Text = "Service local", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 6);
        root.Controls.Add(watchFolder, 1, 6);
        root.SetColumnSpan(watchFolder, 2);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        actions.Controls.Add(runButton);
        actions.Controls.Add(cancelButton);
        root.Controls.Add(actions, 1, 7);
        root.SetColumnSpan(actions, 2);

        root.Controls.Add(log, 0, 8);
        root.SetColumnSpan(log, 3);
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Controls.Add(root);
    }

    private static void AddPathRow(TableLayoutPanel layout, int row, string label, TextBox value, string buttonText, EventHandler handler)
    {
        var button = new Button { Text = buttonText, Dock = DockStyle.Fill };
        button.Click += handler;
        layout.Controls.Add(new Label { Text = label, Anchor = AnchorStyles.Left, AutoSize = true }, 0, row);
        layout.Controls.Add(value, 1, row);
        layout.Controls.Add(button, 2, row);
    }

    private void LoadSettings()
    {
        repositoryPath.Text = string.IsNullOrWhiteSpace(settings.RepositoryPath) ? FindRepository() : settings.RepositoryPath;
        outputPath.Text = string.IsNullOrWhiteSpace(settings.OutputPath)
            ? Path.Combine(repositoryPath.Text, "outputs")
            : settings.OutputPath;
        nativeFp16.Checked = settings.NativeFp16;
        demucsShifts.Value = Math.Clamp(settings.DemucsShifts, (int)demucsShifts.Minimum, (int)demucsShifts.Maximum);
        demucsOverlap.Value = Math.Clamp(settings.DemucsOverlap, demucsOverlap.Minimum, demucsOverlap.Maximum);
        watchFolder.Checked = settings.WatchFolder;
        sourcePath.Text = settings.SourcePath;
        UpdateSourceMode();
    }

    private string FindRepository()
    {
        var candidates = new[]
        {
            AppContext.BaseDirectory,
            Directory.GetParent(AppContext.BaseDirectory)?.FullName,
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AI-Music-Separation")
        };

        return candidates.FirstOrDefault(path => path is not null && Directory.Exists(Path.Combine(path, "models")))
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AI-Music-Separation");
    }

    private void DiscoverModels()
    {
        profiles.Clear();
        var modelDirectory = Path.Combine(repositoryPath.Text, "models");
        if (!Directory.Exists(modelDirectory))
        {
            modelSelector.DataSource = null;
            modelDescription.Text = "Choisissez un dépôt contenant le dossier models.";
            return;
        }

        var knownProfiles = new Dictionary<string, ModelProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["vocals_mel_band_roformer.ckpt"] = new("vocals_mel_band_roformer.ckpt", ModelTier.Best, "Voix + instrumental. Meilleur choix général pour isoler les voix."),
            ["htdemucs_6s.yaml"] = new("htdemucs_6s.yaml", ModelTier.High, "Six pistes : voix, batterie, basse, guitare, piano et autres."),
            ["htdemucs.yaml"] = new("htdemucs.yaml", ModelTier.Medium, "Quatre pistes : voix, batterie, basse et autres."),
            ["htdemucs_ft.yaml"] = new("htdemucs_ft.yaml", ModelTier.High, "Quatre pistes Demucs affinées : voix, batterie, basse et autres."),
            ["hdemucs_mmi.yaml"] = new("hdemucs_mmi.yaml", ModelTier.Medium, "Quatre pistes Demucs. Bon compromis qualité / rapidité."),
            ["bs_roformer_vocals_revive_v3e_unwa.ckpt"] = new("bs_roformer_vocals_revive_v3e_unwa.ckpt", ModelTier.Best, "Voix + instrumental. Spécialisé dans la restitution des voix."),
            ["bs_roformer_vocals_revive_v2_unwa.ckpt"] = new("bs_roformer_vocals_revive_v2_unwa.ckpt", ModelTier.High, "Voix + instrumental. Variante de restauration vocale."),
            ["mel_band_roformer_vocals_becruily.ckpt"] = new("mel_band_roformer_vocals_becruily.ckpt", ModelTier.High, "Voix + instrumental. Variante MelBand-RoFormer."),
            ["melband_roformer_inst_v1e_plus.ckpt"] = new("melband_roformer_inst_v1e_plus.ckpt", ModelTier.High, "Instrumental + voix. Optimisé pour l'instrumental."),
            ["mel_band_roformer_instrumental_becruily.ckpt"] = new("mel_band_roformer_instrumental_becruily.ckpt", ModelTier.High, "Instrumental + voix. Variante MelBand-RoFormer."),
            ["mel_band_roformer_instrumental_fv7z_gabox.ckpt"] = new("mel_band_roformer_instrumental_fv7z_gabox.ckpt", ModelTier.High, "Instrumental + voix. Variante MelBand-RoFormer."),
            ["mel_band_roformer_kim_ft2_bleedless_unwa.ckpt"] = new("mel_band_roformer_kim_ft2_bleedless_unwa.ckpt", ModelTier.Best, "Voix + instrumental. Privilégie la réduction des fuites entre pistes."),
            ["bs_roformer_instrumental_resurrection_unwa.ckpt"] = new("bs_roformer_instrumental_resurrection_unwa.ckpt", ModelTier.Best, "Instrumental + voix. Modèle spécialisé dans l'instrumental.")
        };

        foreach (var file in Directory.EnumerateFiles(modelDirectory)
                     .Where(path => Path.GetExtension(path).Equals(".ckpt", StringComparison.OrdinalIgnoreCase) ||
                                    Path.GetExtension(path).Equals(".yaml", StringComparison.OrdinalIgnoreCase))
                     .OrderBy(Path.GetFileName))
        {
            var name = Path.GetFileName(file);
            profiles.Add(knownProfiles.GetValueOrDefault(name, CreateUnclassifiedProfile(name)));
        }

        modelSelector.DataSource = profiles;
        var selected = profiles.FindIndex(profile => profile.FileName.Equals(settings.ModelFileName, StringComparison.OrdinalIgnoreCase));
        modelSelector.SelectedIndex = selected >= 0 ? selected : Math.Max(0, profiles.FindIndex(profile => profile.FileName == "vocals_mel_band_roformer.ckpt"));
        ShowSelectedModelDetails();
    }

    private void ShowSelectedModelDetails()
    {
        if (modelSelector.SelectedItem is not ModelProfile profile)
        {
            return;
        }

        modelDescription.Text = $"{profile.Tier.Label()} — {profile.Tier.QualityDescription()} | "
            + $"{profile.Tier.SpeedDescription()} | GPU conseillé : {profile.Tier.RecommendedVram()}.{Environment.NewLine}"
            + $"{profile.Description}{Environment.NewLine}"
            + "Indication de compromis : la qualité dépend aussi du morceau source.";
        var demucs = profile.FileName.Contains("demucs", StringComparison.OrdinalIgnoreCase);
        demucsShifts.Enabled = demucs;
        demucsOverlap.Enabled = demucs;
    }

    private void BrowseRepository(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog { Description = "Choisissez la racine du dépôt AI Separator Studio" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            repositoryPath.Text = dialog.SelectedPath;
            if (string.IsNullOrWhiteSpace(outputPath.Text) || outputPath.Text.EndsWith("\\outputs", StringComparison.OrdinalIgnoreCase))
            {
                outputPath.Text = Path.Combine(dialog.SelectedPath, "outputs");
            }

            DiscoverModels();
        }
    }

    private void BrowseSource(object? sender, EventArgs e)
    {
        if (watchFolder.Checked)
        {
            using var folderDialog = new FolderBrowserDialog { Description = "Choisissez le dossier à surveiller" };
            if (folderDialog.ShowDialog(this) == DialogResult.OK)
            {
                sourcePath.Text = folderDialog.SelectedPath;
            }

            return;
        }

        using var fileDialog = new OpenFileDialog
        {
            Title = "Choisissez un fichier audio ou vidéo",
            Filter = "Audio et vidéo|*.wav;*.flac;*.mp3;*.m4a;*.aac;*.ogg;*.mp4;*.mkv;*.mov;*.webm|Tous les fichiers|*.*"
        };
        if (fileDialog.ShowDialog(this) == DialogResult.OK)
        {
            sourcePath.Text = fileDialog.FileName;
        }
    }

    private void BrowseOutput(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog { Description = "Choisissez le dossier de sortie", SelectedPath = outputPath.Text };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            outputPath.Text = dialog.SelectedPath;
        }
    }

    private void UpdateSourceMode()
    {
        sourcePath.Text = watchFolder.Checked && File.Exists(sourcePath.Text) ? string.Empty : sourcePath.Text;
    }

    private async Task StartAsync()
    {
        if (!ValidateConfiguration(out var error))
        {
            MessageBox.Show(this, error, "Configuration invalide", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        SaveSettings();
        isStopping = false;
        runButton.Enabled = false;
        cancelButton.Enabled = true;

        if (watchFolder.Checked)
        {
            StartWatcher();
            AppendLog($"Surveillance active : {sourcePath.Text}");
            foreach (var file in Directory.EnumerateFiles(sourcePath.Text).Where(IsSupportedMediaFile))
            {
                await QueueSeparationAsync(file);
            }

            return;
        }

        await QueueSeparationAsync(sourcePath.Text);
        runButton.Enabled = true;
        cancelButton.Enabled = false;
    }

    private bool ValidateConfiguration(out string error)
    {
        if (!File.Exists(Path.Combine(repositoryPath.Text, "inference-env", "Scripts", "audio-separator.exe")))
        {
            error = "audio-separator.exe est introuvable. Créez l'environnement inference-env selon le README.";
            return false;
        }

        if (!Directory.Exists(Path.Combine(repositoryPath.Text, "models")))
        {
            error = "Le dossier models est introuvable dans le dépôt sélectionné.";
            return false;
        }

        if (modelSelector.SelectedItem is not ModelProfile)
        {
            error = "Sélectionnez un modèle disponible.";
            return false;
        }

        if (watchFolder.Checked ? !Directory.Exists(sourcePath.Text) : !File.Exists(sourcePath.Text))
        {
            error = watchFolder.Checked ? "Choisissez un dossier existant à surveiller." : "Choisissez un fichier source existant.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private void StartWatcher()
    {
        watcher?.Dispose();
        watcher = new FileSystemWatcher(sourcePath.Text)
        {
            IncludeSubdirectories = false,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };
        watcher.Created += async (_, eventArgs) => await OnSourceFileChangedAsync(eventArgs.FullPath);
        watcher.Renamed += async (_, eventArgs) => await OnSourceFileChangedAsync(eventArgs.FullPath);
        trayIcon.Visible = true;
    }

    private async Task OnSourceFileChangedAsync(string path)
    {
        if (!IsSupportedMediaFile(path))
        {
            return;
        }

        await Task.Delay(TimeSpan.FromSeconds(3));
        if (!isStopping && File.Exists(path))
        {
            await QueueSeparationAsync(path);
        }
    }

    private static bool IsSupportedMediaFile(string path) => MediaExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    private async Task QueueSeparationAsync(string inputPath)
    {
        if (!queuedPaths.Add(inputPath))
        {
            return;
        }

        try
        {
            await separationGate.WaitAsync();
            try
            {
                await RunSeparationAsync(inputPath);
            }
            finally
            {
                separationGate.Release();
            }
        }
        finally
        {
            queuedPaths.Remove(inputPath);
        }
    }

    private async Task RunSeparationAsync(string inputPath)
    {
        if (modelSelector.SelectedItem is not ModelProfile model)
        {
            return;
        }

        Directory.CreateDirectory(outputPath.Text);
        var executable = Path.Combine(repositoryPath.Text, "inference-env", "Scripts", "audio-separator.exe");
        var torchLib = Path.Combine(repositoryPath.Text, "inference-env", "Lib", "site-packages", "torch", "lib");
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = repositoryPath.Text,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.Environment["PATH"] = $"{torchLib};{startInfo.Environment["PATH"]}";
        startInfo.ArgumentList.Add(inputPath);
        startInfo.ArgumentList.Add("--model_file_dir");
        startInfo.ArgumentList.Add(Path.Combine(repositoryPath.Text, "models"));
        startInfo.ArgumentList.Add("--output_dir");
        startInfo.ArgumentList.Add(outputPath.Text);
        startInfo.ArgumentList.Add("--output_format");
        startInfo.ArgumentList.Add("WAV");
        startInfo.ArgumentList.Add("-m");
        startInfo.ArgumentList.Add(model.FileName);

        if (nativeFp16.Checked && !model.FileName.Contains("demucs", StringComparison.OrdinalIgnoreCase))
        {
            startInfo.ArgumentList.Add("--use_native_fp16");
        }

        if (model.FileName.Contains("demucs", StringComparison.OrdinalIgnoreCase))
        {
            startInfo.ArgumentList.Add("--demucs_shifts");
            startInfo.ArgumentList.Add(demucsShifts.Value.ToString());
            startInfo.ArgumentList.Add("--demucs_overlap");
            startInfo.ArgumentList.Add(demucsOverlap.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        AppendLog($"> {Path.GetFileName(inputPath)} avec {model.FileName}");
        try
        {
            activeProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            activeProcess.OutputDataReceived += (_, eventArgs) => { if (eventArgs.Data is not null) AppendLog(eventArgs.Data); };
            activeProcess.ErrorDataReceived += (_, eventArgs) => { if (eventArgs.Data is not null) AppendLog($"ERREUR: {eventArgs.Data}"); };

            if (!activeProcess.Start())
            {
                throw new InvalidOperationException("Impossible de démarrer audio-separator.");
            }

            activeProcess.BeginOutputReadLine();
            activeProcess.BeginErrorReadLine();
            await activeProcess.WaitForExitAsync();
            if (activeProcess.ExitCode == 0)
            {
                AppendLog($"Terminé : {Path.GetFileName(inputPath)}");
            }
            else if (!isStopping)
            {
                AppendLog($"Échec (code {activeProcess.ExitCode}) : {Path.GetFileName(inputPath)}");
            }
        }
        catch (Exception exception)
        {
            AppendLog($"ERREUR: {exception.Message}");
        }
        finally
        {
            activeProcess?.Dispose();
            activeProcess = null;
        }
    }

    private void StopCurrentWork()
    {
        isStopping = true;
        watcher?.Dispose();
        watcher = null;
        if (activeProcess is { HasExited: false })
        {
            activeProcess.Kill(entireProcessTree: true);
        }

        trayIcon.Visible = false;
        runButton.Enabled = true;
        cancelButton.Enabled = false;
        AppendLog("Arrêt demandé.");
    }

    private void AppendLog(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendLog(message));
            return;
        }

        log.AppendText($"{DateTime.Now:HH:mm:ss}  {message}{Environment.NewLine}");
        log.SelectionStart = log.TextLength;
        log.ScrollToCaret();
    }

    private void ConfigureTrayIcon()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Afficher", null, (_, _) => { Show(); WindowState = FormWindowState.Normal; Activate(); });
        menu.Items.Add("Arrêter la surveillance", null, (_, _) => StopCurrentWork());
        menu.Items.Add("Quitter", null, (_, _) => { isStopping = true; Close(); });
        trayIcon.Icon = SystemIcons.Application;
        trayIcon.ContextMenuStrip = menu;
        trayIcon.DoubleClick += (_, _) => { Show(); WindowState = FormWindowState.Normal; Activate(); };
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs eventArgs)
    {
        if (watcher is not null && !isStopping)
        {
            eventArgs.Cancel = true;
            Hide();
            trayIcon.Visible = true;
            return;
        }

        SaveSettings();
        StopCurrentWork();
        trayIcon.Dispose();
    }

    private void SaveSettings()
    {
        settings = new AppSettings
        {
            RepositoryPath = repositoryPath.Text,
            SourcePath = sourcePath.Text,
            OutputPath = outputPath.Text,
            ModelFileName = (modelSelector.SelectedItem as ModelProfile)?.FileName ?? string.Empty,
            NativeFp16 = nativeFp16.Checked,
            DemucsShifts = Decimal.ToInt32(demucsShifts.Value),
            DemucsOverlap = demucsOverlap.Value,
            WatchFolder = watchFolder.Checked
        };
        settings.Save();
    }

    private static ModelProfile CreateUnclassifiedProfile(string fileName)
    {
        var tier = fileName.Contains("demucs", StringComparison.OrdinalIgnoreCase)
            ? ModelTier.Low
            : ModelTier.Medium;
        return new ModelProfile(fileName, tier, "Modèle local détecté. Niveau estimé d'après sa famille ; consultez sa documentation pour son usage précis.");
    }

    private sealed record ModelProfile(string FileName, ModelTier Tier, string Description)
    {
        public override string ToString() => $"{Tier.Badge()} {Tier.Label()} — {FileName}";
    }
}

internal enum ModelTier
{
    Low,
    Medium,
    High,
    Best
}

internal static class ModelTierExtensions
{
    public static string Badge(this ModelTier tier) => tier switch
    {
        ModelTier.Low => "[1/4]",
        ModelTier.Medium => "[2/4]",
        ModelTier.High => "[3/4]",
        ModelTier.Best => "[4/4]",
        _ => throw new ArgumentOutOfRangeException(nameof(tier))
    };

    public static string Label(this ModelTier tier) => tier switch
    {
        ModelTier.Low => "Faible",
        ModelTier.Medium => "Moyen",
        ModelTier.High => "Élevé",
        ModelTier.Best => "Meilleur",
        _ => throw new ArgumentOutOfRangeException(nameof(tier))
    };

    public static string QualityDescription(this ModelTier tier) => tier switch
    {
        ModelTier.Low => "qualité de base",
        ModelTier.Medium => "bon compromis qualité / temps",
        ModelTier.High => "qualité élevée",
        ModelTier.Best => "qualité maximale recommandée",
        _ => throw new ArgumentOutOfRangeException(nameof(tier))
    };

    public static string SpeedDescription(this ModelTier tier) => tier switch
    {
        ModelTier.Low => "rapide",
        ModelTier.Medium => "vitesse équilibrée",
        ModelTier.High => "plus lent",
        ModelTier.Best => "le plus exigeant",
        _ => throw new ArgumentOutOfRangeException(nameof(tier))
    };

    public static string RecommendedVram(this ModelTier tier) => tier switch
    {
        ModelTier.Low => "4 Go ou plus",
        ModelTier.Medium => "6 Go ou plus",
        ModelTier.High => "8 Go ou plus",
        ModelTier.Best => "10 Go ou plus",
        _ => throw new ArgumentOutOfRangeException(nameof(tier))
    };
}

internal sealed class AppSettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AISeparatorStudio",
        "settings.json");

    public string RepositoryPath { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public string OutputPath { get; init; } = string.Empty;
    public string ModelFileName { get; init; } = string.Empty;
    public bool NativeFp16 { get; init; } = true;
    public int DemucsShifts { get; init; } = 2;
    public decimal DemucsOverlap { get; init; } = 0.25M;
    public bool WatchFolder { get; init; }

    public static AppSettings Load()
    {
        try
        {
            return File.Exists(SettingsPath)
                ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) ?? new AppSettings()
                : new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
