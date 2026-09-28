using System.Xml.Serialization;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "depthOfField")]
	public class DepthOfField
	{
		[XmlAttribute(AttributeName = "enabled")]
		public string Enabled { get; set; }
		[XmlAttribute(AttributeName = "visualizeFocus")]
		public string VisualizeFocus { get; set; }
		[XmlAttribute(AttributeName = "focusType")]
		public string FocusType { get; set; }
		[XmlAttribute(AttributeName = "focalLength")]
		public string FocalLength { get; set; }
		[XmlAttribute(AttributeName = "focalSize")]
		public string FocalSize { get; set; }
		[XmlAttribute(AttributeName = "aperture")]
		public string Aperture { get; set; }
		[XmlAttribute(AttributeName = "maxBlurSize")]
		public string MaxBlurSize { get; set; }
		[XmlAttribute(AttributeName = "highResolution")]
		public string HighResolution { get; set; }
		[XmlAttribute(AttributeName = "nearBlur")]
		public string NearBlur { get; set; }
		[XmlAttribute(AttributeName = "foregroundOverlap")]
		public string ForegroundOverlap { get; set; }
		[XmlAttribute(AttributeName = "blurType")]
		public string BlurType { get; set; }
		[XmlAttribute(AttributeName = "blurSampleCount")]
		public string BlurSampleCount { get; set; }
		[XmlAttribute(AttributeName = "dx11BokehScale")]
		public string Dx11BokehScale { get; set; }
		[XmlAttribute(AttributeName = "dx11BokehIntensity")]
		public string Dx11BokehIntensity { get; set; }
		[XmlAttribute(AttributeName = "dx11BokehThreshold")]
		public string Dx11BokehThreshold { get; set; }
		[XmlAttribute(AttributeName = "dx11SpawnHeuristic")]
		public string Dx11SpawnHeuristic { get; set; }
		[XmlAttribute(AttributeName = "fixUpscaledScreenshots")]
		public string FixUpscaledScreenshots { get; set; }
		[XmlAttribute(AttributeName = "index")]
		public string Index { get; set; }
	}
}