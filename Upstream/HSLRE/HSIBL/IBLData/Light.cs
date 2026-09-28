using System.Xml.Serialization;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "light")]
	public class Light
	{
		[XmlAttribute(AttributeName = "index")]
		public string Index { get; set; }
		[XmlAttribute(AttributeName = "enabled")]
		public string Enabled { get; set; }
		[XmlAttribute(AttributeName = "sampleCount")]
		public string SampleCount { get; set; }
		[XmlAttribute(AttributeName = "scatteringCoef")]
		public string ScatteringCoef { get; set; }
		[XmlAttribute(AttributeName = "extinctionCoef")]
		public string ExtinctionCoef { get; set; }
		[XmlAttribute(AttributeName = "mieG")]
		public string MieG { get; set; }
		[XmlAttribute(AttributeName = "skyboxExtinctionCoef")]
		public string SkyboxExtinctionCoef { get; set; }
		[XmlAttribute(AttributeName = "maxRayLength")]
		public string MaxRayLength { get; set; }
		[XmlAttribute(AttributeName = "heightFog")]
		public string HeightFog { get; set; }
		[XmlAttribute(AttributeName = "heightScale")]
		public string HeightScale { get; set; }
		[XmlAttribute(AttributeName = "groundLevel")]
		public string GroundLevel { get; set; }
		[XmlAttribute(AttributeName = "noise")]
		public string Noise { get; set; }
		[XmlAttribute(AttributeName = "noiseScale")]
		public string NoiseScale { get; set; }
		[XmlAttribute(AttributeName = "noiseIntensity")]
		public string NoiseIntensity { get; set; }
		[XmlAttribute(AttributeName = "noiseIntensityOffset")]
		public string NoiseIntensityOffset { get; set; }
		[XmlAttribute(AttributeName = "noiseVelocityX")]
		public string NoiseVelocityX { get; set; }
		[XmlAttribute(AttributeName = "noiseVelocityY")]
		public string NoiseVelocityY { get; set; }
	}
}