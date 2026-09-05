using System;
using TheRaceForSpace.Campaign;
using TheRaceForSpace.Core;
using TheRaceForSpace.Funding;
using TheRaceForSpace.KspIntegration;
using TheRaceForSpace.Objectives;

namespace TheRaceForSpace.ControllerTests
{
    internal static class FundingOfferControllerTests
    {
        private const double KerbinDaySeconds = 21600.0;
        private const double FundingIntervalSeconds = 90.0 * KerbinDaySeconds;

        public static void PreOrbitContractsOpenFourInitialOffersAndLockRemaining()
        {
            ResetEnvironment();
            CampaignSettings.RivalProgressChance = 0.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselMonitor.SetUnavailable();

            var controller = new CampaignController();
            controller.Refresh();

            Require(FindObjectiveFundingContract(controller, ObjectiveCatalogue.DirectedPower1Id).IsOffered,
                "Directed Power I should be offered at campaign start.");
            Require(FindObjectiveFundingContract(controller, ObjectiveCatalogue.Mass1Id).IsOffered,
                "Mass I should be offered at campaign start.");
            Require(FindObjectiveFundingContract(controller, ObjectiveCatalogue.Control1Id).IsOffered,
                "Control I should be offered at campaign start.");
            Require(FindObjectiveFundingContract(controller, ObjectiveCatalogue.Biome1Id).IsOffered,
                "Biome I should be offered at campaign start.");
            Require(!FindObjectiveFundingContract(controller, ObjectiveCatalogue.ProbeOrbitId).IsOffered,
                "Probe Orbit should be locked at campaign start.");
            Equal(4, CountOfferedPreOrbitObjectives(controller));
            Equal(16, CountLockedPreOrbitObjectives(controller));
            Equal(0, CountOfferedNormalObjectives(controller));
        }

        public static void RivalPreOrbitCompletionUnlocksNextLevelForSponsorReview()
        {
            ResetEnvironment();
            CampaignSettings.RivalProgressChance = 0.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselMonitor.SetUnavailable();

            var controller = new CampaignController();
            controller.Refresh();

            controller.FindAgencyById(CampaignController.AsterAgencyId)
                .RecordObjectiveCompletion(ObjectiveCatalogue.DirectedPower1Id, 10.0);
            Planetarium.CurrentUniversalTime = 20.0;
            controller.Refresh(false);

            ObjectiveFundingContract first = FindObjectiveFundingContract(
                controller,
                ObjectiveCatalogue.DirectedPower1Id);
            ObjectiveFundingContract second = FindObjectiveFundingContract(
                controller,
                ObjectiveCatalogue.DirectedPower2Id);
            ObjectiveFundingContract third = FindObjectiveFundingContract(
                controller,
                ObjectiveCatalogue.DirectedPower3Id);

            Require(first.HasStarted,
                "A rival completing Directed Power I should start its shared payout lifecycle.");
            Require(!second.IsOffered,
                "Directed Power II should remain Unlocked until a sponsor review offers it.");
            Require(controller.IsObjectiveFundingContractAvailable(second),
                "A rival completion should globally unlock Directed Power II.");
            Require(!controller.IsObjectiveFundingContractAvailable(third),
                "Directed Power III should remain locked until any agency completes level two.");
        }

        public static void UnlockedPreOrbitLevelsJoinSponsorReview()
        {
            ResetEnvironment();
            CampaignSettings.RivalProgressChance = 0.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselMonitor.SetUnavailable();

            var controller = new CampaignController();
            controller.Refresh();

            string[] openingObjectiveIds =
            {
                ObjectiveCatalogue.DirectedPower1Id,
                ObjectiveCatalogue.Mass1Id,
                ObjectiveCatalogue.Control1Id,
                ObjectiveCatalogue.Biome1Id
            };
            string[] secondLevelIds =
            {
                ObjectiveCatalogue.DirectedPower2Id,
                ObjectiveCatalogue.Mass2Id,
                ObjectiveCatalogue.Control2Id,
                ObjectiveCatalogue.Biome2Id
            };

            for (int objectiveIndex = 0; objectiveIndex < openingObjectiveIds.Length; objectiveIndex++)
            {
                controller.FindAgencyById(CampaignController.AsterAgencyId)
                    .RecordObjectiveCompletion(openingObjectiveIds[objectiveIndex], 10.0);
            }

            Planetarium.CurrentUniversalTime = 20.0;
            controller.Refresh(false);

            for (int objectiveIndex = 0; objectiveIndex < secondLevelIds.Length; objectiveIndex++)
            {
                ObjectiveFundingContract contract = FindObjectiveFundingContract(controller, secondLevelIds[objectiveIndex]);
                Require(controller.IsObjectiveFundingContractAvailable(contract),
                    "Each second-level pre-orbit contract should unlock after its predecessor completes.");
                Require(!contract.IsOffered,
                    "Unlocked pre-orbit contracts should wait for the sponsor review.");
            }

            Planetarium.CurrentUniversalTime = FundingIntervalSeconds;
            controller.Refresh(false);

            int offeredSecondLevelCount = 0;
            for (int objectiveIndex = 0; objectiveIndex < secondLevelIds.Length; objectiveIndex++)
            {
                if (FindObjectiveFundingContract(controller, secondLevelIds[objectiveIndex]).IsOffered)
                {
                    offeredSecondLevelCount++;
                }
            }

            Equal(4, offeredSecondLevelCount);
            Equal(8, CountOfferedPreOrbitObjectives(controller));
        }

