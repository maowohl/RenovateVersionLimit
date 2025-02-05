using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Maowohl.RenovateVersionLimit.Classes;

public class RenovateFile
{
    [JsonPropertyName("$schema")]
    public string Schema { get; set; } = "https://docs.renovatebot.com/renovate-schema.json";
    [JsonPropertyName("packageRules")]
    public List<PackageRule> PackageRules { get; set; } = new();

    public class PackageRule
    {
        [JsonPropertyName("matchPackageNames")]
        public List<string> MatchPackageNames { get; set; } = new();
        [JsonPropertyName("allowedVersions")]
        public string AllowedVersions { get; set; } = string.Empty;
    }

    public void AddPackage(string packageName, string allowedVersion)
    {
        PackageRules.Add(new PackageRule
        {
            MatchPackageNames = new List<string> { packageName },
            AllowedVersions = $"<={allowedVersion}",
        });
    }

    public void SaveToFile(string path)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // does not escape < and >
        }));
    }
}