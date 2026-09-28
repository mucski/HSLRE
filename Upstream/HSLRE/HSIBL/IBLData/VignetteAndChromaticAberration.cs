using System.Xml.Serialization;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "vignetteAndChromaticAberration")]
	public class VignetteAndChromaticAberration
	{
		[XmlAttribute(AttributeName = "enabled")]
		public string Enabled { get; set; }
		[XmlAttribute(AttributeName = "intensity")]
		public string Intensity { get; set; }
		[XmlAttribute(AttributeName = "blur")]
		public string Blur { get; set; }
		[XmlAttribute(AttributeName = "blurSpread")]
		public string BlurSpread { get; set; }
		[XmlAttribute(AttributeName = "mode")]
		public string Mode { get; set; }
		[XmlAttribute(AttributeName = "chromaticAberration")]
		public string ChromaticAberration { get; set; }
		[XmlAttribute(AttributeName = "axialAberration")]
		public string AxialAberration { get; set; }
		[XmlAttribute(AttributeName = "luminanceDependency")]
		public string LuminanceDependency { get; set; }
		[XmlAttribute(AttributeName = "blurDistance")]
		public string BlurDistance { get; set; }
		[XmlAttribute(AttributeName = "index")]
		public string Index { get; set; }
	}
}