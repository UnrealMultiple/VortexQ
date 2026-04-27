using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Text;
using Vortex.Bot;
using Vortex.Bot.Extension;

class Program
{
    static async Task Main(string[] args)
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;
        ShowApplicationInfo();
        CheckConfigurationFile();
        await Host.CreateApplicationBuilder(args)
                .ConfigureConfiguration(configuration => configuration
                    .AddJsonFile(Path.GetFullPath(Constants.ConfigFileName), false, true)
                )
                .ConfigureCore()
                .Build()
                .RunAsync();
    }
    static void CheckConfigurationFile()
    {
        if (!File.Exists(Constants.ConfigFileName))
        {
            {
                Console.WriteLine($"{Constants.ConfigFileName} not found. Generating...");
                using var input = typeof(Program).Assembly.GetManifestResourceStream(Constants.ConfigResourceName) ?? throw new Exception("Default configuration file not found");
                using var output = File.OpenWrite(Constants.ConfigFileName);
                input.CopyTo(output);
            }

            Console.WriteLine("Please edit the configuration file");
            Console.WriteLine("and press any key to continue starting the application.");
            Console.ReadKey();
        }
    }
    static void ShowApplicationInfo()
    {
        Console.WriteLine(Constants.Banner);

        Console.WriteLine($"Version: {Constants.ImplementationVersion}");
    }
}