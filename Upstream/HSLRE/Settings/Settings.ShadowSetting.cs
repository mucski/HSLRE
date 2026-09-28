using Config;

namespace HSLRE
{
	public static partial class Settings
	{
		public class ShadowSetting : BaseSystem
		{
			public ShadowSetting(string elementName) : base(elementName)
			{
			}

			public override void Init()
			{
				this.ShadowDistance = 20f;
				this.ShadowProjection = 0;
				this.ShadowCascades = 4;
				this.ShadowCascade2Split = 0.3333333f;
				this.ShadowCascade4Split_x = 0.06666667f;
				this.ShadowCascade4Split_y = 0.2f;
				this.ShadowCascade4Split_z = 0.4666667f;
				this.ShadowNearPlaneOffset = 2f;
			}

			public float ShadowDistance;

			public int ShadowProjection;

			public int ShadowCascades;

			public float ShadowCascade2Split;

			public float ShadowCascade4Split_x;

			public float ShadowCascade4Split_y;

			public float ShadowCascade4Split_z;

			public float ShadowNearPlaneOffset;
		}
	}
}
