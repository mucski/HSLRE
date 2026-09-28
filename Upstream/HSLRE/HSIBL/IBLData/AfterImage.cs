using System.Xml.Serialization;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "afterImage")]
	public class AfterImage
	{
		[XmlAttribute(AttributeName = "enabled")]
		public string Enabled { get; set; }
		[XmlAttribute(AttributeName = "framesToMix")]
		public string FramesToMix { get; set; }
		[XmlAttribute(AttributeName = "falloffPower")]
		public string FalloffPower { get; set; }
		[XmlAttribute(AttributeName = "index")]
		public string Index { get; set; }
	}
}