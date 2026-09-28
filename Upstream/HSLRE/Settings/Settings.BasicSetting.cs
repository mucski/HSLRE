using Config;
using UnityEngine;

namespace HSLRE
{
	public static partial class Settings
	{
		public class BasicSetting : BaseSystem
		{
			public BasicSetting(string elementName) : base(elementName)
			{
			}

			public override void Init()
			{
				this.msaa = 8;
				this.CameraFOV = 23f;
				this.DisableFog = false;
				this.CharaMakerReform = false;
				this.CharaMakerBackgroundType = (int)CameraClearFlags.Skybox;
				this.CharaMakerBackground = new Color(0.192157f, 0.301961f, 0.47451f, 1f);
			}

			public int msaa;
			public float CameraFOV;
			public bool CharaMakerReform;
			public int CharaMakerBackgroundType;
			public Color CharaMakerBackground;
			public bool DisableFog;
		}
	}
}
