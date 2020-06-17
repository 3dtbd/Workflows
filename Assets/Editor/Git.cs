using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using threeDtbd.Workflow.PackageManagement;

namespace threeDtbd.Workflow.VersionControl
{
    [InitializeOnLoad]
    public class Git
    {
        /// <summary>
        /// Add a Git repository into the current project as a Git remote.
        /// </summary>
        /// <param name="gitURI">The URI of the repository to clone.</param>
        /// <param name="path">The path relative to the `Assets/` folder into which the repository should be cloned.</param>
        public static string AddAsSubmodule(AssetDescriptor desc, string path)
        {
            Process process = Process("submodule add " + desc.gitURI, path);
            process.WaitForExit();
            string line = process?.StandardError.ReadLine();
            if (!string.IsNullOrEmpty(line) && !line.StartsWith("Cloning into"))
            {
                throw new Exception("Git error: " + line);
            }

            return process?.StandardOutput.ReadToEnd();
        }

        public static bool IsRemotePresent(AssetDescriptor desc)
        {
            Process process = Process("config --file .gitmodules --list", "..");
            process.WaitForExit();
            string output = process?.StandardError.ReadToEnd();
            if (!string.IsNullOrEmpty(output) )
            {
                throw new Exception("Git error: " + output);
            }

            output = process?.StandardOutput.ReadToEnd();
            return output.Contains(desc.gitURI);
        }

        /// <summary>
        /// Start a git process.
        /// </summary>
        /// <param name="arguments">The command and argumetns to pass to the Git executable.</param>
        /// <param name="path">The path of the working directory, relative to the Assets folder.</param>
        /// <returns></returns>
        public static Process Process(string arguments, string path)
        {
            path = Application.dataPath + "/" + path;
            Directory.CreateDirectory(path);

            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                // TODO make path to Git configurable
                FileName = "C:\\Program Files\\Git\\bin\\git.exe",
                Arguments = arguments,
                WorkingDirectory = path,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            return System.Diagnostics.Process.Start(processInfo);
        }
    }
}
