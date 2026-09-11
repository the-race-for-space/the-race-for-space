using System;
using System.Collections.Generic;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Core;

namespace TheRaceForSpace.Rivals
{
    /// <summary>
    /// Authoritative funding calculation for one rival funding boundary. The calculation is side-effect
    /// free so CampaignController can apply it at the exact agreed point in the funding-day sequence.
    /// </summary>
    internal sealed class RivalFundingBreakdown
    {
        internal RivalFundingBreakdown(
            double baseIncome,
            double objectiveIncome,
            double satelliteIncome,
            double kerbalPayroll,
            double insuranceDeduction)
        {
            BaseIncome = baseIncome;
            ObjectiveIncome = objectiveIncome;
            SatelliteIncome = satelliteIncome;
            GrossIncome = baseIncome + objectiveIncome + satelliteIncome;
            KerbalPayroll = kerbalPayroll;
            InsuranceDeduction = insuranceDeduction;
            NetPayout = GrossIncome - KerbalPayroll - InsuranceDeduction;
        }

        public double BaseIncome { get; private set; }
        public double ObjectiveIncome { get; private set; }
        public double SatelliteIncome { get; private set; }
        public double GrossIncome { get; private set; }
        public double KerbalPayroll { get; private set; }
        public double InsuranceDeduction { get; private set; }
        public double NetPayout { get; private set; }
    }

    /// <summary>
    /// KSP-independent rival programme development rules. Research and facility construction are invoked
    /// by the campaign funding-boundary coordinator; this class does not own a timer or funding sequence.
    /// </summary>
    internal static class RivalDevelopmentSimulation
    {
        private const double KerbinDaySeconds = 21600.0;
        private const double ScienceCostComparisonTolerance = 0.000001;

        private static readonly Random SharedRandom = new Random();

        internal static int GetFacilityLevel(AgencyState rivalAgency, RivalFacilityType facility)
        {
            RivalProgramState programme;
            if (!TryGetRivalProgramme(rivalAgency, out programme)
                || !Enum.IsDefined(typeof(RivalFacilityType), facility))
            {
                return 0;
            }

            int level;
            if (!programme.FacilityLevels.TryGetValue(facility, out level))
            {
                // Missing levels are treated as the compatibility/default Level 1 rather than granting
                // an upgraded capability to malformed or partially migrated state.
                return 1;
            }

            return Math.Max(1, Math.Min(3, level));
        }

        internal static double GetAdministrationBaseIncomeFunds(AgencyState rivalAgency)
        {
            switch (GetFacilityLevel(rivalAgency, RivalFacilityType.Administration))
            {
                case 1:
                    return NormalizeNonNegative(CampaignSettings.RivalAdministrationLevel1BaseIncomeFunds);
                case 2:
                    return NormalizeNonNegative(CampaignSettings.RivalAdministrationLevel2BaseIncomeFunds);
                case 3:
                    return NormalizeNonNegative(CampaignSettings.RivalAdministrationLevel3BaseIncomeFunds);
                default:
                    return 0.0;
            }
        }

