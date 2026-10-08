// Hosting pieces of the chaos mod (same idea as the trainers' TrainerHost, own files):
//   ChaosPaths   <game>\StreamEmber\...
//   ChaosLog     <game>\StreamEmber\Logs\ChaosMod.log, errors throttled
//   ChaosConfig  <game>\StreamEmber\Config\ChaosMod.ini
//   ChaosPage    the chaos page in the overlay (GitHub Pages, MHud kit from the CDN) and whether it is the current page
//   Guard        runs one part of a tick; an exception is logged and only that part is skipped
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using StreamEmber.Overlay;
using StreamEmber.Trainers;

namespace StreamEmber.ChaosMod
{
    internal static class ChaosPaths
    {
        private static string s_root;

        /// <summary>&lt;game&gt;\StreamEmber</summary>
        public static string Root
        {
            get
            {
                if (s_root == null)
                {
                    string exe = Process.GetCurrentProcess().MainModule.FileName;
                    s_root = Path.Combine(Path.GetDirectoryName(exe), "StreamEmber");
                }
                return s_root;
            }
        }

        public static string ConfigFile => Path.Combine(Path.Combine(Root, "Config"), "ChaosMod.ini");
        public static string LogFile => Path.Combine(Path.Combine(Root, "Logs"), "ChaosMod.log");
    }

    internal static class ChaosLog
    {
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, int> ErrorCounts = new Dictionary<string, int>();
        private static bool s_started;

        public static void Info(string message) => Write("INFO", message);
        public static void Warn(string message) => Write("WARN", message);

        /// <summary>Logs an exception of one area: the first 10, then every 1000th (a broken frame repeats 60x a second).</summary>
        public static void Error(string area, Exception ex)
        {
            int n;
            lock (Gate)
            {
                ErrorCounts.TryGetValue(area, out n);
                ErrorCounts[area] = ++n;
            }
            if (n <= 10 || n % 1000 == 0)
            {
                Write("ERROR", area + " failed (#" + n + "): " + ex);
            }
        }

        private static void Write(string level, string message)
        {
            try
            {
                lock (Gate)
                {
                    string path = ChaosPaths.LogFile;
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    string line = "[" + DateTime.Now.ToString("HH:mm:ss.fff") + "] [" + level + "] " + message + Environment.NewLine;
                    if (!s_started)
                    {
                        s_started = true;
                        // New log per game session; the previous session's log is kept next to it
                        try
                        {
                            if (File.Exists(path))
                            {
                                File.Copy(path, Path.Combine(Path.GetDirectoryName(path),
                                    Path.GetFileNameWithoutExtension(path) + ".previous.log"), true);
                            }
                        }
                        catch
                        {
                        }
                        File.WriteAllText(path, line, new UTF8Encoding(false));
                    }
                    else
                    {
                        File.AppendAllText(path, line, new UTF8Encoding(false));
                    }
                }
            }
            catch
            {
                // Logging must never break the game
            }
        }
    }

    internal sealed class ChaosConfig
    {
        /// <summary>Chaos page. Empty in the file = the published page.</summary>
        public string UiUrl;
        public Keys MenuKey = Keys.F6;
        /// <summary>MHud theme of the page (frontier, oldwest, modern, neon, tactical, minimal).</summary>
        public string Theme = "frontier";
        /// <summary>Automatic mode: a random effect every <see cref="AutoInterval"/> seconds (off by default).</summary>
        public bool AutoMode;
        public int AutoInterval = 45;
        /// <summary>Effects that write version-dependent script globals (honor). Off by default.</summary>
        public bool Experimental;

