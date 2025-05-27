using System.Collections.Generic;
using Assets.Json_Files;

namespace Assets
{
    public class JulangEnvironment
    {
        public string Type { get; set; }
        public Dimension Dimensions { get; set; }
        public List<LightSource> Lighting { get; set; }
        public string Background { get; set; }

        public JulangEnvironment(Environment2 environment)
        {
            this.Type = environment.Type;
            this.Dimensions = new Dimension(environment.Dimensions);
            this.Lighting = new List<LightSource>();
            foreach (LightSource2 lightSource2 in environment.Lighting)
            {
                this.Lighting.Add(new LightSource(lightSource2));
            }
            this.Background = environment.Background;
        }
    }

    public class Dimension
    {
        public float Width { get; set; }
        public float Height { get; set; }
        public float Depth { get; set; }

        public Dimension(Dimensions3D2 dimensions)
        {
            this.Width = dimensions.Width;
            this.Height = dimensions.Height;
            this.Depth = dimensions.Depth;
        }
    }

    public class LightSource
    {
        public string LightType { get; set; }
        public float? Intensity { get; set; }
        public string Color { get; set; }
        public float? Range { get; set; }
        public float? SpotAngle { get; set; }

        public LightSource(LightSource2 lightSource)
        {
            this.LightType = lightSource.LightType;
            this.Intensity = lightSource.Intensity;
            this.Color = lightSource.Color;
            this.Range = lightSource.Range;
            this.SpotAngle = lightSource.SpotAngle;
        }
    }
}