///-----------------------------------------------------------------
/// Author : Tom Falempin
/// Date : 15/04/2020 11:11
///-----------------------------------------------------------------

using Com.IsartDigital.Common.Utils.Game;
using Com.IsartDigital.Common.Utils.Localization;
using Com.IsartDigital.Common.Utils.Sound;
using Com.IsartDigital.F2P.Monetization;
using Com.IsartDigital.F2P.Object.Units.Squad;
using Com.IsartDigital.F2P.Screens;
using Com.IsartDigital.F2P.SessionDatas;
using Com.IsartDigital.F2P.Settings;
using Com.IsartDigital.F2P.Units;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.SceneManagement;

namespace Com.IsartDigital.F2P.Managers {
	public class GameManager : ADestroyObject {
		[Header("Debug")]
		[SerializeField] protected bool enableLogsGameManager = false;
		[Header("Managers")]
		[SerializeField] protected UIManager uiManager = default;
		[SerializeField] protected LocalizationManager localizationManager = default;
		[SerializeField] protected Loader loader = default;
		[Header("Settings")]
		[SerializeField] protected GameManagerSettings settings = default;
		[SerializeField] protected List<SquadType> squadsType = default;
		[SerializeField] protected List<PositioningInArmy> positionsInArmy = default;

		protected string currenNameLoop;
		protected bool isMusicMute = false;

		private static bool _isInGame;
		private static bool _isInDailyQuest;
		private static int _worldIndex;
		private static int _levelIndex;
		private static bool _isInFTUE;

		public static int WorldIndex => _worldIndex;
		public static int LevelIndex => _levelIndex;
		public static bool IsInGame => _isInGame;
		public static bool IsInDailyQuest => _isInDailyQuest;
		public static bool IsInFTUE => _isInFTUE;

		protected MyIAPManager myIAPManager;

		public static Credentials Credentials;
		public static PlayerDatas PlayerDatas;

		protected ServerManager myServer;

		protected LevelManager levelManager;

		protected AdsManager adsManager;

		protected List<SquadPattern> selectedSquads;

		private int CurrenciesGain = 0;

		private float time;

		protected void Awake() {
			myIAPManager = new MyIAPManager();
			myIAPManager.OnPurchaseSuccess += MyIAPManager_OnPurchaseSuccess;
			myIAPManager.OnPurchaseFail += MyIAPManager_OnPurchaseFail;
		}

		#region IAP
		public void BuyRemoveAds() {
			myIAPManager.BuyProduct("remove_ads"); //Puis on rajoute un nouveau bouton ou autre, et on appelle la méthode
		}

		protected void MyIAPManager_OnPurchaseSuccess(string productId) {
			Debug.Log(string.Concat("[IAP] Purchased product success: ", productId));


			/*switch (productId) {
				case "remove_ads":
					// Appeler sur votre admanager le remoads pour cacher la bannière et ne plus afficher d'interstitielles
					// Sauvegarder dans le profil du joueur qu'il a acheté le remove ads
					break;
				default:
					Debug.LogError("[IAP] Product not found");
					break;
			}*/
		}

		protected void MyIAPManager_OnPurchaseFail(string productId, PurchaseFailureReason reason) {
			Debug.Log(string.Concat("[IAP] Purchased product fail: ", productId, " - ", reason));
		}
		#endregion
		#region Game Management
		// ============================================================================
		//						    ***** GAME MANAGEMENT *****
		// ============================================================================
		/// <summary>
		/// Load en premier le fichier de localization anglais pour les traductions+ abonnement d'event (ne pas oublier de les remove dans OnDestroy)
		/// </summary>
		protected void Start() {
			//Localization
			LocalizationManager.LoadLocalizedText(Path.Combine(settings.LocalizationFolderName, settings.LocalizationEnFileName));

			//Sound
			PlayUIMusic();

			//Event
			uiManager.OnLoadFinish += UIManager_OnLoadFinish;

			LevelSelector.Instance.OnPlay += LevelSelector_OnPlay;
			LevelSelector.Instance.OnDailyQuestPlay += LevelSelector_OnDailyQuestPlay;

			PauseScreen lPauseScreen = PauseScreen.Instance;
			lPauseScreen.OnResume += PauseSreen_OnResume;
			lPauseScreen.OnLeaveLevel += PauseScreen_OnLeaveLevel;

			Hud lHud = Hud.Instance;
			lHud.OnPause += Hud_OnPause;

			WinScreen.Instance.OnNext += WinScreen_Next;

			GameOverScreen lGameOverScreen = GameOverScreen.Instance;
			lGameOverScreen.OnNext += GameOver_Next;
			lGameOverScreen.OnReplay += GameOver_OnReplay;

			Options lOptions = Options.Instance;
			lOptions.OnMusicVolume += Options_OnMusicVolume;
			lOptions.OnSFXVolume += Options_OnSFXVolume;

			SelectionSquad selectionSquad = SelectionSquad.Instance;
			selectionSquad.OnPlay += SelectionSquad_OnPlay;
			selectionSquad.OnBack += SelectionSquad_OnBack;

			//Loading
			loader.LoadSceneAsync(settings.MainSceneName, true);
			loader.OnPreloadDone += StartMain;

			KPIManager.InitKPI();
		}

