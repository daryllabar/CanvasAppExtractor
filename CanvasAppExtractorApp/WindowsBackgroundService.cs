using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace CanvasAppExtractorApp
{
    public sealed class WindowsBackgroundService
    {
        private readonly ILogger<WindowsBackgroundService> _logger;
        private readonly NotifyIcon _notifier;
        private readonly CanvasAppExtractorSettings _settings;
        private readonly FileSystemWatcher _fileWatcher;

        public static readonly string Divider = Environment.NewLine + "****************************************************************************************************" + Environment.NewLine;

        public WindowsBackgroundService(FileSystemWatcher fileWatcher, CanvasAppExtractorSettings settings, ILogger<WindowsBackgroundService> logger,
            NotifyIcon notifier)
        {
            _logger = logger;
            _notifier = notifier;
            _settings = settings;
            _fileWatcher = fileWatcher;

            if (string.IsNullOrWhiteSpace(_settings.PacPath))
            {
                throw new ArgumentException(@"PacPath is required", nameof(settings));
            }

            if (_settings.ExtractDirStartsWithMapping.Count == 0)
            {
                throw new ArgumentException(@"ExtractDirStartsWithMapping is required", nameof(settings));
            }

            var sb = new StringBuilder("Mapping Values Loaded: " + Environment.NewLine);
            foreach(var mapping in _settings.ExtractDirStartsWithMapping)
            {
                sb.AppendLine($"{mapping.Key}: {mapping.Value}");
            }

            _logger.LogInformation("{Mappings}", sb + Divider);

            _fileWatcher.Path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
            _fileWatcher.Created += OnNewFileCreated;
            _fileWatcher.Renamed += OnNewFileCreated;
            _fileWatcher.EnableRaisingEvents = true;
        }

        private void OnNewFileCreated(object sender, FileSystemEventArgs e)
        {
            try
            {
                if (Path.GetExtension(e.FullPath) != ".msapp")
                {
                    _logger.LogTrace("{FilePath} was not an .msapp file, skipping.", e.FullPath);
                    return;
                }

                var downloadFile = new FileInfo(e.FullPath);
                _logger.LogInformation(Divider + "Processing App file: " + downloadFile);
                var appExtractDir = GetExtractDirectory(downloadFile.FullName);
                if (appExtractDir.Contains("$V$"))
                {
                    appExtractDir = ReplaceVersion(appExtractDir);
                }

                UnpackMsApp(downloadFile, appExtractDir);
            }
            catch (Exception ex)
            {
                _notifier.ShowBalloonTip(5000, "Error", "Error processing new file!", ToolTipIcon.Error);
                _logger.LogError(ex, "Error processing new value {FilePath}", e.FullPath);
            }
        }

        private void UnpackMsApp(FileInfo downloadFile, string appExtractDir)
        {
            var args = $@"canvas unpack --msapp ""{downloadFile}"" --sources ""{appExtractDir}""";

            var output = new StringBuilder();
            output.AppendLine(_settings.PacPath + " " + args);
            using (var p = new Process())
            {
                p.StartInfo.Arguments = args;
                p.StartInfo.FileName = _settings.PacPath;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.Start();

                var sr = p.StandardOutput;
                while (!sr.EndOfStream)
                {
                    output.AppendLine(sr.ReadLine());
                }
                p.WaitForExit();
            }


            output.AppendLine();
            var appDir = new DirectoryInfo(appExtractDir);
            output.AppendLine($@"Command to Pack: {_settings.PacPath} canvas pack --sources ""{appDir.FullName}"" --msapp ""{Path.Combine(downloadFile.Directory!.FullName, appDir.Name)}.msapp""");
            _logger.LogInformation(output + Divider);
            _notifier.ShowBalloonTip(1000, "Success!", $"Unpack of {downloadFile.Name} succeeded!", ToolTipIcon.Info);
        }

        private string GetExtractDirectory(string downloadPath)
        {
            if (_settings.ExtractDirStartsWithMapping.TryGetValue(Path.GetFileNameWithoutExtension(downloadPath), out var appExtractDir))
            {
                return appExtractDir;
            }

            var nameParts = Path.GetFileName(downloadPath).Split(' ', '.', '_');
            var name = string.Empty;
            foreach (var namePart in nameParts)
            {
                name += (string.IsNullOrWhiteSpace(name) ? string.Empty : " ") + namePart;
                if (_settings.ExtractDirStartsWithMapping.TryGetValue(name, out var found))
                {
                    return found;
                }
            }
            throw new Exception($"Unable to find {string.Join(' ', nameParts)} in {string.Join(',', _settings.ExtractDirStartsWithMapping.Keys)}");
        }

        private string ReplaceVersion(string path)
        {
            var parts = new List<string>();
            foreach (var dir in Split(new DirectoryInfo(path)))
            {
                if (dir.Name.Contains("$V$"))
                {
                    var existing = Directory.GetDirectories(dir.Parent!.FullName, dir.Name.Replace("$V$", "*")).MaxBy(d => d);
                    if (existing == null)
                    {
                        parts.Add(dir.Name.Replace("$V$", "1"));
                    }
                    else
                    {
                        var index = dir.Name.IndexOf("$V$", StringComparison.Ordinal);
                        var info = new DirectoryInfo(existing);
                        var tempNumber = info.Name.Substring(index, info.Name.Length - index);
                        var numberStr = string.Empty;
                        foreach (var chr in tempNumber)
                        {
                            if (!char.IsNumber(chr))
                            {
                                break;
                            }
                            numberStr += chr;
                        }
                        var number = int.Parse(numberStr) + 1;
                        parts.Add(dir.Name.Replace("$V$", number.ToString()));
                    }
                }
                else
                {
                    parts.Add(dir.Name);
                }
            }

            return Path.Combine(parts.ToArray());
        }

        private List<DirectoryInfo> Split(DirectoryInfo path)
        {
            if (path == null) throw new ArgumentNullException("path");
            var ret = new List<DirectoryInfo>();
            if (path.Parent != null) ret.AddRange(Split(path.Parent));
            ret.Add(path);
            return ret;
        }
    }
}
