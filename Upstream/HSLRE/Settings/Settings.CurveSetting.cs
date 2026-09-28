using Config;

namespace HSLRE
{
	public static partial class Settings
	{
		public class CurveSetting : BaseSystem
		{
			public CurveSetting(string elementName) : base(elementName)
			{
			}

			public override void Init()
			{
				this.Enabled = false;
				this.Curve = 1;
				this.CurveName = "SampleCurve2";
				this.CurveSaturation = 1f;
			}

			public bool Enabled;

			public int Curve;

			public string CurveName;

			public float CurveSaturation;
		}
	}
}
