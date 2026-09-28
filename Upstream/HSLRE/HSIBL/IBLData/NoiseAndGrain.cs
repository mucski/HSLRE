using System.Xml.Serialization;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "noiseAndGrain")]
	public class NoiseAndGrain
	{
		[XmlAttribute(AttributeName = "enabled")]
		public string Enabled { get; set; }
		[XmlAttribute(AttributeName = "dx11Grain")]
		public string Dx11Grain { get; set; }
		[XmlAttribute(AttributeName = "monochrome")]
		public string Monochrome { get; set; }
		[XmlAttribute(AttributeName = "intensityMultiplier")]
		public string IntensityMultiplier { get; set; }
		[XmlAttribute(AttributeName = "generalIntensity")]
		public string GeneralIntensity { get; set; }
		[XmlAttribute(AttributeName = "blackIntensity")]
		public string BlackIntensity { get; set; }
		[XmlAttribute(AttributeName = "whiteIntensity")]
		public string WhiteIntensity { get; set; }
		[XmlAttribute(AttributeName = "midGrey")]
		public string MidGrey { get; set; }
		[XmlAttribute(AttributeName = "intensitiesX")]
		public string IntensitiesX { get; set; }
		[XmlAttribute(AttributeName = "intensitiesY")]
		public string IntensitiesY { get; set; }
		[XmlAttribute(AttributeName = "intensitiesZ")]
		public string IntensitiesZ { get; set; }
		[XmlAttribute(AttributeName = "filterMode")]
		public string FilterMode { get; set; }
		[XmlAttribute(AttributeName = "softness")]
		public string Softness { get; set; }
		[XmlAttribute(AttributeName = "monochromeTiling")]
		public string MonochromeTiling { get; set; }
		[XmlAttribute(AttributeName = "tilingX")]
		public string TilingX { get; set; }
		[XmlAttribute(AttributeName = "tilingY")]
		public string TilingY { get; set; }
		[XmlAttribute(AttributeName = "tilingZ")]
		public string TilingZ { get; set; }
		[XmlAttribute(AttributeName = "fixUpscaledScreenshots")]
		public string FixUpscaledScreenshots { get; set; }
		[XmlAttribute(AttributeName = "index")]
		public string Index { get; set; }
	}
}