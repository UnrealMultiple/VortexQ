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

        string templatePath = Path.Combine(TemplatesBasePath, $"{templateName}.html");
        if (File.Exists(templatePath))
        {
            string content = File.ReadAllText(templatePath);
            TemplateCache[templateName] = content;
            return content;
        }

        string? resourceContent = LoadFromEmbeddedResource(templateName);
        if (resourceContent != null)
        {
            TemplateCache[templateName] = resourceContent;
            return resourceContent;
        }

        throw new FileNotFoundException($"找不到邮件模板: {templateName}");
    }

    public static string RenderTemplate(string templateName, Dictionary<string, string> variables)
    {
        string template = GetTemplate(templateName);

        foreach (KeyValuePair<string, string> variable in variables)
        {
            template = template.Replace($"{{{{{variable.Key}}}}}", variable.Value);
        }

        return template;
    }

    public static string RenderTemplate<T>(string templateName, T model) where T : class
    {
        string template = GetTemplate(templateName);
        PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (PropertyInfo property in properties)
        {
            string value = property.GetValue(model)?.ToString() ?? string.Empty;
            template = template.Replace($"{{{{{property.Name}}}}}", value);
        }

        return template;
    }

    private static string? LoadFromEmbeddedResource(string templateName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        string resourceName = $"Vortex.Bot.Resources.Templates.{templateName}.html";

        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
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

        string templatePath = Path.Combine(TemplatesBasePath, $"{templateName}.html");
        if (File.Exists(templatePath)) return true;

        var assembly = Assembly.GetExecutingAssembly();
        string resourceName = $"Vortex.Bot.Resources.Templates.{templateName}.html";
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
