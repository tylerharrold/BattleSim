namespace BattleSim.App.Services;

public static class DevelopmentTemplatePaths
{
    public const string DevelopmentUnitTemplateRelativePath = "DevData/UnitTemplates";

    public static string GetBuiltInTemplateDirectory()
    {
        return Path.Combine(AppContext.BaseDirectory, "Data", "UnitTemplates");
    }

    public static string GetDevelopmentTemplateDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "BattleSim.sln")))
            {
                return Path.Combine(directory.FullName, "DevData", "UnitTemplates");
            }

            directory = directory.Parent;
        }

        // Development fallback only. In packaged builds this keeps generated templates out of
        // the copied built-in sample folder, but still gives the prototype a writable location.
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BattleSim",
            "Development",
            "UnitTemplates");
    }
}
