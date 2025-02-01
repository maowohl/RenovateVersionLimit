namespace renovate_nuget_version_limit.Classes;

public class Dependency
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public int Level { get; set; } = 0;
}