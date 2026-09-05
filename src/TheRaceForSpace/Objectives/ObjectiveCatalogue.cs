using System;
using System.Collections.Generic;

namespace TheRaceForSpace.Objectives
{
    /// <summary>
    /// Defines the objectiveCompletion objectives used by the current prototype.
    /// </summary>
    public static class ObjectiveCatalogue
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

        private const double PreOrbitRewardFundsPerLevel = 10000.0;
        private const double PreOrbitRivalProgressCostFundsPerLevel = 2000.0;
        private const double DirectedPowerMaximumAltitudeMeters = 70000.0;

        private static readonly IList<ObjectiveDefinition> PreOrbitDefinitions =
            new List<ObjectiveDefinition>
            {
                CreatePreOrbitObjective(
                    DirectedPower1Id,
                    "Directed Power I",
                    ObjectiveType.DirectedPower,
                    PreOrbitContractLine.DirectedPower,
                    1,
                    PreOrbitContractCriteria.DirectedPower(600.0, DirectedPowerMaximumAltitudeMeters),
                    "Reach 600 m/s without exceeding 70 km altitude, then impact Kerbin.",
                    null),
                CreatePreOrbitObjective(
                    DirectedPower2Id,
                    "Directed Power II",
                    ObjectiveType.DirectedPower,
                    PreOrbitContractLine.DirectedPower,
                    2,
                    PreOrbitContractCriteria.DirectedPower(1100.0, DirectedPowerMaximumAltitudeMeters),
                    "Reach 1,100 m/s without exceeding 70 km altitude, then impact Kerbin.",
                    DirectedPower1Id),
                CreatePreOrbitObjective(
                    DirectedPower3Id,
                    "Directed Power III",
                    ObjectiveType.DirectedPower,
                    PreOrbitContractLine.DirectedPower,
                    3,
                    PreOrbitContractCriteria.DirectedPower(1400.0, DirectedPowerMaximumAltitudeMeters),
                    "Reach 1,400 m/s without exceeding 70 km altitude, then impact Kerbin.",
                    DirectedPower2Id),
                CreatePreOrbitObjective(
                    DirectedPower4Id,
                    "Directed Power IV",
                    ObjectiveType.DirectedPower,
                    PreOrbitContractLine.DirectedPower,
                    4,
                    PreOrbitContractCriteria.DirectedPower(1700.0, DirectedPowerMaximumAltitudeMeters),
                    "Reach 1,700 m/s without exceeding 70 km altitude, then impact Kerbin.",
                    DirectedPower3Id),
                CreatePreOrbitObjective(
                    DirectedPower5Id,
                    "Directed Power V",
                    ObjectiveType.DirectedPower,
                    PreOrbitContractLine.DirectedPower,
                    5,
                    PreOrbitContractCriteria.DirectedPower(2000.0, DirectedPowerMaximumAltitudeMeters),
                    "Reach 2,000 m/s without exceeding 70 km altitude, then impact Kerbin.",
                    DirectedPower4Id),

                CreatePreOrbitObjective(
                    Mass1Id,
                    "Mass I",
                    ObjectiveType.DeliveredMass,
                    PreOrbitContractLine.Mass,
                    1,
                    PreOrbitContractCriteria.Mass(1.0, 25000.0),
                    "Carry at least 1 t of remaining vessel mass 25 km from the Space Centre.",
                    null),
                CreatePreOrbitObjective(
                    Mass2Id,
                    "Mass II",
                    ObjectiveType.DeliveredMass,
                    PreOrbitContractLine.Mass,
                    2,
                    PreOrbitContractCriteria.Mass(2.5, 75000.0),
                    "Carry at least 2.5 t of remaining vessel mass 75 km from the Space Centre.",
                    Mass1Id),
                CreatePreOrbitObjective(
                    Mass3Id,
                    "Mass III",
                    ObjectiveType.DeliveredMass,
                    PreOrbitContractLine.Mass,
                    3,
                    PreOrbitContractCriteria.Mass(5.0, 150000.0),
                    "Carry at least 5 t of remaining vessel mass 150 km from the Space Centre.",
                    Mass2Id),
                CreatePreOrbitObjective(
                    Mass4Id,
                    "Mass IV",
                    ObjectiveType.DeliveredMass,
                    PreOrbitContractLine.Mass,
                    4,
                    PreOrbitContractCriteria.Mass(10.0, 300000.0),
                    "Carry at least 10 t of remaining vessel mass 300 km from the Space Centre.",
                    Mass3Id),
                CreatePreOrbitObjective(
                    Mass5Id,
                    "Mass V",
                    ObjectiveType.DeliveredMass,
                    PreOrbitContractLine.Mass,
                    5,
                    PreOrbitContractCriteria.Mass(20.0, 600000.0),
                    "Carry at least 20 t of remaining vessel mass 600 km from the Space Centre.",
                    Mass4Id),

                CreatePreOrbitObjective(
                    Control1Id,
                    "Control I",
                    ObjectiveType.AltitudeHold,
                    PreOrbitContractLine.Control,
                    1,
                    PreOrbitContractCriteria.Control(2000.0, 5000.0, 30.0),
                    "With crew aboard, remain between 2-5 km for 30 seconds, then land safely on Kerbin.",
                    null,
                    ObjectiveCrewRequirement.Crewed),
                CreatePreOrbitObjective(
                    Control2Id,
                    "Control II",
                    ObjectiveType.AltitudeHold,
                    PreOrbitContractLine.Control,
                    2,
                    PreOrbitContractCriteria.Control(8000.0, 12000.0, 45.0),
                    "With crew aboard, remain between 8-12 km for 45 seconds, then land safely on Kerbin.",
                    Control1Id,
                    ObjectiveCrewRequirement.Crewed),
                CreatePreOrbitObjective(
                    Control3Id,
                    "Control III",
                    ObjectiveType.AltitudeHold,
                    PreOrbitContractLine.Control,
                    3,
                    PreOrbitContractCriteria.Control(15000.0, 25000.0, 60.0),
                    "With crew aboard, remain between 15-25 km for 60 seconds, then land safely on Kerbin.",
                    Control2Id,
                    ObjectiveCrewRequirement.Crewed),
                CreatePreOrbitObjective(
                    Control4Id,
                    "Control IV",
                    ObjectiveType.AltitudeHold,
                    PreOrbitContractLine.Control,
                    4,
                    PreOrbitContractCriteria.Control(30000.0, 40000.0, 75.0),
                    "With crew aboard, remain between 30-40 km for 75 seconds, then land safely on Kerbin.",
                    Control3Id,
                    ObjectiveCrewRequirement.Crewed),
                CreatePreOrbitObjective(
                    Control5Id,
                    "Control V",
                    ObjectiveType.AltitudeHold,
                    PreOrbitContractLine.Control,
                    5,
                    PreOrbitContractCriteria.Control(50000.0, 65000.0, 90.0),
                    "With crew aboard, remain between 50-65 km for 90 seconds, then land safely on Kerbin.",
                    Control4Id,
                    ObjectiveCrewRequirement.Crewed),

                CreatePreOrbitObjective(
                    Biome1Id,
                    "Biome I - Grasslands",
                    ObjectiveType.BiomeVisit,
                    PreOrbitContractLine.Biome,
                    1,
                    PreOrbitContractCriteria.Biome("Grasslands"),
                    "Visit Kerbin's Grasslands biome without entering orbit.",
                    null),
                CreatePreOrbitObjective(
                    Biome2Id,
                    "Biome II - Highlands",
                    ObjectiveType.BiomeVisit,
                    PreOrbitContractLine.Biome,
                    2,
                    PreOrbitContractCriteria.Biome("Highlands"),
                    "Visit Kerbin's Highlands biome without entering orbit.",
                    Biome1Id),
                CreatePreOrbitObjective(
                    Biome3Id,
                    "Biome III - Mountains",
                    ObjectiveType.BiomeVisit,
                    PreOrbitContractLine.Biome,
                    3,
                    PreOrbitContractCriteria.Biome("Mountains"),
                    "Visit Kerbin's Mountains biome without entering orbit.",
                    Biome2Id),
                CreatePreOrbitObjective(
                    Biome4Id,
                    "Biome IV - Deserts",
                    ObjectiveType.BiomeVisit,
                    PreOrbitContractLine.Biome,
                    4,
                    PreOrbitContractCriteria.Biome("Deserts"),
                    "Visit Kerbin's Deserts biome without entering orbit.",
                    Biome3Id),
                CreatePreOrbitObjective(
                    Biome5Id,
                    "Biome V - Ice Caps",
                    ObjectiveType.BiomeVisit,
                    PreOrbitContractLine.Biome,
                    5,
                    PreOrbitContractCriteria.Biome("Ice Caps"),
                    "Visit Kerbin's Ice Caps biome without entering orbit.",
                    Biome4Id)
            }.AsReadOnly();

