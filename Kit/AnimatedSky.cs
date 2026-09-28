using System.Xml;
using UnityEngine;
using UnityEngine.Rendering;

namespace HSIBL
{
	/// <summary>
	/// Animated Sky (addition): a generated sky with drifting clouds (presets in the style of Koikatsu's sky
	/// states). While on it is the scene's skybox, so it also feeds ambient light and reflections. Those are
	/// only refreshed when a setting changes: moving clouds never trigger the expensive environment / probe
	/// update, so the per-frame cost is just the sky shader on background pixels.
	/// The backdrop variant (lightsScene = false) is the same sky drawn only as the camera's background through a
	/// Skybox component: ambient light and reflections stay whatever the cubemap / scene gives them.
	/// </summary>
	internal sealed class AnimatedSky
	{
		internal static readonly string[] Presets = { "Sunny", "Noon", "Evening", "Night" };

		private struct Palette
		{
			internal Color Zenith, Horizon, Ground, Sun, CloudLight, CloudShadow;
			internal float SunSize, Stars;
		}

		private static readonly Palette[] _palettes =
		{
			new Palette { Zenith = new Color(0.18f, 0.42f, 0.88f), Horizon = new Color(0.68f, 0.84f, 1f), Ground = new Color(0.32f, 0.33f, 0.35f), Sun = new Color(1f, 0.95f, 0.85f), CloudLight = new Color(1f, 1f, 1f), CloudShadow = new Color(0.62f, 0.68f, 0.78f), SunSize = 0.035f, Stars = 0f },
			new Palette { Zenith = new Color(0.1f, 0.3f, 0.78f), Horizon = new Color(0.55f, 0.75f, 0.98f), Ground = new Color(0.34f, 0.34f, 0.36f), Sun = new Color(1f, 0.98f, 0.92f), CloudLight = new Color(1f, 1f, 1f), CloudShadow = new Color(0.7f, 0.75f, 0.84f), SunSize = 0.03f, Stars = 0f },
			new Palette { Zenith = new Color(0.22f, 0.2f, 0.45f), Horizon = new Color(1f, 0.55f, 0.32f), Ground = new Color(0.2f, 0.15f, 0.16f), Sun = new Color(1f, 0.55f, 0.25f), CloudLight = new Color(1f, 0.62f, 0.45f), CloudShadow = new Color(0.35f, 0.26f, 0.4f), SunSize = 0.045f, Stars = 0f },
			new Palette { Zenith = new Color(0.01f, 0.02f, 0.06f), Horizon = new Color(0.05f, 0.08f, 0.16f), Ground = new Color(0.02f, 0.02f, 0.03f), Sun = new Color(0.55f, 0.6f, 0.75f), CloudLight = new Color(0.22f, 0.25f, 0.33f), CloudShadow = new Color(0.04f, 0.05f, 0.09f), SunSize = 0.025f, Stars = 1f },
		};

		internal bool On;
		internal int Preset;
		internal readonly bool LightsScene;
		private readonly string _element;
		internal float Coverage, CloudSpeed, WindAngle, SunHeight, SunAzimuth, Exposure;

		private static readonly int _oTimeId = Shader.PropertyToID("_OTime");
		private Material _material;
		private bool _applied;
		private Material _previousSkybox;
		private AmbientMode _previousAmbient;
		private DefaultReflectionMode _previousReflection;
		private float _time;
		private int _shownHash;
		// lighting refreshes re-render the reflection probe: at most 4 a second while a slider is dragged
		private const float RefreshInterval = 0.25f;
		private bool _refreshPending;
		private float _lastRefresh = -1f;
		private Skybox _camSkybox;
		private bool _camSkyboxAdded;

		internal AnimatedSky(bool lightsScene, string element)
		{
			this.LightsScene = lightsScene;
			this._element = element;
			this.Reset();
		}

		internal void Reset()
		{
			this.On = false;
			this.SetPreset(0);
		}

