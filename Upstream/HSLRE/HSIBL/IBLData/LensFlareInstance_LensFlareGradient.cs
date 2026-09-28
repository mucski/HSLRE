using System.Xml.Serialization;
using System.Collections.Generic;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "LensFlareInstance.LensFlareGradient")]
	public class LensFlareInstance_LensFlareGradient
	{
		[XmlElement(ElementName = "colorKey")]
		public List<ColorKey> ColorKey { get; set; }
		[XmlElement(ElementName = "alphaKey")]
		public List<AlphaKey> AlphaKey { get; set; }
	}
}