using RecipeEngine.Api.Commands;
using RecipeEngine.Api.Settings;
using RecipeEngine.Modules.Wrench.Models;
using RecipeEngine.Modules.Wrench.Settings;

namespace Gltfast.Cookbook.Settings;

public class GltfastSettings : AnnotatedSettingsBase
{
    // Path from the root of the repository where packages are located.
    readonly string[] PackagesRootPaths = { "Packages" };

    // update this to list all packages in this repo that you want to release.
    Dictionary<string, PackageOptions> PackageOptions = new()
    {
        {
            "com.unity.cloud.gltfast",
            new PackageOptions
            {
                ReleaseOptions = new ReleaseOptions { IsReleasing = true },
                ValidationOptions = new ValidationOptions
                {
                    UtrTestingYamatoTimeout = 60,
                    ProjectPath = "test-cloud.gltfast",
                },
            }
        }
    };

    public GltfastSettings()
    {
        Wrench = new WrenchSettings(
            PackagesRootPaths,
            PackageOptions,
            wrenchCsProjectPath: "Tools/CI/Gltfast.Cookbook.csproj"
        );

        var gltfast = Wrench.Packages["com.unity.cloud.gltfast"];
        gltfast.InternalDependencies.Add("com.unity.formats.gltf.validator");
        gltfast.PackJobOptions.PrePackCommands.Add(
            new Command("unity-config package add dependency " +
                "com.unity.formats.gltf.validator@0.2.0-preview.1 " +
                "--package-path Packages/com.unity.cloud.gltfast.tests"
                )
            );
        gltfast.PackJobOptions.PrePackCommands.Add(
            new Command("cp .yamato/ValidationExceptions.json* Packages/com.unity.cloud.gltfast"
            )
        );

        Wrench.PvpProfilesToCheck = new HashSet<string> { "supported" };
    }

    public WrenchSettings Wrench { get; private set; }
}
