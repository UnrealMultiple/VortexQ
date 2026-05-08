#pragma warning disable CS8618   // Naming Styles
namespace Music.QQ.Internal.User;

using System;
using System.Text.Json.Serialization;

public partial class HomePageData
{
    [JsonPropertyName("Info")]
    public DataInfo Info { get; set; }

    [JsonPropertyName("Status")]
    public long Status { get; set; }

    [JsonPropertyName("Prompt")]
    public Prompt Prompt { get; set; }

    [JsonPropertyName("TabDetail")]
    public TabDetail TabDetail { get; set; }
}

public partial class DataInfo
{
    [JsonPropertyName("BaseInfo")]
    public BaseInfo BaseInfo { get; set; }

    [JsonPropertyName("Singer")]
    public Singer Singer { get; set; }

    [JsonPropertyName("Pet")]
    public Pet Pet { get; set; }

    [JsonPropertyName("Putoo")]
    public Putoo Putoo { get; set; }

    [JsonPropertyName("SuperSubscription")]
    public SuperSubscription SuperSubscription { get; set; }

    [JsonPropertyName("Certificate")]
    public Certificate Certificate { get; set; }

    [JsonPropertyName("WxVideoChannel")]
    public WxVideoChannel WxVideoChannel { get; set; }

    [JsonPropertyName("Icons")]
    public InfoElement[] Icons { get; set; }

    [JsonPropertyName("Share")]
    public Share Share { get; set; }

    [JsonPropertyName("VisitorNum")]
    public Num VisitorNum { get; set; }

    [JsonPropertyName("FriendsNum")]
    public Num FriendsNum { get; set; }

    [JsonPropertyName("FansNum")]
    public Num FansNum { get; set; }

    [JsonPropertyName("FollowNum")]
    public Num FollowNum { get; set; }

    [JsonPropertyName("IsFollowed")]
    public long IsFollowed { get; set; }

    [JsonPropertyName("Setting")]
    public Setting Setting { get; set; }

    [JsonPropertyName("NewIconInfo")]
    public NewIconInfo NewIconInfo { get; set; }

    [JsonPropertyName("ButtonList")]
    public object ButtonList { get; set; }

    [JsonPropertyName("MusicWorldEntry")]
    public MusicWorldEntry MusicWorldEntry { get; set; }

    [JsonPropertyName("IP")]
    public Ip Ip { get; set; }

    [JsonPropertyName("UrgeUpdate")]
    public Black UrgeUpdate { get; set; }

    [JsonPropertyName("MedalList")]
    public MedalList[] MedalList { get; set; }

    [JsonPropertyName("Constellation")]
    public Constellation Constellation { get; set; }

    [JsonPropertyName("Gender")]
    public Gender Gender { get; set; }

    [JsonPropertyName("BgScheme")]
    public string BgScheme { get; set; }

    [JsonPropertyName("NumButtonList")]
    public object NumButtonList { get; set; }

    [JsonPropertyName("Black")]
    public Black Black { get; set; }

    [JsonPropertyName("CertificateList")]
    public object[] CertificateList { get; set; }

    [JsonPropertyName("BGInfoList")]
    public object BgInfoList { get; set; }

    [JsonPropertyName("DzNum")]
    public Num DzNum { get; set; }
}

public partial class BaseInfo
{
    [JsonPropertyName("IsHost")]
    public long IsHost { get; set; }

    [JsonPropertyName("IsSinger")]
    public long IsSinger { get; set; }

    [JsonPropertyName("EncryptedUin")]
    public string EncryptedUin { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; }

    [JsonPropertyName("Avatar")]
    public Uri Avatar { get; set; }

    [JsonPropertyName("BackgroundImage")]
    public Uri BackgroundImage { get; set; }

    [JsonPropertyName("BackgroundImageType")]
    public long BackgroundImageType { get; set; }

    [JsonPropertyName("Pendant")]
    public Pendant Pendant { get; set; }

    [JsonPropertyName("BigAvatar")]
    public Uri BigAvatar { get; set; }

    [JsonPropertyName("UserType")]
    public long UserType { get; set; }

    [JsonPropertyName("BgImgExt")]
    public BgImgExt BgImgExt { get; set; }
}

public partial class BgImgExt
{
    [JsonPropertyName("PSCoverList")]
    public object[] PsCoverList { get; set; }

    [JsonPropertyName("ModuleID")]
    public string ModuleId { get; set; }

    [JsonPropertyName("RecordWallScheme")]
    public string RecordWallScheme { get; set; }

