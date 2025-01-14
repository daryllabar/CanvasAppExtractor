using System.Diagnostics;
using System.Text;

namespace CanvasAppExtractor
{
    public sealed class WindowsBackgroundService : BackgroundService
    {
        private readonly ILogger<WindowsBackgroundService> _logger;
        private readonly CanvasAppExtractorSettings _settings;
        private readonly FileSystemWatcher _fileWatcher;
        private bool _disposed;

        public WindowsBackgroundService(FileSystemWatcher fileWatcher, ILogger<WindowsBackgroundService> logger, CanvasAppExtractorSettings settings)
        {
            _logger = logger;
            _settings = settings;
            _fileWatcher = fileWatcher;

            if (string.IsNullOrWhiteSpace(_settings.PacPath))
            {
                throw new ArgumentException("PacPath is required", nameof(settings));
            }

            if (_settings.ExtractDirStartsWithMapping.Count == 0)
            {
                throw new ArgumentException("ExtractDirStartsWithMapping is required", nameof(settings));
            }

            if (_settings.AutoOpenResultFile && !_settings.CreateResultFile)
            {
                throw new ArgumentException("Invalid Configuration.  If AutoOpenResultFile is set to true, CreateResultFile must also be true.", nameof(settings));
            }

            var sb = new StringBuilder("Mapping Values Loaded: " + Environment.NewLine);
            foreach(var mapping in _settings.ExtractDirStartsWithMapping)
            {
                sb.AppendLine($"{mapping.Key}: {mapping.Value}");
            }

            _logger.LogInformation("{Mappings}", sb.ToString());

            _fileWatcher.Path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
            _fileWatcher.Created += OnNewFileCreated;
            _fileWatcher.Renamed += OnNewFileCreated;
            _fileWatcher.EnableRaisingEvents = true;
        }

        private void OnNewFileCreated(object sender, FileSystemEventArgs e)
        {
            try
            {
                _logger.LogInformation("New file created: {file}", e.FullPath);
                if (Path.GetExtension(e.FullPath) != ".msapp")
                {
                    _logger.LogTrace("{FilePath} was not an .msapp file, skipping.", e.FullPath);
                    return;
                }

                var downloadFile = new FileInfo(e.FullPath);
                _logger.LogInformation("Found App " + downloadFile);
                var appExtractDir = GetExtractDirectory(downloadFile.FullName);
                if (appExtractDir.Contains("$V$"))
                {
                    appExtractDir = ReplaceVersion(appExtractDir);
                }

                var output = UnpackMsApp(downloadFile, appExtractDir);
                CreateResultFile(output);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing new value {FilePath}", e.FullPath);
            }
        }

        private void CreateResultFile(string output)
        {
            if (!_settings.CreateResultFile)
            {
                return;
            }

            var fileInfo = new FileInfo(_settings.ResultFilePath);
            if (fileInfo.Directory is { Exists: false })
            {
                try
                {
                    Directory.CreateDirectory(fileInfo.Directory.FullName);
                }
                catch(Exception ex)
                {
                    throw new Exception("Unable to create Directory for Result File Creation: " + _settings.ResultFilePath, ex);
                }
            }

            const int maxRetries = 5;
            var retryCount = 0;
            var success = false;
            while (!success)
            {
                try
                {
                    File.WriteAllText(fileInfo.FullName, output);
                    success = true;
                }
                catch (Exception)
                {
                    retryCount++;
                    if (retryCount < maxRetries)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(retryCount * 10)); // Exponential backoff
                    }
                    else
                    {
                        throw new Exception("Unable to write Result File to: " + _settings.ResultFilePath);
                    }
                }
            }

            if (_settings.AutoOpenResultFile)
            {
                Process.Start("explorer", "\"" + fileInfo.FullName + "\"");
            }
        }

        private string UnpackMsApp(FileInfo downloadFile, string appExtractDir)
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
            _logger.LogInformation(output.ToString());
            return output.ToString();
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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var watcherActive = true;
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    }
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                RemoveFolderWatch();
            }
            catch (OperationCanceledException)
            {
                // When the stopping token is canceled, for example, a call made from services.msc,
                // we shouldn't exit with a non-zero exit code. In other words, this is expected...
                RemoveFolderWatch();
            }
            catch (Exception ex)
            {
                RemoveFolderWatch();
                _logger.LogError(ex, "{Message} - {Details}", ex.Message, ex.ToString());
                // Terminates this process and returns an exit code to the operating system.
                // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
                // performs one of two scenarios:
                // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
                // 2. When set to "StopHost": will cleanly stop the host, and log errors.
                //
                // In order for the Windows Service Management system to leverage configured
                // recovery options, we need to terminate the process with a non-zero exit code.
                Environment.Exit(1);
            }
            return;

            void RemoveFolderWatch()
            {
                if (watcherActive)
                {
                    _fileWatcher.Created -= OnNewFileCreated;
                    _fileWatcher.Renamed -= OnNewFileCreated;
                    watcherActive = false;
                }
            }
        }

        public override void Dispose()
        {
            Dispose(true);
            //GC.SuppressFinalize(this);
            base.Dispose();
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _fileWatcher.Dispose();
            }
            _disposed = true;
        }
    }
}
