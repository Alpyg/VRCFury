using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;
using VF.Builder;
using VF.Builder.Haptics;
using VF.Component;
using VF.Feature.Base;
using VF.Injector;
using VF.Utils;
using VF.Utils.Controller;

namespace VF.Service {
    /**
     * This can build the contacts needed for haptic component depth animations
     */
    [VFService]
    internal class HapticAnimContactsService {
        [VFAutowired] private readonly MathService math;
        [VFAutowired] private readonly SmoothingService smoothing;
        [VFAutowired] private readonly ActionClipService actionClipService;
        [VFAutowired] private readonly AvatarManager avatarManager;
        private ControllerManager fx => avatarManager.GetFx();
        [VFAutowired] private readonly HapticContactsService hapticContacts;
        [VFAutowired] private readonly ClipFactoryService clipFactory;

        public void CreateAnims(
            ICollection<VRCFuryHapticSocket.DepthActionNew> actions,
            VFGameObject spsComponentOwner,
            string name,
            SpsDepthContacts contacts
        ) {
            var actionNum = 0;
            foreach (var depthAction in actions) {
                actionNum++;
                var prefix = $"{name}/Anim/{actionNum}";

                var unsmoothed = depthAction.units == VRCFuryHapticSocket.DepthActionUnits.Plugs
                    ? ( depthAction.enableSelf ? contacts.closestDistancePlugLengths.Value : contacts.others.distancePlugLengths.Value )
                    : depthAction.units == VRCFuryHapticSocket.DepthActionUnits.Meters
                    ? ( depthAction.enableSelf ? contacts.closestDistanceMeters.Value : contacts.others.distanceMeters.Value )
                    : ( depthAction.enableSelf ? contacts.closestDistanceLocal.Value : contacts.others.distanceLocal.Value );
                var mapped = math.Map(
                    $"{prefix}/Mapped",
                    unsmoothed,
                    depthAction.range.Max(), depthAction.range.Min(),
                    0, 1
                );
                var smoothed = smoothing.Smooth(
                    $"{prefix}/Smoothed",
                    mapped,
                    depthAction.smoothingSeconds
                );

                var layer = fx.NewLayer("Depth Animation " + actionNum + " for " + name);
                var on = layer.NewState("On");

                var clip = actionClipService.LoadState(prefix, depthAction.actionSet, spsComponentOwner);
                if (clip.IsStatic()) {
                    var tree = clipFactory.New1D(prefix + " tree", smoothed);
                    tree.Add(0, clipFactory.GetEmptyClip());
                    tree.Add(1, clip);
                    on.WithAnimation(tree);
                } else {
                    clip.SetLooping(false);
                    on.WithAnimation(clip).MotionTime(smoothed);
                    if (depthAction.reverseClip) {
                        clip.Reverse();
                    }
                }
            }
        }
    }
}
