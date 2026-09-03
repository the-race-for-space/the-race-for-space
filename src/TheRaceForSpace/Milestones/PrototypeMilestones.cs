using System;
using System.Collections.Generic;

namespace TheRaceForSpace.Milestones
{
    /// <summary>
    /// Defines the achievement milestones used by the current prototype.
    /// </summary>
    public static class PrototypeMilestones
    {
        public const string DirectedPower1Id = "directed-power-1";
        public const string DirectedPower2Id = "directed-power-2";
        public const string DirectedPower3Id = "directed-power-3";
        public const string DirectedPower4Id = "directed-power-4";
        public const string DirectedPower5Id = "directed-power-5";
        public const string Mass1Id = "mass-1";
        public const string Mass2Id = "mass-2";
        public const string Mass3Id = "mass-3";
        public const string Mass4Id = "mass-4";
        public const string Mass5Id = "mass-5";
        public const string Control1Id = "control-1";
        public const string Control2Id = "control-2";
        public const string Control3Id = "control-3";
        public const string Control4Id = "control-4";
        public const string Control5Id = "control-5";
        public const string Biome1Id = "biome-1";
        public const string Biome2Id = "biome-2";
        public const string Biome3Id = "biome-3";
        public const string Biome4Id = "biome-4";
        public const string Biome5Id = "biome-5";

        public const string ProbeOrbitId = "probe-orbit";
        public const string CrewedOrbitId = "crewed-orbit";
        public const string MunProbeOrbitId = "mun-probe-orbit";
        public const string MinmusProbeOrbitId = "minmus-probe-orbit";
        public const string DunaProbeOrbitId = "duna-probe-orbit";
        public const string MunCrewedOrbitId = "mun-crewed-orbit";
        public const string MinmusCrewedOrbitId = "minmus-crewed-orbit";
        public const string DunaCrewedOrbitId = "duna-crewed-orbit";
        public const string MohoProbeOrbitId = "moho-probe-orbit";
        public const string MohoCrewedOrbitId = "moho-crewed-orbit";
        public const string EveProbeOrbitId = "eve-probe-orbit";
        public const string EveCrewedOrbitId = "eve-crewed-orbit";
        public const string GillyProbeOrbitId = "gilly-probe-orbit";
        public const string GillyCrewedOrbitId = "gilly-crewed-orbit";
        public const string IkeProbeOrbitId = "ike-probe-orbit";
        public const string IkeCrewedOrbitId = "ike-crewed-orbit";
        public const string DresProbeOrbitId = "dres-probe-orbit";
        public const string DresCrewedOrbitId = "dres-crewed-orbit";
        public const string JoolProbeOrbitId = "jool-probe-orbit";
        public const string JoolCrewedOrbitId = "jool-crewed-orbit";
        public const string LaytheProbeOrbitId = "laythe-probe-orbit";
        public const string LaytheCrewedOrbitId = "laythe-crewed-orbit";
        public const string VallProbeOrbitId = "vall-probe-orbit";
        public const string VallCrewedOrbitId = "vall-crewed-orbit";
        public const string TyloProbeOrbitId = "tylo-probe-orbit";
        public const string TyloCrewedOrbitId = "tylo-crewed-orbit";
        public const string BopProbeOrbitId = "bop-probe-orbit";
        public const string BopCrewedOrbitId = "bop-crewed-orbit";
        public const string PolProbeOrbitId = "pol-probe-orbit";
        public const string PolCrewedOrbitId = "pol-crewed-orbit";
        public const string EelooProbeOrbitId = "eeloo-probe-orbit";
        public const string EelooCrewedOrbitId = "eeloo-crewed-orbit";

        private const double StarterRewardFundsPerLevel = 10000.0;
        private const double StarterRivalProgressCostFundsPerLevel = 1000.0;

