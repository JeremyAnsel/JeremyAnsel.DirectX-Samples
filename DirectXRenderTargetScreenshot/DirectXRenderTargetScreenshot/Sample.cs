using JeremyAnsel.DirectX.D3D11;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DirectXRenderTargetScreenshot
{
    class Sample
    {
        public Sample(D3D11FeatureLevel minimalFeatureLevel, string repository, string category, string name)
        {
            this.MinimalFeatureLevel = minimalFeatureLevel;
            this.Repository = repository;
            this.Category = category;
            this.Name = name;

            this.Title = this.Name;
            this.Description = "Description of " + this.Name + ".";

            string directory = this.SampleDirectory;
            string readmePath = Path.Combine(directory, "readme.txt");

            if (File.Exists(readmePath))
            {
                string[] lines = File.ReadAllLines(readmePath);

                if (lines.Length >= 3)
                {
                    this.Title = lines[0];
                    this.Description = lines[2];
                }
            }
        }

        public D3D11FeatureLevel MinimalFeatureLevel { get; }

        public string Repository { get; }

        public string Category { get; }

        public string Name { get; }

        public string Title { get; }

        public string Description { get; }

        public string SampleDirectory
        {
            get
            {
                return Path.Combine(this.Repository, this.Category, this.Name);
            }
        }
    }
}
