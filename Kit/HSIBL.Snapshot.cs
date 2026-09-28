using System;
using System.IO;
using System.Xml;
using UnityEngine;

namespace HSIBL
{
	public partial class HSIBL
	{
		// Before/after compare (addition): "before" is a snapshot of every HSLRE setting, written with the same
		// SaveConfig / LoadConfig the scene and preset files use. Compare swaps the snapshot in and keeps the
		// current settings aside until it is switched back.
		private string _snapshotXml;
		private string _compareCurrentXml;
		private bool _compareLoading;

		/// <summary>True while the snapshot is shown instead of the current settings.</summary>
		internal bool ShowingSnapshot => this._compareCurrentXml != null;

		internal bool HasSnapshot => this._snapshotXml != null;

		/// <summary>Remembers the current settings as "before". Ignored while the snapshot itself is shown.</summary>
		internal void TakeSnapshot()
		{
			if (this.ShowingSnapshot)
				return;
			this._snapshotXml = this.SettingsToXml();
		}

		/// <summary>Shows the snapshot (true) or goes back to the current settings (false).</summary>
		internal void ShowSnapshot(bool show)
		{
			if (show == this.ShowingSnapshot || (show && this._snapshotXml == null))
				return;
			if (show)
			{
				string current = this.SettingsToXml();
				if (current == null)
					return;
				this._compareCurrentXml = current;
				this.LoadSettingsXml(this._snapshotXml);
			}
			else
			{
				string current = this._compareCurrentXml;
				this._compareCurrentXml = null;
				this.LoadSettingsXml(current);
			}
		}

		/// <summary>
		/// A scene or preset loaded while the snapshot was shown: what is loaded now counts as the current
		/// settings, so the ones kept aside are dropped (the snapshot itself stays).
		/// </summary>
		private void CompareOnExternalLoad()
		{
			if (this._compareLoading == false)
				this._compareCurrentXml = null;
		}

		private string SettingsToXml()
		{
			try
			{
				using (StringWriter sw = new StringWriter())
				{
					using (XmlTextWriter writer = new XmlTextWriter(sw))
					{
						writer.WriteStartElement("root");
						this.SaveConfig(writer);
						writer.WriteEndElement();
					}
					return sw.ToString();
				}
			}
			catch (Exception e)
			{
				Debug.LogError("HSLRE: could not take a settings snapshot.\n" + e);
				return null;
			}
		}

		private void LoadSettingsXml(string xml)
		{
			if (xml == null)
				return;
			try
			{
				XmlDocument doc = new XmlDocument();
				doc.LoadXml(xml);
				this._compareLoading = true;
				this.LoadConfig(doc.FirstChild);
			}
			catch (Exception e)
			{
				Debug.LogError("HSLRE: could not apply the settings snapshot.\n" + e);
			}
			finally
			{
				this._compareLoading = false;
			}
		}
	}
}
