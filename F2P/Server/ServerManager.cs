///-----------------------------------------------------------------
/// Author : Tom FALEMPIN
/// Date : 27/04/2020 14:23
///-----------------------------------------------------------------

using Com.IsartDigital.F2P.Monetization;
using Com.IsartDigital.F2P.Screens;
using Com.IsartDigital.F2P.SessionDatas;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Com.IsartDigital.F2P.Managers {
	public class ServerManager : MonoBehaviour{

		// ============================================================================
		//							  ***** SINGLETON *****
		// ============================================================================
		private static ServerManager instance;
		public static ServerManager Instance { get { return instance; } }

		public delegate void ServerManangerCredentialsEventHandler(Credentials credentials);
		public event ServerManangerCredentialsEventHandler OnLoginSuccess;

		public delegate void ServerManangerPlayerDatasEventHandler(PlayerDatas playerDatas);
		public event ServerManangerPlayerDatasEventHandler OnGettingDatasSuccess;

		public delegate void ServerManangerEventHandler();
		public event ServerManangerEventHandler OnStartFTUE;
		/*public event ServerManangerPlayerDatasEventHandler OnGettingDatasFailed;*/

		static private string Token;

		private void Awake()
		{
			if (instance)
			{
				Destroy(gameObject);
				return;
			}

			instance = this;
		}

		private void Start()
		{
			Screens.Login.Instance.OnLoginClicked += Login_OnLoginClicked;
			Screens.Login.Instance.OnSignUpClicked += Login_OnSignUpClicked;
			Leaderboard.Instance.GetLeaderBoard += Leaderboard_GetLeaderBoard;
		}

		private void Leaderboard_GetLeaderBoard(bool isLocal)
		{
			string localization;
			if (isLocal) localization = GameManager.PlayerDatas.country;
			else localization = "global";

			GetLeaderboard(localization);
		}

		private void Login_OnLoginClicked(string id, string password)
		{
			//Debug.LogWarning(string.Concat("[ServerManager] Trying login connection with ", id, ", ", password));
			Login(new Credentials(id, password));
		}

		private void Login_OnSignUpClicked(string id, string password)
		{
			//Debug.LogWarning(string.Concat("[ServerManager] Trying sign up connection with ", id, ", ", password));
			Signup(new Credentials(id, password));
		}

		public void Login(Credentials credentials, bool FromLocalCredentials = false)
		{
			StartCoroutine(LoginCoroutine(credentials, FromLocalCredentials));
		}

		private IEnumerator LoginCoroutine(Credentials credentials, bool FromLocalCredentials)
		{
			string json = JsonUtility.ToJson(credentials);

			string url = "https://isartdigitalf2pscrawl.herokuapp.com/users/login";
			using (UnityWebRequest req = PostJson(url, json))
			{
				yield return req.SendWebRequest();

				if (req.isNetworkError || req.isHttpError)
				{
					Debug.LogWarning(string.Concat("[ServerManager] Cannot access to server or database at Login, reason : ", req.downloadHandler.text));
					if(FromLocalCredentials) UIManager.Instance.AddScreen(Screens.Login.Instance);
					else Screens.Login.Instance.LoginFailed(req.downloadHandler.text);
				}
				else
				{
					Credentials receivedCreds = JsonUtility.FromJson<Credentials>(req.downloadHandler.text);
					credentials.token = Token = receivedCreds.token;
					credentials.id  = receivedCreds.id;

					Screens.Login.Instance.LoginSuccess();

					OnLoginSuccess?.Invoke(credentials);
					StartCoroutine(GetPlayerDatas());
				}
			}
		}

		public void Signup(Credentials credentials)
		{
			StartCoroutine(SignUpCoroutine(credentials));
		}

		private IEnumerator SignUpCoroutine(Credentials credentials)
		{
			//string json = JsonUtility.ToJson(credentials);
			string json = string.Concat("{ \"username\" : \"", credentials.username, "\",\"password\" : \"", credentials.password, "\",\"country\" : \"", System.Globalization.RegionInfo.CurrentRegion, "\" }");

			string url = "https://isartdigitalf2pscrawl.herokuapp.com/users/signup";
			using (UnityWebRequest req = PostJson(url, json))
			{
				yield return req.SendWebRequest();

				if (req.isNetworkError || req.isHttpError)
				{
					Debug.LogWarning(string.Concat("[ServerManager] Cannot access to server or database at Sign Up, reason : ", req.downloadHandler.text));
					Screens.Login.Instance.LoginFailed(req.downloadHandler.text);
				}
				else
				{
					Credentials receivedCreds = JsonUtility.FromJson<Credentials>(req.downloadHandler.text);
					credentials.token = Token = receivedCreds.token;
					credentials.id = receivedCreds.id;

					OnLoginSuccess?.Invoke(credentials);
					//PlayerDatas playerDatas = new PlayerDatas();
					//OnGettingDatasSuccess?.Invoke(playerDatas);
					//LastPubDate date = new LastPubDate(credentials.id, System.DateTime.Now);
					OnStartFTUE?.Invoke();
					//SaveLastPubDate(date);
					//AdsManager.Instance.GetLastPubDate(date);
					//SavePlayerDatas(playerDatas);
					
				}
			}
		}

		private IEnumerator GetPlayerDatas() { 

			string url = "https://isartdigitalf2pscrawl.herokuapp.com/playerdatas/get";

			using (UnityWebRequest req = UnityWebRequest.Get(url))
			{
				req.SetRequestHeader("Authorization", "Bearer " + Token);

				yield return req.SendWebRequest();

				if (req.isNetworkError || req.isHttpError)
				{
					Debug.LogWarning(string.Concat("[ServerManager] Cannot get playerDatas, reason : ", req.downloadHandler.text));
					OnStartFTUE?.Invoke();
				}
				else
				{
					PlayerDatas receivedPlayerDatas = JsonUtility.FromJson<PlayerDatas>(req.downloadHandler.text);
					if (receivedPlayerDatas.worlds.Count == 0 || receivedPlayerDatas.squads.Count == 0) OnStartFTUE?.Invoke();
					else
					{
						OnGettingDatasSuccess?.Invoke(receivedPlayerDatas);
						GetLastPubDate(GameManager.Credentials.id);
					}
					/*receivedPlayerDatas.softCurrency = 100;
					receivedPlayerDatas.noAds = true;

					if (receivedPlayerDatas.worlds.Count == 0 || receivedPlayerDatas.squads.Count == 0)
					{
						receivedPlayerDatas.AfterTutosGift(GameManager.Credentials.id);
					}*/

					//StartCoroutine(SavePlayerDatasCoroutine(receivedPlayerDatas));
				}
			}
		}

		public void SaveLevel(Level level)
		{
			StartCoroutine(SaveLevelCoroutine(level));
		}

		private IEnumerator SaveLevelCoroutine(Level level)
		{
			string json = JsonUtility.ToJson(level);

			string url = "https://isartdigitalf2pscrawl.herokuapp.com/level/save";
			using (UnityWebRequest req = PostJson(url, json))
			{
				req.SetRequestHeader("Authorization", "Bearer " + Token);

				yield return req.SendWebRequest();

				if (req.isNetworkError || req.isHttpError)
					Debug.LogWarning(string.Concat("[ServerManager] Cannot save level, reason : ", req.downloadHandler.text));
			}
		}

		public void SaveNoAds(bool noAds)
		{
			StartCoroutine(SaveNoAdsCoroutine(noAds));
		}
		

		private IEnumerator SaveNoAdsCoroutine(bool noAds)
		{
			string json = JsonUtility.ToJson(noAds);

			string url = "https://isartdigitalf2pscrawl.herokuapp.com/noAds/save";
			using (UnityWebRequest req = PostJson(url, json))
			{
				req.SetRequestHeader("Authorization", "Bearer " + Token);

				yield return req.SendWebRequest();

				if (req.isNetworkError || req.isHttpError)
					Debug.LogWarning(string.Concat("[ServerManager] Cannot save noAds, reason : ", req.downloadHandler.text));
			}
		}


		public void SaveSquad(Squad squad)
		{
			StartCoroutine(SaveSquadCoroutine(squad));
		}

		private IEnumerator SaveSquadCoroutine(Squad squad)
		{
			string json = JsonUtility.ToJson(squad);

			string url = "https://isartdigitalf2pscrawl.herokuapp.com/squad/save";
			using (UnityWebRequest req = PostJson(url, json))
			{
				req.SetRequestHeader("Authorization", "Bearer " + Token);

				yield return req.SendWebRequest();

				if (req.isNetworkError || req.isHttpError)
					Debug.LogWarning(string.Concat("[ServerManager] Cannot save squad, reason : ", req.downloadHandler.text));
			}
		}

		public void SavePlayerDatas(PlayerDatas playerDatas)
		{
			StartCoroutine(SavePlayerDatasCoroutine(playerDatas));
		}

		private IEnumerator SavePlayerDatasCoroutine(PlayerDatas playerDatas)
		{
			string json = JsonUtility.ToJson(playerDatas);

			string url = "https://isartdigitalf2pscrawl.herokuapp.com/playerdatas/save";
			using (UnityWebRequest req = PostJson(url, json))
			{
				req.SetRequestHeader("Authorization", "Bearer " + Token);

				yield return req.SendWebRequest();

				if (req.isNetworkError || req.isHttpError)
					Debug.LogWarning(string.Concat("[ServerManager] Cannot save playerDatas, reason : ", req.downloadHandler.text));
			}
		}

		public void SaveCurrencies(int softCurrency, int hardCurrency)
		{
			StartCoroutine(SaveCurrenciesCoroutine(softCurrency, hardCurrency));
		}

		private IEnumerator SaveCurrenciesCoroutine(int softCurrency, int hardCurrency)
		{
			string json = string.Concat("{ \"softCurrency\" : \"", softCurrency, "\", \"hardCurrency\" : \"", hardCurrency, "\" }");

			string url = "https://isartdigitalf2pscrawl.herokuapp.com/currencies/save";
			using (UnityWebRequest req = PostJson(url, json))
			{
				req.SetRequestHeader("Authorization", "Bearer " + Token);

				yield return req.SendWebRequest();

				if (req.isNetworkError || req.isHttpError)
					Debug.LogWarning(string.Concat("[ServerManager] Cannot save currencies, reason : ", req.downloadHandler.text));
			}
		}

		public void SaveRankedLevel(RankedLevel rankedLevel)
		{
			StartCoroutine(SaveRankedLevelCoroutine(rankedLevel));
		}

		private IEnumerator SaveRankedLevelCoroutine(RankedLevel rankedLevel)
		{
			string json = JsonUtility.ToJson(rankedLevel);

			string url = "https://isartdigitalf2pscrawl.herokuapp.com/rankedLevel/save";
			using (UnityWebRequest req = PostJson(url, json))
			{
				req.SetRequestHeader("Authorization", "Bearer " + Token);

				yield return req.SendWebRequest();

				if (req.isNetworkError || req.isHttpError)
					Debug.LogWarning(string.Concat("[ServerManager] Cannot save rankedLevel, reason : ", req.downloadHandler.text));
			}
		}

		public void SaveWorld(World world)
		{
			StartCoroutine(SaveWorldCoroutine(world));
		}

		private IEnumerator SaveWorldCoroutine(World world)
		{
			string json = JsonUtility.ToJson(world);

			string url = "https://isartdigitalf2pscrawl.herokuapp.com/world/save";
			using (UnityWebRequest req = PostJson(url, json))
			{
				req.SetRequestHeader("Authorization", "Bearer " + Token);

				yield return req.SendWebRequest();

				if (req.isNetworkError || req.isHttpError)
					Debug.LogWarning(string.Concat("[ServerManager] Cannot save world, reason : ", req.downloadHandler.text));
			}
		}

		public void SaveLastPubDate(LastPubDate date)
		{
			StartCoroutine(SaveLastPubDateCoroutine(date));
		}

		private IEnumerator SaveLastPubDateCoroutine(LastPubDate date)
		{
			string json = JsonUtility.ToJson(date);

			//Debug.Log(json);
			string url = "https://isartdigitalf2pscrawl.herokuapp.com/lastPubDate/save";
			using (UnityWebRequest req = PostJson(url, json))
			{
				req.SetRequestHeader("Authorization", "Bearer " + Token);

				yield return req.SendWebRequest();

				if (req.isNetworkError || req.isHttpError)
					Debug.LogWarning(string.Concat("[ServerManager] Cannot save LastPubDate, reason : ", req.downloadHandler.text));
			}
		}

		public void GetLastPubDate(string id)
		{
			StartCoroutine(GetLastPubDateCoroutine(id));		}

		private IEnumerator GetLastPubDateCoroutine(string id)
		{
			string json = string.Concat("{ \"id\" : \"", id, "\" }");
			string url = "https://isartdigitalf2pscrawl.herokuapp.com/lastPubDate/get";
			using (UnityWebRequest req = PostJson(url, json))
			{
				req.SetRequestHeader("Authorization", "Bearer " + Token);

				yield return req.SendWebRequest();

				if (req.isNetworkError || req.isHttpError)
				{
					Debug.LogWarning(string.Concat("[ServerManager] Cannot get LastPubDate, reason : ", req.downloadHandler.text));
					
					LastPubDate date = new LastPubDate(GameManager.Credentials.id, System.DateTime.Now);
					SaveLastPubDate(date);
					AdsManager.Instance.GetLastPubDate(date);
				}

				else
				{
					LastPubDate date = JsonUtility.FromJson<LastPubDate>(req.downloadHandler.text);
					AdsManager.Instance.GetLastPubDate(date);
				}
			}
		}

		public void GetLeaderboard(string localization)
		{
			StartCoroutine(GetLeaderboardCoroutine(localization));
		}

		private IEnumerator GetLeaderboardCoroutine(string localization)
		{
			string json = string.Concat("{ \"localization\" : \"", localization, "\", \"startDay\" : \"", RankedLevel.GetLastStartDay(), "\" }");

			string url = "https://isartdigitalf2pscrawl.herokuapp.com/leaderboard";
			using (UnityWebRequest req = PostJson(url, json))
			{
				req.SetRequestHeader("Authorization", "Bearer " + Token);

				yield return req.SendWebRequest();

				if (req.isNetworkError || req.isHttpError)
				{
					Debug.LogWarning(string.Concat("[ServerManager] Cannot get leaderboard, reason : ", req.downloadHandler.text));
				}
				else
				{
					LeaderBoardPlayerList leaderboard = JsonUtility.FromJson<LeaderBoardPlayerList>("{\"list\":" + req.downloadHandler.text + "}");
					Leaderboard.Instance.SetLeaderBoard(leaderboard.list);
				}
			}
		}

		static public UnityWebRequest PostJson(string url, string json)
		{
			UnityWebRequest req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);

			req.downloadHandler = new DownloadHandlerBuffer();
			req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
			req.uploadHandler.contentType = "application/json";

			return req;
		}

		// ============================================================================
		//							  ***** DESTROY *****
		// ============================================================================
		private void OnDestroy()
		{
			if(Screens.Login.Instance)
				Screens.Login.Instance.OnLoginClicked -= Login_OnLoginClicked;
			if (this == instance) instance = null;
		}
	}
}