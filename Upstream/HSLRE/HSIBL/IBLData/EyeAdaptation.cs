using System.Xml.Serialization;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "eyeAdaptation")]
	public class EyeAdaptation
	{
		[XmlAttribute(AttributeName = "enabled")]
		public string Enabled { get; set; }
		[XmlAttribute(AttributeName = "middleGrey")]
		public string MiddleGrey { get; set; }
		[XmlAttribute(AttributeName = "min")]
		public string Min { get; set; }
		[XmlAttribute(AttributeName = "max")]
		public string Max { get; set; }
		[XmlAttribute(AttributeName = "speed")]
		public string Speed { get; set; }
	}
}