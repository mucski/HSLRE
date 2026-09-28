using Config;
using UnityEngine;

namespace HSLRE
{
	public static partial class Settings
	{
		public class LensAberrationSetting : BaseSystem
		{
			public LensAberrationSetting(string elementName) : base(elementName)
			{
			}

			public override void Init()
			{
				this.DistortionEnabled = false;
				this.DistortionAmount = 0f;
				this.DistortionAmountX = 1f;
				this.DistortionAmountY = 1f;
				this.DistortionScale = 1f;
				this.VignetteEnabled = false;
				this.VignetteBlur = 0f;
				this.VignetteIntensity = 1.4f;
				this.VignetteSmoothness = 0.8f;
				this.VignetteRoundness = 1f;
				this.VignetteDesaturate = 0f;
				this.VignetteColor = Color.black;
				this.ChromaticAberrationEnabled = false;
				this.ChromaticAberrationAmount = 0f;
				this.ChromaticColor = Color.green;
			}

			public float DistortionAmount;

			public float DistortionScale;

			public float DistortionAmountX;

			public float DistortionAmountY;

			public bool DistortionEnabled;

			public bool VignetteEnabled;

			public float VignetteBlur;

			public Color VignetteColor;

			public float VignetteIntensity;

			public float VignetteSmoothness;

			public float VignetteRoundness;

			public float VignetteDesaturate;

			public Color ChromaticColor;

			public bool ChromaticAberrationEnabled;

			public float ChromaticAberrationAmount;
		}
	}
}