		/// <summary>Picks a preset and resets the cloud / sun settings to that preset's own values.</summary>
		internal void SetPreset(int preset)
		{
			this.Preset = Mathf.Clamp(preset, 0, Presets.Length - 1);
			switch (this.Preset)
			{
				case 0: this.Coverage = 0.45f; this.SunHeight = 55f; break;
				case 1: this.Coverage = 0.25f; this.SunHeight = 80f; break;
				case 2: this.Coverage = 0.4f; this.SunHeight = 6f; break;
				default: this.Coverage = 0.25f; this.SunHeight = 40f; break; // the moon
			}
			this.CloudSpeed = 0.2f;
			this.WindAngle = 30f;
			this.SunAzimuth = 140f;
			this.Exposure = 1f;
		}

		/// <summary>Every frame. Returns true when ambient light / reflections should be refreshed.</summary>
		internal bool Tick(Camera cam, bool hideSkybox)
		{
			if (this.LightsScene == false)
			{
				this.TickBackdrop(cam, hideSkybox);
				return false;
			}
			bool refresh = false;
			if (this.On && this._applied && RenderSettings.skybox != this._material)
			{
				// something else (a cubemap picked from the list) took the skybox over: step aside
				this.On = false;
				this._applied = false;
			}
			if (this.On && this._applied == false)
			{
				Material m = this.Material();
				if (m == null)
				{
					this.On = false;
					return false;
				}
				this._previousSkybox = RenderSettings.skybox;
				this._previousAmbient = RenderSettings.ambientMode;
				this._previousReflection = RenderSettings.defaultReflectionMode;
				RenderSettings.skybox = m;
				RenderSettings.ambientMode = AmbientMode.Skybox;
				RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
				this._applied = true;
				this._shownHash = 0;
				refresh = true;
			}
			else if (this.On == false && this._applied)
			{
				// give back what was there, unless something else replaced the sky meanwhile
				if (RenderSettings.skybox == this._material)
				{
					RenderSettings.skybox = this._previousSkybox;
					RenderSettings.ambientMode = this._previousAmbient;
					RenderSettings.defaultReflectionMode = this._previousReflection;
					refresh = true;
				}
				this._applied = false;
			}
			if (this.On == false)
				return refresh;

			this._time += Time.unscaledDeltaTime * this.CloudSpeed * 0.05f;
			this._material.SetFloat(_oTimeId, this._time);
			if (cam != null && hideSkybox == false)
				cam.clearFlags = CameraClearFlags.Skybox;

			int hash = this.SettingsHash();
			if (hash != this._shownHash)
			{
				this._shownHash = hash;
				this.ApplySettings(); // the sky itself updates at once
				this._refreshPending = true; // lighting follows setting changes only, never the moving clouds
			}
			if (this._refreshPending && Time.unscaledTime - this._lastRefresh >= RefreshInterval)
			{
				this._refreshPending = false;
				this._lastRefresh = Time.unscaledTime;
				refresh = true;
			}
			return refresh;
		}

		/// <summary>Backdrop: only the camera's own Skybox component shows this sky, RenderSettings are never touched.</summary>
		private void TickBackdrop(Camera cam, bool hideSkybox)
		{
			bool show = this.On && hideSkybox == false && cam != null;
			if (this._camSkybox != null && (show == false || this._camSkybox.gameObject != cam.gameObject))
				this.ReleaseCamSkybox();
			if (show == false)
				return;
			Material m = this.Material();
			if (m == null)
			{
				this.On = false;
				return;
			}
			if (this._camSkybox == null)
			{
				this._camSkybox = cam.GetComponent<Skybox>();
				this._camSkyboxAdded = this._camSkybox == null;
				if (this._camSkyboxAdded)
					this._camSkybox = cam.gameObject.AddComponent<Skybox>();
				this._shownHash = 0;
			}
			if (this._camSkybox.material != m)
				this._camSkybox.material = m;
			if (this._camSkybox.enabled == false)
				this._camSkybox.enabled = true;
			cam.clearFlags = CameraClearFlags.Skybox;

			this._time += Time.unscaledDeltaTime * this.CloudSpeed * 0.05f;
			m.SetFloat(_oTimeId, this._time);
			int hash = this.SettingsHash();
			if (hash != this._shownHash)
			{
				this._shownHash = hash;
				this.ApplySettings();
			}
		}

		private void ReleaseCamSkybox()
		{
			if (this._camSkybox != null)
			{
				if (this._camSkyboxAdded)
					Object.Destroy(this._camSkybox);
				else if (this._camSkybox.material == this._material)
					this._camSkybox.material = null;
			}
			this._camSkybox = null;
			this._camSkyboxAdded = false;
		}

