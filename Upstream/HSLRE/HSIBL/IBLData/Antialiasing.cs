using System.Xml.Serialization;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "antialiasing")]
	public class Antialiasing
	{
		[XmlAttribute(AttributeName = "enabled")]
		public string Enabled { get; set; }
		[XmlAttribute(AttributeName = "mode")]
		public string Mode { get; set; }
		[XmlAttribute(AttributeName = "edgeThresholdMin")]
		public string EdgeThresholdMin { get; set; }
		[XmlAttribute(AttributeName = "edgeThreshold")]
		public string EdgeThreshold { get; set; }
		[XmlAttribute(AttributeName = "edgeSharpness")]
		public string EdgeSharpness { get; set; }
		[XmlAttribute(AttributeName = "offsetScale")]
		public string OffsetScale { get; set; }
		[XmlAttribute(AttributeName = "blurRadius")]
		public string BlurRadius { get; set; }
		[XmlAttribute(AttributeName = "showGeneratedNormals")]
		public string ShowGeneratedNormals { get; set; }
		[XmlAttribute(AttributeName = "dlaaSharp")]
		public string DlaaSharp { get; set; }
		[XmlAttribute(AttributeName = "index")]
		public string Index { get; set; }
	}
}