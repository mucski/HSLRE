using System.Xml.Serialization;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "ssao")]
	public class Ssao
	{
		[XmlAttribute(AttributeName = "enabled")]
		public string Enabled { get; set; }
		[XmlAttribute(AttributeName = "samples")]
		public string Samples { get; set; }
		[XmlAttribute(AttributeName = "downsampling")]
		public string Downsampling { get; set; }
		[XmlAttribute(AttributeName = "radius")]
		public string Radius { get; set; }
		[XmlAttribute(AttributeName = "intensity")]
		public string Intensity { get; set; }
		[XmlAttribute(AttributeName = "distance")]
		public string Distance { get; set; }
		[XmlAttribute(AttributeName = "bias")]
		public string Bias { get; set; }
		[XmlAttribute(AttributeName = "lumContribution")]
		public string LumContribution { get; set; }
		[XmlAttribute(AttributeName = "occlusionColorR")]
		public string OcclusionColorR { get; set; }
		[XmlAttribute(AttributeName = "occlusionColorG")]
		public string OcclusionColorG { get; set; }
		[XmlAttribute(AttributeName = "occlusionColorB")]
		public string OcclusionColorB { get; set; }
		[XmlAttribute(AttributeName = "cutoffDistance")]
		public string CutoffDistance { get; set; }
		[XmlAttribute(AttributeName = "cutoffFalloff")]
		public string CutoffFalloff { get; set; }
		[XmlAttribute(AttributeName = "blurPasses")]
		public string BlurPasses { get; set; }
		[XmlAttribute(AttributeName = "blurBilateralThreshold")]
		public string BlurBilateralThreshold { get; set; }
		[XmlAttribute(AttributeName = "useHighPrecisionDepthMap")]
		public string UseHighPrecisionDepthMap { get; set; }
		[XmlAttribute(AttributeName = "blur")]
		public string Blur { get; set; }
		[XmlAttribute(AttributeName = "blurDownsampling")]
		public string BlurDownsampling { get; set; }
		[XmlAttribute(AttributeName = "debugAO")]
		public string DebugAO { get; set; }
	}
}