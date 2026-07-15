// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

[InitializeOnLoad]
static class MainWindowTitle
{
    const string k_ProjectDefault = "glTFast-Test";
    const string k_GltfastPackageName = "com.unity.cloud.gltfast";

    static string s_PackageUnityVersion;
    static ReleaseType s_PackageReleaseType;
    static bool s_PackageInfoLoaded;

    static MainWindowTitle()
    {
        EditorApplication.updateMainWindowTitle += Decorate;
        EditorApplication.UpdateMainWindowTitle();
    }

    static void Decorate(ApplicationTitleDescriptor desc)
    {
        var isWorkTree = IsWorktree(out var worktreeName);
        string project;
        if (desc.projectName == k_ProjectDefault)
        {
            project = "";
        }
        else if (desc.projectName.StartsWith("glTFast-Test"))
        {
            var subProject = desc.projectName[13..];
            if (subProject == "BuiltIn")
            {
                project = " 🏛️BiRP";
            }
            else if (subProject == "HDRP")
            {
                project = " 💎HDRP";
            }
            else if (subProject == "minimalistic")
            {
                project = " 🪶minimalistic";
            }
            else if (subProject == "entities")
            {
                project = " ⚛️Entities";
            }
            else
            {
                project = $" 📁{subProject}";
            }
        }
        else
        {
            project = desc.projectName;
        }
        EnsurePackageInfoLoaded();
        var experimental = s_PackageReleaseType switch
        {
            ReleaseType.Experimental => "🟥",
            ReleaseType.Preview => "🔵",
            _ => "✅"
        };
        var worktree = isWorkTree ? $"🌳{worktreeName} " : "";
        var version = ShowUnityVersion(desc.unityVersion) ? $" ◼️{desc.unityVersion}" : "";
        desc.title = $"{experimental}glTFast {worktree}{project}{version}";
    }

    static bool ShowUnityVersion(string unityVersion)
    {
        EnsurePackageInfoLoaded();
        return s_PackageUnityVersion == null
            || MajorMinor(unityVersion) != MajorMinor(s_PackageUnityVersion);
    }

    static string MajorMinor(string version)
    {
        var first = version.IndexOf('.');
        if (first < 0) return version;
        var second = version.IndexOf('.', first + 1);
        return second < 0 ? version : version.Substring(0, second);
    }

    static void EnsurePackageInfoLoaded()
    {
        if (s_PackageInfoLoaded) return;
        var info = PackageInfo.FindForAssetPath($"Packages/{k_GltfastPackageName}/package.json");
        if (info == null) return;
        var path = Path.Combine(info.resolvedPath, "package.json");
        if (!File.Exists(path)) return;
        var json = File.ReadAllText(path);
        var unity = Regex.Match(json, "\"unity\"\\s*:\\s*\"([^\"]*)\"");
        if (unity.Success) s_PackageUnityVersion = unity.Groups[1].Value;
        var version = Regex.Match(json, "\"version\"\\s*:\\s*\"([^\"]*)\"");
        if (version.Success)
        {
            if (Regex.IsMatch(version.Groups[1].Value, "-(?:exp)\\.\\d+"))
            {
                s_PackageReleaseType = ReleaseType.Experimental;
            }
            else if (Regex.IsMatch(version.Groups[1].Value, "-(?:pre|preview)\\.\\d+"))
            {
                s_PackageReleaseType = ReleaseType.Preview;
            }
        }

        s_PackageInfoLoaded = true;
    }

    static bool IsWorktree(out string worktreeName)
    {
        worktreeName = null;
        var dir = new DirectoryInfo(Path.GetDirectoryName(Application.dataPath)!);
        while (dir != null)
        {
            var gitPath = Path.Combine(dir.FullName, ".git");
            if (Directory.Exists(gitPath)) return false;
            if (File.Exists(gitPath))
            {
                var line = File.ReadAllText(gitPath).Trim();
                // "gitdir: /…/.git/worktrees/<name>"
                if (line.StartsWith("gitdir:"))
                    worktreeName = new DirectoryInfo(line.Substring(7).Trim()).Name;
                return true;
            }
            dir = dir.Parent;
        }
        return false;
    }

    enum ReleaseType
    {
        Undefined,
        Experimental,
        Preview
    }
}
