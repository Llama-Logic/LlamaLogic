// <auto-generated>
//   This file was generated by a tool; you should avoid making direct changes.
//   Consider using 'partial classes' to extend these types
//   Input: Lot.proto
// </auto-generated>

#region Designer generated code
#pragma warning disable CS0612, CS0618, CS1591, CS3021, CS8981, IDE0079, IDE1006, RCS1036, RCS1057, RCS1085, RCS1192
namespace EA.Sims4.Network
{

    [global::ProtoBuf.ProtoContract()]
    public partial class LotInfoItem : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"zone_id", DataFormat = global::ProtoBuf.DataFormat.FixedSize)]
        public ulong ZoneId
        {
            get => __pbn__ZoneId.GetValueOrDefault();
            set => __pbn__ZoneId = value;
        }
        public bool ShouldSerializeZoneId() => __pbn__ZoneId != null;
        public void ResetZoneId() => __pbn__ZoneId = null;
        private ulong? __pbn__ZoneId;

        [global::ProtoBuf.ProtoMember(2, Name = @"name")]
        [global::System.ComponentModel.DefaultValue("")]
        public string Name
        {
            get => __pbn__Name ?? "";
            set => __pbn__Name = value;
        }
        public bool ShouldSerializeName() => __pbn__Name != null;
        public void ResetName() => __pbn__Name = null;
        private string __pbn__Name;

        [global::ProtoBuf.ProtoMember(3, Name = @"world_id")]
        public uint WorldId
        {
            get => __pbn__WorldId.GetValueOrDefault();
            set => __pbn__WorldId = value;
        }
        public bool ShouldSerializeWorldId() => __pbn__WorldId != null;
        public void ResetWorldId() => __pbn__WorldId = null;
        private uint? __pbn__WorldId;

        [global::ProtoBuf.ProtoMember(4, Name = @"lot_template_id")]
        public uint LotTemplateId
        {
            get => __pbn__LotTemplateId.GetValueOrDefault();
            set => __pbn__LotTemplateId = value;
        }
        public bool ShouldSerializeLotTemplateId() => __pbn__LotTemplateId != null;
        public void ResetLotTemplateId() => __pbn__LotTemplateId = null;
        private uint? __pbn__LotTemplateId;

        [global::ProtoBuf.ProtoMember(5, Name = @"lot_description_id", DataFormat = global::ProtoBuf.DataFormat.FixedSize)]
        public ulong LotDescriptionId
        {
            get => __pbn__LotDescriptionId.GetValueOrDefault();
            set => __pbn__LotDescriptionId = value;
        }
        public bool ShouldSerializeLotDescriptionId() => __pbn__LotDescriptionId != null;
        public void ResetLotDescriptionId() => __pbn__LotDescriptionId = null;
        private ulong? __pbn__LotDescriptionId;

        [global::ProtoBuf.ProtoMember(6, Name = @"venue_type_name")]
        public LocalizedString VenueTypeName { get; set; }

        [global::ProtoBuf.ProtoMember(7, Name = @"household_name")]
        [global::System.ComponentModel.DefaultValue("")]
        public string HouseholdName
        {
            get => __pbn__HouseholdName ?? "";
            set => __pbn__HouseholdName = value;
        }
        public bool ShouldSerializeHouseholdName() => __pbn__HouseholdName != null;
        public void ResetHouseholdName() => __pbn__HouseholdName = null;
        private string __pbn__HouseholdName;

        [global::ProtoBuf.ProtoMember(8, Name = @"venue_type")]
        public ResourceKey VenueType { get; set; }

        [global::ProtoBuf.ProtoMember(9, Name = @"region_description_id")]
        public uint RegionDescriptionId
        {
            get => __pbn__RegionDescriptionId.GetValueOrDefault();
            set => __pbn__RegionDescriptionId = value;
        }
        public bool ShouldSerializeRegionDescriptionId() => __pbn__RegionDescriptionId != null;
        public void ResetRegionDescriptionId() => __pbn__RegionDescriptionId = null;
        private uint? __pbn__RegionDescriptionId;

        [global::ProtoBuf.ProtoMember(10, Name = @"region_name")]
        [global::System.ComponentModel.DefaultValue("")]
        public string RegionName
        {
            get => __pbn__RegionName ?? "";
            set => __pbn__RegionName = value;
        }
        public bool ShouldSerializeRegionName() => __pbn__RegionName != null;
        public void ResetRegionName() => __pbn__RegionName = null;
        private string __pbn__RegionName;

        [global::ProtoBuf.ProtoMember(11, Name = @"region_icon")]
        public ResourceKey RegionIcon { get; set; }

        [global::ProtoBuf.ProtoMember(12, Name = @"house_description_id", DataFormat = global::ProtoBuf.DataFormat.FixedSize)]
        public ulong HouseDescriptionId
        {
            get => __pbn__HouseDescriptionId.GetValueOrDefault();
            set => __pbn__HouseDescriptionId = value;
        }
        public bool ShouldSerializeHouseDescriptionId() => __pbn__HouseDescriptionId != null;
        public void ResetHouseDescriptionId() => __pbn__HouseDescriptionId = null;
        private ulong? __pbn__HouseDescriptionId;

    }

    [global::ProtoBuf.ProtoContract()]
    public partial class LotPlexExteriorUpdate : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"zone_id", DataFormat = global::ProtoBuf.DataFormat.FixedSize)]
        public ulong ZoneId
        {
            get => __pbn__ZoneId.GetValueOrDefault();
            set => __pbn__ZoneId = value;
        }
        public bool ShouldSerializeZoneId() => __pbn__ZoneId != null;
        public void ResetZoneId() => __pbn__ZoneId = null;
        private ulong? __pbn__ZoneId;

        [global::ProtoBuf.ProtoMember(2, Name = @"plex_exterior_house_desc_id")]
        public uint PlexExteriorHouseDescId
        {
            get => __pbn__PlexExteriorHouseDescId.GetValueOrDefault();
            set => __pbn__PlexExteriorHouseDescId = value;
        }
        public bool ShouldSerializePlexExteriorHouseDescId() => __pbn__PlexExteriorHouseDescId != null;
        public void ResetPlexExteriorHouseDescId() => __pbn__PlexExteriorHouseDescId = null;
        private uint? __pbn__PlexExteriorHouseDescId;

    }

}

#pragma warning restore CS0612, CS0618, CS1591, CS3021, CS8981, IDE0079, IDE1006, RCS1036, RCS1057, RCS1085, RCS1192
#endregion
