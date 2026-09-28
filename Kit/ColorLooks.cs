using System;
using System.Collections.Generic;
using System.IO;
using HSLRE.CustomEffects;
using UnityEngine;

namespace HSIBL
{
	/// <summary>
	/// Color Looks (addition): named color grades in the style of Koikatsu's "Shade" list. Two looks, each
	/// with a strength, are baked into one strip LUT that goes into the Tonemapping &amp; Color Grading
	/// effect's custom LUT slot (userLut) (applied in its final pass in gamma space, whether or not tonemapping itself
	/// is on). Built-in looks are generated here; LUT images in UserData\LUT\ (the folder HSLRE's config
	/// already reads LUTs from) are listed after them.
	/// </summary>
	internal sealed class ColorLooks
	{
		// A look maps a gamma-space color to a graded one. A plain delegate, not Func&lt;Color, Color&gt;:
		// generic delegates over Unity value types can break assembly loading on the game's Mono.
		private delegate Color LookFunc(Color c);

		private sealed class Look
		{
			internal string Name;
			internal LookFunc Apply;
		}

		// 64 steps with point filtering. With bilinear filtering the shader's strip lookup blends neighbouring
		// slices at their edges, which showed as specks (cyan in bright reds, red dots in darks) even with an
		// identity LUT; point filtering reads exactly one texel, and 64 steps keep that from showing as banding.
		private const int Size = 64;
		private const int Count = Size * Size * Size;
		internal const string NoneName = "None";
		private static readonly string _lutFolder = Path.Combine(Application.dataPath, "../UserData/LUT/");

		private readonly List<Look> _looks = new List<Look>();
		internal string[] Names { get; private set; }

		internal int Look1, Look2;
		internal float Strength1 = 1f, Strength2 = 1f;

		private Texture2D _baked;
		private Color32[] _pixels;
		// each look's result on the LUT grid, computed once per pick; strength changes only blend
		private Color32[] _table1, _table2;
		private int _table1Look = -1, _table2Look = -1;
		private int _bakedLook1 = -1, _bakedLook2 = -1;
		private float _bakedStrength1 = -1f, _bakedStrength2 = -1f;
		private bool _wroteLut;
		private TonemappingColorGrading.LUTSettings _configLut;

		internal ColorLooks()
		{
			this.Reload();
		}

		internal bool Active => (this.Look1 > 0 && this.Strength1 > 0f) || (this.Look2 > 0 && this.Strength2 > 0f);

		internal static string LutFolder => Path.GetFullPath(_lutFolder);

		/// <summary>Rebuilds the list: built-in looks, then the LUT images found in UserData\LUT\. Keeps the current picks by name.</summary>
		internal void Reload()
		{
			string name1 = this.NameOf(this.Look1), name2 = this.NameOf(this.Look2);
			this._looks.Clear();
			AddBuiltIns(this._looks);
			try
			{
				if (Directory.Exists(_lutFolder))
					foreach (string file in Directory.GetFiles(_lutFolder, "*.png"))
					{
						LookFunc f = LoadLutFile(file);
						if (f != null)
							this._looks.Add(new Look { Name = Path.GetFileNameWithoutExtension(file), Apply = f });
					}
			}
			catch (Exception e)
			{
				Debug.LogError("HSLRE: could not read the LUT folder.\n" + e);
			}
			string[] names = new string[this._looks.Count + 1];
			names[0] = NoneName;
			for (int i = 0; i < this._looks.Count; i++)
				names[i + 1] = this._looks[i].Name;
			this.Names = names;
			this.Look1 = this.IndexOf(name1);
			this.Look2 = this.IndexOf(name2);
			this._bakedLook1 = this._bakedLook2 = -1;
			this._table1Look = this._table2Look = -1;
		}

		internal string NameOf(int index)
		{
			return this.Names != null && index > 0 && index < this.Names.Length ? this.Names[index] : NoneName;
		}

