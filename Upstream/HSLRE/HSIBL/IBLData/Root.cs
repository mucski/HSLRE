using System;
using System.Xml.Serialization;

namespace HSIBL.IBLData
{

	[XmlRoot(ElementName = "root")]
	public class Root
	{
		[XmlElement(ElementName = "cubemap")]
		public Cubemap Cubemap { get; set; }
		[XmlElement(ElementName = "skybox")]
		public Skybox Skybox { get; set; }
		[XmlElement(ElementName = "reflection")]
		public Reflection Reflection { get; set; }
		[XmlElement(ElementName = "defaultLight")]
		public DefaultLight DefaultLight { get; set; }
		[XmlElement(ElementName = "smaa")]
		public Smaa Smaa { get; set; }
		[XmlElement(ElementName = "lens")]
		public Lens Lens { get; set; }
		[XmlElement(ElementName = "tonemapping")]
		public Tonemapping Tonemapping { get; set; }
		[XmlElement(ElementName = "colorGrading")]
		public ColorGrading ColorGrading { get; set; }
		[XmlElement(ElementName = "eyeAdaptation")]
		public EyeAdaptation EyeAdaptation { get; set; }
		[XmlElement(ElementName = "bloom")]
		public Bloom Bloom { get; set; }
		[XmlElement(ElementName = "ssao")]
		public Ssao Ssao { get; set; }
		[XmlElement(ElementName = "sunShafts")]
		public SunShafts SunShafts { get; set; }
		[XmlElement(ElementName = "depthOfField")]
		public DepthOfField DepthOfField { get; set; }
		[XmlElement(ElementName = "ssr")]
		public Ssr Ssr { get; set; }
		[XmlElement(ElementName = "bloomAndFlares")]
		public BloomAndFlares BloomAndFlares { get; set; }
		[XmlElement(ElementName = "colorCorrectionCurves")]
		public ColorCorrectionCurves ColorCorrectionCurves { get; set; }
		[XmlElement(ElementName = "vignetteAndChromaticAberration")]
		public VignetteAndChromaticAberration VignetteAndChromaticAberration { get; set; }
		[XmlElement(ElementName = "antialiasing")]
		public Antialiasing Antialiasing { get; set; }
		[XmlElement(ElementName = "noiseAndGrain")]
		public NoiseAndGrain NoiseAndGrain { get; set; }
		[XmlElement(ElementName = "motionBlur")]
		public MotionBlur MotionBlur { get; set; }
		[XmlElement(ElementName = "afterImage")]
		public AfterImage AfterImage { get; set; }
		[XmlElement(ElementName = "volumetricLights")]
		public VolumetricLights VolumetricLights { get; set; }
		[XmlElement(ElementName = "segi")]
		public Segi Segi { get; set; }
		[XmlElement(ElementName = "amplifyBloom")]
		public AmplifyBloom AmplifyBloom { get; set; }
		[XmlElement(ElementName = "blur")]
		public Blur Blur { get; set; }
		[XmlElement(ElementName = "effectsOrder")]
		public EffectsOrder EffectsOrder { get; set; }
		[XmlAttribute(AttributeName = "version")]
		public string Version { get; set; }
	}
}