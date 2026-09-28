using Config;
using UnityEngine;

namespace HSLRE
{
	public static partial class Settings
	{
		public class AmplifyBloomSetting : BaseSystem
		{
			public AmplifyBloomSetting(string elementName) : base(elementName)
			{
			}

			public override void Init()
			{
				this.enabled = false;
				this.UpscaleQuality = "Realistic";
				this.MainThresholdSize = "Full";
				this.CurrentPrecisionMode = "Low";
				this.BloomRange = 500;
				this.OverallIntensity = 0.8f;
				this.OverallThreshold = 0.53f;
				this.BloomScale = 1f;
				this.UpscaleBlurRadius = 1f;
				this.TemporalFilteringActive = false;
				this.TemporalFilteringValue = 0.05f;
				this.SeparateFeaturesThreshold = false;
				this.FeaturesThreshold = 0.05f;
				this.ApplyLensDirt = true;
				this.LensDirtStrength = 2;
				this.LensDirtTexture = "DirtHighContrast";
				this.ApplyLensStardurst = true;
				this.LensStarburstStrength = 2;
				this.LensStardurstTex = "Starburst";
				this.BokehFilterInstanceApplyBokeh = false;
				this.BokehFilterInstanceApplyOnBloomSource = false;
				this.BokehFilterInstanceApertureShape = "Hexagon";
				this.BokehFilterInstanceOffsetRotation = 0;
				this.BokehFilterInstanceBokehSampleRadius = 0.5f;
				this.BokehFilterInstanceAperture = 0.05f;
				this.BokehFilterInstanceFocalLength = 0.018f;
				this.BokehFilterInstanceFocalDistance = 1.34f;
				this.BokehFilterInstanceMaxCoCDiameter = 0.18f;
				this.LensFlareInstanceApplyLensFlare = true;
				this.LensFlareInstanceOverallIntensity = 1;
				this.LensFlareInstanceLensFlareGaussianBlurAmount = 1;
				this.LensFlareInstanceLensFlareNormalizedGhostsIntensity = 0.8f;
				this.LensFlareInstanceLensFlareGhostAmount = 3;
				this.LensFlareInstanceLensFlareGhostsDispersal = 0.228f;
				this.LensFlareInstanceLensFlareGhostChrDistortion = 2;
				this.LensFlareInstanceLensFlareGhostsPowerFactor = 1;
				this.LensFlareInstanceLensFlareGhostsPowerFalloff = 4;
				this.LensFlareInstanceLensFlareNormalizedHaloIntensity = 0.1f;
				this.LensFlareInstanceLensFlareHaloWidth = 0.573f;
				this.LensFlareInstanceLensFlareHaloChrDistortion = 1.51f;
				this.LensFlareInstanceLensFlareHaloPowerFactor = 1;
				this.LensFlareInstanceLensFlareHaloPowerFalloff = 128;
				this.LensGlareInstanceApplyLensGlare = true;
				this.LensGlareInstanceIntensity = 0.17f;
				this.LensGlareInstanceOverallStreakScale = 1;
				this.LensGlareInstanceOverallTint = Color.white;
				this.LensGlareInstanceCurrentGlare = "CheapLens";
				this.LensGlareInstancePerPassDisplacement = 4;
				this.LensGlareInstanceGlareMaxPassCount = 4;
				this.fixUpscaledScreenshots = true;
			}

			public bool enabled;
			public int LensGlareInstanceGlareMaxPassCount;
			public float LensGlareInstancePerPassDisplacement;
			public string LensGlareInstanceCurrentGlare;
			public Color LensGlareInstanceOverallTint;
			public float LensGlareInstanceOverallStreakScale;
			public float LensGlareInstanceIntensity;
			public bool LensGlareInstanceApplyLensGlare;
			public float LensFlareInstanceLensFlareHaloPowerFalloff;
			public float LensFlareInstanceLensFlareHaloPowerFactor;
			public float LensFlareInstanceLensFlareHaloChrDistortion;
			public float LensFlareInstanceLensFlareHaloWidth;
			public float LensFlareInstanceLensFlareNormalizedHaloIntensity;
			public float LensFlareInstanceLensFlareGhostsPowerFalloff;
			public float LensFlareInstanceLensFlareGhostsPowerFactor;
			public float LensFlareInstanceLensFlareGhostChrDistortion;
			public float LensFlareInstanceLensFlareGhostsDispersal;
			public int LensFlareInstanceLensFlareGhostAmount;
			public float LensFlareInstanceLensFlareNormalizedGhostsIntensity;
			public int LensFlareInstanceLensFlareGaussianBlurAmount;
			public float LensFlareInstanceOverallIntensity;
			public bool LensFlareInstanceApplyLensFlare;
			public float BokehFilterInstanceMaxCoCDiameter;
			public float BokehFilterInstanceFocalDistance;
			public float BokehFilterInstanceFocalLength;
			public float BokehFilterInstanceAperture;
			public float BokehFilterInstanceBokehSampleRadius;
			public float BokehFilterInstanceOffsetRotation;
			public string BokehFilterInstanceApertureShape;
			public bool BokehFilterInstanceApplyOnBloomSource;
			public bool BokehFilterInstanceApplyBokeh;
			public string LensStardurstTex;
			public float LensStarburstStrength;
			public bool ApplyLensStardurst;
			public string LensDirtTexture;
			public float LensDirtStrength;
			public bool ApplyLensDirt;
			public float FeaturesThreshold;
			public bool SeparateFeaturesThreshold;
			public float TemporalFilteringValue;
			public bool TemporalFilteringActive;
			public float UpscaleBlurRadius;
			public float BloomScale;
			public float OverallThreshold;
			public float OverallIntensity;
			public float BloomRange;
			public string CurrentPrecisionMode;
			public string MainThresholdSize;
			public string UpscaleQuality;
			public bool fixUpscaledScreenshots;
		}
	}
}
