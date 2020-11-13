///-----------------------------------------------------------------
/// Author : Tom FALEMPIN
/// Date : 14/01/2020 11:44
///-----------------------------------------------------------------

using Com.IsartDigital.Platformer.LeaderBoadDatas;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Com.DefaultCompany.Platformer.Platformer.Managers
{
	public class DataBaseManager : MonoBehaviour
	{ 

		///////////////////////////////////////////////////////////////////
		///																///
		///   PERMET LA CONNEXION AU SERVEUR ET LA BASE DE DONNEES EN	///
		///  EN LIGNE ET/OU LA SAUVEGARDE LOCALE EN FONCTION DE L'ETAT	///
		///                  ACTUEL DE LA CONNEXION						///
		///																///
		///   EN CAS D'ECHEC AVEC LA CONNEXION AU SERVEUR, LANCE DES	///
		///  TESTS REGULIERS POUR RETABLIR LA CONNEXION ET LA SYNCHRO	///
		///           -NISATION DES DONNEES AVEC CE DERNIER				///
		///																///
		///   PERMET AUSSI UN AUTO-LOGIN EN SAUVEGARDANT LA DERNIERE	///
		///           CONNEXION USERNAME PASSWORD ENREGISTREE			///
		///																///
		///////////////////////////////////////////////////////////////////


		///ALL EVENT HANDLER
		//

		public delegate void DataBaseEventHandler(string infos);
		
		public static event DataBaseEventHandler OnConnexionComplete;
		public static event DataBaseEventHandler OnConnexionFailed;
		public static event DataBaseEventHandler AutoLoginFailed;

		public delegate void SecondDataBaseEventHandler(PlayerDatasList players);
		public static event SecondDataBaseEventHandler SendLeaderBoard;

		//
		/////////////////////////////////////////////////////////////////////////


		///LOCAL INFORMATIONS ABOUT CURRENT USER
		//

		static private string token;
		static private Credentials localCreds;

		//
		////////////////////////////////////////


		///FOR LOCAL SAVE 
		//

		static private string filePath => Path.Combine(Application.persistentDataPath, fileNameCredentials);
		static private string fileNameCredentials = "Credentials.json";
		static private string fileNameDataBase = "Level.json";

		public bool isAlreadyLogged => File.Exists(filePath);

		//
		////////////////////////////////////////////////////////////////////////////////////////////////////


		static private int TimeBeforeReconnection = 10;
		static public string SIGNIN = "signin";
		static public string SIGNUP = "signup";


		/// BOOLEAN
		//

		static private bool IsInLocal;
		static private bool isAlreadyTryCreateAccount;
		static private bool isTryingLocalAccountReconnect;

		//
		//////////////////////////////////////////////////


		static public DataBaseManager Instance => instance;
		static private DataBaseManager instance;


		private void Start()
		{
			if (instance == null) instance = this;
			else Destroy(gameObject);

			localCreds = new Credentials("local", "local");

			StopAllCoroutines();

			DontDestroyOnLoad(gameObject);
			StartCoroutine(TestConnection());
		}


		/// <summary>
		/// 
		/// PERMET LE LOGIN AUTOMATIQUE EN RECUPERANT LES INFORMATIONS DE CONNEXION LOCALES
		/// 
		/// </summary>
		public void AutoLogin()
		{
			Credentials savedCredentials = JsonUtility.FromJson<Credentials>(File.ReadAllText(filePath));
			StartCoroutine(SignCoroutine(SIGNIN, savedCredentials.username, savedCredentials.password));
		}


		/// <summary>
		/// 
		/// LANCE LA COROUTINE DE SIGN
		/// 
		/// PREND EN PARAMETRES LE CREDENTIAL ET LA FIN D'URL DE LA ROUTE DE SIGN (signup / signin)
		/// 
		/// </summary>
		/// <param name="creds"></param>
		/// <param name="endUrl"></param>
		public void Sign(Credentials creds, string endUrl)
		{
			localCreds = creds;
			StartCoroutine(SignCoroutine(endUrl, creds.username, creds.password));
		}


		/// <summary>
		/// 
		/// LANCE UNE REQUETE SUR users/signin OU users/signup EN FONCTION DE endUrl
		/// SI ECHEC, PASSE EN LOCAL ET LANCE LA COROUTINE DE TEST DE CONNECTION
		/// 
		/// </summary>
		/// <param name="endUrl"></param>
		/// <param name="username"></param>
		/// <param name="password"></param>
		/// <returns></returns>
		private IEnumerator SignCoroutine(string endUrl, string username, string password)
		{
			Credentials creds = new Credentials(username, password);
			string json = JsonUtility.ToJson(creds);

			string url = "https://comisartdigitalsophia.herokuapp.com/users/" + endUrl;
			using (UnityWebRequest req = PostJson(url, json))
			{
				yield return req.SendWebRequest();

				if (req.isNetworkError)
				{
					if(!IsInLocal)
						StartCoroutine(TestConnection());
					IsInLocal = true;

					string CredentialsJson = JsonUtility.ToJson(creds);
					File.WriteAllText(filePath, CredentialsJson);

					OnConnexionComplete?.Invoke("local");
				}
				else if (req.isHttpError)
				{
					if (!IsInLocal)
						StartCoroutine(TestConnection());

					IsInLocal = true;

					if (isAlreadyLogged && !isTryingLocalAccountReconnect)
					{
						if (isAlreadyTryCreateAccount)
						{
							AutoLoginFailed?.Invoke("404");
						}
						else
						{
							isAlreadyTryCreateAccount = true;
							Sign(creds, SIGNUP);
						}
					}
					else if (isTryingLocalAccountReconnect)
						Sign(creds, SIGNUP);
				}
				else
				{
					token = req.downloadHandler.text;

					string CredentialsJson = JsonUtility.ToJson(creds);
					File.WriteAllText(filePath, CredentialsJson);

					OnConnexionComplete?.Invoke("online");
				}
			}
		}


		/// <summary>
		/// 
		/// SI IsInLocal, SAVE EN LOCAL, SINON LANCE LA COROUTINE POUR SAVE ONLINE
		/// 
		/// </summary>
		/// <param name="level"></param>
		/// <param name="time"></param>
		/// <param name="score"></param>
		/// <param name="objects"></param>
		/// <param name="lives"></param>
		public void SaveLevel(int level, float time, float score, int objects, int lives)
		{

			level = Mathf.Clamp(level, 1, 2);
			Debug.Log(level);

			if (IsInLocal)
			{
				localSave(level, time, score, objects, lives);
			}
			else if (token != null)
				StartCoroutine(SaveCoroutine(new PlayerToSave(level, time, score, objects, lives, token)));
			else localSave(level, time, score, objects, lives);
		}


		/// <summary>
		/// 
		/// SAVE LOCALEMENT EN JSON
		/// 
		/// </summary>
		/// <param name="level"></param>
		/// <param name="time"></param>
		/// <param name="score"></param>
		/// <param name="objects"></param>
		/// <param name="lives"></param>
		private void localSave(int level, float time, float score, int objects, int lives)
		{
			string filePath = Path.Combine(Application.persistentDataPath, fileNameDataBase);
			LocalPlayerToSaveList list;

			if (File.Exists(filePath))
			{
				list = JsonUtility.FromJson<LocalPlayerToSaveList>(File.ReadAllText(filePath));
			}
			else
			{
				list = new LocalPlayerToSaveList();
				list.list = new System.Collections.Generic.List<LocalPlayerToSave>();
			}

			list.list.Add(new LocalPlayerToSave(time, score, objects, lives, localCreds.username, localCreds.password, level));

			string ListJson = JsonUtility.ToJson(list);
			File.WriteAllText(filePath, ListJson);
		}


		/// <summary>
		/// 
		/// ENVOIE UNE REQUETE AU SERVEUR AFIN DE SAUVEGARDER SUR LE SERVEUR
		/// SI ECHEC, LANCE LA SAUVEGARDE LOCALE
		/// 
		/// </summary>
		/// <param name="toSave"></param>
		/// <returns></returns>
		private IEnumerator SaveCoroutine(PlayerToSave toSave)
		{
			string json = JsonUtility.ToJson(toSave);
			string url = "https://comisartdigitalsophia.herokuapp.com/level/save";

			using (UnityWebRequest req = PostJson(url, json))
			{
				req.SetRequestHeader("Authorization", "Bearer " + token);

				yield return req.SendWebRequest();

				if (req.isNetworkError || req.isHttpError)
				{
					if (!IsInLocal)
					{
						StartCoroutine(TestConnection());
					}

					localSave(toSave.level, toSave.time, toSave.score, toSave.objects, toSave.lives);
					IsInLocal = true;
				}
				else
				{
					IsInLocal = false;
					//Debug.Log("saved online on level : " + toSave.level);
				}
			}
		}


		/// <summary>
		/// 
		/// SI LE JEU EST EN LOCAL OU QUE LE TOKEN DE CONNEXION EST NUL, LANCE LE LEADERBOARD LOCAL SINON LANCE LA REQUETE POUR LE SERVEUR
		/// 
		/// </summary>
		/// <param name="level"></param>
		public void LeaderBoard(uint level)
		{
			if (level == 0) level = 1;

			if (IsInLocal)
			{
				LocalLeaderBoard(level);
			}
			else {
				if(token != null)
					StartCoroutine(LeaderBoardCoroutine(level));
				LocalLeaderBoard(level);
			}
		}


		/// <summary>
		/// 
		/// RECUPERE LE TOP 5 DES JOUEURS SUR LA SAUVEGARDE LOCALE 
		/// 
		/// </summary>
		/// <param name="level"></param>
		private void LocalLeaderBoard(uint level)
		{
			string filePath = Path.Combine(Application.persistentDataPath, fileNameDataBase);
			LocalPlayerToSaveList list;

			if (File.Exists(filePath))
			{
				list = JsonUtility.FromJson<LocalPlayerToSaveList>(File.ReadAllText(filePath));
			}
			else
			{
				list = new LocalPlayerToSaveList();
				list.list = new System.Collections.Generic.List<LocalPlayerToSave>();
			}

			PlayerDatasList leaderboardList = new PlayerDatasList();
			leaderboardList.list = new System.Collections.Generic.List<PlayerDatas>();

			foreach (LocalPlayerToSave player in list.list)
			{
				if (player.level != level) list.list.Remove(player);
			}

			list.list.Sort((p1, p2) => p2.score.CompareTo(p1.score));
			if(list.list.Count > 5)
				list.list.RemoveRange(5, list.list.Count - 5);

			foreach(LocalPlayerToSave player in list.list)
			{
				leaderboardList.list.Add(new PlayerDatas(player.score.ToString(), player.objects.ToString(), player.lives.ToString(), player.time.ToString(), player.name));
			}

			SendLeaderBoard?.Invoke(leaderboardList);
		}


		/// <summary>
		/// 
		/// ENVOIE UNE REQUETE AU SERVEUR POU RECUPERER POUR UN CERTAIN NIVEAU LE LEADERBOARD DES JOUEURS 
		/// (top 5)
		/// 
		/// SI REUSSITE, ENVOIE UN EVENT AVEC LA LIST
		/// 
		/// </summary>
		/// <param name="level"></param>
		/// <returns></returns>
		private IEnumerator LeaderBoardCoroutine(uint level)
		{
			string json = "{ \"level\" :" + level + "}";

			string url = "https://comisartdigitalsophia.herokuapp.com/level/leaderboard";

			using (UnityWebRequest req = PostJson(url, json))
			{
				req.SetRequestHeader("Authorization", "Bearer " + token);

				yield return req.SendWebRequest();

				if (req.isNetworkError || req.isHttpError)
				{
					if (!IsInLocal)
						StartCoroutine(TestConnection());
					IsInLocal = true;
					LocalLeaderBoard(level);
				}
				else
				{
					IsInLocal = false;
					PlayerDatasList top5Players;
					top5Players = JsonUtility.FromJson<PlayerDatasList>("{\"list\" : " + req.downloadHandler.text + "}");
					Debug.Log(top5Players.list.Count);
					SendLeaderBoard?.Invoke(top5Players);
				}
			}
		}



		/// <summary>
		/// 
		/// TESTE REGULIEREMENT LA CONNECTION EN MODE HORS-LIGNE ET SYNCHRONISE LES DONNEES SI REUSSITE
		/// CONNECTE LE JOUEUR SI BESOIN UNE FOIS LA CONNECTION ETABLIE
		/// 
		/// </summary>
		/// <returns></returns>
		private IEnumerator TestConnection()
		{
			yield return new WaitForSeconds(TimeBeforeReconnection);

			string url = "https://comisartdigitalsophia.herokuapp.com/test";
			using (UnityWebRequest req = UnityWebRequest.Get(url))
			{
				yield return req.SendWebRequest();

				if (req.isNetworkError || req.isHttpError)
				{
					IsInLocal = true;
					StartCoroutine(TestConnection());
				}
				else
				{
					IsInLocal = false;
					if(token == null && localCreds != null)
						Sign(localCreds, SIGNIN);
					TryToSaveOnline();
				}
			}
		}


		/// <summary>
		/// 
		/// PERMET DE SAUVEGARDER LES DONNES LOCALES VERS LA SAUVEGARDE ONLINE
		/// 
		/// </summary>
		private void TryToSaveOnline()
		{
			string filePath = Path.Combine(Application.persistentDataPath, fileNameDataBase);
			LocalPlayerToSaveList list;

			if (File.Exists(filePath))
			{
				list = JsonUtility.FromJson<LocalPlayerToSaveList>(File.ReadAllText(filePath));
			}
			else
			{
				list = new LocalPlayerToSaveList();
				list.list = new System.Collections.Generic.List<LocalPlayerToSave>();
			}

			foreach (LocalPlayerToSave player in list.list)
			{
				StartCoroutine(TryToSaveOnlineCoroutine(player));
			}
		}


		/// <summary>
		/// 
		/// ENVOIE UNE REQUETE AU SERVEUR POUR SAUVEGARDER UN PLAYER LOCAL
		/// DEMANDE LA SUPPRESSION DU PLAYER LOCAL
		/// 
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		private IEnumerator TryToSaveOnlineCoroutine(LocalPlayerToSave player)
		{
			string json = JsonUtility.ToJson(player);
			string url = "https://comisartdigitalsophia.herokuapp.com/level/localToOnline";

			using (UnityWebRequest req = PostJson(url, json))
			{

				yield return req.SendWebRequest();

				if (req.isNetworkError || req.isHttpError)
				{
					IsInLocal = true;
				}
				else
				{
					RemovePlayerInLocalDataBase(player);
					IsInLocal = false;
				}
			}
		}


		/// <summary>
		/// 
		/// SUPPRIME UN PLAYER LOCAL PRECIS 
		/// 
		/// </summary>
		/// <param name="player"></param>
		private void RemovePlayerInLocalDataBase(LocalPlayerToSave player)
		{
			string filePath = Path.Combine(Application.persistentDataPath, fileNameDataBase);
			LocalPlayerToSaveList list;

			if (File.Exists(filePath))
			{
				list = JsonUtility.FromJson<LocalPlayerToSaveList>(File.ReadAllText(filePath));
			}
			else
			{
				list = new LocalPlayerToSaveList();
				list.list = new System.Collections.Generic.List<LocalPlayerToSave>();
			}

			for (int i = list.list.Count - 1; i >= 0; i--)
			{
				if (list.list[i].name == player.name)
				{
					list.list.Remove(list.list[i]);
				}
			}

			string ListJson = JsonUtility.ToJson(list);
			File.WriteAllText(filePath, ListJson);
		}


		/// <summary>
		/// 
		/// 
		/// 
		/// </summary>
		/// <param name="url"></param>
		/// <param name="json"></param>
		/// <returns></returns>
		static public UnityWebRequest PostJson(string url, string json)
		{
			UnityWebRequest req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);

			req.downloadHandler = new DownloadHandlerBuffer();
			req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
			req.uploadHandler.contentType = "application/json";

			return req;
		}


		/// <summary>
		/// 
		/// 
		/// 
		/// </summary>
		private void OnDestroy()
		{
			if (instance == this) instance = null;
		}


		/// <summary>
		/// 
		/// 
		/// 
		/// </summary>
		private void OnApplicationQuit()
		{
			StopAllCoroutines();
		}
	}
}