using System.Reflection;

namespace Vortex.Bot.Utility;

public static class MailTemplateUtility
{
    private static readonly string TemplatesBasePath = Path.Combine(AppContext.BaseDirectory, "Resources", "Templates");

    private static readonly Dictionary<string, string> TemplateCache = [];

    public static string GetTemplate(string templateName)
    {
        if (TemplateCache.TryGetValue(templateName, out string? cachedTemplate))
        {
            return cachedTemplate;
        }

        var templatePath = Path.Combine(TemplatesBasePath, $"{templateName}.html");
        if (File.Exists(templatePath))
        {
            var content = File.ReadAllText(templatePath);
            TemplateCache[templateName] = content;
            return content;
        }

        var resourceContent = LoadFromEmbeddedResource(templateName);
        if (resourceContent != null)
        {
            TemplateCache[templateName] = resourceContent;
            return resourceContent;
        }

        throw new FileNotFoundException($"找不到邮件模板: {templateName}");
    }

    public static string RenderTemplate(string templateName, Dictionary<string, string> variables)
    {
        var template = GetTemplate(templateName);

        foreach (var variable in variables)
        {
            template = template.Replace($"{{{{{variable.Key}}}}}", variable.Value);
        }

        return template;
    }

    public static string RenderTemplate<T>(string templateName, T model) where T : class
    {
        var template = GetTemplate(templateName);
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var value = property.GetValue(model)?.ToString() ?? string.Empty;
            template = template.Replace($"{{{{{property.Name}}}}}", value);
        }

        return template;
    }

    private static string? LoadFromEmbeddedResource(string templateName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"Vortex.Bot.Resources.Templates.{templateName}.html";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return null;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static void ReloadTemplates()
    {
        TemplateCache.Clear();
    }

    public static bool TemplateExists(string templateName)
    {
        if (TemplateCache.ContainsKey(templateName)) return true;

        var templatePath = Path.Combine(TemplatesBasePath, $"{templateName}.html");
        if (File.Exists(templatePath)) return true;

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"Vortex.Bot.Resources.Templates.{templateName}.html";
        return assembly.GetManifestResourceStream(resourceName) != null;
    }
}

public class RegisterEmailModel
{
    public string ServerName { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    public string QQNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
