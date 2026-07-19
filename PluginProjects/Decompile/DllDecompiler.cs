using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using System.IO.Compression;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;

namespace Decompile;
#nullable disable
public class DllDecompiler : IDisposable
{
    #region 公共属性
    public Dictionary<string, string> DecompiledFiles { get; } = [];
    public Exception LastError { get; private set; }
    #endregion

    #region 私有字段
    private PEFile _peFile;
    private CSharpDecompiler _decompiler;
    private UniversalAssemblyResolver _resolver;
    private readonly HashSet<string> _processedTypes = [];
    #endregion

    #region 加载方法
    public bool LoadFromFile(string filePath)
    {
        try
        {
            byte[] buffer = File.ReadAllBytes(filePath);
            using var stream = new MemoryStream(buffer);
            _peFile = new PEFile(
                filePath,
                stream,
                PEStreamOptions.PrefetchMetadata | PEStreamOptions.PrefetchEntireImage
            );
            InitializeResolver(filePath);
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex;
            return false;
        }
    }

    public bool LoadFromBuffer(byte[] buffer, string virtualName = "MemoryAssembly.dll")
    {
        try
        {
            using var stream = new MemoryStream(buffer);
            _peFile = new PEFile(
                virtualName,
                stream,
                PEStreamOptions.PrefetchMetadata | PEStreamOptions.PrefetchEntireImage
            );
            InitializeResolver(virtualName);
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex;
            return false;
        }
    }
    #endregion

    #region 核心反编译逻辑
    public bool DecompileAll()
    {
        try
        {
            if (_decompiler?.TypeSystem?.MainModule == null)
                return false;

            foreach (var typeDef in _decompiler.TypeSystem.MainModule.TypeDefinitions)
            {
                if (ShouldSkipType(typeDef) ||
                    _processedTypes.Contains(typeDef.FullTypeName.FullName))
                    continue;

                var result = ProcessType(typeDef);
                if (result.HasValue)
                {
                    DecompiledFiles[result.Value.FileName] = result.Value.Code;
                    _processedTypes.Add(typeDef.FullTypeName.FullName);
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex;
            return false;
        }
    }
    #endregion

    #region ZIP 导出
    /// <summary>
    /// 将反编译结果导出为 ZIP 字节数组，全程在内存中完成，不写磁盘。
    /// </summary>
    /// <param name="archiveName">ZIP 内根目录名，默认 "Decompiled"</param>
    public byte[] ExportToZip(string archiveName = "Decompiled")
    {
        try
        {
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var (fileName, code) in DecompiledFiles)
                {
                    var entryName = string.IsNullOrEmpty(archiveName)
                        ? fileName
                        : $"{archiveName}/{fileName}";
                    var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                    writer.Write(code);
                }
            }
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            LastError = ex;
            return null;
        }
    }

    /// <summary>
    /// 一键完成：从 byte[] 加载 → 反编译 → 导出 ZIP，全程无磁盘 IO。
    /// </summary>
    /// <param name="assemblyBytes">程序集字节数据</param>
    /// <param name="virtualName">虚拟文件名</param>
    /// <param name="archiveName">ZIP 内根目录名</param>
    /// <returns>ZIP 字节数组，失败返回 null 并可通过 LastError 查看原因</returns>
    public byte[] LoadDecompileAndZip(byte[] assemblyBytes, string virtualName = "MemoryAssembly.dll", string archiveName = "Decompiled")
    {
        if (!LoadFromBuffer(assemblyBytes, virtualName))
            return null;

        if (!DecompileAll())
            return null;

        return ExportToZip(archiveName);
    }
    #endregion

    #region 类型处理逻辑
    private static bool ShouldSkipType(ITypeDefinition typeDef)
    {
        return typeDef.IsCompilerGenerated() ||
               typeDef.Name.Contains("<") ||
               typeDef.DeclaringType != null;
    }

