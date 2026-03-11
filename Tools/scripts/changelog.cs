// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ChangelogCs;

static class MainClass
{
    static int Main(string[] args)
    {
        var arguments = Arguments.Parse(args);

        var changelog = LoadMarkdown(arguments.InputFile ?? "CHANGELOG.md");

        switch (arguments.Command)
        {
            case Command.Release:
            {
                ReleaseInfo release;
                if (arguments.ReleaseType == ReleaseType.Explicit)
                {
                    release = arguments.Release!.Value;
                    changelog.Release(release);
                }
                else
                {
                    release = changelog.Release(arguments.ReleaseType, arguments.ReleaseDate);
                }

                var destination = arguments.OutputFile ?? arguments.InputFile ?? "CHANGELOG.md";
                using var output = new FileStream(destination, FileMode.Create, FileAccess.Write);
                var writer = new StreamWriter(output);
                changelog.WriteTo(writer);
                writer.Flush();
                output.Flush();

                Console.WriteLine($"Released `{release}` in {destination}");
                return 0;
            }
            case Command.Info:
                Info(changelog);
                return 0;
        }

        Console.WriteLine("Usage: changelog <command> [-i <inputFile>] [-o <outputFile>]");
        Console.WriteLine("Usage: changelog release [major|minor|patch|<custom version>] [-d <date>]");
        return 1;
    }

    static Section LoadMarkdown(string filePath)
    {
        var lines = File.ReadAllText(filePath);
        return new Section(lines);
    }

    static void Info(Section changelog)
    {
        var unreleased = changelog
            .OfType<Section>()
            .FirstOrDefault(x => x.IsUnreleased)
            ?? throw new InvalidDataException("Changelog file must contain a level 2 'Unreleased' section.");

        var lastRelease = changelog
            .OfType<Section>()
            .FirstOrDefault(x => x.IsRelease);

        Console.WriteLine(
            lastRelease == null
                ? "Unreleased changes:"
                : $"Unreleased changes since {lastRelease.ReleaseInfo!.Value.Date:yyyy-MM-dd}:"
            );

        foreach (var content in unreleased)
        {
            Console.WriteLine(content.Text);
        }
    }
}

public partial class Section : Content
{
    readonly Section? m_Parent;
    readonly int m_Level;

    public readonly ReleaseInfo? ReleaseInfo;
    public readonly bool IsUnreleased;

    public bool IsRelease => ReleaseInfo != null;

    public Section(string markdown) : base(string.Empty)
    {
        var lines = markdown.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        if (lines.Length < 2)
        {
            throw new InvalidDataException("Changelog file is empty.");
        }
        if (lines[0] != "# Changelog")
        {
            throw new InvalidDataException("Changelog file must start with header `# Changelog`.");
        }
        m_Parent = null;
        m_Level = 0;
        Text = lines[0];
        InitializeFromLines(lines);
    }

    Section(Section? parent, string text) : base(text)
    {
        m_Parent = parent;
        m_Level = GetLevel(text);
        if (m_Level == 2)
        {
            if (ChangelogCs.ReleaseInfo.TryParseMarkdown(text, out var releaseInfo))
            {
                ReleaseInfo = releaseInfo;
            }
            else
            {
                IsUnreleased = UnreleasedReleaseRegex().IsMatch(text);
            }
        }
    }

    void InitializeFromLines(IList<string> lines)
    {
        var currentHeader = this;

        for (var line = 1; line < lines.Count; line++)
        {
            var lineText = lines[line];
            if (lineText.StartsWith('#'))
            {
                var level = GetLevel(lineText);

                while (true)
                {
                    if (level > currentHeader.m_Level)
                    {
                        var header = new Section(currentHeader, lineText);
                        currentHeader.Add(header);
                        currentHeader = header;
                        break;
                    }

                    currentHeader = currentHeader.m_Parent ?? throw new InvalidDataException($"Unexpected header level at line {line + 1}: {lineText}");
                }
            }
            else
            {
                var contentLine = new Content(lineText);
                currentHeader.Add(contentLine);
            }
        }
    }

