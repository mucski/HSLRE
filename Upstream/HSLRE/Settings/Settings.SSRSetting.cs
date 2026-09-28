using Config;

namespace HSLRE
{
	public static partial class Settings
	{
		public class SSRSetting : BaseSystem
		{
			public SSRSetting(string elementName) : base(elementName)
			{
			}

			public override void Init()
			{
				this.SSRenabled = true;
				this.SSRscreenEdgeFading = 0.396f;
				this.SSRmaxDistance = 465.0f;
				this.SSRfadeDistance = 262.0f;
				this.SSRreflectionMultiplier = 1.470f;
				this.SSRenableHDR = true;
				this.SSRadditiveReflection = false;
				this.SSRmaxSteps = 219;
				this.SSRrayStepSize = 3;
				this.SSRwidthModifier = 1.2f;
				this.SSRsmoothFallbackThreshold = 0.2f;
				this.SSRdistanceBlur = 1f;
				this.SSRfresnelFade = 0.2f;
				this.SSRfresnelFadePower = 2f;
				this.SSRsmoothFallbackDistance = 0.042f;
				this.SSRuseTemporalConfidence = true;
				this.SSRtemporalFilterStrength = 0.05f;
				this.SSRtreatBackfaceHitAsMiss = false;
				this.SSRallowBackwardsRays = false;
				this.SSRtraceBehindObjects = true;
				this.SSRhighQualitySharpReflections = true;
				this.SSRtraceEverywhere = false;
				this.SSRresolution = 2;
				this.SSRbilateralUpsample = true;
				this.SSRimproveCorners = true;
				this.SSRreduceBanding = true;
				this.SSRhighlightSuppression = false;
				this.SSRdebugMode = 0;
			}

			public bool SSRenabled;

			public float SSRscreenEdgeFading;

			public float SSRmaxDistance;

			public float SSRfadeDistance;

			public float SSRreflectionMultiplier;

			public bool SSRenableHDR;

			public bool SSRadditiveReflection;

			public int SSRmaxSteps;

			public int SSRrayStepSize;

			public float SSRwidthModifier;

			public float SSRsmoothFallbackThreshold;

			public float SSRdistanceBlur;

			public float SSRfresnelFade;

			public float SSRfresnelFadePower;

			public float SSRsmoothFallbackDistance;

			public bool SSRuseTemporalConfidence;

			public float SSRtemporalFilterStrength;

			public bool SSRtreatBackfaceHitAsMiss;

			public bool SSRallowBackwardsRays;

			public bool SSRtraceBehindObjects;

			public bool SSRhighQualitySharpReflections;

			public bool SSRtraceEverywhere;

			public int SSRresolution;

			public bool SSRbilateralUpsample;

			public bool SSRimproveCorners;

			public bool SSRreduceBanding;

			public bool SSRhighlightSuppression;

			public int SSRdebugMode;
		}
	}
}
