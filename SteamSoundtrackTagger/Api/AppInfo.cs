using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SteamSoundtrackTagger.Api;

public class AppInfo
{
    [JsonPropertyName("common")]
    public CommonData Common { get; set; }
    [JsonPropertyName("albummetadata")]
    public AlbumMetadataData AlbumMetadata { get; set; }
    
    public class CommonData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("parent")]
        public string Parent { get; set; }
    }
    
    public class AlbumMetadataData
    {
        [JsonPropertyName("tracks")]
        public Dictionary<string, TracksData> Tracks { get; set; }
        [JsonPropertyName("metadata")]
        public MetadataData Metadata { get; set; }
        [JsonPropertyName("cdn_assets")]
        public CdnAssetsData CdnAssets { get; set; }
        
        public class CdnAssetsData
        {
            [JsonPropertyName("album_cover")]
            public string AlbumCover { get; set; }
        }
        
        public class TracksData
        {
            [JsonPropertyName("discnumber")]
            public string DiscNumber { get; set; }
            [JsonPropertyName("tracknumber")]
            public string TrackNumber { get; set; }
            [JsonPropertyName("originalname")]
            public string OriginalName { get; set; }
            [JsonPropertyName("m")]
            public string Minutes { get; set; }
            [JsonPropertyName("s")]
            public string Seconds { get; set; }
        }
        
        public class MetadataData
        {
            [JsonPropertyName("artist")]
            public ArtistData Artist { get; set; }
            [JsonPropertyName("composer")]
            public ComposerData Composer { get; set; }
            [JsonPropertyName("label")]
            public LabelData Label { get; set; }
        }
        
        public class ArtistData
        {
            [JsonPropertyName("english")]
            public string English { get; set; }
        }
        
        public class ComposerData
        {
            [JsonPropertyName("english")]
            public string English { get; set; }
        }
        
        public class LabelData
        {
            [JsonPropertyName("english")]
            public string English { get; set; }
        }
    }
}