        private static readonly IList<ObjectiveDefinition> Definitions =
            new List<ObjectiveDefinition>
            {
                new ObjectiveDefinition(
                    ProbeOrbitId,
                    "Probe Orbit",
                    "Kerbin",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Kerbin with an uncrewed Probe or Relay vessel.",
                    CreateProbeOrbitUnlockRule()),
                new ObjectiveDefinition(
                    CrewedOrbitId,
                    "Crewed Orbit",
                    "Kerbin",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.Crewed,
                    "Achieve orbit around Kerbin with at least one live Kerbal aboard.",
                    UnlockRuleDefinition.AnyAgencyObjectiveCompletion(ProbeOrbitId)),
                new ObjectiveDefinition(
                    MunProbeOrbitId,
                    "Mun Probe Orbit",
                    "Mun",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Mun with an uncrewed Probe or Relay vessel.",
                    UnlockRuleDefinition.AnyAgencyObjectiveCompletion(ProbeOrbitId)),
                new ObjectiveDefinition(
                    MinmusProbeOrbitId,
                    "Minmus Probe Orbit",
                    "Minmus",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Minmus with an uncrewed Probe or Relay vessel.",
                    UnlockRuleDefinition.AnyAgencyObjectiveCompletion(ProbeOrbitId)),
                new ObjectiveDefinition(
                    DunaProbeOrbitId,
                    "Duna Probe Orbit",
                    "Duna",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Duna with an uncrewed Probe or Relay vessel.",
                    CreateInterplanetaryProbeUnlockRule()),
                new ObjectiveDefinition(
                    MunCrewedOrbitId,
                    "Mun Crewed Orbit",
                    "Mun",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.Crewed,
                    "Achieve orbit around Mun with at least one live Kerbal aboard.",
                    UnlockRuleDefinition.AnyAgencyObjectiveCompletion(CrewedOrbitId)),
                new ObjectiveDefinition(
                    MinmusCrewedOrbitId,
                    "Minmus Crewed Orbit",
                    "Minmus",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.Crewed,
                    "Achieve orbit around Minmus with at least one live Kerbal aboard.",
                    UnlockRuleDefinition.AnyAgencyObjectiveCompletion(CrewedOrbitId)),
                new ObjectiveDefinition(
                    DunaCrewedOrbitId,
                    "Duna Crewed Orbit",
                    "Duna",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.Crewed,
                    "Achieve orbit around Duna with at least one live Kerbal aboard.",
                    CreateInterplanetaryCrewedUnlockRule()),
                new ObjectiveDefinition(
                    MohoProbeOrbitId,
                    "Moho Probe Orbit",
                    "Moho",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Moho with an uncrewed Probe or Relay vessel.",
                    CreateInterplanetaryProbeUnlockRule()),
                new ObjectiveDefinition(
                    MohoCrewedOrbitId,
                    "Moho Crewed Orbit",
                    "Moho",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.Crewed,
                    "Achieve orbit around Moho with at least one live Kerbal aboard.",
                    CreateInterplanetaryCrewedUnlockRule()),
                new ObjectiveDefinition(
                    EveProbeOrbitId,
                    "Eve Probe Orbit",
                    "Eve",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Eve with an uncrewed Probe or Relay vessel.",
                    CreateInterplanetaryProbeUnlockRule()),
                new ObjectiveDefinition(
                    EveCrewedOrbitId,
                    "Eve Crewed Orbit",
                    "Eve",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.Crewed,
                    "Achieve orbit around Eve with at least one live Kerbal aboard.",
                    CreateInterplanetaryCrewedUnlockRule()),
                new ObjectiveDefinition(
                    GillyProbeOrbitId,
                    "Gilly Probe Orbit",
                    "Gilly",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Gilly with an uncrewed Probe or Relay vessel.",
                    UnlockRuleDefinition.AnyAgencyObjectiveCompletion(EveProbeOrbitId)),
                new ObjectiveDefinition(
                    GillyCrewedOrbitId,
                    "Gilly Crewed Orbit",
                    "Gilly",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.Crewed,
                    "Achieve orbit around Gilly with at least one live Kerbal aboard.",
                    UnlockRuleDefinition.AnyAgencyObjectiveCompletion(EveCrewedOrbitId)),
                new ObjectiveDefinition(
                    IkeProbeOrbitId,
                    "Ike Probe Orbit",
                    "Ike",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Ike with an uncrewed Probe or Relay vessel.",
                    UnlockRuleDefinition.AnyAgencyObjectiveCompletion(DunaProbeOrbitId)),
                new ObjectiveDefinition(
                    IkeCrewedOrbitId,
                    "Ike Crewed Orbit",
                    "Ike",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.Crewed,
                    "Achieve orbit around Ike with at least one live Kerbal aboard.",
                    UnlockRuleDefinition.AnyAgencyObjectiveCompletion(DunaCrewedOrbitId)),
                new ObjectiveDefinition(
                    DresProbeOrbitId,
                    "Dres Probe Orbit",
                    "Dres",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Dres with an uncrewed Probe or Relay vessel.",
                    CreateInterplanetaryProbeUnlockRule()),
                new ObjectiveDefinition(
                    DresCrewedOrbitId,
                    "Dres Crewed Orbit",
                    "Dres",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.Crewed,
                    "Achieve orbit around Dres with at least one live Kerbal aboard.",
                    CreateInterplanetaryCrewedUnlockRule()),
                new ObjectiveDefinition(
                    JoolProbeOrbitId,
                    "Jool Probe Orbit",
                    "Jool",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Jool with an uncrewed Probe or Relay vessel.",
                    CreateInterplanetaryProbeUnlockRule()),
                new ObjectiveDefinition(
                    JoolCrewedOrbitId,
                    "Jool Crewed Orbit",
                    "Jool",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.Crewed,
                    "Achieve orbit around Jool with at least one live Kerbal aboard.",
                    CreateInterplanetaryCrewedUnlockRule()),
                new ObjectiveDefinition(
                    LaytheProbeOrbitId,
                    "Laythe Probe Orbit",
                    "Laythe",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Laythe with an uncrewed Probe or Relay vessel.",
                    UnlockRuleDefinition.AnyAgencyObjectiveCompletion(JoolProbeOrbitId)),
                new ObjectiveDefinition(
                    LaytheCrewedOrbitId,
                    "Laythe Crewed Orbit",
                    "Laythe",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.Crewed,
                    "Achieve orbit around Laythe with at least one live Kerbal aboard.",
                    UnlockRuleDefinition.AnyAgencyObjectiveCompletion(JoolCrewedOrbitId)),
                new ObjectiveDefinition(
                    VallProbeOrbitId,
                    "Vall Probe Orbit",
                    "Vall",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Vall with an uncrewed Probe or Relay vessel.",
                    UnlockRuleDefinition.AnyAgencyObjectiveCompletion(JoolProbeOrbitId)),
                new ObjectiveDefinition(
                    VallCrewedOrbitId,
                    "Vall Crewed Orbit",
                    "Vall",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.Crewed,
                    "Achieve orbit around Vall with at least one live Kerbal aboard.",
                    UnlockRuleDefinition.AnyAgencyObjectiveCompletion(JoolCrewedOrbitId)),
                new ObjectiveDefinition(
                    TyloProbeOrbitId,
                    "Tylo Probe Orbit",
                    "Tylo",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Tylo with an uncrewed Probe or Relay vessel.",
                    UnlockRuleDefinition.AnyAgencyObjectiveCompletion(JoolProbeOrbitId)),
                new ObjectiveDefinition(
                    TyloCrewedOrbitId,
                    "Tylo Crewed Orbit",
                    "Tylo",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.Crewed,
                    "Achieve orbit around Tylo with at least one live Kerbal aboard.",
                    UnlockRuleDefinition.AnyAgencyObjectiveCompletion(JoolCrewedOrbitId)),
                new ObjectiveDefinition(
                    BopProbeOrbitId,
                    "Bop Probe Orbit",
                    "Bop",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Bop with an uncrewed Probe or Relay vessel.",
                    UnlockRuleDefinition.AnyAgencyObjectiveCompletion(JoolProbeOrbitId)),
                new ObjectiveDefinition(
                    BopCrewedOrbitId,
                    "Bop Crewed Orbit",
                    "Bop",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.Crewed,
                    "Achieve orbit around Bop with at least one live Kerbal aboard.",
                    UnlockRuleDefinition.AnyAgencyObjectiveCompletion(JoolCrewedOrbitId)),
                new ObjectiveDefinition(
                    PolProbeOrbitId,
                    "Pol Probe Orbit",
                    "Pol",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Pol with an uncrewed Probe or Relay vessel.",
                    UnlockRuleDefinition.AnyAgencyObjectiveCompletion(JoolProbeOrbitId)),
                new ObjectiveDefinition(
                    PolCrewedOrbitId,
                    "Pol Crewed Orbit",
                    "Pol",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.Crewed,
                    "Achieve orbit around Pol with at least one live Kerbal aboard.",
                    UnlockRuleDefinition.AnyAgencyObjectiveCompletion(JoolCrewedOrbitId)),
                new ObjectiveDefinition(
                    EelooProbeOrbitId,
                    "Eeloo Probe Orbit",
                    "Eeloo",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Eeloo with an uncrewed Probe or Relay vessel.",
                    CreateInterplanetaryProbeUnlockRule()),
                new ObjectiveDefinition(
                    EelooCrewedOrbitId,
                    "Eeloo Crewed Orbit",
                    "Eeloo",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.Crewed,
                    "Achieve orbit around Eeloo with at least one live Kerbal aboard.",
                    CreateInterplanetaryCrewedUnlockRule())
            }.AsReadOnly();

