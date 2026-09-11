using System;
using System.Collections.Generic;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Core;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Objectives;

namespace TheRaceForSpace.Rivals
{
    /// <summary>
    /// Result of resolving one due rival live mission. The mission itself is removed from the
    /// active list once this result is produced; the result keeps the resolved snapshot for callers.
    /// </summary>
    internal sealed class RivalLiveMissionResolution
    {
        internal RivalLiveMissionResolution(
            RivalLiveMissionState mission,
            bool wasValid,
            bool succeeded,
            bool objectiveRecorded,
            string satelliteBodyName,
            int lostKerbalCount,
            double scienceAwarded)
        {
            Mission = mission;
            WasValid = wasValid;
            Succeeded = succeeded;
            ObjectiveRecorded = objectiveRecorded;
            SatelliteBodyName = satelliteBodyName;
            LostKerbalCount = lostKerbalCount;
            ScienceAwarded = scienceAwarded;
        }

        public RivalLiveMissionState Mission { get; private set; }
        public bool WasValid { get; private set; }
        public bool Succeeded { get; private set; }
        public bool ObjectiveRecorded { get; private set; }
        public string SatelliteBodyName { get; private set; }
        public int LostKerbalCount { get; private set; }
        public double ScienceAwarded { get; private set; }
    }

    /// <summary>
    /// KSP-independent creation and resolution rules for launched rival Contract and Science missions.
    /// RivalSimulation will later own chronological scheduling; this class only operates on one requested
    /// launch or due mission and never creates a realtime polling loop.
    /// </summary>
    internal static class RivalLiveMissionSimulation
    {
        private const double KerbinDaySeconds = 21600.0;
        private const string BodyLocationPrefix = "body:";
        private const string KerbinLocationPrefix = "kerbin:";
        private const string KscLocationPrefix = "ksc:";

        private static readonly Random SharedRandom = new Random();

        internal static double CalculateSuccessChancePercent(int difficulty)
        {
            if (difficulty < 1 || difficulty > 10)
            {
                return 0.0;
            }

            return 95.0 - (difficulty * 5.0);
        }

        internal static RivalLiveMissionState TryCreateContractMission(
            AgencyState rivalAgency,
            string contractId,
            double launchUniversalTime,
            IList<ObjectiveFundingContract> objectiveFundingContracts,
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts)
        {
            return TryCreateContractMission(
                rivalAgency,
                contractId,
                launchUniversalTime,
                objectiveFundingContracts,
                satelliteNetworkFundingContracts,
                SharedRandom.Next());
        }

        internal static RivalLiveMissionState TryCreateContractMission(
            AgencyState rivalAgency,
            string contractId,
            double launchUniversalTime,
            IList<ObjectiveFundingContract> objectiveFundingContracts,
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts,
            int outcomeSeed)
        {
            RivalProgramState programme;
            if (!TryGetRivalProgramme(rivalAgency, out programme)
                || string.IsNullOrEmpty(contractId)
                || !IsFiniteNonNegative(launchUniversalTime))
            {
                return null;
            }

            ObjectiveFundingContract objectiveFundingContract = FindObjectiveFundingContract(
                contractId,
                objectiveFundingContracts);
            if (objectiveFundingContract != null)
            {
                ObjectiveDefinition objective = ObjectiveCatalogue.FindById(objectiveFundingContract.Id);
                if (objective == null
                    || !objectiveFundingContract.IsOffered
                    || objectiveFundingContract.IsExpired
                    || rivalAgency.HasCompletedObjective(objective.Id)
                    || HasLiveContractTarget(rivalAgency, objective.Id))
                {
                    return null;
                }

                string locationId = GetObjectiveLocationId(objective);
                string satelliteBodyName = IsSatelliteProducingObjective(objective)
                    ? objective.CelestialBodyName
                    : null;
                return TryCreateMission(
                    rivalAgency,
                    RivalMissionType.Contract,
                    objective.Id,
                    null,
                    locationId,
                    launchUniversalTime,
                    objective.Difficulty,
                    objective.RequiredKerbalCount,
                    0.0,
                    outcomeSeed,
                    satelliteBodyName,
                    satelliteNetworkFundingContracts);
            }

            SatelliteNetworkFundingContract networkContract = FindSatelliteNetworkFundingContract(
                contractId,
                satelliteNetworkFundingContracts);
            if (networkContract == null
                || !networkContract.IsAvailable
                || !networkContract.IsOffered
                || string.IsNullOrEmpty(networkContract.CelestialBodyName))
            {
                return null;
            }

            return TryCreateMission(
                rivalAgency,
                RivalMissionType.Contract,
                networkContract.Id,
                null,
                GetOrbitalLocationId(networkContract.CelestialBodyName),
                launchUniversalTime,
                networkContract.Difficulty,
                networkContract.RequiredKerbalCount,
                0.0,
                outcomeSeed,
                networkContract.CelestialBodyName,
                satelliteNetworkFundingContracts);
        }

