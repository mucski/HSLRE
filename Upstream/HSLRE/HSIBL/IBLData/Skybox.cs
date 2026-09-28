using System.Xml.Serialization;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "skybox")]
	public class Skybox
	{
		[XmlAttribute(AttributeName = "proceduralExposure")]
		public string ProceduralExposure { get; set; }
		[XmlAttribute(AttributeName = "proceduralSunsize")]
		public string ProceduralSunsize { get; set; }
		[XmlAttribute(AttributeName = "proceduralAtmospherethickness")]
		public string ProceduralAtmospherethickness { get; set; }
		[XmlAttribute(AttributeName = "proceduralSkytintR")]
		public string ProceduralSkytintR { get; set; }
		[XmlAttribute(AttributeName = "proceduralSkytintG")]
		public string ProceduralSkytintG { get; set; }
		[XmlAttribute(AttributeName = "proceduralSkytintB")]
		public string ProceduralSkytintB { get; set; }
		[XmlAttribute(AttributeName = "proceduralGroundcolorR")]
		public string ProceduralGroundcolorR { get; set; }
		[XmlAttribute(AttributeName = "proceduralGroundcolorG")]
		public string ProceduralGroundcolorG { get; set; }
		[XmlAttribute(AttributeName = "proceduralGroundcolorB")]
		public string ProceduralGroundcolorB { get; set; }
		[XmlAttribute(AttributeName = "classicRotation")]
		public string ClassicRotation { get; set; }
		[XmlAttribute(AttributeName = "classicExposure")]
		public string ClassicExposure { get; set; }
		[XmlAttribute(AttributeName = "classicTintR")]
		public string ClassicTintR { get; set; }
		[XmlAttribute(AttributeName = "classicTintG")]
		public string ClassicTintG { get; set; }
		[XmlAttribute(AttributeName = "classicTintB")]
		public string ClassicTintB { get; set; }
		[XmlAttribute(AttributeName = "ambientIntensity")]
		public string AmbientIntensity { get; set; }
	}
}