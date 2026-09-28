using System.Xml.Serialization;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "amplifyBloom")]
	public class AmplifyBloom
	{
		[XmlElement(ElementName = "LensFlareInstance.LensFlareGradient")]
		public LensFlareInstance_LensFlareGradient LensFlareInstance_LensFlareGradient { get; set; }
		[XmlElement(ElementName = "LensGlareInstance.CromaticColorGradient")]
		public LensGlareInstance_CromaticColorGradient LensGlareInstance_CromaticColorGradient { get; set; }
		[XmlAttribute(AttributeName = "enabled")]
		public string Enabled { get; set; }
		[XmlAttribute(AttributeName = "UpscaleQuality")]
		public string UpscaleQuality { get; set; }
		[XmlAttribute(AttributeName = "MainThresholdSize")]
		public string MainThresholdSize { get; set; }
		[XmlAttribute(AttributeName = "CurrentPrecisionMode")]
		public string CurrentPrecisionMode { get; set; }
		[XmlAttribute(AttributeName = "BloomRange")]
		public string BloomRange { get; set; }
		[XmlAttribute(AttributeName = "OverallIntensity")]
		public string OverallIntensity { get; set; }
		[XmlAttribute(AttributeName = "OverallThreshold")]
		public string OverallThreshold { get; set; }
		[XmlAttribute(AttributeName = "UpscaleBlurRadius")]
		public string UpscaleBlurRadius { get; set; }
		[XmlAttribute(AttributeName = "DebugToScreen")]
		public string DebugToScreen { get; set; }
		[XmlAttribute(AttributeName = "TemporalFilteringActive")]
		public string TemporalFilteringActive { get; set; }
		[XmlAttribute(AttributeName = "TemporalFilteringValue")]
		public string TemporalFilteringValue { get; set; }
		[XmlAttribute(AttributeName = "SeparateFeaturesThreshold")]
		public string SeparateFeaturesThreshold { get; set; }
		[XmlAttribute(AttributeName = "FeaturesThreshold")]
		public string FeaturesThreshold { get; set; }
		[XmlAttribute(AttributeName = "ApplyLensDirt")]
		public string ApplyLensDirt { get; set; }
		[XmlAttribute(AttributeName = "LensDirtStrength")]
		public string LensDirtStrength { get; set; }
		[XmlAttribute(AttributeName = "LensDirtTexture")]
		public string LensDirtTexture { get; set; }
		[XmlAttribute(AttributeName = "ApplyLensStardurst")]
		public string ApplyLensStardurst { get; set; }
		[XmlAttribute(AttributeName = "LensStarburstStrength")]
		public string LensStarburstStrength { get; set; }
		[XmlAttribute(AttributeName = "LensStardurstTex")]
		public string LensStardurstTex { get; set; }
		[XmlAttribute(AttributeName = "BokehFilterInstance.ApplyBokeh")]
		public string BokehFilterInstance_ApplyBokeh { get; set; }
		[XmlAttribute(AttributeName = "BokehFilterInstance.ApplyOnBloomSource")]
		public string BokehFilterInstance_ApplyOnBloomSource { get; set; }
		[XmlAttribute(AttributeName = "BokehFilterInstance.ApertureShape")]
		public string BokehFilterInstance_ApertureShape { get; set; }
		[XmlAttribute(AttributeName = "BokehFilterInstance.OffsetRotation")]
		public string BokehFilterInstance_OffsetRotation { get; set; }
		[XmlAttribute(AttributeName = "BokehFilterInstance.BokehSampleRadius")]
		public string BokehFilterInstance_BokehSampleRadius { get; set; }
		[XmlAttribute(AttributeName = "BokehFilterInstance.Aperture")]
		public string BokehFilterInstance_Aperture { get; set; }
		[XmlAttribute(AttributeName = "BokehFilterInstance.FocalLength")]
		public string BokehFilterInstance_FocalLength { get; set; }
		[XmlAttribute(AttributeName = "BokehFilterInstance.FocalDistance")]
		public string BokehFilterInstance_FocalDistance { get; set; }
		[XmlAttribute(AttributeName = "BokehFilterInstance.MaxCoCDiameter")]
		public string BokehFilterInstance_MaxCoCDiameter { get; set; }
		[XmlAttribute(AttributeName = "LensFlareInstance.ApplyLensFlare")]
		public string LensFlareInstance_ApplyLensFlare { get; set; }
		[XmlAttribute(AttributeName = "LensFlareInstance.OverallIntensity")]
		public string LensFlareInstance_OverallIntensity { get; set; }
		[XmlAttribute(AttributeName = "LensFlareInstance.LensFlareGaussianBlurAmount")]
		public string LensFlareInstance_LensFlareGaussianBlurAmount { get; set; }
		[XmlAttribute(AttributeName = "LensFlareInstance.LensFlareNormalizedGhostsIntensity")]
		public string LensFlareInstance_LensFlareNormalizedGhostsIntensity { get; set; }
		[XmlAttribute(AttributeName = "LensFlareInstance.LensFlareGhostAmount")]
		public string LensFlareInstance_LensFlareGhostAmount { get; set; }
		[XmlAttribute(AttributeName = "LensFlareInstance.LensFlareGhostsDispersal")]
		public string LensFlareInstance_LensFlareGhostsDispersal { get; set; }
		[XmlAttribute(AttributeName = "LensFlareInstance.LensFlareGhostChrDistortion")]
		public string LensFlareInstance_LensFlareGhostChrDistortion { get; set; }
		[XmlAttribute(AttributeName = "LensFlareInstance.LensFlareGhostsPowerFactor")]
		public string LensFlareInstance_LensFlareGhostsPowerFactor { get; set; }
		[XmlAttribute(AttributeName = "LensFlareInstance.LensFlareGhostsPowerFalloff")]
		public string LensFlareInstance_LensFlareGhostsPowerFalloff { get; set; }
		[XmlAttribute(AttributeName = "LensFlareInstance.LensFlareNormalizedHaloIntensity")]
		public string LensFlareInstance_LensFlareNormalizedHaloIntensity { get; set; }
		[XmlAttribute(AttributeName = "LensFlareInstance.LensFlareHaloWidth")]
		public string LensFlareInstance_LensFlareHaloWidth { get; set; }
		[XmlAttribute(AttributeName = "LensFlareInstance.LensFlareHaloChrDistortion")]
		public string LensFlareInstance_LensFlareHaloChrDistortion { get; set; }
		[XmlAttribute(AttributeName = "LensFlareInstance.LensFlareHaloPowerFactor")]
		public string LensFlareInstance_LensFlareHaloPowerFactor { get; set; }
		[XmlAttribute(AttributeName = "LensFlareInstance.LensFlareHaloPowerFalloff")]
		public string LensFlareInstance_LensFlareHaloPowerFalloff { get; set; }
		[XmlAttribute(AttributeName = "LensGlareInstance.ApplyLensGlare")]
		public string LensGlareInstance_ApplyLensGlare { get; set; }
		[XmlAttribute(AttributeName = "LensGlareInstance.Intensity")]
		public string LensGlareInstance_Intensity { get; set; }
		[XmlAttribute(AttributeName = "LensGlareInstance.OverallStreakScale")]
		public string LensGlareInstance_OverallStreakScale { get; set; }
		[XmlAttribute(AttributeName = "LensGlareInstance.OverallTintR")]
		public string LensGlareInstance_OverallTintR { get; set; }
		[XmlAttribute(AttributeName = "LensGlareInstance.OverallTintG")]
		public string LensGlareInstance_OverallTintG { get; set; }
		[XmlAttribute(AttributeName = "LensGlareInstance.OverallTintB")]
		public string LensGlareInstance_OverallTintB { get; set; }
		[XmlAttribute(AttributeName = "LensGlareInstance.OverallTintA")]
		public string LensGlareInstance_OverallTintA { get; set; }
		[XmlAttribute(AttributeName = "LensGlareInstance.CurrentGlare")]
		public string LensGlareInstance_CurrentGlare { get; set; }
		[XmlAttribute(AttributeName = "LensGlareInstance.PerPassDisplacement")]
		public string LensGlareInstance_PerPassDisplacement { get; set; }
		[XmlAttribute(AttributeName = "fixUpscaledScreenshots")]
		public string FixUpscaledScreenshots { get; set; }
		[XmlAttribute(AttributeName = "LensGlareInstance.GlareMaxPassCount")]
		public string LensGlareInstance_GlareMaxPassCount { get; set; }
		[XmlAttribute(AttributeName = "index")]
		public string Index { get; set; }
	}
}