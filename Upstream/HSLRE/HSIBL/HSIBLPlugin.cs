using IllusionPlugin;
using UnityEngine;
using ToolBox;
using ToolBox.Extensions;

namespace HSIBL
{
	public class HSIBLPlugin : GenericPlugin, IEnhancedPlugin
	{
		public static HSIBLPlugin _self;

		public override string Name => "HSIBL";
		//public override string Version { get { return Assembly.GetExecutingAssembly().GetName().Version.ToString(); } }
		public override string[] Filter => new[] { "HoneySelect_64", "HoneySelect_32", "StudioNEO_32", "StudioNEO_64", "Honey Select Unlimited_64", "Honey Select Unlimited_32" };

		protected override void Awake()
		{
			base.Awake();
			_self = this;
		}

		protected override void LevelLoaded(int level)
		{
			switch (binary)
			{
				case Binary.Game:
					switch (level)
					{
						case 15:
						case 21:
						case 22:
							this.ExecuteDelayed(() =>
							{
								new GameObject("HSIBL").AddComponent<HSIBL>();
							});
							break;
					}
					break;
				case Binary.Studio:
					if (level == 3)
						this.ExecuteDelayed(() =>
						{
							new GameObject("HSIBL").AddComponent<HSIBL>();
						});
					break;
			}
		}
	}
}
