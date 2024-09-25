using Content.Server.Dragon;
using Content.Server.NPC;
using Content.Server.NPC.Systems;
using Content.Shared.Mobs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using System.Linq;
using System.Numerics;

namespace Content.Server.PeriodicMobSpawner;

public sealed class PeriodicMobSpawnerSystem : EntitySystem
{

    //system that can be used to periodically spawn mobs
    //todo should probably generalise into periodically spawning any entity

    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        //track when spawned mobs die or get revived
        SubscribeLocalEvent<SpawnedByPeriodicMobSpawnerComponent, MobStateChangedEvent>(OnSpawnedMobStateChanged);

        //track when the spawned by tracker comp is shut down
        SubscribeLocalEvent<SpawnedByPeriodicMobSpawnerComponent, ComponentShutdown>(OnSpawnedMobComponentShutdown);

        //track when the spawner comp is shut down
        //might not actually be needed, I'm not 100% on how this works
        //should probably use it to get rid of no-longer-needed spawned by components?
        SubscribeLocalEvent<PeriodicMobSpawnerComponent, ComponentShutdown>(OnSpawnerComponentShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        //get everything w/ a periodicSpawnerComp
        var query = EntityQueryEnumerator<PeriodicMobSpawnerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {

            //increment the spawn timer
            comp.TimeSinceLastSpawn += frameTime;

            //if the spawn timer elapses, restart it
            if (comp.TimeSinceLastSpawn >= comp.SpawnDelay)
            {
                comp.TimeSinceLastSpawn = 0;

                //if we have space for a new spawned mob, spawn one
                if (comp.Spawned.Count < comp.MaxSpawns)
                {

                    var ent = Spawn(comp.SpawnPrototype, xform.Coordinates);
                    comp.Spawned.Add(ent);

                    var spawnedByComp = AddComp<SpawnedByPeriodicMobSpawnerComponent>(ent);
                    spawnedByComp.SpawnedBy = uid;

                    _npc.SetBlackboard(ent, NPCBlackboard.FollowTarget, new EntityCoordinates(uid, Vector2.Zero));

                    //todo play a sound on mob spawn?

                }
            }
        }
    }

    private void OnSpawnedMobStateChanged(EntityUid uid, SpawnedByPeriodicMobSpawnerComponent component, MobStateChangedEvent args)
    {
        //if the spawner hasn't had it's spawning component removed
        if (TryComp<PeriodicMobSpawnerComponent>(component.SpawnedBy, out var spawner))
        {
            //if the mob died, remove it from the spawned list
            if (args.NewMobState == MobState.Dead)
            {
                spawner.Spawned.Remove(uid);
            }
            //else, re-add it to the spawned list
            //might want to always have a mob dying remove it from the list forever?
            else
            {
                spawner.Spawned.Add(uid);
            }
        }
    }

    private void OnSpawnedMobComponentShutdown(EntityUid uid, SpawnedByPeriodicMobSpawnerComponent component, ComponentShutdown args)
    {
        //if a spawnedBy comp gets removed from an entity, remove it from the set of spawned things
        if (TryComp<PeriodicMobSpawnerComponent>(component.SpawnedBy, out var spawner))
        {
            spawner.Spawned.Remove(uid);
        }
    }

    private void OnSpawnerComponentShutdown(EntityUid uid, PeriodicMobSpawnerComponent component, ComponentShutdown args)
    {
        //todo neet do figure out if this is actually used? dunno when shutdown will get called
        //should remove the spawner tracker components from everything in the spawned list, but idk how to properly do that
    }
}


