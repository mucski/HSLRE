using Config;
using UnityEngine;

namespace HSLRE
{
	public static partial class Settings
	{
		public class NoiseAndGrainSetting : BaseSystem
		{
			public NoiseAndGrainSetting(string elementName) : base(elementName)
			{
			}

			public override void Init()
			{
				this.enabled = false;
				this.intensityMultiplier = 0.25f;
				this.generalIntensity = 0.5f;
				this.blackIntensity = 1f;
				this.whiteIntensity = 1f;
				this.midGrey = 0.2f;
				this.dx11Grain = false;
				this.softness = 0;
				this.monochrome = false;
				this.intensities = new Vector3(1f, 1f, 1f);
				this.tiling = new Vector3(64f, 64f, 64f);
				this.monochromeTiling = 64f;
				this.filterMode = "Bilinear";
				this.fixUpscaledScreenshots = true;
			}

			public bool enabled;
			public bool dx11Grain;
			public bool monochrome;
			public float intensityMultiplier;
			public float generalIntensity;
			public float blackIntensity;
			public float whiteIntensity;
			public float midGrey;
			public Vector3 intensities;
			public string filterMode;
			public float softness;
			public float monochromeTiling;
			public Vector3 tiling;
			public bool fixUpscaledScreenshots;
		}
	}
}
