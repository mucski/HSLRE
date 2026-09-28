using Config;

namespace HSLRE
{
	public static partial class Settings
	{
		public class SMAASetting : BaseSystem
		{
			public SMAASetting(string elementName) : base(elementName)
			{
			}

			public override void Init()
			{
				this.enabled = true;
				this.quality = 3;
				this.edgeDetectionMethod = 2;
				this.diagonalDetection = true;
				this.cornerDetection = true;
				this.threshold = 0.05f;
				this.depthThreshold = 0.01f;
				this.maxSearchSteps = 32;
				this.maxDiagonalSearchSteps = 16;
				this.cornerRounding = 25;
				this.localContrastAdaptationFactor = 2f;
				this.predication = false;
				this.predicationThreshold = 0.01f;
				this.predicationScale = 2f;
				this.predicationStrength = 0.4f;
				this.temporal = false;
				this.temporalFuzzSize = 2f;

			}

			public bool enabled;
			public int quality;
			public int edgeDetectionMethod;
			public bool diagonalDetection;
			public bool cornerDetection;
			public float threshold;
			public float depthThreshold;
			public int maxSearchSteps;
			public int maxDiagonalSearchSteps;
			public int cornerRounding;
			public float localContrastAdaptationFactor;
			public bool predication;
			public float predicationThreshold;
			public float predicationScale;
			public float predicationStrength;
			public bool temporal;
			public float temporalFuzzSize;
		}
	}
}
