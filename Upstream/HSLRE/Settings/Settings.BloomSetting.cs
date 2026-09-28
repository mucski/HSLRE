using Config;

namespace HSLRE
{
	public static partial class Settings
	{
		public class BloomSetting : BaseSystem
		{
			public BloomSetting(string elementName) : base(elementName)
			{
			}

			public override void Init()
			{
				this.enabled = true;
				this.threshold = 0.9f;
				this.softKnee = 0.5f;
				this.raduis = 2f;
				this.intensity = 0.7f;
				this.antiFlicker = false;
			}

			public bool enabled;

			public float intensity;

			public float threshold;

			public float softKnee;

			public float raduis;

			public bool antiFlicker;
		}
	}
}