		private Material Material()
		{
			if (this._material == null)
			{
				Shader s = OverlayShaders.Get("Sky");
				if (s == null)
					return null;
				this._material = new Material(s) { hideFlags = HideFlags.DontSave };
			}
			return this._material;
		}

		private void ApplySettings()
		{
			Palette p = _palettes[this.Preset];
			Material m = this._material;
			m.SetColor("_Zenith", p.Zenith);
			m.SetColor("_Horizon", p.Horizon);
			m.SetColor("_Ground", p.Ground);
			m.SetColor("_SunColor", p.Sun);
			m.SetColor("_CloudLight", p.CloudLight);
			m.SetColor("_CloudShadow", p.CloudShadow);
			m.SetFloat("_SunSize", p.SunSize);
			m.SetFloat("_Stars", p.Stars);
			m.SetFloat("_Coverage", this.Coverage);
			m.SetFloat("_CloudOpacity", this.Preset == 3 ? 0.8f : 1f);
			m.SetFloat("_Exposure", this.Exposure);
			float el = this.SunHeight * Mathf.Deg2Rad, az = this.SunAzimuth * Mathf.Deg2Rad;
			m.SetVector("_SunDir", new Vector4(Mathf.Cos(el) * Mathf.Sin(az), Mathf.Sin(el), Mathf.Cos(el) * Mathf.Cos(az), 0f));
			float wa = this.WindAngle * Mathf.Deg2Rad;
			m.SetVector("_Wind", new Vector4(Mathf.Cos(wa), Mathf.Sin(wa), 0f, 0f));
		}

		private int SettingsHash()
		{
			unchecked
			{
				int h = 17;
				h = h * 31 + this.Preset;
				h = h * 31 + this.Coverage.GetHashCode();
				h = h * 31 + this.WindAngle.GetHashCode();
				h = h * 31 + this.SunHeight.GetHashCode();
				h = h * 31 + this.SunAzimuth.GetHashCode();
				h = h * 31 + this.Exposure.GetHashCode();
				return h == 0 ? 1 : h;
			}
		}

		// ------------------------------------------------------------------ save / load

		internal void Save(XmlWriter w)
		{
			w.WriteStartElement(this._element);
			w.WriteAttributeString("on", XmlConvert.ToString(this.On));
			w.WriteAttributeString("preset", XmlConvert.ToString(this.Preset));
			w.WriteAttributeString("coverage", XmlConvert.ToString(this.Coverage));
			w.WriteAttributeString("cloudSpeed", XmlConvert.ToString(this.CloudSpeed));
			w.WriteAttributeString("windAngle", XmlConvert.ToString(this.WindAngle));
			w.WriteAttributeString("sunHeight", XmlConvert.ToString(this.SunHeight));
			w.WriteAttributeString("sunAzimuth", XmlConvert.ToString(this.SunAzimuth));
			w.WriteAttributeString("exposure", XmlConvert.ToString(this.Exposure));
			w.WriteEndElement();
		}

		internal void Load(XmlNode n)
		{
			this.SetPreset(I(n, "preset", 0));
			this.On = B(n, "on", false);
			this.Coverage = F(n, "coverage", this.Coverage);
			this.CloudSpeed = F(n, "cloudSpeed", this.CloudSpeed);
			this.WindAngle = F(n, "windAngle", this.WindAngle);
			this.SunHeight = F(n, "sunHeight", this.SunHeight);
			this.SunAzimuth = F(n, "sunAzimuth", this.SunAzimuth);
			this.Exposure = F(n, "exposure", this.Exposure);
		}

		private static string A(XmlNode n, string name) => n.Attributes != null && n.Attributes[name] != null ? n.Attributes[name].Value : null;

		private static bool B(XmlNode n, string name, bool fallback) { string s = A(n, name); bool v; return s != null && bool.TryParse(s, out v) ? v : fallback; }

		private static int I(XmlNode n, string name, int fallback) { string s = A(n, name); try { return s != null ? XmlConvert.ToInt32(s) : fallback; } catch { return fallback; } }

		private static float F(XmlNode n, string name, float fallback) { string s = A(n, name); try { return s != null ? XmlConvert.ToSingle(s) : fallback; } catch { return fallback; } }
	}
}
