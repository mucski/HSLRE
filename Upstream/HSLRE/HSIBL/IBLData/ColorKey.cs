using System.Xml.Serialization;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "colorKey")]
	public class ColorKey
	{
		[XmlAttribute(AttributeName = "r")]
		public string R { get; set; }
		[XmlAttribute(AttributeName = "g")]
		public string G { get; set; }
		[XmlAttribute(AttributeName = "b")]
		public string B { get; set; }
		[XmlAttribute(AttributeName = "time")]
		public string Time { get; set; }
	}
}