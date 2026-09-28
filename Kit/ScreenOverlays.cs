using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace HSIBL
{
	/// <summary>
	/// Screen overlays (addition): film grain, light leaks, CRT, halftone, paper, glitch, VHS and letterbox,
	/// drawn by one camera component after HSLRE's whole effect stack. Each enabled overlay is one full-screen
	/// pass; with none enabled the component is switched off and costs nothing.
	/// </summary>
	internal sealed class ScreenOverlays
	{
		internal bool Freeze;

		internal bool GrainOn, GrainColored;
		internal float GrainStrength, GrainSize, GrainDust, GrainScratches;

		internal bool LeaksOn;
		internal float LeaksStrength, LeaksSpeed, LeaksSeed;
		internal Color LeaksColor;

		internal bool CrtOn;
		internal float CrtScanlines, CrtDensity, CrtMask, CrtCurvature;

		internal bool HalftoneOn, HalftoneColored;
		internal float HalftoneSize, HalftoneAngle, HalftoneMix;

		internal bool PaperOn;
		internal int PaperType;
		internal float PaperStrength, PaperScale;

		internal bool GlitchOn;
		internal float GlitchSplit, GlitchFrequency, GlitchBurst;

		internal bool VhsOn;
		internal float VhsBleed, VhsNoise, VhsWobble;

		internal bool LetterboxOn;
		internal int LetterboxPreset;
		internal float LetterboxCustom, LetterboxOpacity;
		internal Color LetterboxColor;

		internal static readonly string[] PaperTypes = { "Paper", "Canvas" };
		internal static readonly string[] LetterboxPresets = { "2.39:1", "2:1", "1.85:1", "4:3", "Custom" };
		private static readonly float[] _letterboxAspects = { 2.39f, 2f, 1.85f, 4f / 3f };

		private OverlayRenderer _renderer;

		internal ScreenOverlays()
		{
			this.Reset();
		}

		internal bool AnyOn => this.GrainOn || this.LeaksOn || this.CrtOn || this.HalftoneOn || this.PaperOn || this.GlitchOn || this.VhsOn || this.LetterboxOn;

		internal float LetterboxAspect => this.LetterboxPreset >= 0 && this.LetterboxPreset < _letterboxAspects.Length ? _letterboxAspects[this.LetterboxPreset] : Mathf.Max(0.5f, this.LetterboxCustom);

		/// <summary>Everything off, every setting back to its default.</summary>
		internal void Reset()
		{
			this.Freeze = false;
			this.GrainOn = false; this.GrainColored = false; this.GrainStrength = 0.25f; this.GrainSize = 1.5f; this.GrainDust = 0.3f; this.GrainScratches = 0.2f;
			this.LeaksOn = false; this.LeaksStrength = 0.5f; this.LeaksSpeed = 0.2f; this.LeaksSeed = 1f; this.LeaksColor = new Color(1f, 0.55f, 0.25f);
			this.CrtOn = false; this.CrtScanlines = 0.5f; this.CrtDensity = 360f; this.CrtMask = 0.3f; this.CrtCurvature = 0.2f;
			this.HalftoneOn = false; this.HalftoneColored = true; this.HalftoneSize = 8f; this.HalftoneAngle = 45f; this.HalftoneMix = 1f;
			this.PaperOn = false; this.PaperType = 0; this.PaperStrength = 0.5f; this.PaperScale = 1f;
			this.GlitchOn = false; this.GlitchSplit = 4f; this.GlitchFrequency = 0.2f; this.GlitchBurst = 0.5f;
			this.VhsOn = false; this.VhsBleed = 0.5f; this.VhsNoise = 0.3f; this.VhsWobble = 0.3f;
			this.LetterboxOn = false; this.LetterboxPreset = 0; this.LetterboxCustom = 2.39f; this.LetterboxOpacity = 1f; this.LetterboxColor = Color.black;
		}

		/// <summary>Every frame: the component exists and runs only while an overlay is on.</summary>
		internal void Tick(Camera cam)
		{
			bool on = this.AnyOn;
			if (on && cam != null && (this._renderer == null || this._renderer.gameObject != cam.gameObject))
			{
				// added the first time an overlay is used, so it sits after the effects other plugins added at startup
				if (this._renderer != null)
					this._renderer.enabled = false; // the camera changed: the old one stops drawing overlays
				this._renderer = cam.gameObject.GetComponent<OverlayRenderer>() ?? cam.gameObject.AddComponent<OverlayRenderer>();
				this._renderer.Settings = this;
			}
			if (this._renderer != null && this._renderer.enabled != on)
				this._renderer.enabled = on;
		}

		// ------------------------------------------------------------------ save / load

		internal void Save(XmlWriter w)
		{
			w.WriteStartElement("screenOverlays");
			W(w, "freeze", this.Freeze);
			W(w, "grain", this.GrainOn); W(w, "grainColored", this.GrainColored); W(w, "grainStrength", this.GrainStrength); W(w, "grainSize", this.GrainSize); W(w, "grainDust", this.GrainDust); W(w, "grainScratches", this.GrainScratches);
			W(w, "leaks", this.LeaksOn); W(w, "leaksStrength", this.LeaksStrength); W(w, "leaksSpeed", this.LeaksSpeed); W(w, "leaksSeed", this.LeaksSeed); W(w, "leaksColor", this.LeaksColor);
			W(w, "crt", this.CrtOn); W(w, "crtScanlines", this.CrtScanlines); W(w, "crtDensity", this.CrtDensity); W(w, "crtMask", this.CrtMask); W(w, "crtCurvature", this.CrtCurvature);
			W(w, "halftone", this.HalftoneOn); W(w, "halftoneColored", this.HalftoneColored); W(w, "halftoneSize", this.HalftoneSize); W(w, "halftoneAngle", this.HalftoneAngle); W(w, "halftoneMix", this.HalftoneMix);
			W(w, "paper", this.PaperOn); W(w, "paperType", this.PaperType); W(w, "paperStrength", this.PaperStrength); W(w, "paperScale", this.PaperScale);
			W(w, "glitch", this.GlitchOn); W(w, "glitchSplit", this.GlitchSplit); W(w, "glitchFrequency", this.GlitchFrequency); W(w, "glitchBurst", this.GlitchBurst);
			W(w, "vhs", this.VhsOn); W(w, "vhsBleed", this.VhsBleed); W(w, "vhsNoise", this.VhsNoise); W(w, "vhsWobble", this.VhsWobble);
			W(w, "letterbox", this.LetterboxOn); W(w, "letterboxPreset", this.LetterboxPreset); W(w, "letterboxCustom", this.LetterboxCustom); W(w, "letterboxOpacity", this.LetterboxOpacity); W(w, "letterboxColor", this.LetterboxColor);
			w.WriteEndElement();
		}

		internal void Load(XmlNode n)
		{
			this.Reset();
			this.Freeze = B(n, "freeze", this.Freeze);
			this.GrainOn = B(n, "grain", this.GrainOn); this.GrainColored = B(n, "grainColored", this.GrainColored); this.GrainStrength = F(n, "grainStrength", this.GrainStrength); this.GrainSize = F(n, "grainSize", this.GrainSize); this.GrainDust = F(n, "grainDust", this.GrainDust); this.GrainScratches = F(n, "grainScratches", this.GrainScratches);
			this.LeaksOn = B(n, "leaks", this.LeaksOn); this.LeaksStrength = F(n, "leaksStrength", this.LeaksStrength); this.LeaksSpeed = F(n, "leaksSpeed", this.LeaksSpeed); this.LeaksSeed = F(n, "leaksSeed", this.LeaksSeed); this.LeaksColor = C(n, "leaksColor", this.LeaksColor);
			this.CrtOn = B(n, "crt", this.CrtOn); this.CrtScanlines = F(n, "crtScanlines", this.CrtScanlines); this.CrtDensity = F(n, "crtDensity", this.CrtDensity); this.CrtMask = F(n, "crtMask", this.CrtMask); this.CrtCurvature = F(n, "crtCurvature", this.CrtCurvature);
			this.HalftoneOn = B(n, "halftone", this.HalftoneOn); this.HalftoneColored = B(n, "halftoneColored", this.HalftoneColored); this.HalftoneSize = F(n, "halftoneSize", this.HalftoneSize); this.HalftoneAngle = F(n, "halftoneAngle", this.HalftoneAngle); this.HalftoneMix = F(n, "halftoneMix", this.HalftoneMix);
			this.PaperOn = B(n, "paper", this.PaperOn); this.PaperType = I(n, "paperType", this.PaperType); this.PaperStrength = F(n, "paperStrength", this.PaperStrength); this.PaperScale = F(n, "paperScale", this.PaperScale);
			this.GlitchOn = B(n, "glitch", this.GlitchOn); this.GlitchSplit = F(n, "glitchSplit", this.GlitchSplit); this.GlitchFrequency = F(n, "glitchFrequency", this.GlitchFrequency); this.GlitchBurst = F(n, "glitchBurst", this.GlitchBurst);
			this.VhsOn = B(n, "vhs", this.VhsOn); this.VhsBleed = F(n, "vhsBleed", this.VhsBleed); this.VhsNoise = F(n, "vhsNoise", this.VhsNoise); this.VhsWobble = F(n, "vhsWobble", this.VhsWobble);
			this.LetterboxOn = B(n, "letterbox", this.LetterboxOn); this.LetterboxPreset = I(n, "letterboxPreset", this.LetterboxPreset); this.LetterboxCustom = F(n, "letterboxCustom", this.LetterboxCustom); this.LetterboxOpacity = F(n, "letterboxOpacity", this.LetterboxOpacity); this.LetterboxColor = C(n, "letterboxColor", this.LetterboxColor);
		}

		private static void W(XmlWriter w, string name, bool v) { w.WriteAttributeString(name, XmlConvert.ToString(v)); }
		private static void W(XmlWriter w, string name, int v) { w.WriteAttributeString(name, XmlConvert.ToString(v)); }
		private static void W(XmlWriter w, string name, float v) { w.WriteAttributeString(name, XmlConvert.ToString(v)); }

		private static void W(XmlWriter w, string name, Color v)
		{
			w.WriteAttributeString(name, XmlConvert.ToString(v.r) + "," + XmlConvert.ToString(v.g) + "," + XmlConvert.ToString(v.b));
		}

		private static string A(XmlNode n, string name) => n.Attributes != null && n.Attributes[name] != null ? n.Attributes[name].Value : null;

		private static bool B(XmlNode n, string name, bool fallback) { string s = A(n, name); bool v; return s != null && bool.TryParse(s, out v) ? v : fallback; }

		private static int I(XmlNode n, string name, int fallback) { string s = A(n, name); try { return s != null ? XmlConvert.ToInt32(s) : fallback; } catch { return fallback; } }

		private static float F(XmlNode n, string name, float fallback) { string s = A(n, name); try { return s != null ? XmlConvert.ToSingle(s) : fallback; } catch { return fallback; } }

		private static Color C(XmlNode n, string name, Color fallback)
		{
			string s = A(n, name);
			if (s == null)
				return fallback;
			string[] p = s.Split(',');
			try
			{
				return p.Length == 3 ? new Color(XmlConvert.ToSingle(p[0]), XmlConvert.ToSingle(p[1]), XmlConvert.ToSingle(p[2])) : fallback;
			}
			catch
			{
				return fallback;
			}
		}

		// ------------------------------------------------------------------ rendering

		private sealed class OverlayRenderer : MonoBehaviour
		{
			internal ScreenOverlays Settings;
			private readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();
			private readonly List<Material> _passes = new List<Material>(8);
			private float _time;

			private void Update()
			{
				if (this.Settings != null && this.Settings.Freeze == false)
					this._time += Time.unscaledDeltaTime;
			}

			private Material Mat(string name)
			{
				Material m;
				if (this._materials.TryGetValue(name, out m))
					return m;
				Shader s = OverlayShaders.Get(name);
				m = s != null ? new Material(s) { hideFlags = HideFlags.DontSave } : null;
				this._materials[name] = m;
				return m;
			}

			// the overlay being set up; null when its shader is missing, so its settings go nowhere
			private Material _current;

			private void Add(string name)
			{
				this._current = this.Mat(name);
				if (this._current == null)
					return;
				this._current.SetFloat("_OTime", this._time);
				this._passes.Add(this._current);
			}

			private void OnRenderImage(RenderTexture source, RenderTexture destination)
			{
				ScreenOverlays s = this.Settings;
				this._passes.Clear();
				if (s != null)
				{
					// fixed order: what the overlay imitates, from the picture itself outwards to the frame
					if (s.HalftoneOn) { this.Add("Halftone"); this.Set("_DotSize", s.HalftoneSize); this.Set("_Angle", s.HalftoneAngle); this.Set("_Colored", s.HalftoneColored ? 1f : 0f); this.Set("_Mix", s.HalftoneMix); }
					if (s.CrtOn) { this.Add("CRT"); this.Set("_Scanlines", s.CrtScanlines); this.Set("_Density", s.CrtDensity); this.Set("_Mask", s.CrtMask); this.Set("_Curvature", s.CrtCurvature); }
					if (s.VhsOn) { this.Add("VHS"); this.Set("_Bleed", s.VhsBleed); this.Set("_Noise", s.VhsNoise); this.Set("_Wobble", s.VhsWobble); }
					if (s.GlitchOn) { this.Add("Glitch"); this.Set("_Split", s.GlitchSplit); this.Set("_Frequency", s.GlitchFrequency); this.Set("_Burst", s.GlitchBurst); }
					if (s.LeaksOn) { this.Add("LightLeaks"); this.Set("_Strength", s.LeaksStrength); this.Set("_Speed", s.LeaksSpeed); this.Set("_Seed", s.LeaksSeed); this.SetColor("_LeakColor", s.LeaksColor); }
					if (s.GrainOn) { this.Add("Grain"); this.Set("_Strength", s.GrainStrength); this.Set("_Size", s.GrainSize); this.Set("_Colored", s.GrainColored ? 1f : 0f); this.Set("_Dust", s.GrainDust); this.Set("_Scratches", s.GrainScratches); }
					if (s.PaperOn) { this.Add("Paper"); this.Set("_Type", s.PaperType); this.Set("_Strength", s.PaperStrength); this.Set("_Scale", s.PaperScale); }
					if (s.LetterboxOn) { this.Add("Letterbox"); this.Set("_Aspect", s.LetterboxAspect); this.Set("_Opacity", s.LetterboxOpacity); this.SetColor("_BarColor", s.LetterboxColor); }
				}
				if (this._passes.Count == 0)
				{
					Graphics.Blit(source, destination);
					return;
				}
				RenderTexture current = source, temp = null;
				for (int i = 0; i < this._passes.Count; i++)
				{
					bool last = i == this._passes.Count - 1;
					RenderTexture target = last ? destination : RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
					Graphics.Blit(current, target, this._passes[i]);
					if (temp != null)
						RenderTexture.ReleaseTemporary(temp);
					temp = last ? null : target;
					current = target;
				}
			}

			private void Set(string property, float value)
			{
				if (this._current != null)
					this._current.SetFloat(property, value);
			}

			private void SetColor(string property, Color value)
			{
				if (this._current != null)
					this._current.SetColor(property, value);
			}

			private void OnDestroy()
			{
				foreach (Material m in this._materials.Values)
					if (m != null)
						Destroy(m);
				this._materials.Clear();
			}
		}
	}
}
