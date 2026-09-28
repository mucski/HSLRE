using Config;

namespace HSLRE
{
	public static partial class Settings
	{
		public class Preset : BaseSystem
		{
			public Preset(string elementName) : base(elementName)
			{
			}

			public override void Init()
			{
				this.ImageEffectPreset = "Default";
				this.ShadowPreset = "Default";
				this.StylePreset = "Default";
				this.CommonPreset = "Default";
			}

			public string ImageEffectPreset;
			public string ShadowPreset;
			public string StylePreset;
			public string CommonPreset;
		}
	}
}