    [JsonPropertyName("RecordWallStyle")]
    public string RecordWallStyle { get; set; }

    [JsonPropertyName("RecordWallBlurBg")]
    public string RecordWallBlurBg { get; set; }

    [JsonPropertyName("RecordWallFrom")]
    public long RecordWallFrom { get; set; }
}

public partial class Pendant
{
    [JsonPropertyName("HasEntry")]
    public long HasEntry { get; set; }

    [JsonPropertyName("DynamicImg")]
    public Uri DynamicImg { get; set; }

    [JsonPropertyName("StaticImg")]
    public Uri StaticImg { get; set; }

    [JsonPropertyName("PromptText")]
    public string PromptText { get; set; }

    [JsonPropertyName("Scheme")]
    public Uri Scheme { get; set; }

    [JsonPropertyName("ID")]
    public long Id { get; set; }

    [JsonPropertyName("IsDefaultImg")]
    public long IsDefaultImg { get; set; }
}

public partial class Black
{
    [JsonPropertyName("HasEntry")]
    public long HasEntry { get; set; }

    [JsonPropertyName("Status")]
    public long Status { get; set; }
}

public partial class Certificate
{
    [JsonPropertyName("HasEntry")]
    public long HasEntry { get; set; }

    [JsonPropertyName("Info")]
    public InfoElement Info { get; set; }

    [JsonPropertyName("ArrowIcon")]
    public string ArrowIcon { get; set; }
}

public partial class InfoElement
{
    [JsonPropertyName("Title")]
    public string Title { get; set; }

    [JsonPropertyName("IconURL")]
    public string IconUrl { get; set; }

    [JsonPropertyName("Jump")]
    public Jump Jump { get; set; }

    [JsonPropertyName("CarouselList")]
    public object CarouselList { get; set; }

    [JsonPropertyName("CarouselDur")]
    public long CarouselDur { get; set; }
}

public partial class Jump
{
    [JsonPropertyName("JumpType")]
    public long JumpType { get; set; }

    [JsonPropertyName("JumpURL")]
    public string JumpUrl { get; set; }

    [JsonPropertyName("IsNeedLogin")]
    public long IsNeedLogin { get; set; }
}

public partial class Constellation
{
    [JsonPropertyName("Constellation")]
    public string ConstellationConstellation { get; set; }
}

public partial class Num
{
    [JsonPropertyName("HasEntry")]
    public long HasEntry { get; set; }

    [JsonPropertyName("Num")]
    public long NumNum { get; set; }

    [JsonPropertyName("Add")]
    public string Add { get; set; }
}

public partial class Gender
{
    [JsonPropertyName("Gender")]
    public string GenderGender { get; set; }
}

public partial class Ip
{
    [JsonPropertyName("Location")]
    public string Location { get; set; }
}

public partial class MedalList
{
    [JsonPropertyName("Name")]
    public string Name { get; set; }

    [JsonPropertyName("Icon")]
    public Uri Icon { get; set; }

    [JsonPropertyName("Scheme")]
    public Uri Scheme { get; set; }

    [JsonPropertyName("Type")]
    public long Type { get; set; }

    [JsonPropertyName("Level")]
    public long Level { get; set; }

    [JsonPropertyName("MedalID")]
    public string MedalId { get; set; }
}

public partial class MusicWorldEntry
{
    [JsonPropertyName("HasEntry")]
    public long HasEntry { get; set; }

    [JsonPropertyName("BackgroundImage")]
    public string BackgroundImage { get; set; }

    [JsonPropertyName("BackgroundImageType")]
    public long BackgroundImageType { get; set; }

    [JsonPropertyName("MusicWorldBtn")]
    public MusicWorldBtn MusicWorldBtn { get; set; }
}

public partial class MusicWorldBtn
{
    [JsonPropertyName("ButtonType")]
    public long ButtonType { get; set; }

    [JsonPropertyName("ButtonStyle")]
    public long ButtonStyle { get; set; }

    [JsonPropertyName("ButtonInfo")]
    public InfoElement ButtonInfo { get; set; }

    [JsonPropertyName("ArrowIcon")]
    public string ArrowIcon { get; set; }

    [JsonPropertyName("ExtraInfo")]
    public object ExtraInfo { get; set; }

    [JsonPropertyName("ExposureID")]
    public long ExposureId { get; set; }

    [JsonPropertyName("ClickID")]
    public long ClickId { get; set; }
}