        public static void PreOrbitOffersDoNotConsumeNormalObjectiveLimit()
        {
            ResetEnvironment();
            CampaignSettings.RivalProgressChance = 0.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselMonitor.SetUnavailable();

            var controller = new CampaignController();
            controller.Refresh();

            // Keep all four opening pre-orbit contracts offered and uncompleted, then create normal
            // post-Probe availability. PreOrbit offers must not consume the normal two offer slots.
            controller.PlayerAgency.RecordObjectiveCompletion(ObjectiveCatalogue.ProbeOrbitId, 1.0);
            Planetarium.CurrentUniversalTime = 1000.0;
            controller.Refresh(false);

            Planetarium.CurrentUniversalTime = FundingIntervalSeconds;
            controller.Refresh(false);

            Equal(4, CountOfferedPreOrbitObjectives(controller));
            Equal(2, CountOfferedNormalObjectives(controller));
        }

        public static void AnyPreOrbitLevelFiveOffersProbeOrbit()
        {
            string[] levelFiveIds =
            {
                ObjectiveCatalogue.DirectedPower5Id,
                ObjectiveCatalogue.Mass5Id,
                ObjectiveCatalogue.Control5Id,
                ObjectiveCatalogue.Biome5Id
            };

            for (int testIndex = 0; testIndex < levelFiveIds.Length; testIndex++)
            {
                ResetEnvironment();
                CampaignSettings.RivalProgressChance = 0.0;
                Planetarium.CurrentUniversalTime = 0.0;
                KspVesselMonitor.SetUnavailable();

                var controller = new CampaignController();
                controller.Refresh();

                controller.FindAgencyById(CampaignController.AsterAgencyId)
                    .RecordObjectiveCompletion(levelFiveIds[testIndex], 10.0);
                Planetarium.CurrentUniversalTime = 20.0;
                controller.Refresh(false);

                ObjectiveFundingContract probeOrbit = FindObjectiveFundingContract(
                    controller,
                    ObjectiveCatalogue.ProbeOrbitId);
                Require(probeOrbit.IsOffered,
                    "Any agency completing any starter level-five objectiveCompletion should offer Probe Orbit immediately.");
            }
        }

