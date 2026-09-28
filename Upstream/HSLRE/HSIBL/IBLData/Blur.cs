using System.Xml.Serialization;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "blur")]
	public class Blur
	{
		[XmlAttribute(AttributeName = "enabled")]
		public string Enabled { get; set; }
		[XmlAttribute(AttributeName = "downsample")]
		public string Downsample { get; set; }
		[XmlAttribute(AttributeName = "blurSize")]
		public string BlurSize { get; set; }
		[XmlAttribute(AttributeName = "blurIterations")]
		public string BlurIterations { get; set; }
		[XmlAttribute(AttributeName = "blurType")]
		public string BlurType { get; set; }
		[XmlAttribute(AttributeName = "fixUpscaledScreenshots")]
		public string FixUpscaledScreenshots { get; set; }
		[XmlAttribute(AttributeName = "index")]
		public string Index { get; set; }
	}
}