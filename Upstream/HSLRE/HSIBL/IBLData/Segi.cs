using System.Xml.Serialization;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "segi")]
	public class Segi
	{
		[XmlAttribute(AttributeName = "enabled")]
		public string Enabled { get; set; }
		[XmlAttribute(AttributeName = "voxelResolution")]
		public string VoxelResolution { get; set; }
		[XmlAttribute(AttributeName = "voxelAA")]
		public string VoxelAA { get; set; }
		[XmlAttribute(AttributeName = "innerOcclusionLayers")]
		public string InnerOcclusionLayers { get; set; }
		[XmlAttribute(AttributeName = "gaussianMipFilter")]
		public string GaussianMipFilter { get; set; }
		[XmlAttribute(AttributeName = "voxelSpaceSize")]
		public string VoxelSpaceSize { get; set; }
		[XmlAttribute(AttributeName = "shadowSpaceSize")]
		public string ShadowSpaceSize { get; set; }
		[XmlAttribute(AttributeName = "updateGI")]
		public string UpdateGI { get; set; }
		[XmlAttribute(AttributeName = "infiniteBounces")]
		public string InfiniteBounces { get; set; }
		[XmlAttribute(AttributeName = "softSunlight")]
		public string SoftSunlight { get; set; }
		[XmlAttribute(AttributeName = "hsStandardCustomShadersCompatibility")]
		public string HsStandardCustomShadersCompatibility { get; set; }
		[XmlAttribute(AttributeName = "skyColorR")]
		public string SkyColorR { get; set; }
		[XmlAttribute(AttributeName = "skyColorG")]
		public string SkyColorG { get; set; }
		[XmlAttribute(AttributeName = "skyColorB")]
		public string SkyColorB { get; set; }
		[XmlAttribute(AttributeName = "skyColorA")]
		public string SkyColorA { get; set; }
		[XmlAttribute(AttributeName = "skyIntensity")]
		public string SkyIntensity { get; set; }
		[XmlAttribute(AttributeName = "sphericalSkylight")]
		public string SphericalSkylight { get; set; }
		[XmlAttribute(AttributeName = "temporalBlendWeight")]
		public string TemporalBlendWeight { get; set; }
		[XmlAttribute(AttributeName = "useBilateralFiltering")]
		public string UseBilateralFiltering { get; set; }
		[XmlAttribute(AttributeName = "halfResolution")]
		public string HalfResolution { get; set; }
		[XmlAttribute(AttributeName = "stochasticSampling")]
		public string StochasticSampling { get; set; }
		[XmlAttribute(AttributeName = "cones")]
		public string Cones { get; set; }
		[XmlAttribute(AttributeName = "coneTraceSteps")]
		public string ConeTraceSteps { get; set; }
		[XmlAttribute(AttributeName = "coneLength")]
		public string ConeLength { get; set; }
		[XmlAttribute(AttributeName = "coneWidth")]
		public string ConeWidth { get; set; }
		[XmlAttribute(AttributeName = "coneTraceBias")]
		public string ConeTraceBias { get; set; }
		[XmlAttribute(AttributeName = "occlusionStrength")]
		public string OcclusionStrength { get; set; }
		[XmlAttribute(AttributeName = "nearOcclusionStrength")]
		public string NearOcclusionStrength { get; set; }
		[XmlAttribute(AttributeName = "farOcclusionStrength")]
		public string FarOcclusionStrength { get; set; }
		[XmlAttribute(AttributeName = "farthestOcclusionStrength")]
		public string FarthestOcclusionStrength { get; set; }
		[XmlAttribute(AttributeName = "occlusionPower")]
		public string OcclusionPower { get; set; }
		[XmlAttribute(AttributeName = "nearLightGain")]
		public string NearLightGain { get; set; }
		[XmlAttribute(AttributeName = "giGain")]
		public string GiGain { get; set; }
		[XmlAttribute(AttributeName = "secondaryBounceGain")]
		public string SecondaryBounceGain { get; set; }
		[XmlAttribute(AttributeName = "secondaryCones")]
		public string SecondaryCones { get; set; }
		[XmlAttribute(AttributeName = "secondaryOcclusionStrength")]
		public string SecondaryOcclusionStrength { get; set; }
		[XmlAttribute(AttributeName = "doReflections")]
		public string DoReflections { get; set; }
		[XmlAttribute(AttributeName = "reflectionSteps")]
		public string ReflectionSteps { get; set; }
		[XmlAttribute(AttributeName = "reflectionOcclusionPower")]
		public string ReflectionOcclusionPower { get; set; }
		[XmlAttribute(AttributeName = "skyReflectionIntensity")]
		public string SkyReflectionIntensity { get; set; }
		[XmlAttribute(AttributeName = "visualizeSunDepthTexture")]
		public string VisualizeSunDepthTexture { get; set; }
		[XmlAttribute(AttributeName = "visualizeGI")]
		public string VisualizeGI { get; set; }
		[XmlAttribute(AttributeName = "visualizeVoxels")]
		public string VisualizeVoxels { get; set; }
	}
}