        /// <summary>
        /// Returns the current rival roster limit. Zero means unlimited, matching the configured Level 3 sentinel.
        /// </summary>
        internal static int GetKerbalRosterLimit(AgencyState rivalAgency)
        {
            switch (GetFacilityLevel(rivalAgency, RivalFacilityType.AstronautComplex))
            {
                case 1:
                    return Math.Max(0, CampaignSettings.RivalAstronautComplexLevel1KerbalLimit);
                case 2:
                    return Math.Max(0, CampaignSettings.RivalAstronautComplexLevel2KerbalLimit);
                case 3:
                    return Math.Max(0, CampaignSettings.RivalAstronautComplexLevel3KerbalLimit);
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Returns the current total satellite limit. Zero means unlimited at Mission Control Level 3.
        /// </summary>
        internal static int GetSatelliteLimit(AgencyState rivalAgency)
        {
            switch (GetFacilityLevel(rivalAgency, RivalFacilityType.MissionControl))
            {
                case 1:
                    return Math.Max(0, CampaignSettings.RivalMissionControlLevel1SatelliteLimit);
                case 2:
                    return Math.Max(0, CampaignSettings.RivalMissionControlLevel2SatelliteLimit);
                case 3:
                    return Math.Max(0, CampaignSettings.RivalMissionControlLevel3SatelliteLimit);
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Returns the Science-cost ceiling for rival research. The configured Level 3 value may use zero as unlimited.
        /// </summary>
        internal static double GetResearchScienceCostLimit(AgencyState rivalAgency)
        {
            switch (GetFacilityLevel(rivalAgency, RivalFacilityType.ResearchAndDevelopment))
            {
                case 1:
                    return NormalizeNonNegative(
                        CampaignSettings.RivalResearchAndDevelopmentLevel1ScienceCostLimit);
                case 2:
                    return NormalizeNonNegative(
                        CampaignSettings.RivalResearchAndDevelopmentLevel2ScienceCostLimit);
                case 3:
                    return NormalizeNonNegative(
                        CampaignSettings.RivalResearchAndDevelopmentLevel3ScienceCostLimit);
                default:
                    return 0.0;
            }
        }

        internal static int GetTrackingStationLevel(AgencyState rivalAgency)
        {
            return GetFacilityLevel(rivalAgency, RivalFacilityType.TrackingStation);
        }

        /// <summary>
        /// Returns the authoritative normal Launch Progress chance from VAB plus Launch Pad levels.
        /// </summary>
        internal static double GetNormalLaunchProgressChance(AgencyState rivalAgency)
        {
            double chance = GetFacilityChanceContribution(
                    rivalAgency,
                    RivalFacilityType.VehicleAssemblyBuilding,
                    CampaignSettings.RivalNormalLaunchFacilityLevel1Chance,
                    CampaignSettings.RivalNormalLaunchFacilityLevel2BonusChance,
                    CampaignSettings.RivalNormalLaunchFacilityLevel3BonusChance)
                + GetFacilityChanceContribution(
                    rivalAgency,
                    RivalFacilityType.LaunchPad,
                    CampaignSettings.RivalNormalLaunchFacilityLevel1Chance,
                    CampaignSettings.RivalNormalLaunchFacilityLevel2BonusChance,
                    CampaignSettings.RivalNormalLaunchFacilityLevel3BonusChance);
            return ClampChance(chance);
        }

        /// <summary>
        /// Returns the authoritative Science Launch Progress chance from SPH plus Runway levels.
        /// </summary>
        internal static double GetScienceLaunchProgressChance(AgencyState rivalAgency)
        {
            double chance = GetFacilityChanceContribution(
                    rivalAgency,
                    RivalFacilityType.SpaceplaneHangar,
                    CampaignSettings.RivalScienceLaunchFacilityLevel1Chance,
                    CampaignSettings.RivalScienceLaunchFacilityLevel2BonusChance,
                    CampaignSettings.RivalScienceLaunchFacilityLevel3BonusChance)
                + GetFacilityChanceContribution(
                    rivalAgency,
                    RivalFacilityType.Runway,
                    CampaignSettings.RivalScienceLaunchFacilityLevel1Chance,
                    CampaignSettings.RivalScienceLaunchFacilityLevel2BonusChance,
                    CampaignSettings.RivalScienceLaunchFacilityLevel3BonusChance);
            return ClampChance(chance);
        }

        internal static int GetKerbalsOnMission(AgencyState rivalAgency)
        {
            RivalProgramState programme;
            if (!TryGetRivalProgramme(rivalAgency, out programme))
            {
                return 0;
            }

            long assignedKerbals = 0;
            for (int missionIndex = 0; missionIndex < programme.LiveMissions.Count; missionIndex++)
            {
                RivalLiveMissionState mission = programme.LiveMissions[missionIndex];
                if (mission != null && mission.AssignedKerbalCount > 0)
                {
                    assignedKerbals += mission.AssignedKerbalCount;
                    if (assignedKerbals >= int.MaxValue)
                    {
                        return int.MaxValue;
                    }
                }
            }

            return (int)assignedKerbals;
        }

        internal static int GetKerbalsAvailable(AgencyState rivalAgency)
        {
            RivalProgramState programme;
            if (!TryGetRivalProgramme(rivalAgency, out programme))
            {
                return 0;
            }

            return Math.Max(0, Math.Max(0, programme.KerbalsEmployed) - GetKerbalsOnMission(rivalAgency));
        }

        internal static int GetKerbalsRequiredToHire(AgencyState rivalAgency, int requiredKerbals)
        {
            return Math.Max(0, Math.Max(0, requiredKerbals) - GetKerbalsAvailable(rivalAgency));
        }

        internal static double GetKerbalHireCostFunds(AgencyState rivalAgency, int requiredKerbals)
        {
            int missingKerbals = GetKerbalsRequiredToHire(rivalAgency, requiredKerbals);
            double hireCostFunds = missingKerbals
                * NormalizeNonNegative(CampaignSettings.RivalKerbalHireCostFunds);
            return IsFinite(hireCostFunds) ? hireCostFunds : double.MaxValue;
        }

        internal static bool CanHireMissingKerbals(AgencyState rivalAgency, int requiredKerbals)
        {
            RivalProgramState programme;
            if (!TryGetRivalProgramme(rivalAgency, out programme))
            {
                return false;
            }

            int missingKerbals = GetKerbalsRequiredToHire(rivalAgency, requiredKerbals);
            if (missingKerbals == 0)
            {
                return true;
            }

            int rosterLimit = GetKerbalRosterLimit(rivalAgency);
            long resultingRoster = (long)Math.Max(0, programme.KerbalsEmployed) + missingKerbals;
            if (rosterLimit > 0 && resultingRoster > rosterLimit)
            {
                return false;
            }

            double hireCostFunds = GetKerbalHireCostFunds(rivalAgency, requiredKerbals);
            return IsFinite(rivalAgency.Funds)
                && IsFinite(hireCostFunds)
                && rivalAgency.Funds >= hireCostFunds;
        }

        /// <summary>
        /// Hires only the missing crew required to make the requested mission crew available. The caller
        /// is responsible for invoking this only at the agreed 100%-ready launch point.
        /// </summary>
        internal static bool TryHireMissingKerbals(
            AgencyState rivalAgency,
            int requiredKerbals,
            out int hiredKerbals)
        {
            hiredKerbals = 0;
            RivalProgramState programme;
            if (!TryGetRivalProgramme(rivalAgency, out programme)
                || !CanHireMissingKerbals(rivalAgency, requiredKerbals))
            {
                return false;
            }

            int missingKerbals = GetKerbalsRequiredToHire(rivalAgency, requiredKerbals);
            if (missingKerbals == 0)
            {
                return true;
            }

            double hireCostFunds = GetKerbalHireCostFunds(rivalAgency, requiredKerbals);
            rivalAgency.Funds -= hireCostFunds;
            programme.KerbalsEmployed = Math.Max(0, programme.KerbalsEmployed) + missingKerbals;
            hiredKerbals = missingKerbals;
            return true;
        }

        internal static double GetKerbalPayrollFunds(AgencyState rivalAgency)
        {
            RivalProgramState programme;
            if (!TryGetRivalProgramme(rivalAgency, out programme))
            {
                return 0.0;
            }

            double payrollFunds = Math.Max(0, programme.KerbalsEmployed)
                * NormalizeNonNegative(CampaignSettings.RivalKerbalPayrollFundsPerFundingBoundary);
            return IsFinite(payrollFunds) ? payrollFunds : 0.0;
        }

        /// <summary>
        /// Calculates the one funding-boundary breakdown without mutating Funds or insurance state.
        /// CampaignController will later apply the signed NetPayout and clear pending insurance in order.
        /// </summary>
        internal static RivalFundingBreakdown CalculateFundingBreakdown(
            AgencyState rivalAgency,
            double objectiveIncomeFunds,
            double satelliteIncomeFunds)
        {
            RivalProgramState programme;
            if (!TryGetRivalProgramme(rivalAgency, out programme))
            {
                return new RivalFundingBreakdown(0.0, 0.0, 0.0, 0.0, 0.0);
            }

            return new RivalFundingBreakdown(
                GetAdministrationBaseIncomeFunds(rivalAgency),
                NormalizeNonNegative(objectiveIncomeFunds),
                NormalizeNonNegative(satelliteIncomeFunds),
                GetKerbalPayrollFunds(rivalAgency),
                NormalizeNonNegative(programme.PendingInsuranceFunds));
        }

        internal static double GetFacilityUpgradeCostFunds(int sourceLevel)
        {
            switch (sourceLevel)
            {
                case 1:
                    return NormalizeNonNegative(CampaignSettings.RivalFacilityLevel1To2CostFunds);
                case 2:
                    return NormalizeNonNegative(CampaignSettings.RivalFacilityLevel2To3CostFunds);
                default:
                    return -1.0;
            }
        }

        internal static double GetFacilityUpgradeConstructionDays(int sourceLevel)
        {
            switch (sourceLevel)
            {
                case 1:
                    return NormalizePositive(CampaignSettings.RivalFacilityLevel1To2ConstructionDays);
                case 2:
                    return NormalizePositive(CampaignSettings.RivalFacilityLevel2To3ConstructionDays);
                default:
                    return 0.0;
            }
        }

        internal static bool CompleteDueFacilityConstruction(
            AgencyState rivalAgency,
            double fundingBoundaryUniversalTime)
        {
            RivalProgramState programme;
            if (!TryGetRivalProgramme(rivalAgency, out programme)
                || !IsFiniteNonNegative(fundingBoundaryUniversalTime))
            {
                return false;
            }

            bool changed = false;
            for (int constructionIndex = programme.FacilityConstruction.Count - 1;
                constructionIndex >= 0;
                constructionIndex--)
            {
                RivalFacilityConstructionState construction =
                    programme.FacilityConstruction[constructionIndex];
                if (!IsValidConstruction(construction))
                {
                    // Invalid direct/migrated state must not block every later upgrade forever.
                    programme.FacilityConstruction.RemoveAt(constructionIndex);
                    changed = true;
                    continue;
                }

                if (construction.CompletionUniversalTime > fundingBoundaryUniversalTime)
                {
                    continue;
                }

                int currentLevel = GetFacilityLevel(rivalAgency, construction.Facility);
                programme.FacilityLevels[construction.Facility] =
                    Math.Max(currentLevel, construction.TargetLevel);
                programme.FacilityConstruction.RemoveAt(constructionIndex);
                changed = true;
            }

            return changed;
        }

        internal static RivalFacilityConstructionState TryStartFacilityConstruction(
            AgencyState rivalAgency,
            double fundingBoundaryUniversalTime)
        {
            return TryStartFacilityConstruction(
                rivalAgency,
                fundingBoundaryUniversalTime,
                SharedRandom);
        }

        internal static RivalFacilityConstructionState TryStartFacilityConstruction(
            AgencyState rivalAgency,
            double fundingBoundaryUniversalTime,
            Random random)
        {
            RivalProgramState programme;
            if (!TryGetRivalProgramme(rivalAgency, out programme)
                || !IsFiniteNonNegative(fundingBoundaryUniversalTime)
                || !IsFinite(rivalAgency.Funds)
                || programme.FacilityConstruction.Count > 0)
            {
                return null;
            }

            var eligibleFacilities = new List<RivalFacilityType>();
            Array facilities = Enum.GetValues(typeof(RivalFacilityType));
            for (int facilityIndex = 0; facilityIndex < facilities.Length; facilityIndex++)
            {
                RivalFacilityType facility = (RivalFacilityType)facilities.GetValue(facilityIndex);
                int sourceLevel = GetFacilityLevel(rivalAgency, facility);
                double costFunds = GetFacilityUpgradeCostFunds(sourceLevel);
                double constructionDays = GetFacilityUpgradeConstructionDays(sourceLevel);
                if (costFunds >= 0.0
                    && constructionDays > 0.0
                    && rivalAgency.Funds >= costFunds)
                {
                    eligibleFacilities.Add(facility);
                }
            }

            if (eligibleFacilities.Count == 0)
            {
                return null;
            }

            Random selectionRandom = random ?? SharedRandom;
            RivalFacilityType selectedFacility =
                eligibleFacilities[selectionRandom.Next(eligibleFacilities.Count)];
            int selectedSourceLevel = GetFacilityLevel(rivalAgency, selectedFacility);
            int selectedTargetLevel = selectedSourceLevel + 1;
            double selectedCostFunds = GetFacilityUpgradeCostFunds(selectedSourceLevel);
            double selectedConstructionDays = GetFacilityUpgradeConstructionDays(selectedSourceLevel);
            double completionUniversalTime = fundingBoundaryUniversalTime
                + (selectedConstructionDays * KerbinDaySeconds);
            if (!IsFinite(completionUniversalTime))
            {
                return null;
            }

            var construction = new RivalFacilityConstructionState
            {
                Facility = selectedFacility,
                SourceLevel = selectedSourceLevel,
                TargetLevel = selectedTargetLevel,
                StartUniversalTime = fundingBoundaryUniversalTime,
                CompletionUniversalTime = completionUniversalTime,
                CostPaidFunds = selectedCostFunds
            };

            rivalAgency.Funds -= selectedCostFunds;
            programme.FacilityConstruction.Add(construction);
            return construction;
        }

        internal static bool IsTechEligibleForResearch(
            AgencyState rivalAgency,
            RivalTechNodeDefinition techNode)
        {
            RivalProgramState programme;
            if (!TryGetRivalProgramme(rivalAgency, out programme)
                || techNode == null
                || programme.CurrentResearch != null
                || string.Equals(
                    techNode.Id,
                    RivalProgramState.StartingTechId,
                    StringComparison.OrdinalIgnoreCase)
                || RivalTechCatalogue.ContainsTechId(programme.ResearchedTechIds, techNode.Id)
                || !IsFinite(techNode.ScienceCost)
                || techNode.ScienceCost < 0.0
                || !IsFinite(programme.StoredScience)
                || programme.StoredScience < techNode.ScienceCost)
            {
                return false;
            }

            int researchAndDevelopmentLevel = GetFacilityLevel(
                rivalAgency,
                RivalFacilityType.ResearchAndDevelopment);
            double scienceCostLimit = GetResearchScienceCostLimit(rivalAgency);
            if ((researchAndDevelopmentLevel < 3 && techNode.ScienceCost > scienceCostLimit)
                || (researchAndDevelopmentLevel >= 3
                    && scienceCostLimit > 0.0
                    && techNode.ScienceCost > scienceCostLimit))
            {
                return false;
            }

            return techNode.ArePrerequisitesSatisfied(programme.ResearchedTechIds);
        }

        internal static RivalResearchProjectState TryStartResearch(
            AgencyState rivalAgency,
            double fundingBoundaryUniversalTime)
        {
            return TryStartResearch(rivalAgency, fundingBoundaryUniversalTime, SharedRandom);
        }

        internal static RivalResearchProjectState TryStartResearch(
            AgencyState rivalAgency,
            double fundingBoundaryUniversalTime,
            Random random)
        {
            RivalProgramState programme;
            if (!TryGetRivalProgramme(rivalAgency, out programme)
                || programme.CurrentResearch != null
                || !IsFiniteNonNegative(fundingBoundaryUniversalTime))
            {
                return null;
            }

            var cheapestEligibleTech = new List<RivalTechNodeDefinition>();
            double cheapestScienceCost = double.MaxValue;
            for (int techIndex = 0; techIndex < RivalTechCatalogue.All.Count; techIndex++)
            {
                RivalTechNodeDefinition techNode = RivalTechCatalogue.All[techIndex];
                if (!IsTechEligibleForResearch(rivalAgency, techNode))
                {
                    continue;
                }

                if (techNode.ScienceCost < cheapestScienceCost - ScienceCostComparisonTolerance)
                {
                    cheapestScienceCost = techNode.ScienceCost;
                    cheapestEligibleTech.Clear();
                    cheapestEligibleTech.Add(techNode);
                }
                else if (Math.Abs(techNode.ScienceCost - cheapestScienceCost)
                    <= ScienceCostComparisonTolerance)
                {
                    cheapestEligibleTech.Add(techNode);
                }
            }

            if (cheapestEligibleTech.Count == 0)
            {
                return null;
            }

            double researchDurationDays = NormalizePositive(CampaignSettings.RivalResearchDurationDays);
            if (researchDurationDays <= 0.0)
            {
                return null;
            }

            Random selectionRandom = random ?? SharedRandom;
            RivalTechNodeDefinition selectedTech =
                cheapestEligibleTech[selectionRandom.Next(cheapestEligibleTech.Count)];
            double researchReadyUniversalTime = fundingBoundaryUniversalTime
                + (researchDurationDays * KerbinDaySeconds);
            if (!IsFinite(researchReadyUniversalTime))
            {
                return null;
            }

            double eligibleCompletionFundingUniversalTime = CalculateEligibleCompletionFundingTime(
                fundingBoundaryUniversalTime,
                researchReadyUniversalTime);
            if (!IsFinite(eligibleCompletionFundingUniversalTime))
            {
                return null;
            }

            var research = new RivalResearchProjectState
            {
                TechId = selectedTech.Id,
                ScienceCostPaid = selectedTech.ScienceCost,
                StartUniversalTime = fundingBoundaryUniversalTime,
                ResearchReadyUniversalTime = researchReadyUniversalTime,
                EligibleCompletionFundingUniversalTime = eligibleCompletionFundingUniversalTime
            };

            programme.StoredScience = Math.Max(0.0, programme.StoredScience - selectedTech.ScienceCost);
            programme.CurrentResearch = research;
            return research;
        }

        internal static bool CompleteDueResearch(
            AgencyState rivalAgency,
            double fundingBoundaryUniversalTime)
        {
            RivalProgramState programme;
            if (!TryGetRivalProgramme(rivalAgency, out programme)
                || !IsFiniteNonNegative(fundingBoundaryUniversalTime)
                || programme.CurrentResearch == null)
            {
                return false;
            }

            RivalResearchProjectState research = programme.CurrentResearch;
            if (!IsFiniteNonNegative(research.ResearchReadyUniversalTime)
                || research.ResearchReadyUniversalTime > fundingBoundaryUniversalTime
                || !IsFiniteNonNegative(research.EligibleCompletionFundingUniversalTime)
                || research.EligibleCompletionFundingUniversalTime > fundingBoundaryUniversalTime)
            {
                return false;
            }

            RivalTechNodeDefinition techNode = RivalTechCatalogue.GetById(research.TechId);
            if (techNode != null)
            {
                programme.ResearchedTechIds.Add(techNode.Id);
            }

            // An unknown tech ID can only come from malformed/migrated state. Clear a due invalid project
            // rather than permanently blocking the rival from selecting future valid research.
            programme.CurrentResearch = null;
            return true;
        }

        private static double GetFacilityChanceContribution(
            AgencyState rivalAgency,
            RivalFacilityType facility,
            double level1Chance,
            double level2BonusChance,
            double level3BonusChance)
        {
            int level = GetFacilityLevel(rivalAgency, facility);
            if (level <= 0)
            {
                return 0.0;
            }

            double chance = NormalizeNonNegative(level1Chance);
            if (level >= 2)
            {
                chance += NormalizeNonNegative(level2BonusChance);
            }
            if (level >= 3)
            {
                chance += NormalizeNonNegative(level3BonusChance);
            }

            return chance;
        }

        private static double CalculateEligibleCompletionFundingTime(
            double startFundingUniversalTime,
            double researchReadyUniversalTime)
        {
            double fundingIntervalDays = NormalizePositive(CampaignSettings.FundingIntervalDays);
            if (fundingIntervalDays <= 0.0)
            {
                return researchReadyUniversalTime;
            }

            double fundingIntervalSeconds = fundingIntervalDays * KerbinDaySeconds;
            if (!IsFinite(fundingIntervalSeconds) || fundingIntervalSeconds <= 0.0)
            {
                return researchReadyUniversalTime;
            }

            double elapsedSeconds = Math.Max(0.0, researchReadyUniversalTime - startFundingUniversalTime);
            double intervalsUntilEligible = Math.Max(1.0, Math.Ceiling(elapsedSeconds / fundingIntervalSeconds));
            return startFundingUniversalTime + (intervalsUntilEligible * fundingIntervalSeconds);
        }

        private static bool IsValidConstruction(RivalFacilityConstructionState construction)
        {
            return construction != null
                && Enum.IsDefined(typeof(RivalFacilityType), construction.Facility)
                && construction.SourceLevel >= 1
                && construction.SourceLevel <= 2
                && construction.TargetLevel == construction.SourceLevel + 1
                && construction.TargetLevel <= 3
                && IsFiniteNonNegative(construction.StartUniversalTime)
                && IsFiniteNonNegative(construction.CompletionUniversalTime)
                && construction.CompletionUniversalTime >= construction.StartUniversalTime
                && IsFiniteNonNegative(construction.CostPaidFunds);
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

        private static double ClampChance(double chance)
        {
            if (!IsFinite(chance))
            {
                return 0.0;
            }

            return Math.Max(0.0, Math.Min(1.0, chance));
        }

        private static double NormalizeNonNegative(double value)
        {
            return IsFinite(value) ? Math.Max(0.0, value) : 0.0;
        }

        private static double NormalizePositive(double value)
        {
            return IsFinite(value) && value > 0.0 ? value : 0.0;
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