		internal int IndexOf(string name)
		{
			if (this.Names == null || string.IsNullOrEmpty(name))
				return 0;
			for (int i = 1; i < this.Names.Length; i++)
				if (string.Equals(this.Names[i], name, StringComparison.OrdinalIgnoreCase))
					return i;
			return 0;
		}

		internal void Reset()
		{
			this.Look1 = this.Look2 = 0;
			this.Strength1 = this.Strength2 = 1f;
		}

		/// <summary>Bakes when something changed and hands the result to the tonemapping effect's LUT slot.</summary>
		internal void Apply(TonemappingColorGrading tonemapping)
		{
			if (tonemapping == null)
				return;
			if (this.Active == false)
			{
				// give the slot back to whatever HSLRE's own config put there
				if (this._wroteLut)
				{
					tonemapping.lut = this._configLut;
					this._wroteLut = false;
				}
				return;
			}
			if (this._wroteLut == false)
				this._configLut = tonemapping.lut;
			if (this._baked == null || this.Look1 != this._bakedLook1 || this.Look2 != this._bakedLook2
				|| this.Strength1 != this._bakedStrength1 || this.Strength2 != this._bakedStrength2)
				this.Bake();
			if (this._wroteLut == false || ReferenceEquals(tonemapping.lut.texture, this._baked) == false || tonemapping.lut.enabled == false)
			{
				tonemapping.lut = new TonemappingColorGrading.LUTSettings { enabled = true, texture = this._baked, contribution = 1f };
				this._wroteLut = true;
			}
		}

		private void Bake()
		{
			if (this._baked == null)
			{
				// linear: the shader reads the values as they are (it does the gamma conversion itself)
				this._baked = new Texture2D(Size * Size, Size, TextureFormat.RGB24, false, true);
				this._baked.filterMode = FilterMode.Point;
				this._baked.wrapMode = TextureWrapMode.Clamp;
				this._baked.hideFlags = HideFlags.DontSave;
				this._pixels = new Color32[Count];
			}
			float sa = this.Look1 > 0 ? Mathf.Clamp01(this.Strength1) : 0f;
			float sb = this.Look2 > 0 ? Mathf.Clamp01(this.Strength2) : 0f;
			if (sa > 0f)
				this.EnsureTable(ref this._table1, ref this._table1Look, this.Look1);
			if (sb > 0f)
				this.EnsureTable(ref this._table2, ref this._table2Look, this.Look2);
			// The second look is applied to the result of the first. Its cached table only fits while the first
			// look leaves the colors unchanged; otherwise it runs on the blended color.
			LookFunc second = sb > 0f && sa > 0f ? this._looks[this.Look2 - 1].Apply : null;
			float step = 1f / (Size - 1);
			int i = 0;
			for (int g = 0; g < Size; g++)
				for (int bl = 0; bl < Size; bl++)
					for (int r = 0; r < Size; r++, i++)
					{
						// strip layout: x = red + blue slice * size, y = green (row 0 at the bottom); i walks it in order
						Color c = new Color(r * step, g * step, bl * step, 1f);
						if (sa > 0f)
							c = Color.Lerp(c, this._table1[i], sa);
						if (sb > 0f)
							c = Color.Lerp(c, second != null ? Clamp(second(c)) : (Color)this._table2[i], sb);
						this._pixels[i] = c;
					}
			this._baked.SetPixels32(this._pixels);
			this._baked.Apply(false);
			this._bakedLook1 = this.Look1;
			this._bakedLook2 = this.Look2;
			this._bakedStrength1 = this.Strength1;
			this._bakedStrength2 = this.Strength2;
		}

