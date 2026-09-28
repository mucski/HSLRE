using Config;

namespace HSLRE
{
	public static partial class Settings
	{
		public class ToneMapSetting : BaseSystem
		{
			public ToneMapSetting(string elementName) : base(elementName)
			{
			}

			public override void Init()
			{
				this.tonemapper = 1;
				this.exposure = 1f;
				this.Lut = false;
				this.LutName = "Neutral";
				this.LutContribution = 1f;
				this.temperatureShift = 0f;
				this.tint = 0f;
				this.contrast = 1f;
				this.hue = 0f;
				this.saturation = 1f;
				this.value = 1f;
				this.vibrance = 0f;
				this.gain = 1f;
				this.gamma = 1f;
				this.useDithering = true;
			}

			public int tonemapper;

			public float exposure;

			public bool Lut;

			public string LutName;

			public float LutContribution;

			public float tint;

			public float contrast;

			public float temperatureShift;

			public float hue;

			public float saturation;

			public float value;

			public float vibrance;

			public float gain;

			public float gamma;
			public bool useDithering;
		}
	}
}
