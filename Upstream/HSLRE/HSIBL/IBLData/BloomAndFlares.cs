using System.Xml.Serialization;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "bloomAndFlares")]
	public class BloomAndFlares
	{
		[XmlAttribute(AttributeName = "enabled")]
		public string Enabled { get; set; }
		[XmlAttribute(AttributeName = "screenBlendMode")]
		public string ScreenBlendMode { get; set; }
		[XmlAttribute(AttributeName = "hdr")]
		public string Hdr { get; set; }
		[XmlAttribute(AttributeName = "bloomIntensity")]
		public string BloomIntensity { get; set; }
		[XmlAttribute(AttributeName = "bloomThreshold")]
		public string BloomThreshold { get; set; }
		[XmlAttribute(AttributeName = "bloomBlurIterations")]
		public string BloomBlurIterations { get; set; }
		[XmlAttribute(AttributeName = "sepBlurSpread")]
		public string SepBlurSpread { get; set; }
		[XmlAttribute(AttributeName = "useSrcAlphaAsMask")]
		public string UseSrcAlphaAsMask { get; set; }
		[XmlAttribute(AttributeName = "lensflares")]
		public string Lensflares { get; set; }
		[XmlAttribute(AttributeName = "lensflareMode")]
		public string LensflareMode { get; set; }
		[XmlAttribute(AttributeName = "lensflareIntensity")]
		public string LensflareIntensity { get; set; }
		[XmlAttribute(AttributeName = "lensflareThreshold")]
		public string LensflareThreshold { get; set; }
		[XmlAttribute(AttributeName = "hollyStretchWidth")]
		public string HollyStretchWidth { get; set; }
		[XmlAttribute(AttributeName = "hollywoodFlareBlurIterations")]
		public string HollywoodFlareBlurIterations { get; set; }
		[XmlAttribute(AttributeName = "flareColorAR")]
		public string FlareColorAR { get; set; }
		[XmlAttribute(AttributeName = "flareColorAG")]
		public string FlareColorAG { get; set; }
		[XmlAttribute(AttributeName = "flareColorAB")]
		public string FlareColorAB { get; set; }
		[XmlAttribute(AttributeName = "flareColorAA")]
		public string FlareColorAA { get; set; }
		[XmlAttribute(AttributeName = "flareColorBR")]
		public string FlareColorBR { get; set; }
		[XmlAttribute(AttributeName = "flareColorBG")]
		public string FlareColorBG { get; set; }
		[XmlAttribute(AttributeName = "flareColorBB")]
		public string FlareColorBB { get; set; }
		[XmlAttribute(AttributeName = "flareColorBA")]
		public string FlareColorBA { get; set; }
		[XmlAttribute(AttributeName = "flareColorCR")]
		public string FlareColorCR { get; set; }
		[XmlAttribute(AttributeName = "flareColorCG")]
		public string FlareColorCG { get; set; }
		[XmlAttribute(AttributeName = "flareColorCB")]
		public string FlareColorCB { get; set; }
		[XmlAttribute(AttributeName = "flareColorCA")]
		public string FlareColorCA { get; set; }
		[XmlAttribute(AttributeName = "flareColorDR")]
		public string FlareColorDR { get; set; }
		[XmlAttribute(AttributeName = "flareColorDG")]
		public string FlareColorDG { get; set; }
		[XmlAttribute(AttributeName = "flareColorDB")]
		public string FlareColorDB { get; set; }
		[XmlAttribute(AttributeName = "flareColorDA")]
		public string FlareColorDA { get; set; }
		[XmlAttribute(AttributeName = "index")]
		public string Index { get; set; }
	}
}