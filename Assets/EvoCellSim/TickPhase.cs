namespace Assets.EvoCellSim.Core
{
    public enum TickPhase
    {
        PassiveUpkeep = 1,
        SampleLocalContext = 2,
        UpdateExpression = 3,
        EvaluateRules = 4,
        QueueIntents = 5,
        ResolveIntents = 6,
        ApplyPhysics = 7,
        UpdateBondsAndClusters = 8,
        ResolveReproductionDeathMutation = 9,
        UpdateEnvironment = 10,
        BuildSnapshot = 11
    }
}
