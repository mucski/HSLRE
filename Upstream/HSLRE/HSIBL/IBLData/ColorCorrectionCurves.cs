using System.Xml.Serialization;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "colorCorrectionCurves")]
	public class ColorCorrectionCurves
	{
		[XmlAttribute(AttributeName = "enabled")]
		public string Enabled { get; set; }
		[XmlAttribute(AttributeName = "saturation")]
		public string Saturation { get; set; }
		[XmlAttribute(AttributeName = "index")]
		public string Index { get; set; }
	}
}