        internal static RivalLiveMissionState TryCreateScienceMission(
            AgencyState rivalAgency,
            ScienceLaunchPreparationState preparation,
            string locationId,
            double launchUniversalTime)
        {
            return TryCreateScienceMission(
                rivalAgency,
                preparation,
                locationId,
                launchUniversalTime,
                SharedRandom.Next());
        }

        internal static RivalLiveMissionState TryCreateScienceMission(
            AgencyState rivalAgency,
            ScienceLaunchPreparationState preparation,
            string locationId,
            double launchUniversalTime,
            int outcomeSeed)
        {
            RivalProgramState programme;
            if (!TryGetRivalProgramme(rivalAgency, out programme)
                || preparation == null
                || preparation.Subject == null
                || preparation.LaunchProgressPercent < 100
                || preparation.RequiredKerbals < 0
                || !IsFiniteNonNegative(preparation.PlannedScienceReward)
                || !IsFiniteNonNegative(launchUniversalTime)
                || programme.CompletedScienceSubjects.Contains(preparation.Subject)
                || HasLiveScienceSubject(rivalAgency, preparation.Subject)
                || !LocationMatchesScienceSubject(locationId, preparation.Subject))
            {
                return null;
            }

            RivalMissionLocationSettings locationSettings =
                CampaignSettings.GetRivalMissionLocationSettings(locationId);
            if (locationSettings == null)
            {
                return null;
            }

            return TryCreateMission(
                rivalAgency,
                RivalMissionType.Science,
                null,
                preparation.Subject,
                locationId,
                launchUniversalTime,
                locationSettings.ScienceDifficulty,
                preparation.RequiredKerbals,
                preparation.PlannedScienceReward,
                outcomeSeed,
                null,
                null);
        }

        /// <summary>
        /// Returns the earliest active live mission whose completion time is on or before the target UT.
        /// Same-time missions use MissionSequence so save/load and collection ordering stay deterministic.
        /// </summary>
        internal static RivalLiveMissionState GetNextDueMission(
            AgencyState rivalAgency,
            double targetUniversalTime)
        {
            RivalProgramState programme;
            if (!TryGetRivalProgramme(rivalAgency, out programme)
                || !IsFiniteNonNegative(targetUniversalTime))
            {
                return null;
            }

            RivalLiveMissionState selectedMission = null;
            for (int missionIndex = 0; missionIndex < programme.LiveMissions.Count; missionIndex++)
            {
                RivalLiveMissionState mission = programme.LiveMissions[missionIndex];
                if (mission == null
                    || !IsFiniteNonNegative(mission.CompletionUniversalTime)
                    || mission.CompletionUniversalTime > targetUniversalTime)
                {
                    continue;
                }

                if (selectedMission == null
                    || mission.CompletionUniversalTime < selectedMission.CompletionUniversalTime
                    || (mission.CompletionUniversalTime == selectedMission.CompletionUniversalTime
                        && mission.MissionSequence < selectedMission.MissionSequence))
                {
                    selectedMission = mission;
                }
            }

            return selectedMission;
        }

