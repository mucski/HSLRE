using Config;
using UnityStandardAssets.ImageEffects;

namespace HSLRE
{
	public static partial class Settings
	{
		public class AntialiasingSetting : BaseSystem
		{
			public AntialiasingSetting(string elementName) : base(elementName)
			{
			}

			public override void Init()
			{
				this.enabled = false;
				this.mode = AAMode.FXAA3Console;
				this.showGeneratedNormals = false;
				this.offsetScale = 0.2f;
				this.blurRadius = 18f;
				this.edgeThresholdMin = 0.05f;
				this.edgeThreshold = 0.2f;
				this.edgeSharpness = 4f;
				this.dlaaSharp = false;
			}

			public bool enabled = false;
			public AAMode mode;
			public bool showGeneratedNormals;
			public float offsetScale;
			public float blurRadius;
			public float edgeThresholdMin;
			public float edgeThreshold;
			public float edgeSharpness;
			public bool dlaaSharp;
		}
	}
}