        private static readonly IList<MilestoneDefinition> StarterDefinitions =
            new List<MilestoneDefinition>
            {
                CreateStarterMilestone(DirectedPower1Id, "Directed Power I", MilestoneObjectiveType.DirectedPower, StarterContractLine.DirectedPower, 1, "Reach 600 m/s without exceeding 70 km altitude, then impact Kerbin.", null),
                CreateStarterMilestone(DirectedPower2Id, "Directed Power II", MilestoneObjectiveType.DirectedPower, StarterContractLine.DirectedPower, 2, "Reach 1,100 m/s without exceeding 70 km altitude, then impact Kerbin.", DirectedPower1Id),
                CreateStarterMilestone(DirectedPower3Id, "Directed Power III", MilestoneObjectiveType.DirectedPower, StarterContractLine.DirectedPower, 3, "Reach 1,400 m/s without exceeding 70 km altitude, then impact Kerbin.", DirectedPower2Id),
                CreateStarterMilestone(DirectedPower4Id, "Directed Power IV", MilestoneObjectiveType.DirectedPower, StarterContractLine.DirectedPower, 4, "Reach 1,700 m/s without exceeding 70 km altitude, then impact Kerbin.", DirectedPower3Id),
                CreateStarterMilestone(DirectedPower5Id, "Directed Power V", MilestoneObjectiveType.DirectedPower, StarterContractLine.DirectedPower, 5, "Reach 2,000 m/s without exceeding 70 km altitude, then impact Kerbin.", DirectedPower4Id),

                CreateStarterMilestone(Mass1Id, "Mass I", MilestoneObjectiveType.DeliveredMass, StarterContractLine.Mass, 1, "Carry at least 1 t of remaining vessel mass 25 km from the Space Centre.", null),
                CreateStarterMilestone(Mass2Id, "Mass II", MilestoneObjectiveType.DeliveredMass, StarterContractLine.Mass, 2, "Carry at least 2.5 t of remaining vessel mass 75 km from the Space Centre.", Mass1Id),
                CreateStarterMilestone(Mass3Id, "Mass III", MilestoneObjectiveType.DeliveredMass, StarterContractLine.Mass, 3, "Carry at least 5 t of remaining vessel mass 150 km from the Space Centre.", Mass2Id),
                CreateStarterMilestone(Mass4Id, "Mass IV", MilestoneObjectiveType.DeliveredMass, StarterContractLine.Mass, 4, "Carry at least 10 t of remaining vessel mass 300 km from the Space Centre.", Mass3Id),
                CreateStarterMilestone(Mass5Id, "Mass V", MilestoneObjectiveType.DeliveredMass, StarterContractLine.Mass, 5, "Carry at least 20 t of remaining vessel mass 600 km from the Space Centre.", Mass4Id),

                CreateStarterMilestone(Control1Id, "Control I", MilestoneObjectiveType.AltitudeHold, StarterContractLine.Control, 1, "With crew aboard, remain between 2-5 km for 30 seconds, then land safely on Kerbin.", null, MilestoneCrewRequirement.Crewed),
                CreateStarterMilestone(Control2Id, "Control II", MilestoneObjectiveType.AltitudeHold, StarterContractLine.Control, 2, "With crew aboard, remain between 8-12 km for 45 seconds, then land safely on Kerbin.", Control1Id, MilestoneCrewRequirement.Crewed),
                CreateStarterMilestone(Control3Id, "Control III", MilestoneObjectiveType.AltitudeHold, StarterContractLine.Control, 3, "With crew aboard, remain between 15-25 km for 60 seconds, then land safely on Kerbin.", Control2Id, MilestoneCrewRequirement.Crewed),
                CreateStarterMilestone(Control4Id, "Control IV", MilestoneObjectiveType.AltitudeHold, StarterContractLine.Control, 4, "With crew aboard, remain between 30-40 km for 75 seconds, then land safely on Kerbin.", Control3Id, MilestoneCrewRequirement.Crewed),
                CreateStarterMilestone(Control5Id, "Control V", MilestoneObjectiveType.AltitudeHold, StarterContractLine.Control, 5, "With crew aboard, remain between 50-65 km for 90 seconds, then land safely on Kerbin.", Control4Id, MilestoneCrewRequirement.Crewed),

                CreateStarterMilestone(Biome1Id, "Biome I - Grasslands", MilestoneObjectiveType.BiomeVisit, StarterContractLine.Biome, 1, "Visit Kerbin's Grasslands biome without entering orbit.", null),
                CreateStarterMilestone(Biome2Id, "Biome II - Highlands", MilestoneObjectiveType.BiomeVisit, StarterContractLine.Biome, 2, "Visit Kerbin's Highlands biome without entering orbit.", Biome1Id),
                CreateStarterMilestone(Biome3Id, "Biome III - Mountains", MilestoneObjectiveType.BiomeVisit, StarterContractLine.Biome, 3, "Visit Kerbin's Mountains biome without entering orbit.", Biome2Id),
                CreateStarterMilestone(Biome4Id, "Biome IV - Deserts", MilestoneObjectiveType.BiomeVisit, StarterContractLine.Biome, 4, "Visit Kerbin's Deserts biome without entering orbit.", Biome3Id),
                CreateStarterMilestone(Biome5Id, "Biome V - Ice Caps", MilestoneObjectiveType.BiomeVisit, StarterContractLine.Biome, 5, "Visit Kerbin's Ice Caps biome without entering orbit.", Biome4Id)
            }.AsReadOnly();