        /// <summary>
        /// Resolves one due live mission. For a successful Science mission the supplied callback is the
        /// authoritative shared-pool boundary: it returns the Science actually remaining and consumes it.
        /// A successful Science mission is left pending when no callback is supplied, preventing silent
        /// completion with an unconsumed player Science subject before KspScienceAdapter is integrated.
        /// </summary>
        internal static RivalLiveMissionResolution ResolveMission(
            AgencyState rivalAgency,
            RivalLiveMissionState mission,
            double currentUniversalTime,
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts,
            Func<ScienceSubjectKey, double> consumeRemainingScience)
        {
            RivalProgramState programme;
            if (!TryGetRivalProgramme(rivalAgency, out programme)
                || mission == null
                || !programme.LiveMissions.Contains(mission)
                || !IsFiniteNonNegative(currentUniversalTime)
                || !IsFiniteNonNegative(mission.CompletionUniversalTime)
                || currentUniversalTime < mission.CompletionUniversalTime)
            {
                return null;
            }

            ObjectiveDefinition objective = null;
            SatelliteNetworkFundingContract networkContract = null;
            if (mission.MissionType == RivalMissionType.Contract)
            {
                objective = ObjectiveCatalogue.FindById(mission.ContractId);
                if (objective == null)
                {
                    networkContract = FindSatelliteNetworkFundingContract(
                        mission.ContractId,
                        satelliteNetworkFundingContracts);
                }

                if (objective == null && networkContract == null)
                {
                    programme.LiveMissions.Remove(mission);
                    return CreateInvalidResolution(mission);
                }
            }
            else if (mission.MissionType == RivalMissionType.Science)
            {
                if (mission.ScienceSubject == null)
                {
                    programme.LiveMissions.Remove(mission);
                    return CreateInvalidResolution(mission);
                }
            }
            else
            {
                programme.LiveMissions.Remove(mission);
                return CreateInvalidResolution(mission);
            }

            if (string.IsNullOrEmpty(mission.LocationId)
                || !IsFiniteNonNegative(mission.LaunchUniversalTime)
                || !IsFinite(mission.DurationDays)
                || mission.DurationDays <= 0.0
                || mission.CompletionUniversalTime < mission.LaunchUniversalTime
                || mission.Difficulty < 1
                || mission.Difficulty > 10
                || !IsFinite(mission.SuccessChancePercent)
                || mission.SuccessChancePercent < 0.0
                || mission.SuccessChancePercent > 100.0
                || mission.AssignedKerbalCount < 0)
            {
                programme.LiveMissions.Remove(mission);
                return CreateInvalidResolution(mission);
            }

            bool succeeded = GetDeterministicRollPercent(mission.OutcomeSeed, 0)
                < mission.SuccessChancePercent;
            if (succeeded
                && mission.MissionType == RivalMissionType.Science
                && consumeRemainingScience == null)
            {
                return null;
            }

            bool objectiveRecorded = false;
            string satelliteBodyName = null;
            double scienceAwarded = 0.0;
            int lostKerbalCount = 0;

            if (succeeded)
            {
                if (mission.MissionType == RivalMissionType.Contract)
                {
                    if (objective != null)
                    {
                        // Sponsor expiry after launch does not invalidate the objective result. Funding lifecycle
                        // remains in CampaignController; this resolution only records the successful mission.
                        objectiveRecorded = rivalAgency.RecordObjectiveCompletion(
                            objective.Id,
                            mission.CompletionUniversalTime);
                        if (objectiveRecorded && IsSatelliteProducingObjective(objective))
                        {
                            satelliteBodyName = objective.CelestialBodyName;
                            AddSatellite(rivalAgency, satelliteBodyName);
                        }
                    }
                    else
                    {
                        satelliteBodyName = networkContract.CelestialBodyName;
                        AddSatellite(rivalAgency, satelliteBodyName);
                    }
                }
                else
                {
                    double availableScience = consumeRemainingScience(mission.ScienceSubject);
                    scienceAwarded = NormalizeNonNegative(availableScience);
                    programme.StoredScience = AddNonNegativeFinite(
                        programme.StoredScience,
                        scienceAwarded);
                    programme.CompletedScienceSubjects.Add(mission.ScienceSubject);
                }
            }
            else
            {
                lostKerbalCount = ApplyDeterministicCasualties(programme, mission);
            }

            // Removing the mission automatically returns every surviving assigned Kerbal to availability;
            // only casualties permanently reduce KerbalsEmployed.
            programme.LiveMissions.Remove(mission);
            return new RivalLiveMissionResolution(
                mission,
                true,
                succeeded,
                objectiveRecorded,
                satelliteBodyName,
                lostKerbalCount,
                scienceAwarded);
        }

        internal static int GetSatelliteCapacityUsed(
            AgencyState rivalAgency,
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts)
        {
            RivalProgramState programme;
            if (!TryGetRivalProgramme(rivalAgency, out programme))
            {
                return 0;
            }

            long capacityUsed = 0;
            foreach (KeyValuePair<string, int> pair in rivalAgency.SatelliteCountsByBody)
            {
                if (pair.Value > 0)
                {
                    capacityUsed += pair.Value;
                    if (capacityUsed >= int.MaxValue)
                    {
                        return int.MaxValue;
                    }
                }
            }

            for (int missionIndex = 0; missionIndex < programme.LiveMissions.Count; missionIndex++)
            {
                RivalLiveMissionState mission = programme.LiveMissions[missionIndex];
                if (MissionProducesSatellite(mission, satelliteNetworkFundingContracts))
                {
                    capacityUsed++;
                    if (capacityUsed >= int.MaxValue)
                    {
                        return int.MaxValue;
                    }
                }
            }

            return (int)capacityUsed;
        }

