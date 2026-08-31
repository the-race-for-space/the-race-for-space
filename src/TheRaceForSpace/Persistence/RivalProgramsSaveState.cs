using System;
using System.Collections.Generic;
using TheRaceForSpace.Programs;

namespace TheRaceForSpace.Persistence
{
    /// <summary>
    /// Collection-driven persistence for simulated rival programs. Each rival is matched by its
    /// stable program ID so adding or reordering rivals does not require another fixed save field.
    /// </summary>
    public sealed class RivalProgramsSaveState
    {
        private const string RivalNodeName = "RIVAL";

        private readonly Dictionary<string, RivalProgramSaveState> _statesByProgramId =
            new Dictionary<string, RivalProgramSaveState>(StringComparer.OrdinalIgnoreCase);

        public bool HasData
        {
            get { return _statesByProgramId.Count > 0; }
        }

        public void Capture(IList<SpaceProgramState> rivalPrograms)
        {
            _statesByProgramId.Clear();
            if (rivalPrograms == null)
            {
                return;
            }

            for (int programIndex = 0; programIndex < rivalPrograms.Count; programIndex++)
            {
                SpaceProgramState program = rivalPrograms[programIndex];
                if (program == null || program.IsPlayer || string.IsNullOrEmpty(program.Id))
                {
                    continue;
                }

                var state = new RivalProgramSaveState();
                state.Capture(program);
                if (state.HasData)
                {
                    _statesByProgramId[program.Id] = state;
                }
            }
        }

        public void ApplyTo(IList<SpaceProgramState> rivalPrograms)
        {
            if (rivalPrograms == null)
            {
                return;
            }

            for (int programIndex = 0; programIndex < rivalPrograms.Count; programIndex++)
            {
                SpaceProgramState program = rivalPrograms[programIndex];
                if (program == null || program.IsPlayer || string.IsNullOrEmpty(program.Id))
                {
                    continue;
                }

                RivalProgramSaveState state;
                if (_statesByProgramId.TryGetValue(program.Id, out state))
                {
                    state.ApplyTo(program);
                }
            }
        }

        public void Load(ConfigNode node)
        {
            _statesByProgramId.Clear();
            if (node == null)
            {
                return;
            }

            ConfigNode[] rivalNodes = node.GetNodes(RivalNodeName);
            for (int nodeIndex = 0; nodeIndex < rivalNodes.Length; nodeIndex++)
            {
                var state = new RivalProgramSaveState();
                state.Load(rivalNodes[nodeIndex]);
                if (!state.HasData || string.IsNullOrEmpty(state.ProgramId))
                {
                    continue;
                }

                // Stable IDs are unique in the runtime collection. If malformed save data repeats
                // an ID, the last valid node wins rather than inventing another rival identity.
                _statesByProgramId[state.ProgramId] = state;
            }
        }

        public void Save(ConfigNode node)
        {
            if (node == null)
            {
                return;
            }

            var programIds = new List<string>(_statesByProgramId.Keys);
            programIds.Sort(StringComparer.OrdinalIgnoreCase);

            for (int programIndex = 0; programIndex < programIds.Count; programIndex++)
            {
                RivalProgramSaveState state = _statesByProgramId[programIds[programIndex]];
                state.Save(node.AddNode(RivalNodeName));
            }
        }
    }
}