        private static readonly IList<MilestoneDefinition> Definitions =
            new List<MilestoneDefinition>
            {
                new MilestoneDefinition(
                    ProbeOrbitId,
                    "Probe Orbit",
                    "Kerbin",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Kerbin with an uncrewed Probe or Relay vessel.",
                    CreateProbeOrbitUnlockRule()),
                new MilestoneDefinition(
                    CrewedOrbitId,
                    "Crewed Orbit",
                    "Kerbin",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.Crewed,
                    "Achieve orbit around Kerbin with at least one live Kerbal aboard.",
                    UnlockRuleDefinition.AnyAgencyAchievement(ProbeOrbitId)),
                new MilestoneDefinition(
                    MunProbeOrbitId,
                    "Mun Probe Orbit",
                    "Mun",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Mun with an uncrewed Probe or Relay vessel.",
                    UnlockRuleDefinition.AnyAgencyAchievement(ProbeOrbitId)),
                new MilestoneDefinition(
                    MinmusProbeOrbitId,
                    "Minmus Probe Orbit",
                    "Minmus",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Minmus with an uncrewed Probe or Relay vessel.",
                    UnlockRuleDefinition.AnyAgencyAchievement(ProbeOrbitId)),
                new MilestoneDefinition(
                    DunaProbeOrbitId,
                    "Duna Probe Orbit",
                    "Duna",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Duna with an uncrewed Probe or Relay vessel.",
                    CreateInterplanetaryProbeUnlockRule()),
                new MilestoneDefinition(
                    MunCrewedOrbitId,
                    "Mun Crewed Orbit",
                    "Mun",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.Crewed,
                    "Achieve orbit around Mun with at least one live Kerbal aboard.",
                    UnlockRuleDefinition.AnyAgencyAchievement(CrewedOrbitId)),
                new MilestoneDefinition(
                    MinmusCrewedOrbitId,
                    "Minmus Crewed Orbit",
                    "Minmus",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.Crewed,
                    "Achieve orbit around Minmus with at least one live Kerbal aboard.",
                    UnlockRuleDefinition.AnyAgencyAchievement(CrewedOrbitId)),
                new MilestoneDefinition(
                    DunaCrewedOrbitId,
                    "Duna Crewed Orbit",
                    "Duna",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.Crewed,
                    "Achieve orbit around Duna with at least one live Kerbal aboard.",
                    CreateInterplanetaryCrewedUnlockRule()),
                new MilestoneDefinition(
                    MohoProbeOrbitId,
                    "Moho Probe Orbit",
                    "Moho",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Moho with an uncrewed Probe or Relay vessel.",
                    CreateInterplanetaryProbeUnlockRule()),
                new MilestoneDefinition(
                    MohoCrewedOrbitId,
                    "Moho Crewed Orbit",
                    "Moho",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.Crewed,
                    "Achieve orbit around Moho with at least one live Kerbal aboard.",
                    CreateInterplanetaryCrewedUnlockRule()),
                new MilestoneDefinition(
                    EveProbeOrbitId,
                    "Eve Probe Orbit",
                    "Eve",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Eve with an uncrewed Probe or Relay vessel.",
                    CreateInterplanetaryProbeUnlockRule()),
                new MilestoneDefinition(
                    EveCrewedOrbitId,
                    "Eve Crewed Orbit",
                    "Eve",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.Crewed,
                    "Achieve orbit around Eve with at least one live Kerbal aboard.",
                    CreateInterplanetaryCrewedUnlockRule()),
                new MilestoneDefinition(
                    GillyProbeOrbitId,
                    "Gilly Probe Orbit",
                    "Gilly",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Gilly with an uncrewed Probe or Relay vessel.",
                    UnlockRuleDefinition.AnyAgencyAchievement(EveProbeOrbitId)),
                new MilestoneDefinition(
                    GillyCrewedOrbitId,
                    "Gilly Crewed Orbit",
                    "Gilly",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.Crewed,
                    "Achieve orbit around Gilly with at least one live Kerbal aboard.",
                    UnlockRuleDefinition.AnyAgencyAchievement(EveCrewedOrbitId)),
                new MilestoneDefinition(
                    IkeProbeOrbitId,
                    "Ike Probe Orbit",
                    "Ike",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Ike with an uncrewed Probe or Relay vessel.",
                    UnlockRuleDefinition.AnyAgencyAchievement(DunaProbeOrbitId)),
                new MilestoneDefinition(
                    IkeCrewedOrbitId,
                    "Ike Crewed Orbit",
                    "Ike",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.Crewed,
                    "Achieve orbit around Ike with at least one live Kerbal aboard.",
                    UnlockRuleDefinition.AnyAgencyAchievement(DunaCrewedOrbitId)),
                new MilestoneDefinition(
                    DresProbeOrbitId,
                    "Dres Probe Orbit",
                    "Dres",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Dres with an uncrewed Probe or Relay vessel.",
                    CreateInterplanetaryProbeUnlockRule()),
                new MilestoneDefinition(
                    DresCrewedOrbitId,
                    "Dres Crewed Orbit",
                    "Dres",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.Crewed,
                    "Achieve orbit around Dres with at least one live Kerbal aboard.",
                    CreateInterplanetaryCrewedUnlockRule()),
                new MilestoneDefinition(
                    JoolProbeOrbitId,
                    "Jool Probe Orbit",
                    "Jool",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Jool with an uncrewed Probe or Relay vessel.",
                    CreateInterplanetaryProbeUnlockRule()),
                new MilestoneDefinition(
                    JoolCrewedOrbitId,
                    "Jool Crewed Orbit",
                    "Jool",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.Crewed,
                    "Achieve orbit around Jool with at least one live Kerbal aboard.",
                    CreateInterplanetaryCrewedUnlockRule()),
                new MilestoneDefinition(
                    LaytheProbeOrbitId,
                    "Laythe Probe Orbit",
                    "Laythe",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Laythe with an uncrewed Probe or Relay vessel.",
                    UnlockRuleDefinition.AnyAgencyAchievement(JoolProbeOrbitId)),
                new MilestoneDefinition(
                    LaytheCrewedOrbitId,
                    "Laythe Crewed Orbit",
                    "Laythe",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.Crewed,
                    "Achieve orbit around Laythe with at least one live Kerbal aboard.",
                    UnlockRuleDefinition.AnyAgencyAchievement(JoolCrewedOrbitId)),
                new MilestoneDefinition(
                    VallProbeOrbitId,
                    "Vall Probe Orbit",
                    "Vall",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Vall with an uncrewed Probe or Relay vessel.",
                    UnlockRuleDefinition.AnyAgencyAchievement(JoolProbeOrbitId)),
                new MilestoneDefinition(
                    VallCrewedOrbitId,
                    "Vall Crewed Orbit",
                    "Vall",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.Crewed,
                    "Achieve orbit around Vall with at least one live Kerbal aboard.",
                    UnlockRuleDefinition.AnyAgencyAchievement(JoolCrewedOrbitId)),
                new MilestoneDefinition(
                    TyloProbeOrbitId,
                    "Tylo Probe Orbit",
                    "Tylo",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Tylo with an uncrewed Probe or Relay vessel.",
                    UnlockRuleDefinition.AnyAgencyAchievement(JoolProbeOrbitId)),
                new MilestoneDefinition(
                    TyloCrewedOrbitId,
                    "Tylo Crewed Orbit",
                    "Tylo",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.Crewed,
                    "Achieve orbit around Tylo with at least one live Kerbal aboard.",
                    UnlockRuleDefinition.AnyAgencyAchievement(JoolCrewedOrbitId)),
                new MilestoneDefinition(
                    BopProbeOrbitId,
                    "Bop Probe Orbit",
                    "Bop",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Bop with an uncrewed Probe or Relay vessel.",
                    UnlockRuleDefinition.AnyAgencyAchievement(JoolProbeOrbitId)),
                new MilestoneDefinition(
                    BopCrewedOrbitId,
                    "Bop Crewed Orbit",
                    "Bop",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.Crewed,
                    "Achieve orbit around Bop with at least one live Kerbal aboard.",
                    UnlockRuleDefinition.AnyAgencyAchievement(JoolCrewedOrbitId)),
                new MilestoneDefinition(
                    PolProbeOrbitId,
                    "Pol Probe Orbit",
                    "Pol",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Pol with an uncrewed Probe or Relay vessel.",
                    UnlockRuleDefinition.AnyAgencyAchievement(JoolProbeOrbitId)),
                new MilestoneDefinition(
                    PolCrewedOrbitId,
                    "Pol Crewed Orbit",
                    "Pol",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.Crewed,
                    "Achieve orbit around Pol with at least one live Kerbal aboard.",
                    UnlockRuleDefinition.AnyAgencyAchievement(JoolCrewedOrbitId)),
                new MilestoneDefinition(
                    EelooProbeOrbitId,
                    "Eeloo Probe Orbit",
                    "Eeloo",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Eeloo with an uncrewed Probe or Relay vessel.",
                    CreateInterplanetaryProbeUnlockRule()),
                new MilestoneDefinition(
                    EelooCrewedOrbitId,
                    "Eeloo Crewed Orbit",
                    "Eeloo",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.Crewed,
                    "Achieve orbit around Eeloo with at least one live Kerbal aboard.",
                    CreateInterplanetaryCrewedUnlockRule())
            }.AsReadOnly();