public partial class NewIconInfo
{
    [JsonPropertyName("nickname")]
    public Nickname Nickname { get; set; }

    [JsonPropertyName("iconlist")]
    public Iconlist[] Iconlist { get; set; }
}

public partial class Iconlist
{
    [JsonPropertyName("width")]
    public long Width { get; set; }

    [JsonPropertyName("height")]
    public long Height { get; set; }

    [JsonPropertyName("srcUrl")]
    public Uri SrcUrl { get; set; }

    [JsonPropertyName("style")]
    public string Style { get; set; }

    [JsonPropertyName("ext")]
    public string Ext { get; set; }

    [JsonPropertyName("desc")]
    public string Desc { get; set; }

    [JsonPropertyName("Tips")]
    public string Tips { get; set; }

    [JsonPropertyName("Helptxt")]
    public string Helptxt { get; set; }

    [JsonPropertyName("Title")]
    public Title Title { get; set; }

    [JsonPropertyName("GifURL")]
    public string GifUrl { get; set; }

    [JsonPropertyName("GifTimes")]
    public long GifTimes { get; set; }

    [JsonPropertyName("GifWidth")]
    public long GifWidth { get; set; }

    [JsonPropertyName("GifHeight")]
    public long GifHeight { get; set; }

    [JsonPropertyName("GreyURL")]
    public string GreyUrl { get; set; }

    [JsonPropertyName("GreyWidth")]
    public long GreyWidth { get; set; }

    [JsonPropertyName("GreyHeight")]
    public long GreyHeight { get; set; }

    [JsonPropertyName("GreyLeft")]
    public long GreyLeft { get; set; }

    [JsonPropertyName("GreyRight")]
    public long GreyRight { get; set; }

    [JsonPropertyName("GreyText")]
    public string GreyText { get; set; }

    [JsonPropertyName("Flag")]
    public long Flag { get; set; }

    [JsonPropertyName("Hash")]
    public long Hash { get; set; }

    [JsonPropertyName("UIStyle")]
    public long UiStyle { get; set; }

    [JsonPropertyName("Segment")]
    public Segment Segment { get; set; }

    [JsonPropertyName("Animation")]
    public string Animation { get; set; }

    [JsonPropertyName("AmnInfo")]
    public AmnInfo AmnInfo { get; set; }

    [JsonPropertyName("GreyTextColor")]
    public string GreyTextColor { get; set; }
}

public partial class AmnInfo
{
    [JsonPropertyName("GifTimes")]
    public long GifTimes { get; set; }

    [JsonPropertyName("TextColor")]
    public string TextColor { get; set; }

    [JsonPropertyName("BackgroundURL")]
    public string BackgroundUrl { get; set; }
}

public partial class Segment
{
    [JsonPropertyName("Width")]
    public long Width { get; set; }

    [JsonPropertyName("Height")]
    public long Height { get; set; }

    [JsonPropertyName("DarkIconURL")]
    public string DarkIconUrl { get; set; }

    [JsonPropertyName("LightIconURL")]
    public string LightIconUrl { get; set; }

    [JsonPropertyName("StretchLeft")]
    public long StretchLeft { get; set; }

    [JsonPropertyName("StretchRight")]
    public long StretchRight { get; set; }

    [JsonPropertyName("Contents")]
    public Content[] Contents { get; set; }

    [JsonPropertyName("MaxDisplayLen")]
    public long MaxDisplayLen { get; set; }

    [JsonPropertyName("PaddingLeft")]
    public long PaddingLeft { get; set; }

    [JsonPropertyName("PaddingRight")]
    public long PaddingRight { get; set; }

    [JsonPropertyName("JumpURL")]
    public string JumpUrl { get; set; }
}

public partial class Content
{
    [JsonPropertyName("PrefixPixel")]
    public long PrefixPixel { get; set; }

    [JsonPropertyName("Content")]
    public string ContentContent { get; set; }

    [JsonPropertyName("Font")]
    public string Font { get; set; }

    [JsonPropertyName("FontSize")]
    public long FontSize { get; set; }

    [JsonPropertyName("FontColor")]
    public string FontColor { get; set; }

    [JsonPropertyName("MaxDisplayLen")]
    public long MaxDisplayLen { get; set; }
}

public partial class Title
{
    [JsonPropertyName("content")]
    public string Content { get; set; }

    [JsonPropertyName("color")]
    public string Color { get; set; }
}

public partial class Nickname
{
    [JsonPropertyName("lightColor")]
    public string LightColor { get; set; }

