using System.Xml.Serialization;
using System.Collections.Generic;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "LensGlareInstance.CromaticColorGradient")]
	public class LensGlareInstance_CromaticColorGradient
	{
		[XmlElement(ElementName = "colorKey")]
		public List<ColorKey> ColorKey { get; set; }
		[XmlElement(ElementName = "alphaKey")]
		public List<AlphaKey> AlphaKey { get; set; }
	}
}