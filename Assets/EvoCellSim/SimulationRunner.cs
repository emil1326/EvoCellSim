using System.Collections.Generic;

namespace Assets.EvoCellSim.Core
{
    public sealed class SimulationRunner
    {
        private readonly List<TickPhase> phaseTrace = new List<TickPhase>(11);

        public WorldState World { get; }
        public IReadOnlyList<TickPhase> LastTickTrace => phaseTrace;

        public SimulationRunner(SimulationSettings settings)
        {
            World = new WorldState(settings);
        }

        public void Tick()
        {
            phaseTrace.Clear();

            RunPhase(TickPhase.PassiveUpkeep, PassiveUpkeep);
            RunPhase(TickPhase.SampleLocalContext, SampleLocalContext);
            RunPhase(TickPhase.UpdateExpression, UpdateExpression);
            RunPhase(TickPhase.EvaluateRules, EvaluateRules);
            RunPhase(TickPhase.QueueIntents, QueueIntents);
            RunPhase(TickPhase.ResolveIntents, ResolveIntents);
            RunPhase(TickPhase.ApplyPhysics, ApplyPhysics);
            RunPhase(TickPhase.UpdateBondsAndClusters, UpdateBondsAndClusters);
            RunPhase(TickPhase.ResolveReproductionDeathMutation, ResolveReproductionDeathMutation);
            RunPhase(TickPhase.UpdateEnvironment, UpdateEnvironment);
            RunPhase(TickPhase.BuildSnapshot, BuildSnapshot);

            World.AdvanceTick();
        }

        private void RunPhase(TickPhase phase, System.Action phaseAction)
        {
            phaseTrace.Add(phase);
            phaseAction();
        }

        private void PassiveUpkeep()
        {
            World.PassiveUpkeep();
        }

        private void SampleLocalContext()
        {
            World.SampleLocalContext();
        }

        private void UpdateExpression()
        {
            World.UpdateExpression();
        }

        private void EvaluateRules()
        {
            World.QueueIntentsForAllCells();
        }

        private void QueueIntents()
        {
            World.QueueRepairIntents();
        }

        private void ResolveIntents()
        {
            World.ResolveQueuedIntents();
        }

        private void ApplyPhysics()
        {
            World.ApplyPhysics();
        }

        private void UpdateBondsAndClusters()
        {
            World.UpdateBondsAndClusters();
        }

        private void ResolveReproductionDeathMutation()
        {
            World.ApplyDeathAndRepair();
        }

        private void UpdateEnvironment()
        {
            World.UpdateEnvironment();
        }

        private void BuildSnapshot()
        {
            World.BuildSnapshot();
        }
    }
}