    [JsonPropertyName("darkColor")]
    public string DarkColor { get; set; }
}

public partial class Pet
{
    [JsonPropertyName("HasEntry")]
    public long HasEntry { get; set; }

    [JsonPropertyName("PetURL")]
    public string PetUrl { get; set; }

    [JsonPropertyName("PetID")]
    public long PetId { get; set; }
}

public partial class Putoo
{
    [JsonPropertyName("HasEntry")]
    public long HasEntry { get; set; }

    [JsonPropertyName("Type")]
    public long Type { get; set; }

    [JsonPropertyName("Info")]
    public InfoElement Info { get; set; }

    [JsonPropertyName("ArrowIcon")]
    public string ArrowIcon { get; set; }

    [JsonPropertyName("ExtraInfo")]
    public object ExtraInfo { get; set; }
}

public partial class Setting
{
    [JsonPropertyName("HasEntry")]
    public long HasEntry { get; set; }

    [JsonPropertyName("SetBoot")]
    public SetBoot SetBoot { get; set; }
}

public partial class SetBoot
{
    [JsonPropertyName("HasEntry")]
    public long HasEntry { get; set; }

    [JsonPropertyName("ShowTime")]
    public long ShowTime { get; set; }

    [JsonPropertyName("Title")]
    public string Title { get; set; }
}

public partial class Share
{
    [JsonPropertyName("JumpURL")]
    public Uri JumpUrl { get; set; }
}

public partial class Singer
{
    [JsonPropertyName("SingerID")]
    public long SingerId { get; set; }

    [JsonPropertyName("SingerMid")]
    public string SingerMid { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; }

    [JsonPropertyName("SingerPMid")]
    public string SingerPMid { get; set; }

    [JsonPropertyName("ForeignName")]
    public string ForeignName { get; set; }

    [JsonPropertyName("SingerType")]
    public long SingerType { get; set; }

    [JsonPropertyName("IsDead")]
    public long IsDead { get; set; }

    [JsonPropertyName("SingerPic")]
    public string SingerPic { get; set; }

    [JsonPropertyName("BgMagicColor")]
    public string BgMagicColor { get; set; }
}

public partial class SuperSubscription
{
    [JsonPropertyName("HasEntry")]
    public long HasEntry { get; set; }

    [JsonPropertyName("Type")]
    public long Type { get; set; }

    [JsonPropertyName("Info")]
    public InfoElement Info { get; set; }
}

public partial class WxVideoChannel
{
    [JsonPropertyName("HasEntry")]
    public long HasEntry { get; set; }

    [JsonPropertyName("Info")]
    public InfoElement Info { get; set; }
}

public partial class Prompt
{
    [JsonPropertyName("Msg")]
    public string Msg { get; set; }

    [JsonPropertyName("URL")]
    public string Url { get; set; }
}

public partial class TabDetail
{
    [JsonPropertyName("TabList")]
    public TabList[] TabList { get; set; }

    [JsonPropertyName("HasMore")]
    public long HasMore { get; set; }

    [JsonPropertyName("Order")]
    public long Order { get; set; }

    [JsonPropertyName("TabID")]
    public string TabId { get; set; }

    [JsonPropertyName("NeedShowTab")]
    public long NeedShowTab { get; set; }

    [JsonPropertyName("SongTab")]
    public SongTab SongTab { get; set; }

    [JsonPropertyName("AlbumTab")]
    public AlbumTab AlbumTab { get; set; }

    [JsonPropertyName("MomentTab")]
    public MomentTab MomentTab { get; set; }

    [JsonPropertyName("VideoTab")]
    public VideoTab VideoTab { get; set; }

    [JsonPropertyName("DiscTab")]
    public DiscTabClass DiscTab { get; set; }

    [JsonPropertyName("IntroductionTab")]
    public DiscTabClass IntroductionTab { get; set; }

    [JsonPropertyName("ArtistWorksTab")]
    public ArtistWorksTab ArtistWorksTab { get; set; }

    [JsonPropertyName("PutaoProductTab")]
    public PutaoProductTabClass PutaoProductTab { get; set; }

    [JsonPropertyName("ShowTab")]
    public PutaoProductTabClass ShowTab { get; set; }
}

public partial class AlbumTab
{
    [JsonPropertyName("TypeList")]
    public TypeList TypeList { get; set; }

    [JsonPropertyName("AlbumList")]
    public object AlbumList { get; set; }
}

public partial class TypeList
{
    [JsonPropertyName("DefaultID")]
    public long DefaultId { get; set; }

