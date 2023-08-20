using UnityEngine;

namespace ZBase.Foundation.Data.Authoring.Configs.GoogleSheets
{
    [Database]
    [Table(typeof(FileDataTableAsset), "Files", NamingStrategy.SnakeCase)]
    public partial class FileDatabaseDefinition { }

    public sealed partial class FileDataTableAsset : DataTableAsset<int, FileData> { }

    public sealed partial class FileData : IData
    {
        [SerializeField]
        private int _id;

        [SerializeField]
        private string _fileName;

        [SerializeField]
        private string _fileId;

        [SerializeField]
        private string _folderName;

        [SerializeField]
        private string _folderId;

        [SerializeField]
        private string _url;

        [SerializeField]
        private uint _size;

        [SerializeField]
        private string _type;
    }
}