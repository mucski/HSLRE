using System.Xml.Serialization;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "lens")]
	public class Lens
	{
		[XmlAttribute(AttributeName = "fov")]
		public string Fov { get; set; }
		[XmlAttribute(AttributeName = "distortionEnabled")]
		public string DistortionEnabled { get; set; }
		[XmlAttribute(AttributeName = "distortionAmount")]
		public string DistortionAmount { get; set; }
		[XmlAttribute(AttributeName = "distortionAmountX")]
		public string DistortionAmountX { get; set; }
		[XmlAttribute(AttributeName = "distortionAmountY")]
		public string DistortionAmountY { get; set; }
		[XmlAttribute(AttributeName = "distortionScale")]
		public string DistortionScale { get; set; }
		[XmlAttribute(AttributeName = "chromaticAberrationEnabled")]
		public string ChromaticAberrationEnabled { get; set; }
		[XmlAttribute(AttributeName = "chromaticAberrationAmount")]
		public string ChromaticAberrationAmount { get; set; }
		[XmlAttribute(AttributeName = "chromaticAberrationColorR")]
		public string ChromaticAberrationColorR { get; set; }
		[XmlAttribute(AttributeName = "chromaticAberrationColorG")]
		public string ChromaticAberrationColorG { get; set; }
		[XmlAttribute(AttributeName = "chromaticAberrationColorB")]
		public string ChromaticAberrationColorB { get; set; }
		[XmlAttribute(AttributeName = "vignetteEnabled")]
		public string VignetteEnabled { get; set; }
		[XmlAttribute(AttributeName = "vignetteIntensity")]
		public string VignetteIntensity { get; set; }
		[XmlAttribute(AttributeName = "vignetteSmoothness")]
		public string VignetteSmoothness { get; set; }
		[XmlAttribute(AttributeName = "vignetteRoundness")]
		public string VignetteRoundness { get; set; }
		[XmlAttribute(AttributeName = "vignetteDesaturate")]
		public string VignetteDesaturate { get; set; }
		[XmlAttribute(AttributeName = "vignetteBlur")]
		public string VignetteBlur { get; set; }
		[XmlAttribute(AttributeName = "vignetteColorR")]
		public string VignetteColorR { get; set; }
		[XmlAttribute(AttributeName = "vignetteColorG")]
		public string VignetteColorG { get; set; }
		[XmlAttribute(AttributeName = "vignetteColorB")]
		public string VignetteColorB { get; set; }
		[XmlAttribute(AttributeName = "index")]
		public string Index { get; set; }
	}
}