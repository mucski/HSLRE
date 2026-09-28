using Config;
using UnityEngine;

namespace HSLRE
{
	public static partial class Settings
	{
		public class SunShaftsSetting : BaseSystem
		{
			public SunShaftsSetting(string elementName) : base(elementName)
			{
			}

			public override void Init()
			{
				this.enabled = false;
				this.resolution = "Normal";
				this.screenBlendMode = "Screen";
				this.radialBlurIterations = 2;
				this.sunColor = Color.white;
				this.sunThreshold = Color.gray;
				this.sunShaftBlurRadius = 4;
				this.sunShaftIntensity = 3f;
				this.maxRadius = 0.825f;
				this.useDepthTexture = true;
			}

			public bool enabled;
			public string resolution;
			public string screenBlendMode;
			public int radialBlurIterations;
			public Color sunColor;
			public Color sunThreshold;
			public float sunShaftBlurRadius;
			public float sunShaftIntensity;
			public float maxRadius;
			public bool useDepthTexture;
		}
	}
}