        public static void UnlockedFundingWaitsForFundingReview()
        {
            ResetEnvironment();
            CampaignSettings.RivalProgressChance = 0.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselMonitor.SetUnavailable();

            var controller = new CampaignController();
            controller.FindAgencyById(CampaignController.AsterAgencyId).Funds = 0.0;
            controller.FindAgencyById(CampaignController.CobaltAgencyId).Funds = 0.0;
            controller.Refresh();
            OfferAndCompleteAllPreOrbitObjectives(controller, 1.0);

            ObjectiveFundingContract probeOrbit = FindObjectiveFundingContract(
                controller,
                ObjectiveCatalogue.ProbeOrbitId);
            ObjectiveFundingContract crewedOrbit = FindObjectiveFundingContract(
                controller,
                ObjectiveCatalogue.CrewedOrbitId);
            ObjectiveFundingContract munProbeOrbit = FindObjectiveFundingContract(
                controller,
                ObjectiveCatalogue.MunProbeOrbitId);
            ObjectiveFundingContract minmusProbeOrbit = FindObjectiveFundingContract(
                controller,
                ObjectiveCatalogue.MinmusProbeOrbitId);
            SatelliteNetworkFundingContract kerbinNetwork = FindFunding(
                controller,
                FundingContractCatalogue.KerbinNetworkId);

            // This regression begins at the post-Probe-Orbit state. Offer the contract explicitly
            // rather than depending on the starter Level V immediate-offer path.
            probeOrbit.Offer();
            controller.PlayerAgency.RecordObjectiveCompletion(ObjectiveCatalogue.ProbeOrbitId, 1.0);
            controller.PlayerAgency.SetSatelliteCount("Kerbin", 1);

            Planetarium.CurrentUniversalTime = 1000.0;
            controller.Refresh(false);

            Require(probeOrbit.HasStarted, "The Probe Orbit offer should start after completion.");
            Require(controller.IsObjectiveFundingContractAvailable(crewedOrbit), "Crewed Orbit should unlock after Probe Orbit.");
            Require(controller.IsObjectiveFundingContractAvailable(munProbeOrbit), "Mun Probe Orbit should be unlocked.");
            Require(controller.IsObjectiveFundingContractAvailable(minmusProbeOrbit), "Minmus Probe Orbit should be unlocked.");
            Require(kerbinNetwork.IsAvailable, "Kerbin network funding should be unlocked.");
            Require(!crewedOrbit.IsOffered && !munProbeOrbit.IsOffered && !minmusProbeOrbit.IsOffered,
                "Unlocked normal objectiveCompletion funding must wait for a funding review.");
            Require(!kerbinNetwork.IsOffered,
                "Unlocked satellite funding must wait for a funding review.");
            Equal(0.0, controller.GetSatelliteCurrentPayout(controller.PlayerAgency, kerbinNetwork));

            Planetarium.CurrentUniversalTime = FundingIntervalSeconds;
            controller.Refresh(false);

            int newObjectiveOffers = (crewedOrbit.IsOffered ? 1 : 0)
                + (munProbeOrbit.IsOffered ? 1 : 0)
                + (minmusProbeOrbit.IsOffered ? 1 : 0);
            Equal(2, newObjectiveOffers);
            Require(kerbinNetwork.IsOffered,
                "The funding review should offer the only unlocked satellite candidate.");

            Equal(75000.0, CareerFundingAdapter.TotalAddedFunds);
            Equal(1, CareerFundingAdapter.AddFundsCalls);
        }

