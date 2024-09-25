using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.PeriodicMobSpawner.ApocarplypseBeacon
{
    [RegisterComponent]
    public sealed partial class ApocarplypseBeaconComponent : Component
    {

        public int NoSpaceRange = 3;

        public int InterferenceRange = 15;

        public float InterferenceCooldown = 120;

        public float InterferenceTimeRemaining = 0;

    }
}
