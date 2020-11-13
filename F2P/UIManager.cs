///-----------------------------------------------------------------
/// Author : Tom Falempin
/// Date : 15/04/2020 11:41
///-----------------------------------------------------------------

using Com.IsartDigital.Common.Utils.Game;
using Com.IsartDigital.F2P.Screens;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Com.IsartDigital.F2P.Managers {
	public delegate void UIManagerEventHandler();

	public class UIManager : ADestroyObject {
		[SerializeField] protected Transform rootCanvas = default;
		protected AScreen[] screensList;
		protected Stack<AScreen> lastScreensOpen = new Stack<AScreen>();
		public event UIManagerEventHandler OnLoadFinish;

		// ============================================================================
		//							  ***** SINGLETON *****
		// ============================================================================
		private static UIManager instance;
		[SerializeField] private Image background;
		public static UIManager Instance { get { return instance; } }

		private AScreen currentScreen;

		protected void Awake() {
			if (instance) {
				Destroy(gameObject);
				return;
			}

			instance = this;

			screensList = rootCanvas.GetComponentsInChildren<AScreen>(true);
			for (int i = rootCanvas.childCount - 1; i >= 0; i--) {
				rootCanvas.GetChild(i).gameObject.SetActive(true);
			}
		}

		protected void Start() {
			LoadingScreen lLoadinScreen = LoadingScreen.Instance;
			lLoadinScreen.OnClickedFinishButton += LoadingScreen_OnClickedFinishButton;
		}

		protected void OnDestroy() {
			if (this == instance) instance = null;

			LoadingScreen lLoadinScreen = LoadingScreen.Instance;

			if (lLoadinScreen != null) {
				lLoadinScreen.OnClickedFinishButton -= LoadingScreen_OnClickedFinishButton;
			}
		}

		#region Screens Management
		// ============================================================================
		//						  ***** SCREENS MANAGEMENT *****
		// ============================================================================
		public void AddScreen(AScreen screen, bool closeCurrentScreen = true) {
			if (lastScreensOpen.Contains(screen) || screen == currentScreen) {
				Debug.LogError("Screen is already open : " + screen.name);
				return;
			}

			if (currentScreen == null) {
				screen.ActivateScreen();
				currentScreen = screen;
				return;
			}
			
			if (closeCurrentScreen) {
				currentScreen.DeactivateScreen(screen.ActivateScreen);
				currentScreen = screen;
			}
			else {
				screen.ActivateScreen();
				lastScreensOpen.Push(currentScreen);
				currentScreen = screen;
			}
		}

		public void AddScreenWithoutCloseAnimation(AScreen screen) {
			if (lastScreensOpen.Contains(screen) || screen == currentScreen) {
				Debug.LogWarning("Screen is already open :" + screen.name);
				return;
			}
			if (currentScreen == null) {
				Debug.LogError("Use addscreen to add the first screen");
				return;
			}

			currentScreen.DeactivateScreenWithoutAnimation(screen.ActivateScreen);
			currentScreen = screen;
		}

		public void AddScreenWithoutCloseAScreen(AScreen screen, AScreen screenDoesntClose) {
			if (screen == screenDoesntClose) {
				Debug.LogError("Screen and screenDoesntClose is the same thing");
				return;
			}
			if (screen == currentScreen) {
				Debug.LogWarning("Screen is already open");
				return;
			}

			if (currentScreen == screenDoesntClose) {
				lastScreensOpen.Push(currentScreen);
				currentScreen = screen;
				currentScreen.ActivateScreen();
			}
			else {
				AScreen lLastScreen = currentScreen;
				currentScreen = screen;
				lLastScreen.DeactivateScreen(delegate {
					if (screen != currentScreen) return;
					screen.ActivateScreen();
				});
				
			}
		}

		public void ReturnToLastScreenOpen() {
			if (lastScreensOpen.Count == 0) {
				Debug.LogError("No last screen!");
				return;
			}

			if (currentScreen != null) currentScreen.DeactivateScreen();
			currentScreen = lastScreensOpen.Pop();
		}

		public void CloseCurrentScreenWithoutCloseAnimation() {
			if (currentScreen == null) {
				Debug.LogError("Use addscreen to add the first screen");
				return;
			}

			currentScreen.DeactivateScreenWithoutAnimation();
			currentScreen = null;
		}

		public void CloseAllScreens(bool withoutCloseAnimation = false) {
			if (withoutCloseAnimation) {
				currentScreen.DeactivateScreenWithoutAnimation();
				currentScreen = null;

				for (int i = lastScreensOpen.Count - 1; i >= 0; i--) {
					lastScreensOpen.Pop().DeactivateScreenWithoutAnimation();
				}

				return;
			}

			currentScreen.DeactivateScreen();
			currentScreen = null;

			for (int i = lastScreensOpen.Count - 1; i >= 0; i--) {
				lastScreensOpen.Pop().DeactivateScreen();
			}
		}

		public void HideBackGround(bool hide)
		{
			background.enabled = !hide;
		}
		#endregion
		#region Events UI
		// ============================================================================
		//							 ***** EVENTS UI *****
		// ============================================================================
		protected void LoadingScreen_OnClickedFinishButton() {
			OnLoadFinish?.Invoke();
		}
		#endregion
	}
}