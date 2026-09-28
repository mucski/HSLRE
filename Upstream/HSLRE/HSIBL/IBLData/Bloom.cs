using System.Xml.Serialization;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "bloom")]
	public class Bloom
	{
		[XmlAttribute(AttributeName = "enabled")]
		public string Enabled { get; set; }
		[XmlAttribute(AttributeName = "intensity")]
		public string Intensity { get; set; }
		[XmlAttribute(AttributeName = "threshold")]
		public string Threshold { get; set; }
		[XmlAttribute(AttributeName = "softKnee")]
		public string SoftKnee { get; set; }
		[XmlAttribute(AttributeName = "radius")]
		public string Radius { get; set; }
		[XmlAttribute(AttributeName = "antiFlicker")]
		public string AntiFlicker { get; set; }
		[XmlAttribute(AttributeName = "index")]
		public string Index { get; set; }
	}
}