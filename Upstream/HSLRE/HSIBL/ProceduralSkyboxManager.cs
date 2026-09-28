using UnityEngine;

namespace HSIBL
{
	public struct ProceduralSkyboxParams
	{
		internal float exposure;
		internal float sunsize;
		internal float atmospherethickness;
		internal Color skytint;
		internal Color groundcolor;
		// addition: where the sun is, in degrees (the stock shader took it from a light HS never passes to the sky)
		internal float sunheight;
		internal float sunazimuth;
		public ProceduralSkyboxParams(float a, float b, float c, Color A, Color B)
		{
			exposure = a;
			sunsize = b;
			atmospherethickness = c;
			skytint = A;
			groundcolor = B;
			sunheight = 35f;
			sunazimuth = 140f;
		}

		// addition: see SkyboxParams.Equals - avoids per-frame boxing / reflection garbage in HSIBL.Update
		public bool Equals(ProceduralSkyboxParams o) => exposure == o.exposure && sunsize == o.sunsize && atmospherethickness == o.atmospherethickness && skytint == o.skytint && groundcolor == o.groundcolor && sunheight == o.sunheight && sunazimuth == o.sunazimuth;
		public override bool Equals(object obj) => obj is ProceduralSkyboxParams && this.Equals((ProceduralSkyboxParams)obj);
		public override int GetHashCode() => exposure.GetHashCode() ^ sunsize.GetHashCode() * 31 ^ atmospherethickness.GetHashCode() * 17 ^ skytint.GetHashCode() * 7 ^ groundcolor.GetHashCode() * 3;
	};
	class ProceduralSkyboxManager
	{
		public void Init()
		{
			AssetBundle cubemapbundle = AssetBundle.LoadFromFile(Application.dataPath + "/../abdata/plastic/proceduralskybox.unity3d");
			proceduralsky = cubemapbundle.LoadAsset<Material>("Procedural Skybox");
			cubemapbundle.Unload(false);
			cubemapbundle = null;
			// addition: same sky with the sun direction as a property, so the sun disc (and Sun Size) shows up
			Shader withSun = OverlayShaders.Get("ProceduralSky");
			if (proceduralsky != null && withSun != null)
				proceduralsky.shader = withSun;
			//proceduralsky = new Material();
		}
		public void ApplySkybox()
		{
			RenderSettings.skybox = proceduralsky;
			RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
			RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
		}
		public void ApplySkyboxParams()
		{
			if (proceduralsky != null)
			{
				proceduralsky.SetFloat("_SunDisk", 2f);
				proceduralsky.SetFloat("_Exposure", skyboxparams.exposure);
				proceduralsky.SetFloat("_SunSize", skyboxparams.sunsize);
				proceduralsky.SetColor("_SkyTint", skyboxparams.skytint);
				proceduralsky.SetColor("_GroundColor", skyboxparams.groundcolor);
				proceduralsky.SetFloat("_AtmosphereThickness", skyboxparams.atmospherethickness);
				float el = skyboxparams.sunheight * Mathf.Deg2Rad, az = skyboxparams.sunazimuth * Mathf.Deg2Rad;
				proceduralsky.SetVector("_SunDir", new Vector4(Mathf.Cos(el) * Mathf.Sin(az), Mathf.Sin(el), Mathf.Cos(el) * Mathf.Cos(az), 0f));
				proceduralsky.SetColor("_SunColor", Color.white);
			}
		}
		Material proceduralsky;
		public ProceduralSkyboxParams skyboxparams = new ProceduralSkyboxParams(1f, 0.1f, 1f, Color.gray, Color.gray);
		public Material Proceduralsky
		{
			get { return proceduralsky; }
			set { proceduralsky = value; }
		}
	}

}
