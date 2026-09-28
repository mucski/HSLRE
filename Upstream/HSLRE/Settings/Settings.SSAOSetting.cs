using Config;
using UnityEngine;

namespace HSLRE
{
	public static partial class Settings
	{
		public class SSAOSetting : BaseSystem
		{
			public SSAOSetting(string elementName) : base(elementName)
			{
			}

			public override void Init()
			{
				this.SSAOEnabled = true;
				this.SSAOBias = 0.1f;
				this.SSAOBlurBilateralThreshold = 0.1f;
				this.SSAOBlurPasses = 1;
				this.SSAOCutoffDistance = 50f;
				this.SSAOfalloffDistance = 5f;
				this.SSAOLumContribution = 0.5f;
				this.SSAODistance = 0.560f;
				this.SSAORadius = 0.084f;
				this.SSAOSamples = "Medium";
				this.SSAOIntensity = 1.27f;
				this.SSAOUseHighPrecisionDepthMap = false;
				this.SSAODownsampling = 1;
				this.SSAOBlur = "Bilateral";
				this.SSAOBlurDownsampling = false;
				this.SSAOOcclusionColor = new Color32(23, 11, 11, 255);
			}

			public bool SSAOEnabled;
			public string SSAOSamples;
			public float SSAOBias;
			public float SSAOIntensity;
			public float SSAORadius;
			public float SSAOLumContribution;
			public float SSAODistance;
			public float SSAOCutoffDistance;
			public float SSAOfalloffDistance;
			public int SSAOBlurPasses;
			public float SSAOBlurBilateralThreshold;
			public bool SSAOUseHighPrecisionDepthMap;
			public int SSAODownsampling;
			public string SSAOBlur;
			public bool SSAOBlurDownsampling;
			public Color SSAOOcclusionColor;
		}
	}
}
