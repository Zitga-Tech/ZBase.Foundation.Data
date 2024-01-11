namespace ZBase.Foundation.Data.Authoring.Configs.GoogleSheets
{
    [Database]
    [Table(typeof(FileDataTableAsset), "Files", NamingStrategy.SnakeCase)]
    public partial class FileDatabaseDefinition { }

    public sealed partial class FileDataTableAsset : DataTableAsset<int, FileData> { }

    public sealed partial class FileData : IData
    {
        [DataProperty] public int Id => Get_Id();

        [DataProperty] public string FileName => Get_FileName();

        [DataProperty] public string FileId => Get_FileId();

        [DataProperty] public string Type => Get_Type();
    }
}