		private void EnsureTable(ref Color32[] table, ref int tableLook, int look)
		{
			if (table != null && tableLook == look)
				return;
			if (table == null)
				table = new Color32[Count];
			LookFunc f = this._looks[look - 1].Apply;
			float step = 1f / (Size - 1);
			int i = 0;
			for (int g = 0; g < Size; g++)
				for (int bl = 0; bl < Size; bl++)
					for (int r = 0; r < Size; r++, i++)
						table[i] = Clamp(f(new Color(r * step, g * step, bl * step, 1f)));
			tableLook = look;
		}

		// ------------------------------------------------------------------ LUT files

		/// <summary>Reads a strip LUT (width = height²) into a trilinear lookup. Null when the image isn't one.</summary>
		private static LookFunc LoadLutFile(string path)
		{
			Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false, true);
			try
			{
				if (tex.LoadImage(File.ReadAllBytes(path)) == false)
					return null;
				int size = tex.height;
				if (size < 2 || tex.width != size * size)
				{
					Debug.LogWarning("HSLRE: " + Path.GetFileName(path) + " is not a strip LUT (width must be height squared, e.g. 1024x32), skipped.");
					return null;
				}
				Color[] src = tex.GetPixels();
				// Images are stored with green growing either up or down. Black (0,0,0) is the darkest corner
				// of the first slice in any sane grade, so flip if the top row's first texel is the darker one.
				Color bottom = src[0], top = src[(size - 1) * tex.width];
				bool flip = Luma(top) < Luma(bottom);
				float[] data = new float[size * size * size * 3];
				for (int y = 0; y < size; y++)
				{
					int row = flip ? size - 1 - y : y;
					for (int x = 0; x < tex.width; x++)
					{
						Color p = src[row * tex.width + x];
						int i = (y * tex.width + x) * 3;
						data[i] = p.r;
						data[i + 1] = p.g;
						data[i + 2] = p.b;
					}
				}
				return c => SampleStrip(data, size, c);
			}
			catch (Exception e)
			{
				Debug.LogWarning("HSLRE: could not load LUT " + Path.GetFileName(path) + ": " + e.Message);
				return null;
			}
			finally
			{
				UnityEngine.Object.Destroy(tex);
			}
		}

		private static Color SampleStrip(float[] data, int size, Color c)
		{
			float fr = Mathf.Clamp01(c.r) * (size - 1), fg = Mathf.Clamp01(c.g) * (size - 1), fb = Mathf.Clamp01(c.b) * (size - 1);
			int r0 = (int)fr, g0 = (int)fg, b0 = (int)fb;
			int r1 = Mathf.Min(r0 + 1, size - 1), g1 = Mathf.Min(g0 + 1, size - 1), b1 = Mathf.Min(b0 + 1, size - 1);
			float tr = fr - r0, tg = fg - g0, tb = fb - b0;
			Color c00 = Color.Lerp(Texel(data, size, r0, g0, b0), Texel(data, size, r1, g0, b0), tr);
			Color c10 = Color.Lerp(Texel(data, size, r0, g1, b0), Texel(data, size, r1, g1, b0), tr);
			Color c01 = Color.Lerp(Texel(data, size, r0, g0, b1), Texel(data, size, r1, g0, b1), tr);
			Color c11 = Color.Lerp(Texel(data, size, r0, g1, b1), Texel(data, size, r1, g1, b1), tr);
			return Color.Lerp(Color.Lerp(c00, c10, tg), Color.Lerp(c01, c11, tg), tb);
		}

		private static Color Texel(float[] data, int size, int r, int g, int b)
		{
			int i = (g * size * size + b * size + r) * 3;
			return new Color(data[i], data[i + 1], data[i + 2], 1f);
		}

		// ------------------------------------------------------------------ built-in looks

