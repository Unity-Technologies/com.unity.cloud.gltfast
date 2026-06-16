using Gltfast.Cookbook.Settings;
using RecipeEngine.Api.Commands;
using RecipeEngine.Api.Dependencies;
using RecipeEngine.Api.Extensions;
using RecipeEngine.Api.Jobs;
using RecipeEngine.Api.Platforms;
using RecipeEngine.Api.Recipes;
using RecipeEngine.Modules.UnityEditor;
using RecipeEngine.Unity.Abstractions.Editors;

namespace Gltfast.Cookbook.Recipes;

public class CodeFormatRecipe : RecipeBase
{
    const string k_EditorPath = "Editor";
    const string k_SetupLogFile = "UnityProjectSetup.log";

    GltfastSettings m_Settings;

    public CodeFormatRecipe(GltfastSettings settings)
    {
        m_Settings = settings;
        Name = "Code Format";
        Description = "Code formatting";
    }

    protected override ISet<Job> LoadJobs()
    {
        var jobs = new HashSet<IJobBuilder>();
        foreach (var projectPath in m_Settings.ProjectPaths)
        {
            jobs.Add(CreateJob(projectPath));
        }

        jobs.Add(CreateAllJob(jobs));

        return jobs.SelectJobs();
    }

    IJobBuilder CreateAllJob(ISet<IJobBuilder> jobs)
    {
        var deps = new HashSet<Dependency>();
        foreach (var job in jobs)
        {
            deps.Add(new Dependency(Name, job.Id));
        }

        return FluentJob
            .Create(Name)
            .WithDependencies(deps);
    }

    static IJobBuilder CreateJob(string projectPath)
    {
        const string editorVersion = "6000.0";

        var commands = new List<Command>
        {
            UnityEditorCommand
                .Download(
                    new Editor(editorVersion, editorVersion),
                    "unity-downloader-cli",
                    k_EditorPath)
                .ToRetryCommand(3, 10),
            UnityEditorCommand.Execute(
                GetExecutablePath(HostPlatform.Ubuntu),
                builder => SyncSolution(builder, projectPath)),
            new BlockCommand("Certify Git state", [
                "# Ignore changes to ProjectVersion.txt and manifest.json",
                $"git checkout" +
                $" Projects/{projectPath}/ProjectSettings/ProjectVersion.txt" +
                $" Projects/{projectPath}/Packages/manifest.json" +
                $" Packages/com.unity.cloud.gltfast.tests/Tests/Runtime/Export/Materials/**/*.mat",
                "if ! git diff --quiet; then",
                "  git status --porcelain",
                "  MESSAGE=\"Git working directory is not clean after generating solution file and prior to formatting. Please ensure that the project is in a clean state before running the format job.\"",
                "  echo $MESSAGE",
                @"  curl -X POST -d ""{\""title\"": \""Unclean State\"",\""conclusion\"": \""failure\"",\""summary\"": \""$MESSAGE\""}"" -H 'Content-Type: application/json' $YAMATO_REPORTING_SERVER/result",
                "  exit 1",
                "fi"
            ]),
            new(".NET Format", $"dotnet format Projects/{projectPath}/{projectPath}.sln"),
            new BlockCommand("Detect formatting changes", [
                "if git diff --quiet; then",
                "  echo \"No formatting changes detected required.\"",
                "  curl -X POST -d \"{\\\"title\\\": \\\"Code Format Valid\\\",\\\"conclusion\\\": \\\"success\\\"}\" -H 'Content-Type: application/json' $YAMATO_REPORTING_SERVER/result",
                "else",
                "  git diff > format.patch",
                "  echo '{\"title\":\"Code Format Invalid\",\"conclusion\":\"failure\",\"tags\":[\"code-format\",\"slack\"],\"resultType\":\"userFriendly\",\"summary\":\"Some files have incorrect formatting! The `code_format_patch` artifact contains a patch that fixes it.\",\"data\":' > result.json",
                "  cat format.patch | jq -Rs . >> result.json",
                "  echo '}' >> result.json",
                "  curl -X POST -d @result.json -H 'Content-Type: application/json' $YAMATO_REPORTING_SERVER/result",
                "  exit 1",
                "fi",
            ])
        };

        return FluentJob
            .Create($"Code Format {projectPath}")
            .WithAgent("package-ci/ubuntu-22.04:v4", FlavorType.BuildLarge, ResourceType.Vm)
            .WithCommands(commands)
            .WithArtifact("code_format_patch", "format.patch");
    }

    static IUnityEditorExecuteBuilder SyncSolution(IUnityEditorExecuteBuilder builder, string projectPath) =>
        builder
            .WithProjectPath($"Projects/{projectPath}")
            .WithExecuteMethod("Packages.Rider.Editor.RiderScriptEditor.SyncSolution")
            .WithArgs("-upmNoDefaultPackages")
            .WithBatchMode()
            .WithNoGraphics()
            .WithQuit()
            .WithLogs(k_SetupLogFile);

    static string GetExecutablePath(HostPlatform hostPlatform)
    {
        if (hostPlatform.IsLinux)
        {
            return $"{k_EditorPath}/Unity";
        }

        if (hostPlatform.IsMac)
        {
            return $"{k_EditorPath}/Unity.app/Contents/MacOS/Unity";
        }

        if (hostPlatform.IsWindows)
        {
            return $@"{k_EditorPath}\\Unity.exe";
        }

        throw new NotSupportedException($"Platform {hostPlatform} not supported");
    }
}