        /// <summary>
        /// Orbital milestones consumed by the existing vessel-orbit tracker.
        /// </summary>
        public static IList<MilestoneDefinition> All
        {
            get { return Definitions; }
        }

        /// <summary>
        /// The twenty special pre-orbit milestones. Batch B supplies their flight-state evaluation.
        /// </summary>
        public static IList<MilestoneDefinition> StarterContracts
        {
            get { return StarterDefinitions; }
        }

        /// <summary>
        /// Returns the prototype milestone with the supplied stable ID, or null when no definition exists.
        /// </summary>
        public static MilestoneDefinition FindById(string milestoneId)
        {
            if (string.IsNullOrEmpty(milestoneId))
            {
                return null;
            }

            MilestoneDefinition milestone = FindById(StarterDefinitions, milestoneId);
            return milestone ?? FindById(Definitions, milestoneId);
        }

        private static MilestoneDefinition FindById(
            IList<MilestoneDefinition> definitions,
            string milestoneId)
        {
            for (int milestoneIndex = 0; milestoneIndex < definitions.Count; milestoneIndex++)
            {
                MilestoneDefinition milestone = definitions[milestoneIndex];
                if (string.Equals(milestone.Id, milestoneId, StringComparison.OrdinalIgnoreCase))
                {
                    return milestone;
                }
            }

            return null;
        }