    [JsonPropertyName("ItemList")]
    public object ItemList { get; set; }
}

public partial class ArtistWorksTab
{
    [JsonPropertyName("PeriodTag")]
    public TypeList PeriodTag { get; set; }

    [JsonPropertyName("GenreTag")]
    public TypeList GenreTag { get; set; }

    [JsonPropertyName("WorksList")]
    public object WorksList { get; set; }
}

public partial class DiscTabClass
{
    [JsonPropertyName("List")]
    public List[] List { get; set; }
}

public partial class List
{
    [JsonPropertyName("ItemType")]
    public long ItemType { get; set; }

    [JsonPropertyName("AboutList")]
    public object AboutList { get; set; }

    [JsonPropertyName("SingerInfoList")]
    public object SingerInfoList { get; set; }

    [JsonPropertyName("ChoiceSongList")]
    public object ChoiceSongList { get; set; }

    [JsonPropertyName("ChoiceVideoList")]
    public object ChoiceVideoList { get; set; }

    [JsonPropertyName("NewestCommentList")]
    public object NewestCommentList { get; set; }

    [JsonPropertyName("MyMusicList")]
    public MyMusicList[] MyMusicList { get; set; }

    [JsonPropertyName("SimilarArtistsList")]
    public object SimilarArtistsList { get; set; }

    [JsonPropertyName("ArtistAchievementList")]
    public object ArtistAchievementList { get; set; }

    [JsonPropertyName("WormholeList")]
    public WormholeList[] WormholeList { get; set; }

    [JsonPropertyName("DissList")]
    public DissList[] DissList { get; set; }

    [JsonPropertyName("AIVenus")]
    public object AiVenus { get; set; }
}

public partial class DissList
{
    [JsonPropertyName("list")]
    public ListElement[] List { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }
}

public partial class ListElement
{
    [JsonPropertyName("dissid")]
    public long Dissid { get; set; }

    [JsonPropertyName("dirid")]
    public long Dirid { get; set; }

    [JsonPropertyName("picurl")]
    public Uri Picurl { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("subtitle")]
    public string Subtitle { get; set; }

    [JsonPropertyName("icontype")]
    public long Icontype { get; set; }

    [JsonPropertyName("iconurl")]
    public string Iconurl { get; set; }

    [JsonPropertyName("isshow")]
    public long Isshow { get; set; }

    [JsonPropertyName("dir_show")]
    public long DirShow { get; set; }

    [JsonPropertyName("layer_url")]
    public string LayerUrl { get; set; }
}

public partial class MyMusicList
{
    [JsonPropertyName("MyMusic")]
    public MyMusic MyMusic { get; set; }
}

public partial class MyMusic
{
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("infos")]
    public InfoClass[] Infos { get; set; }

    [JsonPropertyName("more")]
    public More More { get; set; }
}

public partial class InfoClass
{
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("picurl")]
    public string Picurl { get; set; }

    [JsonPropertyName("subtitle")]
    public string Subtitle { get; set; }

    [JsonPropertyName("jumpurl")]
    public string Jumpurl { get; set; }

    [JsonPropertyName("type")]
    public long Type { get; set; }

    [JsonPropertyName("laypic")]
    public Uri Laypic { get; set; }

    [JsonPropertyName("disslist")]
    public object Disslist { get; set; }
}

public partial class More
{
    [JsonPropertyName("jumpType")]
    public long JumpType { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }
}

public partial class WormholeList
{
    [JsonPropertyName("WormholeType")]
    public long WormholeType { get; set; }

    [JsonPropertyName("MusicGeneList")]
    public MusicGeneList[] MusicGeneList { get; set; }

    [JsonPropertyName("MusicLibList")]
    public object MusicLibList { get; set; }
}

public partial class MusicGeneList
{
    [JsonPropertyName("Info")]
    public MusicGeneListInfo Info { get; set; }

    [JsonPropertyName("ReportData")]
    public ReportData ReportData { get; set; }
}

public partial class MusicGeneListInfo
{
    [JsonPropertyName("ShowName")]
    public string ShowName { get; set; }

    [JsonPropertyName("Subtitle")]
    public string Subtitle { get; set; }

    [JsonPropertyName("MoreJumpUrl")]
    public string MoreJumpUrl { get; set; }
}

public partial class ReportData
{
    [JsonPropertyName("ListeningReport")]
    public ListeningReport ListeningReport { get; set; }