    private (string FileName, string Code)? ProcessType(ITypeDefinition typeDef)
    {
        try
        {
            var fileName = GenerateFileName(typeDef);

            // 处理文件名冲突（特别是泛型类型）
            if (DecompiledFiles.ContainsKey(fileName))
            {
                var guidPart = Guid.NewGuid().ToString("N")[..4];
                fileName = Path.Combine(
                    Path.GetDirectoryName(fileName),
                    $"{Path.GetFileNameWithoutExtension(fileName)}_{guidPart}.cs"
                );
            }

            var code = GenerateCode(typeDef);
            return (fileName, code);
        }
        catch (Exception ex)
        {
            return ($"Error_{Guid.NewGuid()}.txt",
                $"/* Decompilation Error: {ex.Message}\n{ex.StackTrace}*/");
        }
    }

    private string GenerateFileName(ITypeDefinition typeDef)
    {
        var pathComponents = new List<string>();

        // 处理命名空间
        if (!string.IsNullOrEmpty(typeDef.Namespace))
        {
            pathComponents.AddRange(
                typeDef.Namespace.Split('.')
                    .Select(SanitizeFileName)
            );
        }
        else
        {
            pathComponents.Add("GlobalTypes");
        }

        // 构建类型层级路径
        var typeHierarchy = new Stack<string>();
        IType currentType = typeDef;
        while (currentType is ITypeDefinition def)
        {
            typeHierarchy.Push(SanitizeFileName(def.Name));
            currentType = def.DeclaringType;
        }

        pathComponents.AddRange(typeHierarchy);

        // 构建完整路径
        var fileName = Path.Combine(pathComponents.ToArray()) + ".cs";
        return fileName;
    }

    private string GenerateCode(ITypeDefinition typeDef)
    {
        var code = _decompiler.DecompileTypeAsString(typeDef.FullTypeName);
        return $"// Type: {typeDef.FullTypeName}\n" +
               $"// Assembly: {_peFile.Name}\n" +
               $"// Decompiled at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n\n" +
               code;
    }

    private static string SanitizeFileName(string name)
    {
        // 移除泛型参数计数
        name = Regex.Replace(name, @"`\d+", "");

        // 处理特殊字符
        var sb = new StringBuilder();
        foreach (char c in name)
        {
            sb.Append(c switch
            {
                '<' => "Of_",
                '>' => "",
                '[' => "Array_",
                ']' => "",
                ' ' => "_",
                '.' => "_",
                ',' => "_",
                '&' => "Ref_",
                '*' => "Pointer_",
                '?' => "Nullable_",
                _ when char.IsLetterOrDigit(c) => c,
                _ => "_"
            });
        }

        // 清理连续下划线
        string cleaned = Regex.Replace(sb.ToString(), @"_+", "_");
        cleaned = cleaned.Trim('_', '-');

        // 处理保留名称
        return string.IsNullOrEmpty(cleaned) ? "UnnamedType" : cleaned;
    }
    #endregion

    #region 初始化与依赖管理
    private void InitializeResolver(string assemblyPath)
    {
        _resolver = new UniversalAssemblyResolver(
            assemblyPath,
            false,
            _peFile.DetectTargetFrameworkId(),
            _peFile.DetectRuntimePack(),
            PEStreamOptions.PrefetchEntireImage
        );

        _decompiler = new CSharpDecompiler(
            _peFile,
            _resolver,
            new DecompilerSettings
            {
                ThrowOnAssemblyResolveErrors = false,
                ShowXmlDocumentation = true
            }
        );
    }

    public void AddReferencePath(string directory)
    {
        if (Directory.Exists(directory))
            _resolver?.AddSearchDirectory(directory);
    }
    #endregion

    #region 资源清理
    public void Dispose()
    {
        _peFile?.Dispose();
        _peFile = null;
        _resolver = null;
        _decompiler = null;
        _processedTypes.Clear();
        GC.SuppressFinalize(this);
    }

    ~DllDecompiler() => Dispose();
    #endregion
}