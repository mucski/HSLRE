using System.Xml.Serialization;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "colorGrading")]
	public class ColorGrading
	{
		[XmlAttribute(AttributeName = "enabled")]
		public string Enabled { get; set; }
		[XmlAttribute(AttributeName = "temperatureShift")]
		public string TemperatureShift { get; set; }
		[XmlAttribute(AttributeName = "tint")]
		public string Tint { get; set; }
		[XmlAttribute(AttributeName = "contrast")]
		public string Contrast { get; set; }
		[XmlAttribute(AttributeName = "hue")]
		public string Hue { get; set; }
		[XmlAttribute(AttributeName = "saturation")]
		public string Saturation { get; set; }
		[XmlAttribute(AttributeName = "value")]
		public string Value { get; set; }
		[XmlAttribute(AttributeName = "vibrance")]
		public string Vibrance { get; set; }
		[XmlAttribute(AttributeName = "gain")]
		public string Gain { get; set; }
		[XmlAttribute(AttributeName = "gamma")]
		public string Gamma { get; set; }
	}
}