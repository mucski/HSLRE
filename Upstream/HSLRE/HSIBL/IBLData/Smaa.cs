using System.Xml.Serialization;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "smaa")]
	public class Smaa
	{
		[XmlAttribute(AttributeName = "enabled")]
		public string Enabled { get; set; }
		[XmlAttribute(AttributeName = "debugPass")]
		public string DebugPass { get; set; }
		[XmlAttribute(AttributeName = "quality")]
		public string Quality { get; set; }
		[XmlAttribute(AttributeName = "edgeDetectionMethod")]
		public string EdgeDetectionMethod { get; set; }
		[XmlAttribute(AttributeName = "qualityDiagonalDetection")]
		public string QualityDiagonalDetection { get; set; }
		[XmlAttribute(AttributeName = "qualityCornerDetection")]
		public string QualityCornerDetection { get; set; }
		[XmlAttribute(AttributeName = "qualityThreshold")]
		public string QualityThreshold { get; set; }
		[XmlAttribute(AttributeName = "qualityDepthThreshold")]
		public string QualityDepthThreshold { get; set; }
		[XmlAttribute(AttributeName = "qualityMaxSearchSteps")]
		public string QualityMaxSearchSteps { get; set; }
		[XmlAttribute(AttributeName = "qualityMaxDiagonalSearchSteps")]
		public string QualityMaxDiagonalSearchSteps { get; set; }
		[XmlAttribute(AttributeName = "qualityCornerRounding")]
		public string QualityCornerRounding { get; set; }
		[XmlAttribute(AttributeName = "qualityLocalContrastAdaptationFactor")]
		public string QualityLocalContrastAdaptationFactor { get; set; }
		[XmlAttribute(AttributeName = "predicationEnabled")]
		public string PredicationEnabled { get; set; }
		[XmlAttribute(AttributeName = "predicationThreshold")]
		public string PredicationThreshold { get; set; }
		[XmlAttribute(AttributeName = "predicationScale")]
		public string PredicationScale { get; set; }
		[XmlAttribute(AttributeName = "predicationStrength")]
		public string PredicationStrength { get; set; }
		[XmlAttribute(AttributeName = "temporalEnabled")]
		public string TemporalEnabled { get; set; }
		[XmlAttribute(AttributeName = "temporalFuzzSize")]
		public string TemporalFuzzSize { get; set; }
		[XmlAttribute(AttributeName = "index")]
		public string Index { get; set; }
	}
}