using Config;
using UnityEngine;

namespace HSLRE
{
	public static partial class Settings
	{
		public class BloomAndFlaresSetting : BaseSystem
		{
			public BloomAndFlaresSetting(string elementName) : base(elementName)
			{
			}

			public override void Init()
			{
				this.BloomEnabled = false;
				this.Bloomintensity = 0.42f;
				this.BloomThreshold = 0.33f;
				this.BloomBlurSpread = 0.8f;
				this.BloomScreenBlendMode = "Screen";
				this.BloomHdr = "Auto";
				this.BloomBlurIterations = 2;
				this.BloomUseSrcAlphaAsMask = 0f;
				this.BloomLensflares = false;
				this.BloomLensflareMode = "Anamorphic";
				this.BloomLensflareIntensity = 1f;
				this.BloomLensflareThreshold = 0.3f;
				this.BloomLensFlareStretchWidth = 3.5f;
				this.BloomLensFlareBlurIterations = 2;
				this.BloomFlareColorA = new Color32(102, 102, 204, 191);
				this.BloomFlareColorB = new Color32(102, 204, 204, 191);
				this.BloomFlareColorC = new Color32(204, 102, 204, 191);
				this.BloomFlareColorD = new Color32(204, 102, 0, 191);
			}

			public bool BloomEnabled;
			public float Bloomintensity;
			public float BloomThreshold;
			public float BloomBlurSpread;
			public string BloomScreenBlendMode;
			public string BloomHdr;
			public int BloomBlurIterations;
			public float BloomUseSrcAlphaAsMask;
			public bool BloomLensflares;
			public string BloomLensflareMode;
			public float BloomLensflareIntensity;
			public float BloomLensflareThreshold;
			public float BloomLensFlareStretchWidth;
			public int BloomLensFlareBlurIterations;
			public Color BloomFlareColorA;
			public Color BloomFlareColorB;
			public Color BloomFlareColorC;
			public Color BloomFlareColorD;
		}
	}
}
