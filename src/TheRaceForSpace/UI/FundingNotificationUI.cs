using System;
using System.Collections.Generic;
using KSP.UI.Screens;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Campaign;
using TheRaceForSpace.Core;
using TheRaceForSpace.Funding;
using TheRaceForSpace.KspIntegration;
using TheRaceForSpace.Objectives;
using UnityEngine;

namespace TheRaceForSpace.UI
{
    /// <summary>
    /// Publishes stock KSP inbox messages for important campaign events. Gameplay state remains owned
    /// by the campaign, rival, funding, and KSP-integration systems; this addon only observes live signals.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public sealed class FundingNotificationUI : MonoBehaviour
    {
        private const string PlayerCompletionTitlePrefix = "Funding Target Completed - ";
        private const string PlayerCompletionBodySuffix =
            " has been achieved. Your agency is now eligible for a share of the remaining contract funding.";
        private const string RivalCompletionTitlePrefix = "Rival Objective Completed - ";
        private const string SponsorReviewTitle = "Sponsor Review Complete";
        private const string FundingPayoutTitle = "Campaign Funding Received";

        private sealed class PendingNotification
        {
            public PendingNotification(string title, string body)
            {
                Title = title;
                Body = body;
            }

            public string Title { get; private set; }
            public string Body { get; private set; }
        }

        private static FundingNotificationUI _activeInstance;

        private readonly Queue<PendingNotification> _pendingNotifications =
            new Queue<PendingNotification>();
        private readonly HashSet<string> _knownOfferedTargetKeys =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string _activeSaveFolder;
        private CampaignController _offerSnapshotController;
        private bool _isDuplicateInstance;

        public void Awake()
        {
            // Keep one session-level subscriber so campaign events are observed regardless of which
            // game scene is active when a completion, funding boundary, or sponsor review occurs.
            if (_activeInstance != null && _activeInstance != this)
            {
                _isDuplicateInstance = true;
                Destroy(this);
                return;
            }

            _activeInstance = this;
            AgencyState.ObjectiveCompletionRecorded += OnObjectiveCompletionRecorded;
            CareerFundingAdapter.CampaignFundsAdded += OnCampaignFundsAdded;
        }

        public void Start()
        {
            if (_activeInstance != this)
            {
                return;
            }

            DontDestroyOnLoad(this);
        }

        public void OnDestroy()
        {
            if (_activeInstance != this)
            {
                return;
            }

            AgencyState.ObjectiveCompletionRecorded -= OnObjectiveCompletionRecorded;
            CareerFundingAdapter.CampaignFundsAdded -= OnCampaignFundsAdded;
            ResetCurrentSave();
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

            CampaignController campaignController = ModRuntime.Controller;
            if (campaignController != null && campaignController.NextFundingUniversalTime >= 0.0)
            {
                CaptureNewSponsorOffers(campaignController);
            }

            TryPublishNextNotification();
        }

        private void OnObjectiveCompletionRecorded(AgencyState agency, string objectiveId)
        {
            if (agency == null
                || string.IsNullOrEmpty(objectiveId)
                || !EnsureCurrentSaveFolder())
            {
                return;
            }

            CampaignController campaignController = ModRuntime.Controller;
            if (agency.IsPlayer)
            {
                if (campaignController == null)
                {
                    return;
                }

                ObjectiveFundingContract contract = FindObjectiveFundingContract(
                    campaignController,
                    objectiveId);
                if (contract == null)
                {
                    Debug.LogWarning(
                        "[TheRaceForSpace] Funding notification skipped unknown objective '"
                        + objectiveId
                        + "'.");
                    return;
                }

                // Preserve the existing rule: only an Offered, unexpired target is announced as a
                // player funding completion. Persistence restoration uses the silent restore path.
                if (!contract.IsOffered || contract.IsExpired)
                {
                    return;
                }

                EnqueueNotification(
                    PlayerCompletionTitlePrefix + contract.Name,
                    contract.Name + PlayerCompletionBodySuffix);
                return;
            }

            string objectiveName = GetObjectiveDisplayName(objectiveId);
            EnqueueNotification(
                RivalCompletionTitlePrefix + agency.Name,
                agency.Name + " has completed " + objectiveName + ".");
        }

        private void OnCampaignFundsAdded(double amount)
        {
            if (double.IsNaN(amount)
                || double.IsInfinity(amount)
                || amount <= 0.0
                || !EnsureCurrentSaveFolder())
            {
                return;
            }

            EnqueueNotification(
                FundingPayoutTitle,
                "Your agency received "
                + amount.ToString("N0")
                + " Funds from the campaign funding system.");
        }

        private void CaptureNewSponsorOffers(CampaignController campaignController)
        {
            if (campaignController == null)
            {
                return;
            }

            if (!ReferenceEquals(_offerSnapshotController, campaignController))
            {
                // A freshly created/restored controller may already contain historical offer state.
                // Establish that state silently so loading or quickloading cannot replay old reviews.
                _knownOfferedTargetKeys.Clear();
                CaptureCurrentOffers(campaignController, null);
                _offerSnapshotController = campaignController;
                return;
            }

            var newlyOfferedTargetNames = new List<string>();
            CaptureCurrentOffers(campaignController, newlyOfferedTargetNames);
            if (newlyOfferedTargetNames.Count == 0)
            {
                return;
            }

            string body = newlyOfferedTargetNames.Count == 1
                ? "New funding target offered: " + newlyOfferedTargetNames[0] + "."
                : "New funding targets offered: "
                    + string.Join(", ", newlyOfferedTargetNames.ToArray())
                    + ".";
            EnqueueNotification(SponsorReviewTitle, body);
        }

        private void CaptureCurrentOffers(
            CampaignController campaignController,
            IList<string> newlyOfferedTargetNames)
        {
            for (int contractIndex = 0;
                contractIndex < campaignController.ObjectiveFundingContracts.Count;
                contractIndex++)
            {
                ObjectiveFundingContract contract =
                    campaignController.ObjectiveFundingContracts[contractIndex];
                if (contract == null || !contract.IsOffered)
                {
                    continue;
                }

                string offerKey = "objective:" + contract.Id;
                if (_knownOfferedTargetKeys.Add(offerKey) && newlyOfferedTargetNames != null)
                {
                    newlyOfferedTargetNames.Add(contract.Name);
                }
            }

            for (int contractIndex = 0;
                contractIndex < campaignController.SatelliteNetworkFundingContracts.Count;
                contractIndex++)
            {
                SatelliteNetworkFundingContract contract =
                    campaignController.SatelliteNetworkFundingContracts[contractIndex];
                if (contract == null || !contract.IsOffered)
                {
                    continue;
                }

                string offerKey = "satellite:" + contract.Id;
                if (_knownOfferedTargetKeys.Add(offerKey) && newlyOfferedTargetNames != null)
                {
                    newlyOfferedTargetNames.Add(contract.Name);
                }
            }
        }

        private void EnqueueNotification(string title, string body)
        {
            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(body))
            {
                return;
            }

            _pendingNotifications.Enqueue(new PendingNotification(title, body));
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

            // A session-level addon can survive loading another save. Never carry an unsent message
            // or offer baseline across that boundary because stable IDs may legitimately exist in both.
            _pendingNotifications.Clear();
            _knownOfferedTargetKeys.Clear();
            _offerSnapshotController = null;
            _activeSaveFolder = currentSaveFolder;
            return true;
        }

        private void ResetCurrentSave()
        {
            _pendingNotifications.Clear();
            _knownOfferedTargetKeys.Clear();
            _offerSnapshotController = null;
            _activeSaveFolder = null;
        }

        private void TryPublishNextNotification()
        {
            if (_pendingNotifications.Count == 0 || MessageSystem.Instance == null)
            {
                return;
            }

            PendingNotification notification = _pendingNotifications.Dequeue();
            MessageSystem.Instance.AddMessage(new MessageSystem.Message(
                notification.Title,
                notification.Body,
                MessageSystemButton.MessageButtonColor.GREEN,
                MessageSystemButton.ButtonIcons.MESSAGE));

            Debug.Log(
                "[TheRaceForSpace] Campaign notification sent: '"
                + notification.Title
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

        private static string GetObjectiveDisplayName(string objectiveId)
        {
            ObjectiveDefinition objective = ObjectiveCatalogue.FindById(objectiveId);
            return objective == null || string.IsNullOrEmpty(objective.Name)
                ? objectiveId
                : objective.Name;
        }
    }
}
