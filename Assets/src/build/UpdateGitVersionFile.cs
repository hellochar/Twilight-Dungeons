#if UNITY_EDITOR

using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Diagnostics;
using System.Text;
using Debug = UnityEngine.Debug;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.Text.RegularExpressions;

///https://blog.redbluegames.com/version-numbering-for-games-in-unity-and-git-1d05fca83022

public class UpdateGitVersionFile : IPreprocessBuildWithReport {
  public int callbackOrder => 0;
  public void OnPreprocessBuild(BuildReport report) {
    var gitDescribe = RunGit(@"describe --tags --long --match ""v[0-9]*""");
    Debug.Log("git describe " + gitDescribe);
    var bundleVersion = gitDescribe;
    #if UNITY_IOS
    // iOS cannot have the leading "v" or the full git info. We morph it into "1.8.0.0"
    var regex = @"v(\d+).(\d+).(\d+)-(\d+)";
    Match match = Regex.Match(gitDescribe, regex);
    var major = match.Groups[1].Value; // [0] is the full string, everything after that is the individual group
    var minor = match.Groups[2].Value;
    var patch = match.Groups[3].Value;
    var abbrev = match.Groups[4].Value;
    bundleVersion = $"{major}.{minor}.{patch}{ ((abbrev == "0") ? "" : ("." + abbrev)) }";
    #endif
    PlayerSettings.bundleVersion = bundleVersion;
  }

  /// <summary>
  /// Runs git.exe with the specified arguments and returns the output.
  /// </summary>
  public static string RunGit(string arguments) {
    using (var process = new System.Diagnostics.Process()) {
      var exitCode = process.Run(@"git", arguments, Application.dataPath,
          out var output, out var errors);
      if (exitCode == 0) {
        return output;
      } else {
        throw new GitException(exitCode, errors);
      }
    }
  }
}

public static class ProcessExtensions {
  /* Properties ============================================================================================================= */

  /* Methods ================================================================================================================ */

  /// <summary>
  /// Runs the specified process and waits for it to exit. Its output and errors are
  /// returned as well as the exit code from the process.
  /// See: https://stackoverflow.com/questions/4291912/process-start-how-to-get-the-output
  /// Note that if any deadlocks occur, read the above thread (cubrman's response).
  /// </summary>
  public static int Run(this Process process, string application,
      string arguments, string workingDirectory, out string output,
      out string errors) {
    process.StartInfo = new ProcessStartInfo {
      CreateNoWindow = true,
      UseShellExecute = false,
      RedirectStandardError = true,
      RedirectStandardOutput = true,
      FileName = application,
      Arguments = arguments,
      WorkingDirectory = workingDirectory
    };

    // Use the following event to read both output and errors output.
    var outputBuilder = new StringBuilder();
    var errorsBuilder = new StringBuilder();
    process.OutputDataReceived += (_, args) => outputBuilder.AppendLine(args.Data);
    process.ErrorDataReceived += (_, args) => errorsBuilder.AppendLine(args.Data);

    // Start the process and wait for it to exit.
    process.Start();
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();
    process.WaitForExit();

    output = outputBuilder.ToString().TrimEnd();
    errors = errorsBuilder.ToString().TrimEnd();
    return process.ExitCode;
  }
}

/// <summary>
/// GitException includes the error output from a Git.Run() command as well as the
/// ExitCode it returned.
/// </summary>
public class GitException : InvalidOperationException {
  public GitException(int exitCode, string errors) : base(errors) =>
      this.ExitCode = exitCode;

  /// <summary>
  /// The exit code returned when running the Git command.
  /// </summary>
  public readonly int ExitCode;
}
#endif
