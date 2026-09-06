using System;
using System.Collections.Generic;
using KSP.UI.Screens;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Campaign;
using TheRaceForSpace.Core;
using TheRaceForSpace.Funding;
using UnityEngine;

namespace TheRaceForSpace.UI
{
    /// <summary>
    /// Publishes one stock KSP inbox message when the player completes an Offered objective
    /// funding target. Gameplay completion remains owned by the campaign and tracking systems.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public sealed class FundingNotificationUI : MonoBehaviour
    {
        private const string NotificationTitlePrefix = "Funding Target Completed — ";
        private const string NotificationBodySuffix =
            " has been achieved. Your agency is now eligible for a share of the remaining contract funding.";

        private static FundingNotificationUI _activeInstance;

        private readonly Queue<string> _pendingObjectiveIds = new Queue<string>();
        private string _activeSaveFolder;
        private bool _isDuplicateInstance;

        public void Awake()
        {
            // Keep one session-level subscriber so an objective completion is observed regardless of
            // which game scene is active when the campaign records it.
            if (_activeInstance != null && _activeInstance != this)
            {
                _isDuplicateInstance = true;
                Destroy(this);
                return;
            }

            _activeInstance = this;
            AgencyState.ObjectiveCompletionRecorded += OnObjectiveCompletionRecorded;
        }

        public void Start()
        {
            if (_activeInstance != this)
            {
                return;
            }

            // Objective completion can occur in Flight, Space Center, or during an orbital refresh.
            // Persist this presentation subscriber across scene changes instead of recreating it.
            DontDestroyOnLoad(this);
        }

        public void OnDestroy()
        {
            if (_activeInstance != this)
            {
                return;
            }

            AgencyState.ObjectiveCompletionRecorded -= OnObjectiveCompletionRecorded;
            _pendingObjectiveIds.Clear();
            _activeSaveFolder = null;
            _activeInstance = null;
        }

        public void Update()
        {
            if (_isDuplicateInstance || _activeInstance != this)
            {
                return;
            }

            if (HighLogic.LoadedScene == GameScenes.MAINMENU)
            {
                ResetCurrentSave();
                return;
            }

            if (!HighLogic.LoadedSceneIsGame
                || HighLogic.CurrentGame == null
                || !EnsureCurrentSaveFolder())
            {
                return;
            }

            TryPublishNextNotification();
        }

        private void OnObjectiveCompletionRecorded(AgencyState agency, string objectiveId)
        {
            if (agency == null
                || !agency.IsPlayer
                || string.IsNullOrEmpty(objectiveId)
                || !EnsureCurrentSaveFolder())
            {
                return;
            }

            _pendingObjectiveIds.Enqueue(objectiveId);
        }

        private bool EnsureCurrentSaveFolder()
        {
            string currentSaveFolder = HighLogic.SaveFolder;
            if (string.IsNullOrEmpty(currentSaveFolder))
            {
                return false;
            }

            if (string.Equals(_activeSaveFolder, currentSaveFolder, StringComparison.Ordinal))
            {
                return true;
            }

            // A session-level addon can survive loading a different save. Never carry an unsent
            // notification across that boundary because the objective ID may exist in both saves.
            _pendingObjectiveIds.Clear();
            _activeSaveFolder = currentSaveFolder;
            return true;
        }

        private void ResetCurrentSave()
        {
            if (_activeSaveFolder == null && _pendingObjectiveIds.Count == 0)
            {
                return;
            }

            _pendingObjectiveIds.Clear();
            _activeSaveFolder = null;
        }

        private void TryPublishNextNotification()
        {
            if (_pendingObjectiveIds.Count == 0 || MessageSystem.Instance == null)
            {
                return;
            }

            CampaignController campaignController = ModRuntime.Controller;
            if (campaignController == null)
            {
                return;
            }

            string objectiveId = _pendingObjectiveIds.Peek();
            ObjectiveFundingContract contract = FindObjectiveFundingContract(
                campaignController,
                objectiveId);
            if (contract == null)
            {
                _pendingObjectiveIds.Dequeue();
                Debug.LogWarning(
                    "[TheRaceForSpace] Funding notification skipped unknown objective '"
                    + objectiveId
                    + "'.");
                return;
            }

            // Only an Offered, unexpired funding target represents a mission the player was actively
            // pursuing for sponsor funding. Hidden/unoffered objective state is not announced as a
            // completed funding target.
            if (!contract.IsOffered || contract.IsExpired)
            {
                _pendingObjectiveIds.Dequeue();
                return;
            }

            string title = NotificationTitlePrefix + contract.Name;
            string message = contract.Name + NotificationBodySuffix;
            MessageSystem.Instance.AddMessage(new MessageSystem.Message(
                title,
                message,
                MessageSystemButton.MessageButtonColor.GREEN,
                MessageSystemButton.ButtonIcons.MESSAGE));

            _pendingObjectiveIds.Dequeue();
            Debug.Log(
                "[TheRaceForSpace] Funding completion notification sent for '"
                + contract.Id
                + "'.");
        }

        private static ObjectiveFundingContract FindObjectiveFundingContract(
            CampaignController campaignController,
            string objectiveId)
        {
            for (int contractIndex = 0;
                contractIndex < campaignController.ObjectiveFundingContracts.Count;
                contractIndex++)
            {
                ObjectiveFundingContract contract =
                    campaignController.ObjectiveFundingContracts[contractIndex];
                if (contract != null
                    && string.Equals(contract.Id, objectiveId, StringComparison.OrdinalIgnoreCase))
                {
                    return contract;
                }
            }

            return null;
        }
    }
}
