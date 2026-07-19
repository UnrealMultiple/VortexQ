using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using System.IO.Compression;
using System.Reflection.Metadata;
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
    public byte[] ExportToZip(string archiveName = "")
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

    public byte[] LoadDecompileAndZip(byte[] assemblyBytes, string virtualName = "MemoryAssembly.dll", string archiveName = "")
    {
        DecompiledFiles.Clear();
        _processedTypes.Clear();

        if (!LoadFromBuffer(assemblyBytes, virtualName))
            return null;

        if (!DecompileAll())
            return null;

        return ExportToZip(archiveName);
    }

    public byte[] LoadAndDecompile(byte[] bytes, string virtualName = "MemoryAssembly.dll", string archiveName = "")
    {
        DecompiledFiles.Clear();
        _processedTypes.Clear();

        if (IsZipArchive(bytes))
            return ProcessZipInMemory(bytes, archiveName);

        if (!LoadFromBuffer(bytes, virtualName))
            return null;

        if (!DecompileAll())
            return null;

        return ExportToZip(archiveName);
    }

    private static bool IsZipArchive(byte[] data)
    {
        return data.Length >= 4 &&
               data[0] == 0x50 &&
               data[1] == 0x4B &&
               data[2] == 0x03 &&
               data[3] == 0x04;
    }

    /// <summary>
    /// 从内存中的压缩包读取所有可反编译的程序集，逐个反编译后合并返回一个新的压缩包。
    /// </summary>
    public byte[] ProcessZipInMemory(byte[] zipBytes, string archiveName = "")
    {
        try
        {
            DecompiledFiles.Clear();
            _processedTypes.Clear();

            using var inputZip = new ZipArchive(new MemoryStream(zipBytes), ZipArchiveMode.Read);

            foreach (var entry in inputZip.Entries)
            {
                if (!IsDecompilableFile(entry.Name))
                    continue;

                // 从压缩包中读取程序集字节
                byte[] assemblyBytes;
                using (var entryStream = entry.Open())
                using (var ms = new MemoryStream())
                {
                    entryStream.CopyTo(ms);
                    assemblyBytes = ms.ToArray();
                }

                // 每个程序集独立加载和反编译
                Dispose();
                if (!LoadFromBuffer(assemblyBytes, entry.FullName))
                    continue;

                DecompileAll();

                // 为当前程序集生成 .csproj 文件
                var asmName = GetAssemblyName();
                if (asmName != null)
                {
                    var sanitizedAsm = SanitizeAssemblyName(asmName);
                    var csprojKey = $"{sanitizedAsm}/{asmName}.csproj";
                    if (!DecompiledFiles.ContainsKey(csprojKey))
                        DecompiledFiles[csprojKey] = GenerateCsproj(asmName);
                }
            }

            return ExportToZip(archiveName);
        }
        catch (Exception ex)
        {
            LastError = ex;
            return null;
        }
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

            // 处理文件名冲突（特别是泛型类型），始终使用 / 分隔符
            if (DecompiledFiles.ContainsKey(fileName))
            {
                var guidPart = Guid.NewGuid().ToString("N")[..4];
                var dir = GetDirectoryName(fileName);
                var nameWithoutExt = GetFileNameWithoutExtension(fileName);
                fileName = string.IsNullOrEmpty(dir)
                    ? $"{nameWithoutExt}_{guidPart}.cs"
                    : $"{dir}/{nameWithoutExt}_{guidPart}.cs";
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

        // 程序集名称作为顶级目录，保留点号
        var assemblyName = GetAssemblyName();
        if (assemblyName != null)
            pathComponents.Add(SanitizeAssemblyName(assemblyName));
        else
            pathComponents.Add("UnknownAssembly");

        // 命名空间去掉程序集名前缀，避免目录重复，然后按点号拆分
        var ns = typeDef.Namespace ?? "";
        if (!string.IsNullOrEmpty(ns) && assemblyName != null)
        {
            if (ns == assemblyName)
            {
                // 命名空间等于程序集名，不加额外目录
            }
            else if (ns.StartsWith(assemblyName + "."))
            {
                // 命名空间以程序集名开头，去掉前缀部分
                ns = ns[(assemblyName.Length + 1)..];
                pathComponents.AddRange(ns.Split('.').Select(SanitizeFileName));
            }
            else
            {
                // 无关的命名空间，完整拆分
                pathComponents.AddRange(ns.Split('.').Select(SanitizeFileName));
            }
        }
        else if (!string.IsNullOrEmpty(ns))
        {
            pathComponents.AddRange(ns.Split('.').Select(SanitizeFileName));
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

        // 构建完整路径，始终使用 / 分隔符确保跨平台兼容
        var fileName = string.Join("/", pathComponents) + ".cs";
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

    /// <summary>清理程序集名称，只替换真正不允许的字符，保留点号。</summary>
    private static string SanitizeAssemblyName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "Unknown";

        var sb = new StringBuilder();
        foreach (char c in name)
        {
            sb.Append(c switch
            {
                '<' or '>' or ':' or '"' or '/' or '\\' or '|' or '?' or '*' => "_",
                _ => c
            });
        }

        var cleaned = sb.ToString().Trim('.', ' ');
        return string.IsNullOrEmpty(cleaned) ? "Unknown" : cleaned;
    }

    private static bool IsDecompilableFile(string fileName)
    {
        var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
        return ext is ".dll" or ".exe";
    }

    private string GenerateCsproj(string assemblyName)
    {
        var sb = new StringBuilder();
        sb.AppendLine(@"<Project Sdk=""Microsoft.NET.Sdk"">");
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <TargetFramework>netstandard2.0</TargetFramework>");
        sb.AppendLine($"    <AssemblyName>{assemblyName}</AssemblyName>");
        sb.AppendLine("  </PropertyGroup>");

        // 从 PE 元数据读取程序集引用，生成 <Reference> 项
        try
        {
            if (_peFile?.Reader != null)
            {
                var metadataReader = _peFile.Reader.GetMetadataReader();
                var hasRefs = false;
                foreach (var handle in metadataReader.AssemblyReferences)
                {
                    var reference = metadataReader.GetAssemblyReference(handle);
                    if (!hasRefs)
                    {
                        sb.AppendLine("  <ItemGroup>");
                        hasRefs = true;
                    }
                    var refName = metadataReader.GetString(reference.Name);
                    sb.AppendLine($"    <Reference Include=\"{refName}\" />");
                }
                if (hasRefs)
                    sb.AppendLine("  </ItemGroup>");
            }
        }
        catch { }

        sb.AppendLine("</Project>");
        return sb.ToString();
    }

    /// <summary>获取 / 分隔路径的目录部分，跨平台兼容。</summary>
    private static string GetDirectoryName(string path)
    {
        var lastSlash = path.LastIndexOf('/');
        return lastSlash >= 0 ? path[..lastSlash] : "";
    }

    /// <summary>获取 / 分隔路径的不含扩展名的文件名，跨平台兼容。</summary>
    private static string GetFileNameWithoutExtension(string path)
    {
        var lastSlash = path.LastIndexOf('/');
        var fileName = lastSlash >= 0 ? path[(lastSlash + 1)..] : path;
        var lastDot = fileName.LastIndexOf('.');
        return lastDot >= 0 ? fileName[..lastDot] : fileName;
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

    /// <summary>从 PE 元数据中读取程序集真实名称，不依赖文件名。</summary>
    private string GetAssemblyName()
    {
        try
        {
            if (_peFile?.Reader == null)
                return null;
            var metadataReader = _peFile.Reader.GetMetadataReader();
            var def = metadataReader.GetAssemblyDefinition();
            return metadataReader.GetString(def.Name);
        }
        catch
        {
            return null;
        }
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