		/// <summary>
		/// Lance le jeu (Le LoginScreen).
		/// </summary>
		protected void StartMain() {
			WorldInfos.Init();
			LevelsInfos.Init();
			SquadPattern.Init();

			if (enableLogsGameManager) Debug.Log("Finished loading the Main scene");
			loader.OnPreloadDone -= StartMain;

			//Server
			myServer = ServerManager.Instance;
			myServer.OnLoginSuccess += ServerManager_OnLoginSuccess;
			myServer.OnGettingDatasSuccess += ServerManager_OnGettingDatasSuccess;
			myServer.OnStartFTUE += FTUE;

			Credentials = Credentials.CheckLocalCredentials();
			if (Credentials == null) {
				if (enableLogsGameManager) Debug.LogWarning("[GameManager] No local credentials");
				uiManager.AddScreen(Login.Instance);
			}
			else myServer.Login(Credentials, true);
		}

		private void ServerManager_OnLoginSuccess(Credentials credentials) {
			Credentials = credentials;
		}

		private void ServerManager_OnGettingDatasSuccess(PlayerDatas playerDatas) {
			PlayerDatas = playerDatas;

			MenuHUD lMainMenu = MenuHUD.Instance;

			uiManager.AddScreen(lMainMenu);
			uiManager.AddScreenWithoutCloseAScreen(LevelSelector.Instance, lMainMenu);
		}

		/// <summary>
		/// Lance le chargement d'une scène.
		/// </summary>
		/// <param name="sceneName"></param>
		protected void LaunchLoadingScene(string sceneName) {
			if (enableLogsGameManager) Debug.Log("Start loading");

			//loader.OnProgress += Loader_OnProgress;
			//loader.OnPreloadDone += Loader_OnPreloadDone;
			loader.OnCompleted += Loader_OnCompleted;
			loader.LoadSceneAsync(sceneName, true);

			uiManager.CloseAllScreens(true);
			uiManager.AddScreen(LoadingScreen.Instance);
		}

		/// <summary>
		/// Permet de lancer un niveau quand le chargement de celui-ci est terminé.
		/// </summary>
		protected void StartGame() {
			_isInGame = true;
			uiManager.CloseAllScreens();
			//uiManager.CloseCurrentScreenWithoutCloseAnimation();
			uiManager.HideBackGround(true);
			SelectionSquad.Instance.ShowSquads(WorldIndex, LevelIndex);
			levelManager = GameObject.FindGameObjectWithTag(LevelManager.TAG).GetComponent<LevelManager>();

			levelManager.StartCameraSlide();
			levelManager.OnCameraFinishToward += LevelManager_OnCameraFinishToward;

			PlayGameMusic();
		}

		public void StartFTUE() {
			_isInFTUE = true;
			_isInGame = true;
			uiManager.CloseAllScreens();
			uiManager.HideBackGround(true);
			levelManager = GameObject.FindGameObjectWithTag(LevelManager.TAG).GetComponent<LevelManager>();

			levelManager.StartCameraSlide();
			levelManager.OnCameraFinishToward += LevelManager_OnCameraFinishToward;

			PlayGameMusic();
		}
		#endregion