        private static readonly Dictionary<string, ObjectiveDefinition> ObjectivesById =
            CreateObjectiveIndex();

        /// <summary>
        /// Orbital objectives consumed by the existing vessel-orbit tracker.
        /// </summary>
        public static IList<ObjectiveDefinition> All
        {
            get { return Definitions; }
        }

        /// <summary>
        /// The twenty special pre-orbit objectives. Batch B supplies their flight-state evaluation.
        /// </summary>
        public static IList<ObjectiveDefinition> PreOrbitContracts
        {
            get { return PreOrbitDefinitions; }
        }

        /// <summary>
        /// Returns the prototype objective with the supplied stable ID, or null when no definition exists.
        /// </summary>
        public static ObjectiveDefinition FindById(string objectiveId)
        {
            if (string.IsNullOrEmpty(objectiveId))
            {
                return null;
            }

            ObjectiveDefinition objective;
            return ObjectivesById.TryGetValue(objectiveId, out objective)
                ? objective
                : null;
        }

        private static Dictionary<string, ObjectiveDefinition> CreateObjectiveIndex()
        {
            var objectivesById = new Dictionary<string, ObjectiveDefinition>(
                StringComparer.OrdinalIgnoreCase);

            for (int objectiveIndex = 0;
                objectiveIndex < PreOrbitDefinitions.Count;
                objectiveIndex++)
            {
                ObjectiveDefinition objective = PreOrbitDefinitions[objectiveIndex];
                objectivesById.Add(objective.Id, objective);
            }

            for (int objectiveIndex = 0;
                objectiveIndex < Definitions.Count;
                objectiveIndex++)
            {
                ObjectiveDefinition objective = Definitions[objectiveIndex];
                objectivesById.Add(objective.Id, objective);
            }

            return objectivesById;
        }

