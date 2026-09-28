using System.Xml.Serialization;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "effectsOrder")]
	public class EffectsOrder
	{
		[XmlElement(ElementName = "vignetteAndChromaticAberration")]
		public VignetteAndChromaticAberration VignetteAndChromaticAberration { get; set; }
		[XmlElement(ElementName = "sunShafts")]
		public SunShafts SunShafts { get; set; }
		[XmlElement(ElementName = "smaa")]
		public Smaa Smaa { get; set; }
		[XmlElement(ElementName = "bloom")]
		public Bloom Bloom { get; set; }
		[XmlElement(ElementName = "amplifyBloom")]
		public AmplifyBloom AmplifyBloom { get; set; }
		[XmlElement(ElementName = "tonemapping")]
		public Tonemapping Tonemapping { get; set; }
		[XmlElement(ElementName = "colorCorrectionCurves")]
		public ColorCorrectionCurves ColorCorrectionCurves { get; set; }
		[XmlElement(ElementName = "bloomAndFlares")]
		public BloomAndFlares BloomAndFlares { get; set; }
		[XmlElement(ElementName = "motionBlur")]
		public MotionBlur MotionBlur { get; set; }
		[XmlElement(ElementName = "noiseAndGrain")]
		public NoiseAndGrain NoiseAndGrain { get; set; }
		[XmlElement(ElementName = "lens")]
		public Lens Lens { get; set; }
		[XmlElement(ElementName = "depthOfField")]
		public DepthOfField DepthOfField { get; set; }
		[XmlElement(ElementName = "blur")]
		public Blur Blur { get; set; }
		[XmlElement(ElementName = "antialiasing")]
		public Antialiasing Antialiasing { get; set; }
		[XmlElement(ElementName = "afterImage")]
		public AfterImage AfterImage { get; set; }
	}
}