		private static void AddBuiltIns(List<Look> looks)
		{
			// Koikatsu's list, recreated by eye (not their LUT files)
			Add(looks, "Midday", c => Saturate(Contrast(Mul(c, 1.03f, 1.01f, 0.96f), 1.06f), 1.08f));
			Add(looks, "Evening", c => Lift(Saturate(Mul(Gamma(c, 1.08f), 1.1f, 0.9f, 0.72f), 1.1f), 0.02f));
			Add(looks, "Night", c => Mul(Gamma(Saturate(c, 0.45f), 1.25f), 0.6f, 0.72f, 1.0f));
			Add(looks, "Live Concert", c => Saturate(Contrast(Mul(c, 1.06f, 0.86f, 1.12f), 1.28f), 1.45f));
			Add(looks, "Vivid", c => Saturate(Contrast(c, 1.12f), 1.55f));
			Add(looks, "Nostalgia", c => Mul(Lift(Contrast(Saturate(c, 0.65f), 0.88f), 0.07f), 1.05f, 1.0f, 0.86f));
			Add(looks, "Retro", c => SplitTone(Lift(Saturate(c, 0.75f), 0.05f), new Color(0.2f, 0.45f, 0.42f), new Color(1f, 0.85f, 0.55f), 0.22f));
			Add(looks, "Flat Color", c => Saturate(Posterize(c, 5f), 1.1f));
			Add(looks, "False Color", c => Heat(Luma(c)));
			Add(looks, "Comics", c => Saturate(Contrast(Posterize(c, 4f), 1.2f), 1.4f));
			Add(looks, "Monochrome", c => Gray(Luma(c)));
			Add(looks, "Sepia", c => Sepia(c));
			Add(looks, "Reverse Monochrome", c => Gray(1f - Luma(c)));
			Add(looks, "Reverse Color", c => new Color(1f - c.r, 1f - c.g, 1f - c.b));
			Add(looks, "Red Accents", c => Accent(c, 0f));
			Add(looks, "Blue Accents", c => Accent(c, 0.62f));
			Add(looks, "Green Accents", c => Accent(c, 0.33f));
			// extras
			Add(looks, "Teal & Orange", c => SplitTone(Saturate(c, 1.1f), new Color(0.0f, 0.55f, 0.6f), new Color(1f, 0.6f, 0.25f), 0.3f));
			Add(looks, "Bleach Bypass", c => Contrast(Color.Lerp(c, Overlay(Gray(Luma(c)), c), 0.7f), 1.15f));
			Add(looks, "Cross Process", c => new Color(SCurve(c.r, 1.4f), SCurve(c.g, 1.2f) * 0.97f + 0.03f, Mathf.Lerp(0.12f, 0.88f, c.b)));
			Add(looks, "Warm", c => Mul(c, 1.07f, 1.0f, 0.88f));
			Add(looks, "Cool", c => Mul(c, 0.9f, 1.0f, 1.1f));
			Add(looks, "Faded Film", c => Mul(Lift(Contrast(Saturate(c, 0.78f), 0.85f), 0.09f), 0.98f, 1.02f, 0.97f));
			Add(looks, "Noir", c => Gray(SCurve(Luma(c), 1.9f)));
		}

		private static void Add(List<Look> looks, string name, LookFunc f)
		{
			looks.Add(new Look { Name = name, Apply = f });
		}

		private static float Luma(Color c) => c.r * 0.2126f + c.g * 0.7152f + c.b * 0.0722f;

		private static Color Gray(float v) => new Color(v, v, v);

