using Content.Server.Announcements.Systems;
using Content.Server.NPC;
using Content.Server.Pinpointer;
using Content.Server.Popups;
using Content.Shared.Foldable;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.PeriodicMobSpawner.ApocarplypseBeacon
{
    public sealed class ApocarplypseBeaconSystem : EntitySystem
    {

        //todo might want to move this into shared? not entirely sure how the shared / server split works

        [Dependency] private readonly SharedMapSystem _map = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly PopupSystem _popups = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
        [Dependency] private readonly AnnouncerSystem _announcer = default!;
        [Dependency] private readonly NavMapSystem _navMap = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ApocarplypseBeaconComponent, FoldAttemptEvent>(OnBeaconFoldAttempt);
            SubscribeLocalEvent<ApocarplypseBeaconComponent, FoldedEvent>(OnBeaconFold);

        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            //get everything w/ an ApocarplypseBeacon comp
            var query = EntityQueryEnumerator<ApocarplypseBeaconComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var comp, out var xform))
            {
                //reduce the interference timer
                comp.InterferenceTimeRemaining -= frameTime;
                //if it would drop below 0, clamp it to zero
                if (comp.InterferenceTimeRemaining < 0) comp.InterferenceTimeRemaining = 0;
            }
        }

        private void OnBeaconFold(Entity<ApocarplypseBeaconComponent> ent, ref FoldedEvent args)
        {
            //if the beacon gets folded up, do nothing
            //else, it's been unfolded, so unleash the apocarplypse!
            if (!args.IsFolded)
            {
                _popups.PopupEntity(Loc.GetString("uplink-apocarplypse-unfold-success"), ent.Owner);

                //add the mob spawner comp to the beacon
                var spawnerComp = AddComp<PeriodicMobSpawnerComponent>(ent.Owner);
                //set the spawner to spawn dragon-aligned carps
                spawnerComp.SpawnPrototype = "MobCarpDragon";
                //set the spawner to a max of 10 carps at a time
                spawnerComp.MaxSpawns = 10;

                var query = EntityQueryEnumerator<ApocarplypseBeaconComponent>();
                while (query.MoveNext(out var uid, out var comp))
                {
                    //set the time remaining to the cooldown for all beacons
                    comp.InterferenceTimeRemaining = comp.InterferenceCooldown;
                }

                //send announcements about the rift
                var xform = Transform(ent.Owner);
                _announcer.SendAnnouncement(_announcer.GetAnnouncementId("CarpRift"), Filter.Broadcast(), "carp-rift-warning", colorOverride: Color.Red, localeArgs: ("location", FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString((ent.Owner, xform)))));
                _navMap.SetBeaconEnabled(ent.Owner, true);
            }
        }

        private void OnBeaconFoldAttempt(Entity<ApocarplypseBeaconComponent> ent, ref FoldAttemptEvent args)
        {
            //check if the apocarplypse beacon has a foldable component
            //it always should, but you never know
            if (TryComp<FoldableComponent>(ent.Owner, out var foldableComp))
            {

                //check if we've already been unfolded
                //todo the client(?) tries to refold this when you open the UI, ???
                if (!foldableComp.IsFolded)
                {
                    _popups.PopupEntity(Loc.GetString("uplink-apocarplypse-refold"), ent.Owner);
                    args.Cancelled = true;
                    return;
                }

                var xform = Transform(ent.Owner);
                if (TryComp<MapGridComponent>(xform.GridUid, out var grid))
                {

                    //check if we're too near to space
                    foreach (var tile in _map.GetTilesIntersecting((EntityUid)xform.GridUid, grid, new Circle(_transform.GetWorldPosition(xform), ent.Comp.NoSpaceRange), false))
                    {
                        if (tile.IsSpace())
                        {
                            _popups.PopupEntity(Loc.GetString("uplink-apocarplypse-unfold-space"), ent.Owner);
                            args.Cancelled = true;
                            return;
                        }
                    }

                    //check if we're too close to another beacon
                    foreach (var (comp, riftXform) in EntityQuery<ApocarplypseBeaconComponent, TransformComponent>(true))
                    {
                        //if that other beacon is actually this one, ignore it
                        if (comp == ent.Comp) continue;

                        if (_transform.InRange(riftXform.Coordinates, xform.Coordinates, ent.Comp.InterferenceRange))
                        {
                            _popups.PopupEntity(Loc.GetString("uplink-apocarplypse-unfold-otherbeacon"), ent.Owner);
                            args.Cancelled = true;
                            return;
                        }
                    }
                }
                else
                {
                    //not on a grid, no unfolding!
                    _popups.PopupEntity(Loc.GetString("uplink-apocarplypse-unfold-nogrid"), ent.Owner);
                    args.Cancelled = true;
                    return;
                }

                //check if the interference cooldown is still active
                if (ent.Comp.InterferenceTimeRemaining > 0)
                {
                    _popups.PopupEntity(Loc.GetString("uplink-apocarplypse-unfold-interference"), ent.Owner);
                    args.Cancelled = true;
                    return;
                }
            }
        }
    }
}
