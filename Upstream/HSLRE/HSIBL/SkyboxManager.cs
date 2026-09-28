using UnityEngine;

namespace HSIBL
{
	public struct SkyboxParams
	{
		internal float exposure;
		internal float rotation;
		internal Color tint;
		public SkyboxParams(float a, float b, Color A)
		{
			exposure = a;
			rotation = b;
			tint = A;
		}

		// addition: HSIBL.Update compares these every frame; without a typed Equals that went through
		// ValueType.Equals(object), which boxes and reflects over the fields on the game's Mono (garbage every frame)
		public bool Equals(SkyboxParams o) => exposure == o.exposure && rotation == o.rotation && tint == o.tint;
		public override bool Equals(object obj) => obj is SkyboxParams && this.Equals((SkyboxParams)obj);
		public override int GetHashCode() => exposure.GetHashCode() ^ rotation.GetHashCode() * 31 ^ tint.GetHashCode() * 17;
	};
	class SkyboxManager
	{
		public void ApplySkybox()
		{
			RenderSettings.skybox = skybox;
			RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
			RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
		}

		public void ApplySkyboxParams()
		{
			if (skybox != null)
			{
				skybox.SetFloat("_Exposure", skyboxparams.exposure);
				skybox.SetColor("_Tint", skyboxparams.tint);
				skybox.SetFloat("_Rotation", skyboxparams.rotation);
			}
		}
		Material skybox;
		public SkyboxParams skyboxparams = new SkyboxParams(1f, 0f, Color.gray);
		public Material Skybox { get => skybox; set => skybox = value; }
	}

}