		private static Color Clamp(Color c) => new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), 1f);

		private static Color Mul(Color c, float r, float g, float b) => new Color(c.r * r, c.g * g, c.b * b);

		private static Color Saturate(Color c, float s)
		{
			float l = Luma(c);
			return new Color(l + (c.r - l) * s, l + (c.g - l) * s, l + (c.b - l) * s);
		}

		private static Color Contrast(Color c, float k) => new Color((c.r - 0.5f) * k + 0.5f, (c.g - 0.5f) * k + 0.5f, (c.b - 0.5f) * k + 0.5f);

		/// <summary>Raises the blacks: 0 maps to amount, 1 stays 1.</summary>
		private static Color Lift(Color c, float amount) => new Color(amount + c.r * (1f - amount), amount + c.g * (1f - amount), amount + c.b * (1f - amount));

		private static Color Gamma(Color c, float g) => new Color(Mathf.Pow(Mathf.Max(c.r, 0f), g), Mathf.Pow(Mathf.Max(c.g, 0f), g), Mathf.Pow(Mathf.Max(c.b, 0f), g));

		private static Color Posterize(Color c, float levels) => new Color(Mathf.Round(c.r * (levels - 1f)) / (levels - 1f), Mathf.Round(c.g * (levels - 1f)) / (levels - 1f), Mathf.Round(c.b * (levels - 1f)) / (levels - 1f));

		/// <summary>S-shaped contrast: strength 1 = unchanged, 2 = a full smoothstep, more = steeper.</summary>
		private static float SCurve(float v, float strength)
		{
			float x = Mathf.Clamp01(v);
			float s = x * x * (3f - 2f * x);
			return Mathf.Clamp01(Mathf.LerpUnclamped(x, s, strength - 1f));
		}

		private static float OverlayChannel(float a, float b) => a < 0.5f ? 2f * a * b : 1f - 2f * (1f - a) * (1f - b);

		private static Color Overlay(Color a, Color b) => new Color(OverlayChannel(a.r, b.r), OverlayChannel(a.g, b.g), OverlayChannel(a.b, b.b));

		/// <summary>Tints the shadows toward one color and the highlights toward another.</summary>
		private static Color SplitTone(Color c, Color shadows, Color highlights, float amount)
		{
			float l = Luma(c);
			Color tint = Color.Lerp(shadows, highlights, Mathf.SmoothStep(0.15f, 0.85f, l));
			Color toned = Overlay(c, tint);
			return Color.Lerp(c, toned, amount * 2f);
		}

		private static Color Sepia(Color c)
		{
			return new Color(
				c.r * 0.393f + c.g * 0.769f + c.b * 0.189f,
				c.r * 0.349f + c.g * 0.686f + c.b * 0.168f,
				c.r * 0.272f + c.g * 0.534f + c.b * 0.131f);
		}

		/// <summary>Luminance on a blue - cyan - green - yellow - red ramp.</summary>
		private static Color Heat(float v)
		{
			v = Mathf.Clamp01(v);
			if (v < 0.25f) return Color.Lerp(new Color(0.05f, 0.05f, 0.5f), new Color(0f, 0.6f, 1f), v / 0.25f);
			if (v < 0.5f) return Color.Lerp(new Color(0f, 0.6f, 1f), new Color(0.1f, 0.9f, 0.2f), (v - 0.25f) / 0.25f);
			if (v < 0.75f) return Color.Lerp(new Color(0.1f, 0.9f, 0.2f), new Color(1f, 0.9f, 0f), (v - 0.5f) / 0.25f);
			return Color.Lerp(new Color(1f, 0.9f, 0f), new Color(1f, 0.15f, 0.05f), (v - 0.75f) / 0.25f);
		}

		/// <summary>Keeps colors near one hue (0..1) and turns everything else gray.</summary>
		private static Color Accent(Color c, float hue)
		{
			float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b)), min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
			float delta = max - min;
			float s = max > 0f ? delta / max : 0f;
			float h = 0f;
			if (delta > 0f)
			{
				if (max == c.r) h = ((c.g - c.b) / delta) / 6f;
				else if (max == c.g) h = ((c.b - c.r) / delta + 2f) / 6f;
				else h = ((c.r - c.g) / delta + 4f) / 6f;
				if (h < 0f) h += 1f;
			}
			float d = Mathf.Abs(h - hue);
			d = Mathf.Min(d, 1f - d);
			float keep = Mathf.Clamp01(1f - Mathf.Max(0f, d - 0.04f) / 0.08f) * Mathf.Clamp01(s * 3f);
			return Color.Lerp(Gray(Luma(c)), Saturate(c, 1.15f), keep);
		}
	}
}
