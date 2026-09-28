using System.Xml;
using UnityEngine;
using static HSLRE.HSLRE;

namespace HSIBL
{
	public partial class HSIBL
	{
		// Per-effect "Reset to defaults" (addition). The defaults are every setting as it was when the plugin
		// started (before any scene or preset), written with SaveConfig. Resetting one effect loads just that
		// effect's element(s) back through LoadConfig, keeping its on/off switch and the camera's field of view.
		private XmlDocument _defaults;
		private bool _defaultsTriedInUpdate;
		private bool _partialLoad;

		/// <summary>Remembers the start-up settings. Only the first successful call counts.</summary>
		internal void CaptureDefaults()
		{
			if (this._defaults != null)
				return;
			string xml = this.SettingsToXml();
			if (xml == null)
				return;
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(xml);
			this._defaults = doc;
		}

		/// <summary>From Update: a second try when capturing in Start failed (never after the first frame).</summary>
		private void CaptureDefaultsFallback()
		{
			if (this._defaults != null || this._defaultsTriedInUpdate)
				return;
			this._defaultsTriedInUpdate = true;
			this.CaptureDefaults();
		}

		private string[] DefaultElementsFor(EffectData data)
		{
			// the Tonemapping & Color Grading entry is saved as three elements
			if (ReferenceEquals(data.effect, this._toneMappingManager))
				return new[] { "tonemapping", "colorGrading", "eyeAdaptation" };
			return new[] { data.simpleName };
		}

		internal bool CanResetEffect(EffectData data)
		{
			if (this._defaults == null || data == null || string.IsNullOrEmpty(data.simpleName))
				return false;
			foreach (string name in this.DefaultElementsFor(data))
				if (this._defaults.DocumentElement[name] != null)
					return true;
			return false;
		}

		internal void ResetEffect(EffectData data)
		{
			if (this.CanResetEffect(data) == false)
				return;
			XmlDocument doc = new XmlDocument();
			XmlElement root = doc.CreateElement("root");
			doc.AppendChild(root);
			foreach (string name in this.DefaultElementsFor(data))
			{
				XmlNode source = this._defaults.DocumentElement[name];
				if (source == null)
					continue;
				XmlElement copy = (XmlElement)doc.ImportNode(source, true);
				// a reset changes the settings, not whether the effect is on, nor the camera's field of view
				if (copy.HasAttribute("enabled"))
					copy.SetAttribute("enabled", XmlConvert.ToString(data.enabled));
				if (copy.HasAttribute("fov"))
					copy.SetAttribute("fov", XmlConvert.ToString(this.cam.fieldOfView));
				root.AppendChild(copy);
			}
			bool wasEnabled = data.enabled;
			this._partialLoad = true;
			try
			{
				this.LoadConfig(root);
			}
			catch (System.Exception e)
			{
				Debug.LogError("HSLRE: could not reset " + data.simpleName + ".\n" + e);
			}
			finally
			{
				this._partialLoad = false;
			}
			data.enabled = wasEnabled;
		}
	}
}
