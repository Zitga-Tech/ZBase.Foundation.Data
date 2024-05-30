namespace ZBase.Foundation.Data.Authoring.Configs.GoogleSheets
{
    public sealed partial class FileDataTableAsset : DataTableAsset<int, FileData>, IDataTableAsset { }

    public sealed partial class FileData : IData
    {
        [DataProperty] public int Id => Get_Id();

        [DataProperty] public string FileName => Get_FileName();

        [DataProperty] public string FileId => Get_FileId();

        [DataProperty] public string Type => Get_Type();
    }
}