    [JsonPropertyName("Singers")]
    public SlowDegrees[] Singers { get; set; }

    [JsonPropertyName("Ages")]
    public object[] Ages { get; set; }

    [JsonPropertyName("Genres")]
    public object[] Genres { get; set; }

    [JsonPropertyName("BPM")]
    public Bpm Bpm { get; set; }

    [JsonPropertyName("SlowDegrees")]
    public SlowDegrees SlowDegrees { get; set; }

    [JsonPropertyName("MusicTastes")]
    public object[] MusicTastes { get; set; }

    [JsonPropertyName("Grooving")]
    public Grooving Grooving { get; set; }

    [JsonPropertyName("Personality")]
    public Personality Personality { get; set; }

    [JsonPropertyName("SortArray")]
    public long[] SortArray { get; set; }

    [JsonPropertyName("IsVisitAccount")]
    public long IsVisitAccount { get; set; }

    [JsonPropertyName("TimePreference")]
    public SlowDegrees TimePreference { get; set; }
}

public partial class Bpm
{
    [JsonPropertyName("Base")]
    public Base Base { get; set; }

    [JsonPropertyName("MinScore")]
    public long MinScore { get; set; }

    [JsonPropertyName("MaxScore")]
    public long MaxScore { get; set; }

    [JsonPropertyName("CardBPMActExt")]
    public object[] CardBpmActExt { get; set; }
}

public partial class Base
{
    [JsonPropertyName("TypeTitle")]
    public string TypeTitle { get; set; }

    [JsonPropertyName("KeyWord")]
    public string KeyWord { get; set; }

    [JsonPropertyName("Slogan")]
    public string Slogan { get; set; }

    [JsonPropertyName("Pic")]
    public string Pic { get; set; }

    [JsonPropertyName("EnglishName")]
    public string EnglishName { get; set; }

    [JsonPropertyName("Id")]
    public string Id { get; set; }

    [JsonPropertyName("ShowType")]
    public long ShowType { get; set; }
}

public partial class Grooving
{
    [JsonPropertyName("Base")]
    public Base Base { get; set; }

    [JsonPropertyName("Level")]
    public long Level { get; set; }
}

public partial class ListeningReport
{
    [JsonPropertyName("Report")]
    public object[] Report { get; set; }

    [JsonPropertyName("ShowType")]
    public long ShowType { get; set; }

    [JsonPropertyName("CurrentMonth")]
    public long CurrentMonth { get; set; }
}

public partial class Personality
{
    [JsonPropertyName("Base")]
    public Base Base { get; set; }

    [JsonPropertyName("RealMBTI")]
    public Base RealMbti { get; set; }

    [JsonPropertyName("GuideTxt")]
    public string GuideTxt { get; set; }

    [JsonPropertyName("GuideScheme")]
    public string GuideScheme { get; set; }
}

public partial class SlowDegrees
{
    [JsonPropertyName("Base")]
    public Base Base { get; set; }
}

public partial class MomentTab
{
    [JsonPropertyName("List")]
    public object List { get; set; }

    [JsonPropertyName("CarouselList")]
    public object CarouselList { get; set; }
}

public partial class PutaoProductTabClass
{
    [JsonPropertyName("WebViewURL")]
    public string WebViewUrl { get; set; }
}

public partial class SongTab
{
    [JsonPropertyName("List")]
    public object List { get; set; }

    [JsonPropertyName("SongTagInfoList")]
    public object SongTagInfoList { get; set; }

    [JsonPropertyName("SearchText")]
    public string SearchText { get; set; }

    [JsonPropertyName("IsShowQLIcon")]
    public long IsShowQlIcon { get; set; }
}

public partial class TabList
{
    [JsonPropertyName("TabGroup")]
    public long TabGroup { get; set; }

    [JsonPropertyName("TabID")]
    public string TabId { get; set; }

    [JsonPropertyName("TabName")]
    public string TabName { get; set; }

    [JsonPropertyName("Count")]
    public long Count { get; set; }

    [JsonPropertyName("PageSize")]
    public long PageSize { get; set; }

    [JsonPropertyName("FirstPageMax")]
    public long FirstPageMax { get; set; }

    [JsonPropertyName("WebViewURL")]
    public string WebViewUrl { get; set; }

    [JsonPropertyName("extra")]
    public object Extra { get; set; }
}

public partial class VideoTab
{
    [JsonPropertyName("VideoList")]
    public object VideoList { get; set; }

    [JsonPropertyName("TagList")]
    public object TagList { get; set; }
}
