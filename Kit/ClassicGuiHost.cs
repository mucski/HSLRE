using UnityEngine;

namespace HSIBL
{
	/// <summary>Hosts the original IMGUI window. Only added when UseKitUI is off, so the kit panel pays no OnGUI cost.</summary>
	internal sealed class ClassicGuiHost : MonoBehaviour
	{
		internal HSIBL owner;

		private void OnGUI()
		{
			if (this.owner != null)
				this.owner.DrawClassicGUI();
		}
	}
}
