///-----------------------------------------------------------------
/// Author : Tom FALEMPIN
/// Date : 12/05/2020 11:01
///-----------------------------------------------------------------

using Com.IsartDigital.F2P.Managers;
using Com.IsartDigital.F2P.SessionDatas;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Com.IsartDigital.F2P.Screens {
	public class SelectionSquad : AScreen
	{
		// ============================================================================
		//							  ***** SINGLETON *****
		// ============================================================================
		private static SelectionSquad instance;
		public static SelectionSquad Instance { get { return instance; } }

		private void Awake()
		{
			if (instance)
			{
				Destroy(gameObject);
				return;
			}

			instance = this;
		}

		// ============================================================================
		//							  ***** DESTROY *****
		// ============================================================================
		private void OnDestroy()
		{
			if (this == instance) instance = null;

			for (int i = 0; i < squadCards.Count; i++)
				squadCards[i].OnSelected -= SelectionSquadCard_OnSelected;

			BackButton.onClick.RemoveListener(OnBackButton_Clicked);
			PlayButton.onClick.RemoveListener(OnPlayButton_Clicked);

		}

		// ============================================================================
		//							  ***** INSTANCE *****
		// ============================================================================

		public event ScreenEventHandler OnBackClicked;

		[SerializeField] private GameObject SelectionSquadCardPrefab = null;
		[SerializeField] private GameObject LinePrefab = null;
		[SerializeField] private GameObject BestSquadPanel = null;
		[SerializeField] private GameObject AllSquadsPanel = null;

		[SerializeField] private Button BackButton = null;
		[SerializeField] private Button PlayButton = null;

		[SerializeField] private TextMeshProUGUI WorldName = null;
		[SerializeField] private TextMeshProUGUI LevelName = null;

		private List<List<Squad>> sortedSquads = new List<List<Squad>>();

		private List<GameObject> lines = new List<GameObject>();
		private List<SquadPattern> selectedPatterns = new List<SquadPattern>();
		private List<SelectionSquadCard> selectedSquads = new List<SelectionSquadCard>();
		private List<SelectionSquadCard> squadCards = new List<SelectionSquadCard>();

		public delegate void SelectionSquadEventHandler(List<SquadPattern> squadPatterns);
		public event SelectionSquadEventHandler OnPlay;
		public event ScreenEventHandler OnBack;

		private bool hadBestSquad; 

		override protected void Start()
		{
			base.Start();
			BackButton.onClick.AddListener(OnBackButton_Clicked);
			PlayButton.onClick.AddListener(OnPlayButton_Clicked);
		}

		private void OnPlayButton_Clicked()
		{
			if(selectedSquads.Count > 0)
			{
				OnPlay?.Invoke(selectedPatterns);
			}

			PlayImportantClickSound();
		}

		private void OnBackButton_Clicked()
		{
			OnBack?.Invoke();
			PlayBackSound();
		}

		public void ShowSquads(int worldIndex, int levelIndex)
		{
			WorldName.text = String.Concat(worldIndex +1, ". ", WorldInfos.worldInfos[worldIndex].name);
			LevelName.text = "Level " + (levelIndex +1);
			LevelsInfos infos = LevelsInfos.getLevelInfos(worldIndex, levelIndex);

			int length = lines.Count;
			int i = 0;

			for (i = length - 1; i >= 0; i--)
			{
				Destroy(lines[i]);
				lines.RemoveAt(i);
			}

			for ( i = 0;  i < squadCards.Count;  i++)
				squadCards[i].OnSelected -= SelectionSquadCard_OnSelected;

			hadBestSquad = false;

			GameObject line = Instantiate(LinePrefab, AllSquadsPanel.transform);
			GameObject bestLineSquad = Instantiate(LinePrefab, BestSquadPanel.transform);
			lines.Add(line);
			lines.Add(bestLineSquad);
			GameObject squadCard;

			sortedSquads = new List<List<Squad>>();
			selectedSquads = new List<SelectionSquadCard>();
			selectedPatterns = new List<SquadPattern>();
			squadCards = new List<SelectionSquadCard>();

			List<Squad> squads = GameManager.PlayerDatas.squads;

			length = squads.Count;
			Squad squad;

			int maxLevel = 0;

			for (i = 0; i < length; i++)
				if (maxLevel < squads[i].level) maxLevel = squads[i].level;

			for (i = 0; i < maxLevel; i++)
				sortedSquads.Add(new List<Squad>());

			for (i = 0; i < length; i++)
				sortedSquads[squads[i].level -1].Add(squads[i]);

			int internalLength = 0;
			int howManySquadsInLines = 0;
			int howManyBestSquadsInLines = 0;
			SelectionSquadCard selectionSquadCard;

			for (i = maxLevel - 1; i >= 0; i--)
			{
				internalLength = sortedSquads[i].Count;

				for (int j = internalLength - 1; j >= 0; j--)
				{
					squad = sortedSquads[i][j];
					if (SquadPattern.GetSquadPattern(squad.name, squad.level).name == infos.bestSquadName)
					{
						hadBestSquad = true;
						if (howManyBestSquadsInLines % 4 == 0 && howManyBestSquadsInLines != 0)
						{
							bestLineSquad = Instantiate(LinePrefab, BestSquadPanel.transform);
							lines.Add(bestLineSquad);
							howManyBestSquadsInLines = 1;
						} else howManyBestSquadsInLines++;

						squadCard = Instantiate(SelectionSquadCardPrefab, bestLineSquad.transform);
					} else
					{
						if (howManySquadsInLines % 4 == 0 && howManySquadsInLines != 0)
						{
							line = Instantiate(LinePrefab, AllSquadsPanel.transform);
							lines.Add(line);
							howManySquadsInLines = 1;
						}
						else howManySquadsInLines++;

						squadCard = Instantiate(SelectionSquadCardPrefab, line.transform);
					}

					selectionSquadCard = squadCard.GetComponent<SelectionSquadCard>();
					selectionSquadCard.InitCard(SquadPattern.GetSquadPattern(squad.name, squad.level), squad);
					selectionSquadCard.OnSelected += SelectionSquadCard_OnSelected;
					selectionSquadCard.OnDeSelected += SelectionSquadCard_OnDeSelected;
					squadCards.Add(selectionSquadCard);
					LayoutRebuilder.ForceRebuildLayoutImmediate(BestSquadPanel.GetComponentInParent<RectTransform>());
				}
			}

			BestSquadPanel.SetActive(hadBestSquad);
		}

		private void SelectionSquadCard_OnSelected(SelectionSquadCard selectionSquadCard, SquadPattern squadPattern)
		{
			selectedPatterns.Add(squadPattern);
			selectedSquads.Add(selectionSquadCard);

			if(selectedSquads.Count > 2)
			{
				selectedPatterns.RemoveAt(0);
				selectedSquads[0].isSelectable();
				selectedSquads.RemoveAt(0);
			}
		}

		private void SelectionSquadCard_OnDeSelected(SelectionSquadCard selectionSquadCard, SquadPattern squadPattern)
		{
			selectedPatterns.Remove(squadPattern);
			selectedSquads.Remove(selectionSquadCard);
		}
	}
}