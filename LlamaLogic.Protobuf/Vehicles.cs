// <auto-generated>
//   This file was generated by a tool; you should avoid making direct changes.
//   Consider using 'partial classes' to extend these types
//   Input: Vehicles.proto
// </auto-generated>

#region Designer generated code
#pragma warning disable CS0612, CS0618, CS1591, CS3021, CS8981, IDE0079, IDE1006, RCS1036, RCS1057, RCS1085, RCS1192
namespace EA.Sims4.Network
{

    [global::ProtoBuf.ProtoContract()]
    public partial class VehicleControl : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"control_id")]
        public uint ControlId
        {
            get => __pbn__ControlId.GetValueOrDefault();
            set => __pbn__ControlId = value;
        }
        public bool ShouldSerializeControlId() => __pbn__ControlId != null;
        public void ResetControlId() => __pbn__ControlId = null;
        private uint? __pbn__ControlId;

        [global::ProtoBuf.ProtoMember(2, Name = @"control_type")]
        [global::System.ComponentModel.DefaultValue(VehicleControlType.Unknown)]
        public VehicleControlType ControlType
        {
            get => __pbn__ControlType ?? VehicleControlType.Unknown;
            set => __pbn__ControlType = value;
        }
        public bool ShouldSerializeControlType() => __pbn__ControlType != null;
        public void ResetControlType() => __pbn__ControlType = null;
        private VehicleControlType? __pbn__ControlType;

        [global::ProtoBuf.ProtoMember(3, Name = @"joint_name_hash")]
        public uint JointNameHash
        {
            get => __pbn__JointNameHash.GetValueOrDefault();
            set => __pbn__JointNameHash = value;
        }
        public bool ShouldSerializeJointNameHash() => __pbn__JointNameHash != null;
        public void ResetJointNameHash() => __pbn__JointNameHash = null;
        private uint? __pbn__JointNameHash;

        [global::ProtoBuf.ProtoMember(4, Name = @"reference_joint_name_hash")]
        public uint ReferenceJointNameHash
        {
            get => __pbn__ReferenceJointNameHash.GetValueOrDefault();
            set => __pbn__ReferenceJointNameHash = value;
        }
        public bool ShouldSerializeReferenceJointNameHash() => __pbn__ReferenceJointNameHash != null;
        public void ResetReferenceJointNameHash() => __pbn__ReferenceJointNameHash = null;
        private uint? __pbn__ReferenceJointNameHash;

        [global::ProtoBuf.ProtoMember(5, Name = @"enable_terrain_alignment")]
        public bool EnableTerrainAlignment
        {
            get => __pbn__EnableTerrainAlignment.GetValueOrDefault();
            set => __pbn__EnableTerrainAlignment = value;
        }
        public bool ShouldSerializeEnableTerrainAlignment() => __pbn__EnableTerrainAlignment != null;
        public void ResetEnableTerrainAlignment() => __pbn__EnableTerrainAlignment = null;
        private bool? __pbn__EnableTerrainAlignment;

        [global::ProtoBuf.ProtoMember(6, Name = @"bump_sound_name")]
        [global::System.ComponentModel.DefaultValue("")]
        public string BumpSoundName
        {
            get => __pbn__BumpSoundName ?? "";
            set => __pbn__BumpSoundName = value;
        }
        public bool ShouldSerializeBumpSoundName() => __pbn__BumpSoundName != null;
        public void ResetBumpSoundName() => __pbn__BumpSoundName = null;
        private string __pbn__BumpSoundName;

        [global::ProtoBuf.ProtoContract()]
        public enum VehicleControlType
        {
            [global::ProtoBuf.ProtoEnum(Name = @"UNKNOWN")]
            Unknown = 0,
            [global::ProtoBuf.ProtoEnum(Name = @"WHEEL")]
            Wheel = 1,
        }

    }

    [global::ProtoBuf.ProtoContract()]
    public partial class VehicleData : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"controls")]
        public global::System.Collections.Generic.List<VehicleControl> Controls { get; } = new global::System.Collections.Generic.List<VehicleControl>();

    }

}

#pragma warning restore CS0612, CS0618, CS1591, CS3021, CS8981, IDE0079, IDE1006, RCS1036, RCS1057, RCS1085, RCS1192
#endregion
