using System.Xml.Serialization;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "defaultLight")]
	public class DefaultLight
	{
		[XmlAttribute(AttributeName = "frontAnchoredToCamera")]
		public string FrontAnchoredToCamera { get; set; }
		[XmlAttribute(AttributeName = "frontRotateX")]
		public string FrontRotateX { get; set; }
		[XmlAttribute(AttributeName = "frontRotateY")]
		public string FrontRotateY { get; set; }
		[XmlAttribute(AttributeName = "frontIntensity")]
		public string FrontIntensity { get; set; }
		[XmlAttribute(AttributeName = "frontColorR")]
		public string FrontColorR { get; set; }
		[XmlAttribute(AttributeName = "frontColorG")]
		public string FrontColorG { get; set; }
		[XmlAttribute(AttributeName = "frontColorB")]
		public string FrontColorB { get; set; }
		[XmlAttribute(AttributeName = "backAnchoredToCamera")]
		public string BackAnchoredToCamera { get; set; }
		[XmlAttribute(AttributeName = "backRotateX")]
		public string BackRotateX { get; set; }
		[XmlAttribute(AttributeName = "backRotateY")]
		public string BackRotateY { get; set; }
		[XmlAttribute(AttributeName = "backIntensity")]
		public string BackIntensity { get; set; }
		[XmlAttribute(AttributeName = "backColorR")]
		public string BackColorR { get; set; }
		[XmlAttribute(AttributeName = "backColorG")]
		public string BackColorG { get; set; }
		[XmlAttribute(AttributeName = "backColorB")]
		public string BackColorB { get; set; }
	}
}