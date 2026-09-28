using Config;

namespace HSLRE
{
	public static partial class Settings
	{
		public class DepthOfFieldSetting : BaseSystem
		{
			public DepthOfFieldSetting(string elementName) : base(elementName)
			{
			}

			public override void Init()
			{
				this.enabled = true;
				this.focalLength = 10f;
				this.focalSize = 1f;
				this.aperture = 0.7f;
				this.maxBlurSize = 2f;
				this.highResolution = true;
				this.blurType = "DiscBlur";
				this.blurSampleCount = "High";
				this.nearBlur = false;
				this.foregroundOverlap = 1;
				this.dx11BokehThreshold = 0.5f;
				this.dx11SpawnHeuristic = 0.0875f;
				this.dx11BokehScale = 1.2f;
				this.dx11BokehIntensity = 2.5f;
				this.fixUpscaledScreenshots = true;
			}

			public bool enabled;
			public float focalLength;
			public float focalSize;
			public float aperture;
			public float maxBlurSize;
			public bool highResolution;
			public string blurType;
			public string blurSampleCount;
			public bool nearBlur;
			public float foregroundOverlap;
			public float dx11BokehThreshold;
			public float dx11SpawnHeuristic;
			public float dx11BokehScale;
			public float dx11BokehIntensity;
			public bool fixUpscaledScreenshots;

		}
	}
}
