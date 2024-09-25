

namespace Content.Server.PeriodicMobSpawner
{
    [RegisterComponent]
    public sealed partial class SpawnedByPeriodicMobSpawnerComponent : Component
    {

        /// <summary>
        /// the entity that spawned this entity, should ideally have a periodicMobSpawner component
        /// </summary>
        public EntityUid SpawnedBy;

    }
}
