using System;
using UnityEngine;

namespace HSLRE
{
	public class CameraEventsDispatcher : MonoBehaviour
	{
		private Camera _camera;
		private void Awake() => _camera = GetComponent<Camera>();

		public event Action<Camera> onPreCull;
		private void OnPreCull() => onPreCull?.Invoke(_camera);

		public event Action<Camera> onPreRender;
		private void OnPreRender() => onPreRender?.Invoke(_camera);

		public event Action<Camera> onPostRender;
		private void OnPostRender() => onPostRender?.Invoke(_camera);
	}
}
