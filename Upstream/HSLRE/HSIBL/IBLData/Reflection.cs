using System.Xml.Serialization;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "reflection")]
	public class Reflection
	{
		[XmlAttribute(AttributeName = "enabled")]
		public string Enabled { get; set; }
		[XmlAttribute(AttributeName = "refreshOnDemand")]
		public string RefreshOnDemand { get; set; }
		[XmlAttribute(AttributeName = "timeSlicing")]
		public string TimeSlicing { get; set; }
		[XmlAttribute(AttributeName = "resolution")]
		public string Resolution { get; set; }
		[XmlAttribute(AttributeName = "intensity")]
		public string Intensity { get; set; }
		[XmlAttribute(AttributeName = "shadowDistance")]
		public string ShadowDistance { get; set; }
		[XmlAttribute(AttributeName = "nearClipPlane")]
		public string NearClipPlane { get; set; }
		[XmlAttribute(AttributeName = "positionX")]
		public string PositionX { get; set; }
		[XmlAttribute(AttributeName = "positionY")]
		public string PositionY { get; set; }
		[XmlAttribute(AttributeName = "positionZ")]
		public string PositionZ { get; set; }
		[XmlAttribute(AttributeName = "bounce")]
		public string Bounce { get; set; }
		[XmlAttribute(AttributeName = "boxProjection")]
		public string BoxProjection { get; set; }
		[XmlAttribute(AttributeName = "cullingMask")]
		public string CullingMask { get; set; }
	}
}