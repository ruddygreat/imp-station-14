using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.PeriodicMobSpawner
{
    [RegisterComponent]

    //todo figure out if I want this to be apocarplypse beacon specific or a more generic system
    public sealed partial class PeriodicMobSpawnerComponent : Component
    {
        /// <summary>
        /// The current randomly picked time until the next spawn event
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("SpawnDelay")]
        public float SpawnDelay = 30;

        /// <summary>
        /// The amount of time that has passed since the last spawn attempt
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("TimeSinceLastSpawn")]
        public float TimeSinceLastSpawn = 0;

        /// <summary>
        /// The maximum number of mobs that this component is allowed to spawn
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("MaxSpawns")]
        public int MaxSpawns = 1;

        /// <summary>
        /// The list of live mobs that this component has spawned
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly), DataField("Spawned")]
        public HashSet<EntityUid> Spawned = new();

        /// <summary>
        /// The prototype for the mob that that this component spawns
        /// </summary>
        [DataField("spawn", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string SpawnPrototype = "MobCorgiIan";
    }
}
