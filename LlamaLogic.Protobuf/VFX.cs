// <auto-generated>
//   This file was generated by a tool; you should avoid making direct changes.
//   Consider using 'partial classes' to extend these types
//   Input: VFX.proto
// </auto-generated>

#region Designer generated code
#pragma warning disable CS0612, CS0618, CS1591, CS3021, CS8981, IDE0079, IDE1006, RCS1036, RCS1057, RCS1085, RCS1192
namespace EA.Sims4.Network
{

    [global::ProtoBuf.ProtoContract()]
    public partial class VFXStart : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"object_id")]
        public ulong ObjectId
        {
            get => __pbn__ObjectId.GetValueOrDefault();
            set => __pbn__ObjectId = value;
        }
        public bool ShouldSerializeObjectId() => __pbn__ObjectId != null;
        public void ResetObjectId() => __pbn__ObjectId = null;
        private ulong? __pbn__ObjectId;

        [global::ProtoBuf.ProtoMember(2, Name = @"effect_name", IsRequired = true)]
        public string EffectName { get; set; }

        [global::ProtoBuf.ProtoMember(3, Name = @"actor_id", IsRequired = true)]
        public ulong ActorId { get; set; }

        [global::ProtoBuf.ProtoMember(4, Name = @"joint_name_hash", IsRequired = true)]
        public uint JointNameHash { get; set; }

        [global::ProtoBuf.ProtoMember(5, Name = @"target_actor_id")]
        public ulong TargetActorId
        {
            get => __pbn__TargetActorId.GetValueOrDefault();
            set => __pbn__TargetActorId = value;
        }
        public bool ShouldSerializeTargetActorId() => __pbn__TargetActorId != null;
        public void ResetTargetActorId() => __pbn__TargetActorId = null;
        private ulong? __pbn__TargetActorId;

        [global::ProtoBuf.ProtoMember(6, Name = @"target_joint_name_hash")]
        public uint TargetJointNameHash
        {
            get => __pbn__TargetJointNameHash.GetValueOrDefault();
            set => __pbn__TargetJointNameHash = value;
        }
        public bool ShouldSerializeTargetJointNameHash() => __pbn__TargetJointNameHash != null;
        public void ResetTargetJointNameHash() => __pbn__TargetJointNameHash = null;
        private uint? __pbn__TargetJointNameHash;

        [global::ProtoBuf.ProtoMember(7, Name = @"mirror_effect")]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool MirrorEffect
        {
            get => __pbn__MirrorEffect ?? false;
            set => __pbn__MirrorEffect = value;
        }
        public bool ShouldSerializeMirrorEffect() => __pbn__MirrorEffect != null;
        public void ResetMirrorEffect() => __pbn__MirrorEffect = null;
        private bool? __pbn__MirrorEffect;

        [global::ProtoBuf.ProtoMember(8, Name = @"auto_on_effect")]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool AutoOnEffect
        {
            get => __pbn__AutoOnEffect ?? false;
            set => __pbn__AutoOnEffect = value;
        }
        public bool ShouldSerializeAutoOnEffect() => __pbn__AutoOnEffect != null;
        public void ResetAutoOnEffect() => __pbn__AutoOnEffect = null;
        private bool? __pbn__AutoOnEffect;

        [global::ProtoBuf.ProtoMember(9, Name = @"transition_type")]
        [global::System.ComponentModel.DefaultValue(VFXStartTransitionType.SoftTransition)]
        public VFXStartTransitionType TransitionType
        {
            get => __pbn__TransitionType ?? VFXStartTransitionType.SoftTransition;
            set => __pbn__TransitionType = value;
        }
        public bool ShouldSerializeTransitionType() => __pbn__TransitionType != null;
        public void ResetTransitionType() => __pbn__TransitionType = null;
        private VFXStartTransitionType? __pbn__TransitionType;

        [global::ProtoBuf.ProtoMember(10, Name = @"transform")]
        public Transform Transform { get; set; }

        [global::ProtoBuf.ProtoMember(11, Name = @"target_joint_offset")]
        public Vector3 TargetJointOffset { get; set; }

        [global::ProtoBuf.ProtoMember(12, Name = @"callback_event_id")]
        public uint CallbackEventId
        {
            get => __pbn__CallbackEventId.GetValueOrDefault();
            set => __pbn__CallbackEventId = value;
        }
        public bool ShouldSerializeCallbackEventId() => __pbn__CallbackEventId != null;
        public void ResetCallbackEventId() => __pbn__CallbackEventId = null;
        private uint? __pbn__CallbackEventId;

        [global::ProtoBuf.ProtoContract()]
        public enum VFXStartTransitionType
        {
            [global::ProtoBuf.ProtoEnum(Name = @"SOFT_TRANSITION")]
            SoftTransition = 0,
            [global::ProtoBuf.ProtoEnum(Name = @"HARD_TRANSITION")]
            HardTransition = 1,
        }

    }

    [global::ProtoBuf.ProtoContract()]
    public partial class VFXStop : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"object_id", IsRequired = true)]
        public ulong ObjectId { get; set; }

        [global::ProtoBuf.ProtoMember(2, Name = @"actor_id", IsRequired = true)]
        public ulong ActorId { get; set; }

        [global::ProtoBuf.ProtoMember(3, Name = @"transition_type")]
        [global::System.ComponentModel.DefaultValue(VFXStopTransitionType.SoftTransition)]
        public VFXStopTransitionType TransitionType
        {
            get => __pbn__TransitionType ?? VFXStopTransitionType.SoftTransition;
            set => __pbn__TransitionType = value;
        }
        public bool ShouldSerializeTransitionType() => __pbn__TransitionType != null;
        public void ResetTransitionType() => __pbn__TransitionType = null;
        private VFXStopTransitionType? __pbn__TransitionType;

        [global::ProtoBuf.ProtoContract()]
        public enum VFXStopTransitionType
        {
            [global::ProtoBuf.ProtoEnum(Name = @"SOFT_TRANSITION")]
            SoftTransition = 0,
            [global::ProtoBuf.ProtoEnum(Name = @"HARD_TRANSITION")]
            HardTransition = 1,
        }

    }

    [global::ProtoBuf.ProtoContract()]
    public partial class VFXSetState : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"object_id", IsRequired = true)]
        public ulong ObjectId { get; set; }

        [global::ProtoBuf.ProtoMember(2, Name = @"actor_id", IsRequired = true)]
        public ulong ActorId { get; set; }

        [global::ProtoBuf.ProtoMember(3, Name = @"state_index", IsRequired = true)]
        public int StateIndex { get; set; }

        [global::ProtoBuf.ProtoMember(4, Name = @"transition_type")]
        [global::System.ComponentModel.DefaultValue(VFXTransitionType.HardTransition)]
        public VFXTransitionType TransitionType
        {
            get => __pbn__TransitionType ?? VFXTransitionType.HardTransition;
            set => __pbn__TransitionType = value;
        }
        public bool ShouldSerializeTransitionType() => __pbn__TransitionType != null;
        public void ResetTransitionType() => __pbn__TransitionType = null;
        private VFXTransitionType? __pbn__TransitionType;

    }

    [global::ProtoBuf.ProtoContract()]
    public enum VFXTransitionType
    {
        [global::ProtoBuf.ProtoEnum(Name = @"SOFT_TRANSITION")]
        SoftTransition = 0,
        [global::ProtoBuf.ProtoEnum(Name = @"HARD_TRANSITION")]
        HardTransition = 1,
    }

}

#pragma warning restore CS0612, CS0618, CS1591, CS3021, CS8981, IDE0079, IDE1006, RCS1036, RCS1057, RCS1085, RCS1192
#endregion
