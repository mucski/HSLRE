using System.Xml.Serialization;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "volumetricLights")]
	public class VolumetricLights
	{
		[XmlElement(ElementName = "light")]
		public Light Light { get; set; }
		[XmlAttribute(AttributeName = "enabled")]
		public string Enabled { get; set; }
		[XmlAttribute(AttributeName = "resolution")]
		public string Resolution { get; set; }
	}
}