        public static ChaosConfig Load(string defaultUiUrl)
        {
            var config = new ChaosConfig { UiUrl = defaultUiUrl };
            try
            {
                string path = ChaosPaths.ConfigFile;
                if (!File.Exists(path)) return config;
                foreach (string raw in File.ReadAllLines(path))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line[0] == ';' || line[0] == '#' || line.StartsWith("//")) continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = line.Substring(0, eq).Trim();
                    string value = line.Substring(eq + 1).Trim().Trim('"');
                    switch (key.ToLowerInvariant())
                    {
                        case "uiurl":
                            if (value.Length > 0) config.UiUrl = value;
                            break;
                        case "menukey":
                            if (Enum.TryParse(value, true, out Keys k) && k != Keys.None) config.MenuKey = k;
                            break;
                        case "theme":
                            if (value.Length > 0) config.Theme = value.ToLowerInvariant();
                            break;
                        case "automode":
                            config.AutoMode = IsTrue(value);
                            break;
                        case "autointerval":
                            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int s))
                                config.AutoInterval = Math.Max(10, Math.Min(600, s));
                            break;
                        case "experimental":
                            config.Experimental = IsTrue(value);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                ChaosLog.Error("ChaosMod.ini", ex);
            }
            return config;
        }

        private static bool IsTrue(string value) =>
            value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Opens the chaos page in the overlay once the overlay runs.</summary>
    /// <summary>
    /// The overlay has one page and one message queue for every script. The trainer and the chaos mod each have their
    /// own page, so the page decides who talks to the overlay: a script reads messages and sends only while its own
    /// page is the current one (<see cref="IsCurrent"/>). At startup the page is opened only when the overlay shows
    /// nothing yet; otherwise it is opened by the menu key (<see cref="Claim"/>).
    /// </summary>
    internal sealed class ChaosPage
    {
        private readonly string _url;
        private readonly string _key;
        private bool _startChecked;
        private string _previousUrl;   // page shown before Claim, put back by Release

        public ChaosPage(string url)
        {
            // ?v= keeps the page and the script of one release together in caches
            string version = ProductVersion;
            _url = url.IndexOf('?') < 0 && url.StartsWith("http", StringComparison.OrdinalIgnoreCase) && version != null
                ? url + "?v=" + Uri.EscapeDataString(version)
                : url;
            _key = Key(_url);
        }

        public string Url => _url;

        public static string ProductVersion
        {
            get
            {
                var attribute = (AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute(
                    typeof(ChaosPage).Assembly, typeof(AssemblyInformationalVersionAttribute));
                return attribute?.InformationalVersion;
            }
        }

        /// <summary>True while the overlay shows the chaos page (any version of it).</summary>
        public bool IsCurrent => Key(OverlayBridge.Url) == _key;

        /// <summary>Call every tick once the player is in the world. Opens the page only if the overlay is still
        /// blank, so another script's page (the trainer) is not replaced at startup.</summary>
        public void EnsureStartup()
        {
            if (_startChecked) return;
            _startChecked = true;
            string current = OverlayBridge.Url;
            if (IsBlank(current))
            {
                ChaosLog.Info("Opening " + _url);
                OverlayBridge.LoadUrl(_url);
            }
            else if (Key(current) == _key)
            {
                // Already open (scripts reloaded while the game kept running): ask the page to announce itself again
                ChaosLog.Info("Chaos page already open, asking it for ready");
                Hello();
            }
            else
            {
                ChaosLog.Info("The overlay shows " + current + "; the chaos page opens with the menu key");
            }
        }

        /// <summary>Opens the chaos page now (menu key). <paramref name="reload"/> loads it again even if it is the
        /// current page (it did not answer: network error or not published).</summary>
        public void Claim(bool reload)
        {
            string current = OverlayBridge.Url;
            if (Key(current) != _key) _previousUrl = current;
            ChaosLog.Info((reload ? "Reloading " : "Opening ") + _url + " (overlay showed " + (current ?? "nothing") + ")");
            if (!OverlayBridge.LoadUrl(_url, reload)) Hello();
        }

        /// <summary>The chaos page did not answer (not published: GitHub shows its own opaque 404 page over the
        /// game, or no network): put back the page shown before <see cref="Claim"/>, or a blank one.</summary>
        public void Release()
        {
            if (!IsCurrent) return;
            string back = IsBlank(_previousUrl) ? string.Empty : _previousUrl;
            _previousUrl = null;
            ChaosLog.Info("Putting back " + (back.Length == 0 ? "about:blank" : back));
            OverlayBridge.LoadUrl(back);
        }

        private static void Hello()
        {
            Ui.Begin("chaos:hello").BeginObject().EndObject();
            Ui.Send();
        }

        private static bool IsBlank(string url) =>
            string.IsNullOrEmpty(url) || url.Equals("about:blank", StringComparison.OrdinalIgnoreCase);

        /// <summary>URL without query, fragment and trailing slash, lower case: ?v= and in-page anchors do not matter.</summary>
        public static string Key(string url)
        {
            if (string.IsNullOrEmpty(url)) return string.Empty;
            string key = url.Trim();
            int cut = key.IndexOfAny(new[] { '?', '#' });
            if (cut >= 0) key = key.Substring(0, cut);
            key = key.TrimEnd('/');
            if (key.EndsWith("/index.html", StringComparison.OrdinalIgnoreCase)) key = key.Substring(0, key.Length - 11);
            return key.ToLowerInvariant();
        }
    }

    internal static class Guard
    {
        /// <summary>Runs one part of a tick. Returns false (after logging) when it threw.</summary>
        public static bool Run(string area, Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                ChaosLog.Error(area, ex);
                return false;
            }
        }
    }
}
