using System;
using System.Collections.Generic;

namespace TheRaceForSpace.Milestones
{
    /// <summary>
    /// Defines the achievement milestones used by the current prototype.
    /// </summary>
    public static class PrototypeMilestones
    {
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
                    null),
                new MilestoneDefinition(
                    CrewedOrbitId,
                    "Crewed Orbit",
                    "Kerbin",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.Crewed,
                    "Achieve orbit around Kerbin with at least one live Kerbal aboard.",
                    null),
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

        public static IList<MilestoneDefinition> All
        {
            get { return Definitions; }
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

            for (int milestoneIndex = 0; milestoneIndex < Definitions.Count; milestoneIndex++)
            {
                MilestoneDefinition milestone = Definitions[milestoneIndex];
                if (string.Equals(milestone.Id, milestoneId, StringComparison.OrdinalIgnoreCase))
                {
                    return milestone;
                }
            }

            return null;
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