    public ReleaseInfo Release(ReleaseType releaseType, DateTime? date = null)
    {
        var lastRelease = this
            .OfType<Section>()
            .FirstOrDefault(x => x.IsRelease);

        ReleaseInfo? release = null;

        if (releaseType == ReleaseType.Automatic)
        {
            var unreleased = GetUnreleasedSection();
            var sections = unreleased
                .OfType<Section>()
                .Where(x => x.Text.StartsWith("### "))
                .ToArray();
            if (sections.Any(x => x.Text[4..] == "Removed"))
            {
                releaseType = ReleaseType.Major;
            }
            else if (sections.Any(x => x.Text[4..] is "Added" or "Changed" or "Deprecated"))
            {
                releaseType = ReleaseType.Minor;
            }
            else if (sections.Any(x => x.Text[4..] is "Fixed" or "Security"))
            {
                releaseType = ReleaseType.Patch;
            }

            if (lastRelease == null)
            {
                release = new ReleaseInfo(0, 1, 0, date: date ?? DateTime.Now);
            }
        }

        release ??= lastRelease?.ReleaseInfo!.Value.Increment(releaseType, date)
            ?? throw new InvalidDataException("Changelog file must contain a valid release.");

        Release(release.Value);
        return release.Value;
    }

    public void Release(ReleaseInfo release)
    {
        var unreleased = GetUnreleasedSection();

        // Check if this is a final release (no suffix) that should merge experimental/preview releases
        if (string.IsNullOrEmpty(release.SuffixName))
        {
            var relatedSections = this
                .OfType<Section>()
                .Where(x => x.IsRelease &&
                           x.ReleaseInfo!.Value.Major == release.Major &&
                           x.ReleaseInfo!.Value.Minor == release.Minor &&
                           x.ReleaseInfo!.Value.Patch == release.Patch &&
                           !string.IsNullOrEmpty(x.ReleaseInfo!.Value.SuffixName))
                .ToArray();

            if (relatedSections.Length > 0)
            {
                // Merge the unreleased section with all related experimental/preview sections
                var sectionsToMerge = new List<Section> { unreleased };
                sectionsToMerge.AddRange(relatedSections);

                var merged = Merge(sectionsToMerge.ToArray());
                merged.Text = $"## {release}";
                merged.RemoveEmptySections();

                // Remove the original experimental/preview sections from the changelog
                foreach (var sectionToRemove in relatedSections)
                {
                    Remove(sectionToRemove);
                }

                // Replace unreleased section with merged section
                var unreleasedIndex = IndexOf(unreleased);
                this[unreleasedIndex] = merged;

                if (merged.NonEmptyContentCount() == 0)
                {
                    throw new InvalidOperationException("Final release section cannot be created because there are no changes to merge.");
                }
                return;
            }
        }

        // Standard release without merging
        unreleased.Text = $"## {release}";
        unreleased.RemoveEmptySections();
        if (unreleased.NonEmptyContentCount() == 0)
        {
            throw new InvalidOperationException("'Unreleased' section cannot be released because it does not contain any changes.");
        }
    }

    Section GetUnreleasedSection()
    {
        return this
                .OfType<Section>()
                .FirstOrDefault(x => x.IsUnreleased)
            ?? throw new InvalidDataException("Changelog file must contain a level 2 'Unreleased' section.");
    }

    static Section Merge(params Section[] sections)
    {
        switch (sections.Length)
        {
            case 0:
                throw new ArgumentException("At least one section must be provided", nameof(sections));
            case 1:
                return sections[0]; // No merging needed
        }

        // Create a new section with the same text as the first section
        var merged = new Section(null, sections[0].Text);
        var sectionsIndex = 0;

        // Dictionary to track sections by their Text for merging
        var sectionsByText = new Dictionary<string, Section>();

        foreach (var section in sections)
        {
            foreach (var child in section)
            {
                if (child is Section childSection)
                {
                    if (sectionsByText.TryGetValue(childSection.Text, out var existingSection))
                    {
                        // Merge the children by adding them to the existing section
                        foreach (var grandChild in childSection)
                        {
                            existingSection.Add(grandChild);
                        }
                    }
                    else
                    {
                        // Create a new section and add all its children
                        sectionsByText[childSection.Text] = childSection;
                        merged.Add(childSection);
                    }
                }
                else
                {
                    // Just a content item, put it before the sections
                    merged.Insert(sectionsIndex++, child);
                }
            }
        }

        return merged;
    }

    void RemoveEmptySections()
    {
        for (var i = Count - 1; i >= 0; i--)
        {
            if (this[i] is Section section)
            {
                section.RemoveEmptySections();
                if (section.NonEmptyContentCount() == 0)
                {
                    RemoveAt(i);
                }
            }
        }
    }

