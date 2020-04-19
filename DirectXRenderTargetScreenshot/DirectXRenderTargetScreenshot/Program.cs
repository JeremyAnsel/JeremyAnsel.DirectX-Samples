using JeremyAnsel.DirectX.GameWindow;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DirectXRenderTargetScreenshot
{
    class Program
    {
        const uint ScreenshotImageWidth = 320;
        const uint ScreenshotImageHeight = 180;
        const string ExePath = @"bin\Release\netcoreapp3.1";

        static void Main(string[] args)
        {
            Console.WriteLine("DirectXRenderTargetScreenshot");

            string exeDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string rootDirectory = Path.GetFullPath(Path.Combine(exeDirectory, @"..\..\..\..\.."));
            Console.WriteLine(rootDirectory);

            string imagesDirectory = Path.Combine(rootDirectory, "Images");
            Directory.CreateDirectory(imagesDirectory);

            var samples = new List<Sample>();

            TakeScreenshotRepository(samples, rootDirectory, imagesDirectory);

            Directory.SetCurrentDirectory(rootDirectory);
            WriteSamplesList(samples);
        }

        static void TakeScreenshotRepository(List<Sample> samples, string repository, string imagesDirectory)
        {
            foreach (string categoryDirectory in Directory.EnumerateDirectories(repository))
            {
                TakeScreenshotCategory(samples, repository, Path.GetFileName(categoryDirectory), imagesDirectory);
            }
        }

        static void TakeScreenshotCategory(List<Sample> samples, string repository, string category, string imagesDirectory)
        {
            string categoryDirectory = Path.Combine(repository, category);

            foreach (string nameDirectory in Directory.EnumerateDirectories(categoryDirectory))
            {
                string name = Path.GetFileName(nameDirectory);

                TakeScreenshotSample(samples, repository, category, name, imagesDirectory);
            }
        }

        static void TakeScreenshotSample(List<Sample> samples, string repository, string category, string name, string imagesDirectory)
        {
            string exeFileName = Path.Combine(repository, category, name, ExePath, name + ".dll");

            if (!File.Exists(exeFileName))
            {
                return;
            }

            var assembly = Assembly.LoadFrom(exeFileName);
            var type = assembly.GetType(name + ".MainGameComponent");

            if (type == null)
            {
                return;
            }

            Console.WriteLine(category + "-" + name);
            Directory.SetCurrentDirectory(Path.GetDirectoryName(exeFileName));

            var mainGameComponent = (IGameComponent)Activator.CreateInstance(type);

            samples.Add(new Sample(mainGameComponent.MinimalFeatureLevel, repository, category, name));

            var deviceResourcesOptions = new DeviceResourcesOptions
            {
                ForceWarp = true,
                UseHighestFeatureLevel = false
            };

            var deviceResources = new RenderTargetDeviceResources(
                ScreenshotImageWidth,
                ScreenshotImageHeight,
                mainGameComponent.MinimalFeatureLevel,
                deviceResourcesOptions);

            mainGameComponent.CreateDeviceDependentResources(deviceResources);
            mainGameComponent.CreateWindowSizeDependentResources();
            mainGameComponent.Update(null);
            mainGameComponent.Render();

            deviceResources.SaveBackBuffer(Path.Combine(imagesDirectory, category + "-" + name + ".jpg"));

            mainGameComponent.ReleaseWindowSizeDependentResources();
            mainGameComponent.ReleaseDeviceDependentResources();
            deviceResources.Release();
        }

        static void WriteSamplesList(List<Sample> samples)
        {
            Console.WriteLine("WriteSamplesList");
            const string indexFilename = @"README.md";
            const string screenshotsSection = @"# Screenshots";

            var lines = new List<string>();
            lines.AddRange(File.ReadAllLines(indexFilename).TakeWhile(t => !t.StartsWith(screenshotsSection, StringComparison.OrdinalIgnoreCase)));
            lines.Add(screenshotsSection);

            foreach (var sample in samples)
            {
                //lines.Add(string.Format(CultureInfo.InvariantCulture, "<p style=\"float:left; width:{0}px; height:{1}px; margin:5px\">", ScreenshotImageWidth, ScreenshotImageHeight * 5 / 3));
                //lines.Add(sample.Category + " - " + sample.Title + "<br />");
                //lines.Add(string.Format(CultureInfo.InvariantCulture, "<img src=\"Images/{0}\" /><br />", sample.Category + "-" + sample.Name + ".jpg"));
                //lines.Add(sample.Description + "<br />");
                //lines.Add("</p>");

                lines.Add($"<img align=left src=\"Images/{sample.Category}-{sample.Name}.jpg\" />");
                lines.Add($"{sample.Category} - {sample.Title}<br />");
                lines.Add($"{sample.MinimalFeatureLevel}<br />");
                lines.Add($"{sample.Description}<br />");
                lines.Add($"<br clear=both />");
            }

            File.WriteAllLines(indexFilename, lines);
        }
    }
}