		// ============================================================================
		//							   ***** DESTROY *****
		// ============================================================================
		/// <summary>
		/// Ne pas oublier de remove les events.
		/// </summary>
		protected void OnDestroy() {
			LevelSelector lLevelSelector = LevelSelector.Instance;
			Hud lHud = Hud.Instance;
			PauseScreen lPauseScreen = PauseScreen.Instance;
			Options lOptions = Options.Instance;

			if (lOptions != null) {
				lOptions.OnMusicVolume -= Options_OnMusicVolume;
				lOptions.OnSFXVolume -= Options_OnSFXVolume;
			}

			if (uiManager != null) {
				uiManager.OnLoadFinish -= UIManager_OnLoadFinish;
			}

			if (loader != null) {
				loader.OnProgress -= Loader_OnProgress;
				loader.OnPreloadDone -= Loader_OnPreloadDone;
				loader.OnCompleted -= Loader_OnCompleted;
			}

			if (levelManager != null) {
				levelManager.OnLose -= LevelManager_OnLose;
				levelManager.OnWin -= LevelManager_OnWin;
				levelManager.OnCameraFinishToward -= LevelManager_OnCameraFinishToward;
				levelManager.OnTotalTimeInGame -= LevelManager_OnTotalTimeInGame;
				levelManager.OnGainSoftCurrencies -= LevelManager_OnGainSoftCurrencies;
				levelManager.OnPastTime -= LevelManager_OnPastTime;
			}

			if (myIAPManager != null) {
				myIAPManager.OnPurchaseSuccess -= MyIAPManager_OnPurchaseSuccess;
				myIAPManager.OnPurchaseFail -= MyIAPManager_OnPurchaseFail;
			}

			if (myServer != null) {
				myServer.OnLoginSuccess -= ServerManager_OnLoginSuccess;
				myServer.OnGettingDatasSuccess -= ServerManager_OnGettingDatasSuccess;
				myServer.OnStartFTUE -= FTUE;
			}

			if (lLevelSelector != null) {
				lLevelSelector.OnPlay -= LevelSelector_OnPlay;
				lLevelSelector.OnDailyQuestPlay -= LevelSelector_OnDailyQuestPlay;
			}

			if (lHud != null) {
				lHud.OnPause -= Hud_OnPause;
			}

			if (lPauseScreen != null) {
				lPauseScreen.OnResume -= PauseSreen_OnResume;
			}
		}

		#region Events UIManager
		// ============================================================================
		//							 ***** EVENTS UIMANAGER *****
		// ============================================================================
		protected void UIManager_OnLoadFinish() {
			loader.AllowSceneActivation();
		}

		protected void WinScreen_Next() {
			LoadPreload();
		}

		protected void PauseScreen_OnLeaveLevel() {
			LoadPreload();

			_isInGame = false;
		}

		protected void GameOver_OnReplay() {

			LevelSelector_OnPlay(WorldIndex, LevelIndex);
		}

		protected void GameOver_Next() {
			LoadPreload();
		}

		protected void LoadPreload() {
			uiManager.HideBackGround(false);
			_isInGame = false;
			_isInDailyQuest = false;
			LaunchLoadingScene(settings.MainSceneName);
			PlayUIMusic();
		}

		protected void LevelSelector_OnPlay(int worldIndex, int levelIndex) {
			_isInGame = true;
			_worldIndex = worldIndex;
			_levelIndex = levelIndex;
			_isInDailyQuest = false;
			CurrenciesGain = 0;

			string lNameLevel = settings.LevelSceneName + (_levelIndex + 1);
			bool isExisting = DoesSceneExist(lNameLevel);

			if (enableLogsGameManager) Debug.Log("Level" + _levelIndex + " is existing : " + isExisting);

			if (isExisting) LaunchLoadingScene(lNameLevel);
			else LaunchLoadingScene(settings.TestSceneName);
		}

		private bool DoesSceneExist(string name) {
			if (string.IsNullOrEmpty(name))
				return false;

			for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++) {
				var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
				var lastSlash = scenePath.LastIndexOf("/");
				var sceneName = scenePath.Substring(lastSlash + 1, scenePath.LastIndexOf(".") - lastSlash - 1);

				if (string.Compare(name, sceneName, true) == 0)
					return true;
			}

			return false;
		}

		protected void LevelSelector_OnDailyQuestPlay() {
			_isInGame = true;
			_worldIndex = 0;
			_levelIndex = 0;
			_isInDailyQuest = true;
			CurrenciesGain = 0;

			_worldIndex = System.DateTime.Now.DayOfYear % WorldInfos.worldInfos.Length;
			_levelIndex = System.DateTime.Now.DayOfYear % 10;

			string lSceneName = settings.LevelSceneName + (_levelIndex + 1);

			if (DoesSceneExist(lSceneName))
				LaunchLoadingScene(lSceneName);
			else LaunchLoadingScene(settings.TestSceneName);
		}