    int NonEmptyContentCount()
    {
        var count = 0;
        foreach (var child in this)
        {
            switch (child)
            {
                case Section section:
                    count += section.NonEmptyContentCount();
                    break;
                case not null:
                    if (!string.IsNullOrWhiteSpace(child.Text))
                    {
                        count++;
                    }
                    break;
            }
        }
        return count;
    }

    public void WriteTo(TextWriter writer)
    {
        writer.Write(Text);
        foreach (var child in this)
        {
            switch (child)
            {
                case Section section:
                    writer.WriteLine();
                    section.WriteTo(writer);
                    break;
                case not null:
                    writer.WriteLine();
                    writer.Write(child.Text);
                    break;
            }
        }
    }

    public override string ToString()
    {
        using var writer = new StringWriter();
        WriteTo(writer);
        return writer.ToString();
    }

    static int GetLevel(string text)
    {
        return text.TakeWhile(c => c == '#').Count();
    }

    [GeneratedRegex(@"^##\s+\[?Unreleased\]?\s*$")]
    private static partial Regex UnreleasedReleaseRegex();
}

public class Content(string text) : List<Content>
{
    public string Text { get; protected set; } = text;

    public override string ToString()
    {
        return Text;
    }
}

enum Command
{
    None,
    Release,
    Info
}

enum Argument
{
    None,
    InputFile,
    OutputFile,
    ReleaseType,
    ReleaseDate
}

public enum ReleaseType
{
    Automatic,
    Major,
    Minor,
    Patch,
    FinalizeSuffix,
    Suffix,
    Explicit
}

public readonly partial struct ReleaseInfo(int major, int minor, int patch, string? suffixName = null, int? suffixIteration = null, DateTime? date = null)
    : IEquatable<ReleaseInfo>
{
    public int Major { get; private init; } = major;
    public int Minor { get; private init; } = minor;
    public int Patch { get; private init; } = patch;
    public string? SuffixName { get; private init; } = suffixName;
    public int? SuffixIteration { get; private init; } = suffixIteration;
    public DateTime Date { get; private init; } = date ?? DateTime.Now;

    bool HasSuffix => !string.IsNullOrEmpty(SuffixName) && SuffixIteration.HasValue;

    public static bool TryParseMarkdown(string text, out ReleaseInfo releaseInfo)
    {
        var match = ReleaseInfoRegex().Match(text);
        if (match.Success)
        {
            var suffixName = match.Groups["suffixName"].Value;
            suffixName = string.IsNullOrEmpty(suffixName) ? null : suffixName;
            var suffixIterationStr = match.Groups["suffixIteration"].Value;
            int? suffixIteration = string.IsNullOrEmpty(suffixIterationStr) ? null : int.Parse(suffixIterationStr);
            releaseInfo = new ReleaseInfo
            {
                Major = int.Parse(match.Groups["major"].Value),
                Minor = int.Parse(match.Groups["minor"].Value),
                Patch = int.Parse(match.Groups["patch"].Value),
                SuffixName = suffixName,
                SuffixIteration = suffixIteration,
                Date = DateTime.Parse(match.Groups["date"].Value, DateTimeFormatInfo.InvariantInfo)
            };
            return true;
        }
        releaseInfo = default;
        return false;
    }

    public static bool TryParseVersion(string text, out ReleaseInfo releaseInfo)
    {
        var match = VersionRegex().Match(text);
        if (match.Success)
        {
            var suffixName = match.Groups["suffixName"].Value;
            suffixName = string.IsNullOrEmpty(suffixName) ? null : suffixName;
            var suffixIterationStr = match.Groups["suffixIteration"].Value;
            int? suffixIteration = string.IsNullOrEmpty(suffixIterationStr) ? null : int.Parse(suffixIterationStr);
            releaseInfo = new ReleaseInfo
            {
                Major = int.Parse(match.Groups["major"].Value),
                Minor = int.Parse(match.Groups["minor"].Value),
                Patch = int.Parse(match.Groups["patch"].Value),
                SuffixName = suffixName,
                SuffixIteration = suffixIteration,
                Date = DateTime.Now
            };
            return true;
        }
        releaseInfo = default;
        return false;
    }

    public ReleaseInfo Increment(ReleaseType releaseType, DateTime? date = null)
    {
        if (releaseType == ReleaseType.FinalizeSuffix && !HasSuffix)
        {
            throw new InvalidOperationException("Cannot finalize suffix because the previous release did not have a suffix.");
        }
        return this with
        {
            Major = releaseType == ReleaseType.Major ? Major + 1 : Major,
            Minor = releaseType switch
            {
                ReleaseType.Minor => Minor + 1,
                ReleaseType.Major => 0,
                _ => Minor
            },
            Patch = releaseType switch
            {
                ReleaseType.Patch => Patch + 1,
                ReleaseType.Minor => 0,
                ReleaseType.Major => 0,
                _ => Patch
            },
            SuffixName = releaseType == ReleaseType.Suffix ? SuffixName : null,
            SuffixIteration = releaseType == ReleaseType.Suffix ? SuffixIteration + 1 : null,
            Date = date ?? DateTime.Now
        };
    }

    public override string ToString()
    {
        var version = $"{Major}.{Minor}.{Patch}";
        if (HasSuffix)
        {
            version += $"-{SuffixName}.{SuffixIteration!.Value}";
        }
        return $"[{version}] - {Date:yyyy-MM-dd}";
    }

    public bool Equals(ReleaseInfo other)
    {
        return Major == other.Major &&
               Minor == other.Minor &&
               Patch == other.Patch &&
               SuffixName == other.SuffixName &&
               SuffixIteration == other.SuffixIteration &&
               Date.Date == other.Date.Date;
    }

    public override bool Equals(object? obj)
    {
        return obj is ReleaseInfo other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Major, Minor, Patch, SuffixName, SuffixIteration, Date);
    }

    public static bool operator ==(ReleaseInfo left, ReleaseInfo right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ReleaseInfo left, ReleaseInfo right)
    {
        return !(left == right);
    }

    [GeneratedRegex(@"^##\s+\[?(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(-(?<suffixName>\w+)\.(?<suffixIteration>\d+))?\]?\s-\s(?<date>\d{4}-\d{2}-\d{2})\s*$")]
    private static partial Regex ReleaseInfoRegex();

    [GeneratedRegex(@"^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(-(?<suffixName>\w+)\.(?<suffixIteration>\d+))?$")]
    private static partial Regex VersionRegex();
}