        public static void FundingReviewDoesNotCascadeCompletedOffers()
        {
            ResetEnvironment();
            CampaignSettings.RivalProgressChance = 0.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselMonitor.SetUnavailable();

            var controller = new CampaignController();
            controller.FindAgencyById(CampaignController.AsterAgencyId).Funds = 0.0;
            controller.FindAgencyById(CampaignController.CobaltAgencyId).Funds = 0.0;
            controller.Refresh();
            OfferAndCompleteAllPreOrbitObjectives(controller, 1.0);

            FindObjectiveFundingContract(controller, ObjectiveCatalogue.ProbeOrbitId).Offer();

            // Record every normal uncrewed orbital objective before it is sponsored. PreOrbit
            // contracts are pre-completed in the setup so they do not compete for these review slots.
            for (int contractIndex = 0;
                contractIndex < controller.ObjectiveFundingContracts.Count;
                contractIndex++)
            {
                ObjectiveFundingContract contract =
                    controller.ObjectiveFundingContracts[contractIndex];
                ObjectiveDefinition objective = ObjectiveCatalogue.FindById(contract.Id);
                if (objective != null
                    && objective.ObjectiveType == ObjectiveType.Orbit
                    && objective.CrewRequirement == ObjectiveCrewRequirement.UncrewedProbe)
                {
                    controller.PlayerAgency.RecordObjectiveCompletion(contract.Id, 1.0);
                }
            }

            FindObjectiveFundingContract(controller, ObjectiveCatalogue.CrewedOrbitId).Offer();

            Planetarium.CurrentUniversalTime = 1000.0;
            controller.Refresh(false);

            Planetarium.CurrentUniversalTime = FundingIntervalSeconds;
            controller.Refresh(false);

            int laterOfferedCount = 0;
            ObjectiveFundingContract laterOfferedContract = null;
            for (int contractIndex = 0;
                contractIndex < controller.ObjectiveFundingContracts.Count;
                contractIndex++)
            {
                ObjectiveFundingContract contract =
                    controller.ObjectiveFundingContracts[contractIndex];
                if (!contract.IsOffered
                    || IsPreOrbitObjective(contract)
                    || string.Equals(contract.Id, ObjectiveCatalogue.ProbeOrbitId, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(contract.Id, ObjectiveCatalogue.CrewedOrbitId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                laterOfferedCount++;
                laterOfferedContract = contract;
            }

            Equal(1, laterOfferedCount);
            Require(laterOfferedContract != null && laterOfferedContract.HasStarted,
                "The selected pre-completed contract should start after being offered.");
            Equal(3, CountOfferedNormalObjectives(controller));
        }

        public static void SatelliteFulfilmentWaitsForFundingReview()
        {
            ResetEnvironment();
            CampaignSettings.RivalProgressChance = 0.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselMonitor.SetUnavailable();

            var controller = new CampaignController();
            controller.FindAgencyById(CampaignController.AsterAgencyId).Funds = 0.0;
            controller.FindAgencyById(CampaignController.CobaltAgencyId).Funds = 0.0;
            controller.Refresh();

            SatelliteNetworkFundingContract kerbin = FindFunding(controller, FundingContractCatalogue.KerbinNetworkId);
            SatelliteNetworkFundingContract mun = FindFunding(controller, FundingContractCatalogue.MunNetworkId);
            SatelliteNetworkFundingContract minmus = FindFunding(controller, FundingContractCatalogue.MinmusNetworkId);

            kerbin.Unlock();
            kerbin.Offer();
            mun.Unlock();
            mun.Offer();
            minmus.Unlock();

            controller.PlayerAgency.SetSatelliteCount("Kerbin", 10);
            controller.PlayerAgency.SetSatelliteCount("Mun", 1);

            Planetarium.CurrentUniversalTime = 1000.0;
            controller.Refresh(false);

            Require(kerbin.HasReachedSatelliteTarget,
                "Reaching 100% should permanently fulfil the Kerbin offer slot.");
            Require(!mun.HasReachedSatelliteTarget,
                "A partial Mun network should still consume an unfinished satellite slot.");
            Require(!minmus.IsOffered,
                "The newly vacant satellite slot must remain empty before funding day.");

            Planetarium.CurrentUniversalTime = FundingIntervalSeconds;
            controller.Refresh(false);

            Require(minmus.IsOffered,
                "The next funding review should fill the vacancy created by the fulfilled Kerbin network.");
        }

        public static void SatelliteReviewCapsUnfulfilledOffersAtTwo()
        {
            ResetEnvironment();
            CampaignSettings.RivalProgressChance = 0.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselMonitor.SetUnavailable();

            var controller = new CampaignController();
            controller.Refresh();

            SatelliteNetworkFundingContract kerbin = FindFunding(controller, FundingContractCatalogue.KerbinNetworkId);
            SatelliteNetworkFundingContract mun = FindFunding(controller, FundingContractCatalogue.MunNetworkId);
            SatelliteNetworkFundingContract minmus = FindFunding(controller, FundingContractCatalogue.MinmusNetworkId);
            kerbin.Unlock();
            mun.Unlock();
            minmus.Unlock();

            Planetarium.CurrentUniversalTime = FundingIntervalSeconds;
            controller.Refresh(false);

            int offeredCount = (kerbin.IsOffered ? 1 : 0)
                + (mun.IsOffered ? 1 : 0)
                + (minmus.IsOffered ? 1 : 0);
            Equal(2, offeredCount);
        }

        public static void CrossedFundingBoundariesEachRunSponsorReview()
        {
            ResetEnvironment();
            CampaignSettings.RivalProgressChance = 0.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselMonitor.SetUnavailable();

            var controller = new CampaignController();
            controller.FindAgencyById(CampaignController.AsterAgencyId).Funds = 0.0;
            controller.FindAgencyById(CampaignController.CobaltAgencyId).Funds = 0.0;
            controller.Refresh();
            OfferAndCompleteAllPreOrbitObjectives(controller, 1.0);

            FindObjectiveFundingContract(controller, ObjectiveCatalogue.ProbeOrbitId).Offer();

            for (int contractIndex = 0;
                contractIndex < controller.ObjectiveFundingContracts.Count;
                contractIndex++)
            {
                ObjectiveFundingContract contract =
                    controller.ObjectiveFundingContracts[contractIndex];
                ObjectiveDefinition objective = ObjectiveCatalogue.FindById(contract.Id);
                if (objective != null
                    && objective.ObjectiveType == ObjectiveType.Orbit
                    && objective.CrewRequirement == ObjectiveCrewRequirement.UncrewedProbe)
                {
                    controller.PlayerAgency.RecordObjectiveCompletion(contract.Id, 1.0);
                }
            }

            FindObjectiveFundingContract(controller, ObjectiveCatalogue.CrewedOrbitId).Offer();

            Planetarium.CurrentUniversalTime = FundingIntervalSeconds * 2.0;
            controller.Refresh(false);

            int laterOfferedCount = 0;
            for (int contractIndex = 0;
                contractIndex < controller.ObjectiveFundingContracts.Count;
                contractIndex++)
            {
                ObjectiveFundingContract contract =
                    controller.ObjectiveFundingContracts[contractIndex];
                if (contract.IsOffered
                    && !IsPreOrbitObjective(contract)
                    && !string.Equals(contract.Id, ObjectiveCatalogue.ProbeOrbitId, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(contract.Id, ObjectiveCatalogue.CrewedOrbitId, StringComparison.OrdinalIgnoreCase))
                {
                    laterOfferedCount++;
                }
            }

            Equal(2, laterOfferedCount);
            Equal(FundingIntervalSeconds * 3.0, controller.NextFundingUniversalTime);
        }

        private static int CountOfferedPreOrbitObjectives(CampaignController controller)
        {
            int count = 0;
            for (int contractIndex = 0;
                contractIndex < controller.ObjectiveFundingContracts.Count;
                contractIndex++)
            {
                ObjectiveFundingContract contract = controller.ObjectiveFundingContracts[contractIndex];
                if (contract.IsOffered && IsPreOrbitObjective(contract))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountLockedPreOrbitObjectives(CampaignController controller)
        {
            int count = 0;
            for (int contractIndex = 0;
                contractIndex < controller.ObjectiveFundingContracts.Count;
                contractIndex++)
            {
                ObjectiveFundingContract contract = controller.ObjectiveFundingContracts[contractIndex];
                if (IsPreOrbitObjective(contract)
                    && !contract.IsOffered
                    && !controller.IsObjectiveFundingContractAvailable(contract))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountOfferedNormalObjectives(CampaignController controller)
        {
            int count = 0;
            for (int contractIndex = 0;
                contractIndex < controller.ObjectiveFundingContracts.Count;
                contractIndex++)
            {
                ObjectiveFundingContract contract = controller.ObjectiveFundingContracts[contractIndex];
                if (contract.IsOffered && !IsPreOrbitObjective(contract))
                {
                    count++;
                }
            }

            return count;
        }

        private static void OfferAndCompleteAllPreOrbitObjectives(
            CampaignController controller,
            double completionUniversalTime)
        {
            for (int contractIndex = 0;
                contractIndex < controller.ObjectiveFundingContracts.Count;
                contractIndex++)
            {
                ObjectiveFundingContract contract = controller.ObjectiveFundingContracts[contractIndex];
                if (!IsPreOrbitObjective(contract))
                {
                    continue;
                }

                contract.Offer();
                controller.FindAgencyById(CampaignController.AsterAgencyId)
                    .RecordObjectiveCompletion(contract.Id, completionUniversalTime);
            }
        }

        private static bool IsPreOrbitObjective(ObjectiveFundingContract contract)
        {
            ObjectiveDefinition objective = contract == null
                ? null
                : ObjectiveCatalogue.FindById(contract.Id);
            return objective != null && objective.IsPreOrbitContract;
        }

        private static ObjectiveFundingContract FindObjectiveFundingContract(
            CampaignController controller,
            string contractId)
        {
            for (int contractIndex = 0;
                contractIndex < controller.ObjectiveFundingContracts.Count;
                contractIndex++)
            {
                ObjectiveFundingContract contract =
                    controller.ObjectiveFundingContracts[contractIndex];
                if (contract != null
                    && string.Equals(contract.Id, contractId, StringComparison.OrdinalIgnoreCase))
                {
                    return contract;
                }
            }

            throw new InvalidOperationException("Missing objectiveCompletion contract '" + contractId + "'.");
        }

        private static SatelliteNetworkFundingContract FindFunding(
            CampaignController controller,
            string contractId)
        {
            for (int contractIndex = 0; contractIndex < controller.SatelliteNetworkFundingContracts.Count; contractIndex++)
            {
                SatelliteNetworkFundingContract contract = controller.SatelliteNetworkFundingContracts[contractIndex];
                if (contract != null
                    && string.Equals(contract.Id, contractId, StringComparison.OrdinalIgnoreCase))
                {
                    return contract;
                }
            }

            throw new InvalidOperationException("Missing satellite contract '" + contractId + "'.");
        }

        private static void ResetEnvironment()
        {
            CampaignSettings.ResetToDefaults();
            Planetarium.Reset();
            CareerFundingAdapter.Reset();
            KspVesselMonitor.Reset();
            ModPersistenceScenario.Reset();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!object.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    "Expected '" + expected + "' but got '" + actual + "'.");
            }
        }
    }
}