		public void FTUE() {
			_isInGame = true;
			_isInFTUE = true;
			_worldIndex = 0;
			_levelIndex = 0;
			_isInDailyQuest = false;
			CurrenciesGain = 0;

			PlayerDatas = new PlayerDatas();

			string lSceneName = settings.LevelSceneName + 1;
			if (DoesSceneExist(lSceneName))
				LaunchLoadingScene(lSceneName);
			else LaunchLoadingScene(settings.TestSceneName);
			//LaunchLoadingScene(settings.LevelSceneName);
		}

		private void SelectionSquad_OnPlay(List<SquadPattern> squadPatterns) {
			selectedSquads = squadPatterns;

			if (_isInFTUE) {
				uiManager.AddScreen(Hud.Instance);

				FTUEScreen lInstance = FTUEScreen.Instance;

				uiManager.AddScreenWithoutCloseAScreen(FTUEScreen.Instance, Hud.Instance);
				lInstance.GiveLevelManager(levelManager);
			} if (IsInDailyQuest)
			{
				uiManager.AddScreen(Hud.Instance);
			}
			else uiManager.AddScreenWithoutCloseAnimation(Hud.Instance);

			for (int i = 0; i < squadPatterns.Count; i++)
				squadsType[i].SetValues(squadPatterns[i]);

			if (squadPatterns.Count == 1)
			{
				List<SquadType> squads = new List<SquadType>();
				squads.Add(squadsType[0]);
				levelManager.StartGame(squads, positionsInArmy, _isInDailyQuest);
			} else


			levelManager.StartGame(squadsType, positionsInArmy, _isInDailyQuest);
			levelManager.OnLose += LevelManager_OnLose;
			levelManager.OnWin += LevelManager_OnWin;
			levelManager.OnTotalTimeInGame += LevelManager_OnTotalTimeInGame;
			levelManager.OnPastTime += LevelManager_OnPastTime;
			levelManager.OnGainSoftCurrencies += LevelManager_OnGainSoftCurrencies;
		}

		private void SelectionSquad_OnBack() {
			LoadPreload();
		}

		protected void PauseSreen_OnResume() {
			uiManager.AddScreenWithoutCloseAnimation(Hud.Instance);
			APauseObject.ResumeAll();
			levelManager.ChangePause(true);
			_isInGame = true;
		}

		protected void Hud_OnPause() {
			uiManager.AddScreenWithoutCloseAnimation(PauseScreen.Instance);
			APauseObject.PauseAll();
			levelManager.ChangePause(false);
			_isInGame = false;
		}

		protected void Options_OnMusicVolume(float volume, bool isMusicMute) {
			ChangeMusicVolume(volume);

			this.isMusicMute = isMusicMute;

			if (isMusicMute) {
				SoundManager.Instance.StopMusicLoop();
			}
			else {
				SoundManager.Instance.PlayMusicLoop(currenNameLoop, 1);
			}
		}

		protected void Options_OnSFXVolume(float volume, bool isSFXMute) {
			ChangeSFXVolume(volume);
		}
		#endregion
		#region Events loader
		// ============================================================================
		//							 ***** EVENTS LOADER *****
		// ============================================================================
		/// <summary>
		/// Permet de voir visuellement le chargement
		/// </summary>
		/// <param name="progress">le progrès actuel du chargement</param>
		/// <param name="maxProgress">Le progrès maximale du chargement qui est de 0.9f</param>
		protected void Loader_OnProgress(float progress, float maxProgress) {
			LoadingScreen.Instance.ChangeFillLoadBar(progress / maxProgress);
		}

		/// <summary>
		/// Permet de savoir quand le chargement est terminé et d'activer le button sur le LoadingScreen
		/// </summary>
		protected void Loader_OnPreloadDone() {
			LoadingScreen.Instance.LoadFinish();

			loader.OnProgress -= Loader_OnProgress;
			loader.OnPreloadDone -= Loader_OnPreloadDone;

			if (enableLogsGameManager) Debug.Log("Done loading");
		}