class Arguments
{
    public Command Command { get; private set; } = Command.Info;
    public string? InputFile { get; private set; }
    public string? OutputFile { get; private set; }
    public ReleaseType ReleaseType { get; private set; } = ReleaseType.Patch;
    public ReleaseInfo? Release { get; private set; }
    public DateTime? ReleaseDate { get; private set; }

    public static Arguments Parse(string[] args)
    {
        var arguments = new Arguments();

        Argument currentArgument = Argument.None;

        foreach (var arg in args)
        {
            switch (arg)
            {
                case "release":
                    arguments.Command = Command.Release;
                    arguments.ReleaseType = ReleaseType.Automatic;
                    currentArgument = Argument.ReleaseType;
                    break;
                case "info":
                    arguments.Command = Command.Info;
                    currentArgument = Argument.None;
                    break;
                case "-i":
                case "--input":
                    currentArgument = Argument.InputFile;
                    break;
                case "-o":
                case "--output":
                    currentArgument = Argument.OutputFile;
                    break;
                case "-d":
                case "--date":
                    currentArgument = Argument.ReleaseDate;
                    break;
                default:
                    switch (currentArgument)
                    {
                        case Argument.ReleaseType:
                            arguments.ReleaseType = arg switch
                            {
                                "major" => ReleaseType.Major,
                                "minor" => ReleaseType.Minor,
                                "patch" => ReleaseType.Patch,
                                _ => ReleaseType.Explicit
                            };
                            if (arguments.ReleaseType == ReleaseType.Explicit)
                            {
                                arguments.Release = ReleaseInfo.TryParseVersion(arg, out var releaseInfo)
                                    ? releaseInfo
                                    : throw new ArgumentException($"Invalid release type or version: {arg}");
                            }
                            break;
                        case Argument.InputFile:
                            arguments.InputFile = arg;
                            break;
                        case Argument.OutputFile:
                            arguments.OutputFile = arg;
                            break;
                        case Argument.ReleaseDate:
                            if (DateTime.TryParse(arg, DateTimeFormatInfo.InvariantInfo, out var date))
                            {
                                arguments.ReleaseDate = date;
                                if (arguments.Release != null)
                                {
                                    var r = arguments.Release.Value;
                                    arguments.Release = new ReleaseInfo(r.Major, r.Minor, r.Patch, r.SuffixName, r.SuffixIteration, date);
                                }
                            }
                            else
                            {
                                throw new ArgumentException($"Invalid date format: {arg}");
                            }
                            break;
                        default:
                            throw new ArgumentException($"Unknown argument: {arg}");
                    }
                    break;
            }
        }

        return arguments;
    }
}
