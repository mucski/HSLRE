using System.Xml.Serialization;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "tonemapping")]
	public class Tonemapping
	{
		[XmlAttribute(AttributeName = "enabled")]
		public string Enabled { get; set; }
		[XmlAttribute(AttributeName = "tonemapper")]
		public string Tonemapper { get; set; }
		[XmlAttribute(AttributeName = "exposure")]
		public string Exposure { get; set; }
		[XmlAttribute(AttributeName = "index")]
		public string Index { get; set; }
	}
}