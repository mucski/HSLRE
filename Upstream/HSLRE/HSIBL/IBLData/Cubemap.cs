using System.Xml.Serialization;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "cubemap")]
	public class Cubemap
	{
		[XmlAttribute(AttributeName = "hide")]
		public string Hide { get; set; }
		[XmlAttribute(AttributeName = "index")]
		public string Index { get; set; }
		[XmlAttribute(AttributeName = "fileName")]
		public string FileName { get; set; }
	}
}