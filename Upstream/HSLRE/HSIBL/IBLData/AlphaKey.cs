using System.Xml.Serialization;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "alphaKey")]
	public class AlphaKey
	{
		[XmlAttribute(AttributeName = "alpha")]
		public string Alpha { get; set; }
		[XmlAttribute(AttributeName = "time")]
		public string Time { get; set; }
	}
}