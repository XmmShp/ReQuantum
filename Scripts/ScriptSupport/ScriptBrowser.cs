using System.Runtime.InteropServices;

namespace ReQuantum.ScriptSupport;

public static class ScriptBrowser
{
    public static string? GetLocalBrowserPath()
    {
        return GetCandidatePaths().FirstOrDefault(File.Exists);
    }

    private static IEnumerable<string> GetCandidatePaths()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string[] relativePaths =
            [
                @"Google\Chrome\Application\chrome.exe",
                @"Microsoft\Edge\Application\msedge.exe",
                @"Chromium\Application\chrome.exe",
                @"BraveSoftware\Brave-Browser\Application\brave.exe"
            ];

            foreach (var rel in relativePaths)
            {
                if (!string.IsNullOrEmpty(programFiles))
                {
                    yield return Path.Combine(programFiles, rel);
                }

                if (!string.IsNullOrEmpty(programFilesX86))
                {
                    yield return Path.Combine(programFilesX86, rel);
                }

                if (!string.IsNullOrEmpty(localAppData))
                {
                    yield return Path.Combine(localAppData, rel);
                }
            }

            yield break;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            yield return "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
            yield return "/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge";
            yield return "/Applications/Chromium.app/Contents/MacOS/Chromium";
            yield return "/Applications/Brave Browser.app/Contents/MacOS/Brave Browser";

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(home))
            {
                yield return Path.Combine(home, "Applications/Google Chrome.app/Contents/MacOS/Google Chrome");
            }

            yield break;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            string[] names =
            [
                "google-chrome", "google-chrome-stable",
                "chromium", "chromium-browser",
                "microsoft-edge", "microsoft-edge-stable",
                "brave-browser"
            ];

            string[] dirs = ["/usr/bin", "/usr/local/bin", "/snap/bin"];

            foreach (var dir in dirs)
            {
                foreach (var name in names)
                {
                    yield return Path.Combine(dir, name);
                }
            }
        }
    }
}
