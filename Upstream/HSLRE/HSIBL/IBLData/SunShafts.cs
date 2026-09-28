using System.Xml.Serialization;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "sunShafts")]
	public class SunShafts
	{
		[XmlAttribute(AttributeName = "enabled")]
		public string Enabled { get; set; }
		[XmlAttribute(AttributeName = "useDepthTexture")]
		public string UseDepthTexture { get; set; }
		[XmlAttribute(AttributeName = "resolution")]
		public string Resolution { get; set; }
		[XmlAttribute(AttributeName = "screenBlendMode")]
		public string ScreenBlendMode { get; set; }
		[XmlAttribute(AttributeName = "sunThresholdR")]
		public string SunThresholdR { get; set; }
		[XmlAttribute(AttributeName = "sunThresholdG")]
		public string SunThresholdG { get; set; }
		[XmlAttribute(AttributeName = "sunThresholdB")]
		public string SunThresholdB { get; set; }
		[XmlAttribute(AttributeName = "sunColorR")]
		public string SunColorR { get; set; }
		[XmlAttribute(AttributeName = "sunColorG")]
		public string SunColorG { get; set; }
		[XmlAttribute(AttributeName = "sunColorB")]
		public string SunColorB { get; set; }
		[XmlAttribute(AttributeName = "maxRadius")]
		public string MaxRadius { get; set; }
		[XmlAttribute(AttributeName = "sunShaftBlurRadius")]
		public string SunShaftBlurRadius { get; set; }
		[XmlAttribute(AttributeName = "radialBlurIterations")]
		public string RadialBlurIterations { get; set; }
		[XmlAttribute(AttributeName = "sunShaftIntensity")]
		public string SunShaftIntensity { get; set; }
		[XmlAttribute(AttributeName = "index")]
		public string Index { get; set; }
	}
}