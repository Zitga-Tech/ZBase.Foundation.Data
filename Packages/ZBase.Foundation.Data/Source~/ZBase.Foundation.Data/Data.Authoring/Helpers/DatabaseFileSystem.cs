using System.Collections.Generic;
using System.IO;
using Cathei.BakingSheet.Internal;

namespace ZBase.Foundation.Data.Authoring
{
    public interface IExtendedFileSystem : IFileSystem
    {
        IEnumerable<string> GetFiles(string path, string extension, bool includeSubFolders);

        void Delete(string path, bool recursive = false);
    }

    public class DatabaseFileSystem : FileSystem, IExtendedFileSystem
    {
        public virtual IEnumerable<string> GetFiles(string path, string extension, bool includeSubFolders)
        {
            var option = includeSubFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return Directory.GetFiles(path, "*." + extension, option);
        }

        public virtual void Delete(string path, bool recursive = false)
        {
            Directory.Delete(path, recursive);
        }
    }
}