		protected void Loader_OnCompleted() {
			loader.OnCompleted -= Loader_OnCompleted;
			if (_isInGame)
				if (_isInFTUE) StartFTUE();
				else StartGame();
			else {
				uiManager.CloseAllScreens();
				uiManager.AddScreen(MenuHUD.Instance, false);
				uiManager.AddScreenWithoutCloseAScreen(LevelSelector.Instance, MenuHUD.Instance);
			}
		}
		#endregion
		#region Events LevelManager
		// ============================================================================
		//							 ***** EVENTS LEVELMANAGER *****
		// ============================================================================
		protected void LevelManager_OnLose() {

			_isInGame = false;
			if (enableLogsGameManager) Debug.Log("Lose");
			uiManager.AddScreenWithoutCloseAnimation(GameOverScreen.Instance);
			
			SoundManager.Instance.PlaySFX(settings.JingleLoseName);
		}

		protected void LevelManager_OnPastTime(float time) {
			Hud.Instance.UpdateChrono(Mathf.FloorToInt(time));
		}

		protected void LevelManager_OnTotalTimeInGame(float time) {
			float chrono = (int)(time * 10f);
			chrono /= 10f;

			this.time = chrono;
		}

		protected void LevelManager_OnGainSoftCurrencies(uint softCurrencies) {
			//Here pour afficher et ajouter les soft currencies
			if (enableLogsGameManager) Debug.Log("GAIN: " + softCurrencies + " SoftCurrencies.");
			CurrenciesGain += (int)softCurrencies;
			Hud.Instance.UpdateCurrencies(CurrenciesGain);
		}

		protected void LevelManager_OnWin() {
			_isInGame = false;
			levelManager.ChangePause(false);
			if (enableLogsGameManager) Debug.Log("Win");


			WinScreen lInstance = WinScreen.Instance;
			if (IsInDailyQuest)
			{
				if (enableLogsGameManager) Debug.Log("daily");
				WinScreen.Instance.InitInfos(10000f / time, time);
			}
			else if (_isInFTUE)
			{
				if (enableLogsGameManager) Debug.Log("FTUE");
				lInstance.InitInfos(CurrenciesGain);
				PlayerDatas.AfterTutosGift(Credentials.id);
			}
			else {
				if (enableLogsGameManager) Debug.Log("normal");
				lInstance.InitInfos(CurrenciesGain, selectedSquads, WorldIndex, LevelIndex);
			}
			uiManager.AddScreenWithoutCloseAnimation(lInstance);

			_isInFTUE = false;
			_isInDailyQuest = false;
			PlayerDatas.ChangeSoftCurrency(CurrenciesGain);
			SoundManager.Instance.PlaySFX(settings.JingleWinName);
		}

		protected void LevelManager_OnCameraFinishToward() {
			levelManager.OnCameraFinishToward -= LevelManager_OnCameraFinishToward;
			if (IsInFTUE) {
				List<SquadPattern> squadPatterns = new List<SquadPattern> {
					SquadPattern.GetSquadPattern("FTUE", 1)
				};
				SelectionSquad_OnPlay(squadPatterns);
			} else if (IsInDailyQuest)
			{
				List<SquadPattern> squadPatterns = new List<SquadPattern> {
					SquadPattern.GetSquadPattern(LevelsInfos.getLevelInfos(WorldIndex, LevelIndex).bestSquadName, 3),
					SquadPattern.GetSquadPattern(LevelsInfos.getLevelInfos(WorldIndex, LevelIndex).bestSquadName, 3)
				};
				SelectionSquad_OnPlay(squadPatterns);
			}
			else
				UIManager.Instance.AddScreen(SelectionSquad.Instance);
		}
		#endregion
		#region Sound
		// ============================================================================
		//							   ***** SOUND *****
		// ============================================================================
		protected void PlayGameMusic() {
			currenNameLoop = settings.GameMusicName;
			if (!isMusicMute) SoundManager.Instance.PlayMusicLoop(currenNameLoop, settings.DurationTransitionGameMusic);
		}

		protected void PlayUIMusic() {
			currenNameLoop = settings.UIMusicName;
			if (!isMusicMute) SoundManager.Instance.PlayMusicLoop(currenNameLoop, settings.DurationTransitionUIMusic);
		}

		protected void ChangeExposedParams(string exposedParamName, float value) {
			SoundManager.Instance.ChangeExposedParam(exposedParamName, value);
		}

		protected void ChangeMusicVolume(float volume) {
			ChangeExposedParams(settings.ExposedParamMusicVolume, volume);
		}

		protected void ChangeSFXVolume(float volume) {
			ChangeExposedParams(settings.ExposedParamSFXVolume, volume);
		}
		#endregion
	}
}