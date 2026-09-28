using Config;
using UnityStandardAssets.ImageEffects;

namespace HSLRE
{
	public static partial class Settings
	{
		public class VignetteSetting : BaseSystem
		{
			public VignetteSetting(string elementName) : base(elementName)
			{
			}

			public override void Init()
			{
				this.enabled = false;
				this.mode = VignetteAndChromaticAberration.AberrationMode.Simple;
				this.intensity = 0.036f;
				this.chromaticAberration = 0.2f;
				this.axialAberration = 0.5f;
				this.blur = 0f;
				this.blurSpread = 0.75f;
				this.luminanceDependency = 0.25f;
				this.blurDistance = 2.5f;
			}

			public bool enabled;
			public VignetteAndChromaticAberration.AberrationMode mode;
			public float intensity;
			public float chromaticAberration;
			public float axialAberration;
			public float blur;
			public float blurSpread;
			public float luminanceDependency;
			public float blurDistance;
		}
	}
}