        internal static bool HasLiveContractTarget(AgencyState rivalAgency, string contractId)
        {
            RivalProgramState programme;
            if (!TryGetRivalProgramme(rivalAgency, out programme) || string.IsNullOrEmpty(contractId))
            {
                return false;
            }

            for (int missionIndex = 0; missionIndex < programme.LiveMissions.Count; missionIndex++)
            {
                RivalLiveMissionState mission = programme.LiveMissions[missionIndex];
                if (mission != null
                    && mission.MissionType == RivalMissionType.Contract
                    && string.Equals(mission.ContractId, contractId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool HasLiveScienceSubject(AgencyState rivalAgency, ScienceSubjectKey subject)
        {
            RivalProgramState programme;
            if (!TryGetRivalProgramme(rivalAgency, out programme) || subject == null)
            {
                return false;
            }

            for (int missionIndex = 0; missionIndex < programme.LiveMissions.Count; missionIndex++)
            {
                RivalLiveMissionState mission = programme.LiveMissions[missionIndex];
                if (mission != null
                    && mission.MissionType == RivalMissionType.Science
                    && subject.Equals(mission.ScienceSubject))
                {
                    return true;
                }
            }

            return false;
        }

        internal static double GetDeterministicRollPercent(int outcomeSeed, int rollSequence)
        {
            unchecked
            {
                uint value = (uint)outcomeSeed
                    + (0x9E3779B9u * (uint)(Math.Max(0, rollSequence) + 1));
                value ^= value >> 16;
                value *= 0x7FEB352Du;
                value ^= value >> 15;
                value *= 0x846CA68Bu;
                value ^= value >> 16;
                return (value / 4294967296.0) * 100.0;
            }
        }

        private static RivalLiveMissionState TryCreateMission(
            AgencyState rivalAgency,
            RivalMissionType missionType,
            string contractId,
            ScienceSubjectKey scienceSubject,
            string locationId,
            double launchUniversalTime,
            int difficulty,
            int requiredKerbals,
            double plannedScienceReward,
            int outcomeSeed,
            string satelliteBodyName,
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts)
        {
            RivalProgramState programme;
            RivalMissionLocationSettings locationSettings =
                CampaignSettings.GetRivalMissionLocationSettings(locationId);
            if (!TryGetRivalProgramme(rivalAgency, out programme)
                || locationSettings == null
                || !IsFinite(locationSettings.DurationDays)
                || locationSettings.DurationDays <= 0.0
                || difficulty < 1
                || difficulty > 10
                || requiredKerbals < 0
                || RivalDevelopmentSimulation.GetKerbalsAvailable(rivalAgency) < requiredKerbals)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(satelliteBodyName)
                && !CanReserveSatelliteCapacity(rivalAgency, satelliteNetworkFundingContracts))
            {
                return null;
            }

            double completionUniversalTime = launchUniversalTime
                + (locationSettings.DurationDays * KerbinDaySeconds);
            if (!IsFiniteNonNegative(completionUniversalTime))
            {
                return null;
            }

            var mission = new RivalLiveMissionState
            {
                MissionSequence = GetNextMissionSequence(programme),
                MissionType = missionType,
                ContractId = contractId,
                ScienceSubject = scienceSubject,
                LocationId = locationId,
                LaunchUniversalTime = launchUniversalTime,
                DurationDays = locationSettings.DurationDays,
                CompletionUniversalTime = completionUniversalTime,
                Difficulty = difficulty,
                SuccessChancePercent = CalculateSuccessChancePercent(difficulty),
                AssignedKerbalCount = requiredKerbals,
                PlannedScienceReward = NormalizeNonNegative(plannedScienceReward),
                OutcomeSeed = outcomeSeed
            };

            programme.LiveMissions.Add(mission);
            return mission;
        }

        private static bool CanReserveSatelliteCapacity(
            AgencyState rivalAgency,
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts)
        {
            int satelliteLimit = RivalDevelopmentSimulation.GetSatelliteLimit(rivalAgency);
            return satelliteLimit == 0
                || GetSatelliteCapacityUsed(rivalAgency, satelliteNetworkFundingContracts) < satelliteLimit;
        }

        private static bool MissionProducesSatellite(
            RivalLiveMissionState mission,
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts)
        {
            if (mission == null || mission.MissionType != RivalMissionType.Contract)
            {
                return false;
            }

            ObjectiveDefinition objective = ObjectiveCatalogue.FindById(mission.ContractId);
            if (objective != null)
            {
                return IsSatelliteProducingObjective(objective);
            }

            return FindSatelliteNetworkFundingContract(
                mission.ContractId,
                satelliteNetworkFundingContracts) != null;
        }

        private static bool IsSatelliteProducingObjective(ObjectiveDefinition objective)
        {
            return objective != null
                && objective.ObjectiveType == ObjectiveType.Orbit
                && objective.CrewRequirement == ObjectiveCrewRequirement.UncrewedProbe;
        }

        private static void AddSatellite(AgencyState rivalAgency, string celestialBodyName)
        {
            if (rivalAgency == null || string.IsNullOrEmpty(celestialBodyName))
            {
                return;
            }

            int currentCount = rivalAgency.GetSatelliteCount(celestialBodyName);
            if (currentCount < int.MaxValue)
            {
                rivalAgency.SetSatelliteCount(celestialBodyName, currentCount + 1);
            }
        }

        private static int ApplyDeterministicCasualties(
            RivalProgramState programme,
            RivalLiveMissionState mission)
        {
            int employedKerbals = Math.Max(0, programme.KerbalsEmployed);
            int atRiskKerbals = Math.Min(Math.Max(0, mission.AssignedKerbalCount), employedKerbals);
            double lossChancePercent = ClampChance(CampaignSettings.RivalKerbalLossChance) * 100.0;
            int lostKerbals = 0;

            for (int crewIndex = 0; crewIndex < atRiskKerbals; crewIndex++)
            {
                if (GetDeterministicRollPercent(mission.OutcomeSeed, crewIndex + 1)
                    < lossChancePercent)
                {
                    lostKerbals++;
                }
            }

            if (lostKerbals <= 0)
            {
                return 0;
            }

            programme.KerbalsEmployed = Math.Max(0, employedKerbals - lostKerbals);
            double insuranceFunds = lostKerbals
                * NormalizeNonNegative(CampaignSettings.RivalInsuranceFundsPerLostKerbal);
            programme.PendingInsuranceFunds = AddNonNegativeFinite(
                programme.PendingInsuranceFunds,
                insuranceFunds);
            return lostKerbals;
        }

        private static string GetObjectiveLocationId(ObjectiveDefinition objective)
        {
            if (objective == null)
            {
                return null;
            }

            if (objective.IsPreOrbitContract)
            {
                if (objective.PreOrbitLine == PreOrbitContractLine.Biome)
                {
                    return GetKerbinBiomeLocationId(objective.RequiredBiomeName);
                }

                return CampaignSettings.RivalPreOrbitLocalLocationId;
            }

            if (objective.ObjectiveType == ObjectiveType.Orbit)
            {
                return GetOrbitalLocationId(objective.CelestialBodyName);
            }

            return null;
        }

        private static string GetKerbinBiomeLocationId(string biomeName)
        {
            if (string.Equals(biomeName, "Grasslands", StringComparison.OrdinalIgnoreCase))
            {
                return "kerbin:grasslands";
            }
            if (string.Equals(biomeName, "Highlands", StringComparison.OrdinalIgnoreCase))
            {
                return "kerbin:highlands";
            }
            if (string.Equals(biomeName, "Mountains", StringComparison.OrdinalIgnoreCase))
            {
                return "kerbin:mountains";
            }
            if (string.Equals(biomeName, "Deserts", StringComparison.OrdinalIgnoreCase))
            {
                return "kerbin:deserts";
            }
            if (string.Equals(biomeName, "Ice Caps", StringComparison.OrdinalIgnoreCase))
            {
                return "kerbin:ice-caps";
            }

            return null;
        }

        private static string GetOrbitalLocationId(string celestialBodyName)
        {
            if (string.IsNullOrEmpty(celestialBodyName))
            {
                return null;
            }

            return string.Equals(celestialBodyName, "Kerbin", StringComparison.OrdinalIgnoreCase)
                ? CampaignSettings.RivalKerbinOrbitLocationId
                : BodyLocationPrefix + celestialBodyName;
        }

        private static bool LocationMatchesScienceSubject(
            string locationId,
            ScienceSubjectKey subject)
        {
            if (subject == null
                || string.IsNullOrEmpty(subject.BodyName)
                || string.IsNullOrEmpty(locationId)
                || CampaignSettings.GetRivalMissionLocationSettings(locationId) == null)
            {
                return false;
            }

            if (string.Equals(subject.BodyName, "Kerbin", StringComparison.OrdinalIgnoreCase))
            {
                return locationId.StartsWith(KerbinLocationPrefix, StringComparison.OrdinalIgnoreCase)
                    || locationId.StartsWith(KscLocationPrefix, StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(
                locationId,
                BodyLocationPrefix + subject.BodyName,
                StringComparison.OrdinalIgnoreCase);
        }

        private static long GetNextMissionSequence(RivalProgramState programme)
        {
            long highestSequence = 0;
            for (int missionIndex = 0; missionIndex < programme.LiveMissions.Count; missionIndex++)
            {
                RivalLiveMissionState mission = programme.LiveMissions[missionIndex];
                if (mission != null && mission.MissionSequence > highestSequence)
                {
                    highestSequence = mission.MissionSequence;
                }
            }

            return highestSequence < long.MaxValue
                ? highestSequence + 1
                : FindFirstUnusedPositiveSequence(programme);
        }

        private static long FindFirstUnusedPositiveSequence(RivalProgramState programme)
        {
            for (long candidate = 1; candidate < long.MaxValue; candidate++)
            {
                bool used = false;
                for (int missionIndex = 0; missionIndex < programme.LiveMissions.Count; missionIndex++)
                {
                    RivalLiveMissionState mission = programme.LiveMissions[missionIndex];
                    if (mission != null && mission.MissionSequence == candidate)
                    {
                        used = true;
                        break;
                    }
                }

                if (!used)
                {
                    return candidate;
                }
            }

            return long.MaxValue;
        }

        private static ObjectiveFundingContract FindObjectiveFundingContract(
            string contractId,
            IList<ObjectiveFundingContract> contracts)
        {
            if (string.IsNullOrEmpty(contractId) || contracts == null)
            {
                return null;
            }

            for (int contractIndex = 0; contractIndex < contracts.Count; contractIndex++)
            {
                ObjectiveFundingContract contract = contracts[contractIndex];
                if (contract != null
                    && string.Equals(contract.Id, contractId, StringComparison.OrdinalIgnoreCase))
                {
                    return contract;
                }
            }

            return null;
        }

        private static SatelliteNetworkFundingContract FindSatelliteNetworkFundingContract(
            string contractId,
            IList<SatelliteNetworkFundingContract> contracts)
        {
            if (string.IsNullOrEmpty(contractId) || contracts == null)
            {
                return null;
            }

            for (int contractIndex = 0; contractIndex < contracts.Count; contractIndex++)
            {
                SatelliteNetworkFundingContract contract = contracts[contractIndex];
                if (contract != null
                    && string.Equals(contract.Id, contractId, StringComparison.OrdinalIgnoreCase))
                {
                    return contract;
                }
            }

            return null;
        }

        private static RivalLiveMissionResolution CreateInvalidResolution(RivalLiveMissionState mission)
        {
            return new RivalLiveMissionResolution(
                mission,
                false,
                false,
                false,
                null,
                0,
                0.0);
        }

        private static bool TryGetRivalProgramme(
            AgencyState rivalAgency,
            out RivalProgramState programme)
        {
            programme = rivalAgency == null || rivalAgency.IsPlayer
                ? null
                : rivalAgency.RivalProgram;
            return programme != null;
        }

        private static double AddNonNegativeFinite(double currentValue, double addedValue)
        {
            double normalizedCurrent = NormalizeNonNegative(currentValue);
            double normalizedAdded = NormalizeNonNegative(addedValue);
            double total = normalizedCurrent + normalizedAdded;
            return IsFinite(total) ? total : double.MaxValue;
        }

        private static double NormalizeNonNegative(double value)
        {
            return IsFinite(value) ? Math.Max(0.0, value) : 0.0;
        }

        private static double ClampChance(double chance)
        {
            if (!IsFinite(chance))
            {
                return 0.0;
            }

            return Math.Max(0.0, Math.Min(1.0, chance));
        }

        private static bool IsFiniteNonNegative(double value)
        {
            return IsFinite(value) && value >= 0.0;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
