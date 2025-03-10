// <auto-generated>
//   This file was generated by a tool; you should avoid making direct changes.
//   Consider using 'partial classes' to extend these types
//   Input: Loot.proto
// </auto-generated>

#region Designer generated code
#pragma warning disable CS0612, CS0618, CS1591, CS3021, CS8981, IDE0079, IDE1006, RCS1036, RCS1057, RCS1085, RCS1192
namespace EA.Sims4.Network
{

    [global::ProtoBuf.ProtoContract()]
    public partial class SituationEnded : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"icon_info", IsRequired = true)]
        public IconInfo IconInfo { get; set; }

        [global::ProtoBuf.ProtoMember(3, Name = @"final_score")]
        public uint FinalScore
        {
            get => __pbn__FinalScore.GetValueOrDefault();
            set => __pbn__FinalScore = value;
        }
        public bool ShouldSerializeFinalScore() => __pbn__FinalScore != null;
        public void ResetFinalScore() => __pbn__FinalScore = null;
        private uint? __pbn__FinalScore;

        [global::ProtoBuf.ProtoMember(4, Name = @"final_level")]
        public SituationLevelUpdate FinalLevel { get; set; }

        [global::ProtoBuf.ProtoMember(5, Name = @"sim_ids", DataFormat = global::ProtoBuf.DataFormat.FixedSize)]
        public ulong[] SimIds { get; set; }

        [global::ProtoBuf.ProtoMember(6, Name = @"audio_sting")]
        public ResourceKey AudioSting { get; set; }

    }

}

#pragma warning restore CS0612, CS0618, CS1591, CS3021, CS8981, IDE0079, IDE1006, RCS1036, RCS1057, RCS1085, RCS1192
#endregion
