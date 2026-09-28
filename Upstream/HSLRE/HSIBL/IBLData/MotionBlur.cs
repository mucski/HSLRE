using System.Xml.Serialization;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "motionBlur")]
	public class MotionBlur
	{
		[XmlAttribute(AttributeName = "enabled")]
		public string Enabled { get; set; }
		[XmlAttribute(AttributeName = "velocityScale")]
		public string VelocityScale { get; set; }
		[XmlAttribute(AttributeName = "maxVelocity")]
		public string MaxVelocity { get; set; }
		[XmlAttribute(AttributeName = "minVelocity")]
		public string MinVelocity { get; set; }
		[XmlAttribute(AttributeName = "filterType")]
		public string FilterType { get; set; }
		[XmlAttribute(AttributeName = "rotationScale")]
		public string RotationScale { get; set; }
		[XmlAttribute(AttributeName = "movementScale")]
		public string MovementScale { get; set; }
		[XmlAttribute(AttributeName = "velocityDownsample")]
		public string VelocityDownsample { get; set; }
		[XmlAttribute(AttributeName = "jitter")]
		public string Jitter { get; set; }
		[XmlAttribute(AttributeName = "preview")]
		public string Preview { get; set; }
		[XmlAttribute(AttributeName = "previewScaleX")]
		public string PreviewScaleX { get; set; }
		[XmlAttribute(AttributeName = "previewScaleY")]
		public string PreviewScaleY { get; set; }
		[XmlAttribute(AttributeName = "previewScaleZ")]
		public string PreviewScaleZ { get; set; }
		[XmlAttribute(AttributeName = "index")]
		public string Index { get; set; }
	}
}