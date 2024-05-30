namespace ZBase.Foundation.Data.Authoring.Configs.GoogleSheets
{
    [Database(NamingStrategy.SnakeCase)]
    public partial class FileDatabaseDefinition
    {
        [Table] public FileDataTableAsset Files { get; }
    }
}