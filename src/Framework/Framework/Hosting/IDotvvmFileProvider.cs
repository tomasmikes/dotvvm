using System.IO;
using System.Threading.Tasks;

namespace DotVVM.Framework.Hosting;

public interface IDotvvmFileProvider
{
    Task<Stream> OpenFileAsync(string path);
    Task<bool> FileExistsAsync(string path);
    Task WriteFileAsync(string path);
}