        private static MilestoneDefinition CreateStarterMilestone(
            string id,
            string name,
            MilestoneObjectiveType objectiveType,
            StarterContractLine starterLine,
            int starterLevel,
            string objectiveDescription,
            string previousMilestoneId,
            MilestoneCrewRequirement crewRequirement = MilestoneCrewRequirement.UncrewedProbe)
        {
            UnlockRuleDefinition unlockRule = string.IsNullOrEmpty(previousMilestoneId)
                ? null
                : UnlockRuleDefinition.AnyAgencyAchievement(previousMilestoneId);

            return new MilestoneDefinition(
                id,
                name,
                "Kerbin",
                MilestoneSituation.Orbit,
                crewRequirement,
                objectiveDescription,
                unlockRule,
                objectiveType,
                starterLine,
                starterLevel,
                starterLevel * StarterRewardFundsPerLevel,
                (starterLevel + 1) * StarterRivalProgressCostFundsPerLevel);
        }

        private static UnlockRuleDefinition CreateProbeOrbitUnlockRule()
        {
            return new UnlockRuleDefinition(
                new UnlockPathDefinition(
                    UnlockConditionDefinition.Achievement(
                        DirectedPower5Id,
                        UnlockProgramScope.AnyAgency)),
                new UnlockPathDefinition(
                    UnlockConditionDefinition.Achievement(
                        Mass5Id,
                        UnlockProgramScope.AnyAgency)),
                new UnlockPathDefinition(
                    UnlockConditionDefinition.Achievement(
                        Control5Id,
                        UnlockProgramScope.AnyAgency)),
                new UnlockPathDefinition(
                    UnlockConditionDefinition.Achievement(
                        Biome5Id,
                        UnlockProgramScope.AnyAgency)));
        }

        private static UnlockRuleDefinition CreateInterplanetaryProbeUnlockRule()
        {
            return new UnlockRuleDefinition(
                new UnlockPathDefinition(
                    UnlockConditionDefinition.Achievement(
                        MunProbeOrbitId,
                        UnlockProgramScope.AnyAgency),
                    UnlockConditionDefinition.Achievement(
                        MinmusProbeOrbitId,
                        UnlockProgramScope.AnyAgency)));
        }

        private static UnlockRuleDefinition CreateInterplanetaryCrewedUnlockRule()
        {
            return new UnlockRuleDefinition(
                new UnlockPathDefinition(
                    UnlockConditionDefinition.Achievement(
                        MunCrewedOrbitId,
                        UnlockProgramScope.AnyAgency),
                    UnlockConditionDefinition.Achievement(
                        MinmusCrewedOrbitId,
                        UnlockProgramScope.AnyAgency)));
        }
    }
}