        private static ObjectiveDefinition CreatePreOrbitObjective(
            string id,
            string name,
            ObjectiveType objectiveType,
            PreOrbitContractLine preOrbitLine,
            int preOrbitLevel,
            PreOrbitContractCriteria preOrbitCriteria,
            string objectiveDescription,
            string previousObjectiveId,
            ObjectiveCrewRequirement crewRequirement = ObjectiveCrewRequirement.UncrewedProbe)
        {
            UnlockRuleDefinition unlockRule = string.IsNullOrEmpty(previousObjectiveId)
                ? null
                : UnlockRuleDefinition.AnyAgencyObjectiveCompletion(previousObjectiveId);

            return new ObjectiveDefinition(
                id,
                name,
                "Kerbin",
                ObjectiveSituation.Orbit,
                crewRequirement,
                objectiveDescription,
                unlockRule,
                objectiveType,
                preOrbitLine,
                preOrbitLevel,
                preOrbitLevel * PreOrbitRewardFundsPerLevel,
                (preOrbitLevel + 1) * PreOrbitRivalProgressCostFundsPerLevel,
                preOrbitCriteria);
        }

        private static UnlockRuleDefinition CreateProbeOrbitUnlockRule()
        {
            return new UnlockRuleDefinition(
                new UnlockPathDefinition(
                    UnlockConditionDefinition.ObjectiveCompletion(
                        DirectedPower5Id,
                        UnlockAgencyScope.AnyAgency)),
                new UnlockPathDefinition(
                    UnlockConditionDefinition.ObjectiveCompletion(
                        Mass5Id,
                        UnlockAgencyScope.AnyAgency)),
                new UnlockPathDefinition(
                    UnlockConditionDefinition.ObjectiveCompletion(
                        Control5Id,
                        UnlockAgencyScope.AnyAgency)),
                new UnlockPathDefinition(
                    UnlockConditionDefinition.ObjectiveCompletion(
                        Biome5Id,
                        UnlockAgencyScope.AnyAgency)));
        }

        private static UnlockRuleDefinition CreateInterplanetaryProbeUnlockRule()
        {
            return new UnlockRuleDefinition(
                new UnlockPathDefinition(
                    UnlockConditionDefinition.ObjectiveCompletion(
                        MunProbeOrbitId,
                        UnlockAgencyScope.AnyAgency),
                    UnlockConditionDefinition.ObjectiveCompletion(
                        MinmusProbeOrbitId,
                        UnlockAgencyScope.AnyAgency)));
        }

        private static UnlockRuleDefinition CreateInterplanetaryCrewedUnlockRule()
        {
            return new UnlockRuleDefinition(
                new UnlockPathDefinition(
                    UnlockConditionDefinition.ObjectiveCompletion(
                        MunCrewedOrbitId,
                        UnlockAgencyScope.AnyAgency),
                    UnlockConditionDefinition.ObjectiveCompletion(
                        MinmusCrewedOrbitId,
                        UnlockAgencyScope.AnyAgency)));
        }
    }
}
