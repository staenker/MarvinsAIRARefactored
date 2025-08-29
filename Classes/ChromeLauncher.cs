
using System.Diagnostics;
using System.IO;

namespace MarvinsAIRARefactored.Classes;

public static class ChromeLauncher
{
	/// <summary>
	/// Launch Chrome (preferred) or Edge as a minimal app window to a given URL.
	/// Returns the Process or null if nothing found.
	/// </summary>
	public static Process? LaunchChromeTo( string url )
	{
		var exe = FindChromeOrEdge();
		if ( exe == null ) return null;

		var psi = new ProcessStartInfo( exe )
		{
			Arguments = $"--app=\"{url}\" --disable-translate --disable-infobars --no-first-run",
			UseShellExecute = false,
			CreateNoWindow = true
		};

		return Process.Start( psi );
	}

	/// <summary>
	/// Check if Chrome or Edge is installed.
	/// </summary>
	public static bool IsChromeOrEdgeInstalled()
	{
		return FindChromeOrEdge() != null;
	}

	/// <summary>
	/// Try to locate Chrome or Edge executable. Returns path or null.
	/// </summary>
	private static string? FindChromeOrEdge()
	{
		string[] candidates =
		{
            // Chrome
            Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Google\Chrome\Application\chrome.exe"),
			Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Google\Chrome\Application\chrome.exe"),
			Environment.ExpandEnvironmentVariables(@"%LocalAppData%\Google\Chrome\Application\chrome.exe"),

            // Edge
            Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Microsoft\Edge\Application\msedge.exe"),
			Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Microsoft\Edge\Application\msedge.exe"),
			Environment.ExpandEnvironmentVariables(@"%LocalAppData%\Microsoft\Edge\Application\msedge.exe")
		};

		foreach ( var path in candidates )
		{
			if ( File.Exists( path ) ) return path;
		}

		return null;
	}
}