using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace RSJWYFamework.Runtiem
{
    public static partial class Utility
    {
        /// <summary>
        /// 文件和文件夹操作工具类
        /// </summary>
        public static class FileAndFolder
        {
            private const int FILE_OPERATION_RETRY_COUNT = 3; // 文件操作重试次数
            private const int FILE_OPERATION_RETRY_DELAY_MS = 100; // 文件操作重试间隔(毫秒)

            /// <summary>
            /// 确保目录存在，不存在则创建
            /// </summary>
            /// <param name="directoryPath">目录路径</param>
            /// <returns>true表示创建了新目录，false表示目录已存在</returns>
            public static bool EnsureDirectoryExists(string directoryPath)
            {
                if (string.IsNullOrWhiteSpace(directoryPath))
                {
                    throw new ArgumentException("目录路径不能为空或空白", nameof(directoryPath));
                }

                if (Directory.Exists(directoryPath))
                {
                    return false;
                }

                Directory.CreateDirectory(directoryPath);
                return true;
            }

            /// <summary>
            /// 确保文件存在，不存在则创建空文件
            /// </summary>
            /// <param name="filePath">文件路径</param>
            /// <returns>true表示创建了新文件，false表示文件已存在</returns>
            public static bool EnsureFileExists(string filePath)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException("文件路径不能为空或空白", nameof(filePath));
                }

                if (File.Exists(filePath))
                {
                    return false;
                }

                // 确保父目录存在
                string parentDirectory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(parentDirectory))
                {
                    EnsureDirectoryExists(parentDirectory);
                }

                // 创建并立即关闭文件以避免锁定
                using (File.Create(filePath))
                {
                }

                return true;
            }

            /// <summary>
            /// 确保目录和文件都存在，不存在则创建
            /// </summary>
            /// <param name="filePath">文件完整路径</param>
            public static void EnsureDirectoryAndFileExist(string filePath)
            {
                string directoryPath = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    EnsureDirectoryExists(directoryPath);
                }

                EnsureFileExists(filePath);
            }

            /// <summary>
            /// 安全删除目录及其内容
            /// </summary>
            /// <param name="directoryPath">要删除的目录路径</param>
            /// <param name="recursive">是否递归删除子目录和文件</param>
            /// <returns>true表示删除了目录，false表示目录不存在</returns>
            public static bool DeleteDirectory(string directoryPath, bool recursive = true)
            {
                if (string.IsNullOrWhiteSpace(directoryPath))
                {
                    throw new ArgumentException("目录路径不能为空或空白", nameof(directoryPath));
                }

                if (!Directory.Exists(directoryPath))
                {
                    return false;
                }

                RetryIOOperation(() => Directory.Delete(directoryPath, recursive));
                AppLogger.Log($"目录 '{directoryPath}' 已删除(递归: {recursive})");
                return true;
            }

            /// <summary>
            /// 清空目录内容但不删除目录本身
            /// </summary>
            /// <param name="directoryPath">要清空的目录路径</param>
            public static void ClearDirectory(string directoryPath)
            {
                if (string.IsNullOrWhiteSpace(directoryPath))
                {
                    throw new ArgumentException("目录路径不能为空或空白", nameof(directoryPath));
                }

                if (!Directory.Exists(directoryPath))
                {
                    return;
                }

                // 删除所有文件
                foreach (string file in Directory.GetFiles(directoryPath))
                {
                    RetryIOOperation(() => File.Delete(file));
                }

                // 删除所有子目录
                foreach (string subDirectory in Directory.GetDirectories(directoryPath))
                {
                    DeleteDirectory(subDirectory, true);
                }
            }

            /// <summary>
            /// 规范化路径，统一使用正斜杠
            /// </summary>
            /// <param name="path">要规范化的路径</param>
            /// <returns>规范化后的路径</returns>
            public static string NormalizePath(string path)
            {
                if (string.IsNullOrEmpty(path))
                {
                    return path;
                }

                return path.Replace('\\', '/').TrimEnd('/');
            }

            /// <summary>
            /// 获取文件大小(字节)
            /// </summary>
            /// <param name="filePath">文件路径</param>
            /// <returns>文件大小(字节)</returns>
            public static long GetFileSize(string filePath)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException("文件路径不能为空或空白", nameof(filePath));
                }

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("文件未找到", filePath);
                }

                return new FileInfo(filePath).Length;
            }

            /// <summary>
            /// 使用UTF-8编码读取文件全部文本内容
            /// </summary>
            /// <param name="filePath">文件路径</param>
            /// <returns>文件内容</returns>
            public static string ReadAllText(string filePath)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException("文件路径不能为空或空白", nameof(filePath));
                }

                if (!File.Exists(filePath))
                {
                    return string.Empty;
                }

                return RetryIOOperation(() => File.ReadAllText(filePath, Encoding.UTF8));
            }

            /// <summary>
            /// 复制文件
            /// </summary>
            /// <param name="sourcePath">源文件路径</param>
            /// <param name="destinationPath">目标文件路径</param>
            /// <param name="overwrite">是否覆盖已存在文件</param>
            public static void CopyFile(string sourcePath, string destinationPath, bool overwrite = false)
            {
                if (string.IsNullOrWhiteSpace(sourcePath))
                {
                    throw new ArgumentException("源路径不能为空或空白", nameof(sourcePath));
                }

                if (string.IsNullOrWhiteSpace(destinationPath))
                {
                    throw new ArgumentException("目标路径不能为空或空白", nameof(destinationPath));
                }

                if (!File.Exists(sourcePath))
                {
                    throw new FileNotFoundException("源文件未找到", sourcePath);
                }

                string destinationDirectory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationDirectory))
                {
                    EnsureDirectoryExists(destinationDirectory);
                }

                RetryIOOperation(() => File.Copy(sourcePath, destinationPath, overwrite));
            }

            /// <summary>
            /// 移动文件
            /// </summary>
            /// <param name="sourcePath">源文件路径</param>
            /// <param name="destinationPath">目标文件路径</param>
            public static void MoveFile(string sourcePath, string destinationPath)
            {
                if (string.IsNullOrWhiteSpace(sourcePath))
                {
                    throw new ArgumentException("源路径不能为空或空白", nameof(sourcePath));
                }

                if (string.IsNullOrWhiteSpace(destinationPath))
                {
                    throw new ArgumentException("目标路径不能为空或空白", nameof(destinationPath));
                }

                if (!File.Exists(sourcePath))
                {
                    throw new FileNotFoundException("源文件未找到", sourcePath);
                }

                string destinationDirectory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationDirectory))
                {
                    EnsureDirectoryExists(destinationDirectory);
                }

                if (File.Exists(destinationPath))
                {
                    RetryIOOperation(() => File.Delete(destinationPath));
                }

                RetryIOOperation(() => File.Move(sourcePath, destinationPath));
            }

            /// <summary>
            /// 重命名文件
            /// </summary>
            /// <param name="filePath">文件路径</param>
            /// <param name="newName">新文件名(不含扩展名)</param>
            public static void RenameFile(string filePath, string newName)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException("文件路径不能为空或空白", nameof(filePath));
                }

                if (string.IsNullOrWhiteSpace(newName))
                {
                    throw new ArgumentException("新文件名不能为空或空白", nameof(newName));
                }

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("文件未找到", filePath);
                }

                string directory = Path.GetDirectoryName(filePath);
                string extension = Path.GetExtension(filePath);
                string newPath = Path.Combine(directory ?? string.Empty, newName + extension);

                MoveFile(filePath, newPath);
            }

            /// <summary>
            /// 递归复制目录及其所有内容
            /// </summary>
            /// <param name="sourcePath">源目录路径</param>
            /// <param name="destinationPath">目标目录路径</param>
            public static void CopyDirectory(string sourcePath, string destinationPath)
            {
                if (string.IsNullOrWhiteSpace(sourcePath))
                {
                    throw new ArgumentException("源路径不能为空或空白", nameof(sourcePath));
                }

                if (string.IsNullOrWhiteSpace(destinationPath))
                {
                    throw new ArgumentException("目标路径不能为空或空白", nameof(destinationPath));
                }

                if (!Directory.Exists(sourcePath))
                {
                    throw new DirectoryNotFoundException($"源目录未找到: {sourcePath}");
                }

                EnsureDirectoryExists(destinationPath);

                foreach (string file in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                {
                    string relativePath = file.Substring(sourcePath.Length)
                        .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    string destinationFile = Path.Combine(destinationPath, relativePath);
                    string destinationFileDirectory = Path.GetDirectoryName(destinationFile);

                    if (!string.IsNullOrEmpty(destinationFileDirectory))
                    {
                        EnsureDirectoryExists(destinationFileDirectory);
                    }

                    CopyFile(file, destinationFile, true);
                }
            }

            /// <summary>
            /// 获取项目根目录路径
            /// </summary>
            /// <returns>项目根目录路径</returns>
            public static string GetProjectPath()
            {
                string projectPath = Path.GetDirectoryName(Application.dataPath);
                return NormalizePath(projectPath);
            }

            /// <summary>
            /// 将绝对路径转换为Unity资源路径
            /// </summary>
            /// <param name="absolutePath">绝对路径</param>
            /// <returns>Unity资源路径</returns>
            public static string AbsolutePathToAssetPath(string absolutePath)
            {
                if (string.IsNullOrWhiteSpace(absolutePath))
                {
                    throw new ArgumentException("绝对路径不能为空或空白", nameof(absolutePath));
                }

                string normalizedPath = NormalizePath(absolutePath);
                int assetsIndex = normalizedPath.IndexOf("Assets/", StringComparison.OrdinalIgnoreCase);

                if (assetsIndex < 0)
                {
                    throw new ArgumentException("路径中不包含'Assets'文件夹", nameof(absolutePath));
                }

                return normalizedPath.Substring(assetsIndex);
            }

            /// <summary>
            /// 将Unity资源路径转换为绝对路径
            /// </summary>
            /// <param name="assetPath">Unity资源路径</param>
            /// <returns>绝对路径</returns>
            public static string AssetPathToAbsolutePath(string assetPath)
            {
                if (string.IsNullOrWhiteSpace(assetPath))
                {
                    throw new ArgumentException("资源路径不能为空或空白", nameof(assetPath));
                }

                string projectPath = GetProjectPath();
                return NormalizePath(Path.Combine(projectPath, assetPath));
            }

            /// <summary>
            /// 递归查找指定名称的目录
            /// </summary>
            /// <param name="rootPath">搜索根目录</param>
            /// <param name="directoryName">要查找的目录名称</param>
            /// <returns>找到的目录路径，未找到返回空字符串</returns>
            public static string FindDirectory(string rootPath, string directoryName)
            {
                if (string.IsNullOrWhiteSpace(rootPath))
                {
                    throw new ArgumentException("根路径不能为空或空白", nameof(rootPath));
                }

                if (string.IsNullOrWhiteSpace(directoryName))
                {
                    throw new ArgumentException("目录名称不能为空或空白", nameof(directoryName));
                }

                if (!Directory.Exists(rootPath))
                {
                    throw new DirectoryNotFoundException($"根目录未找到: {rootPath}");
                }

                foreach (string directory in Directory.GetDirectories(rootPath))
                {
                    if (Path.GetFileName(directory).Equals(directoryName, StringComparison.OrdinalIgnoreCase))
                    {
                        return directory;
                    }

                    string result = FindDirectory(directory, directoryName);
                    if (!string.IsNullOrEmpty(result))
                    {
                        return result;
                    }
                }

                return string.Empty;
            }

            /// <summary>
            /// 移除文件扩展名
            /// </summary>
            /// <param name="path">文件路径</param>
            /// <returns>无扩展名的路径</returns>
            public static string RemoveExtension(string path)
            {
                if (string.IsNullOrEmpty(path))
                {
                    return path;
                }

                int lastDotIndex = path.LastIndexOf('.');
                if (lastDotIndex < 0)
                {
                    return path;
                }

                int lastSeparatorIndex = Math.Max(
                    path.LastIndexOf('/'),
                    path.LastIndexOf('\\'));

                if (lastSeparatorIndex > lastDotIndex)
                {
                    return path;
                }

                return path.Substring(0, lastDotIndex);
            }

            /// <summary>
            /// 在系统文件浏览器中打开指定目录
            /// </summary>
            /// <param name="directoryPath">目录路径</param>
            /// <returns>是否成功打开</returns>
            public static bool OpenDirectory(string directoryPath)
            {
                if (string.IsNullOrWhiteSpace(directoryPath))
                {
                    throw new ArgumentException("目录路径不能为空或空白", nameof(directoryPath));
                }

                if (!Directory.Exists(directoryPath))
                {
                    Debug.LogWarning($"目录不存在: {directoryPath}");
                    return false;
                }

                Application.OpenURL($"file:///{directoryPath}");
                return true;
            }

            /// <summary>
            /// 重试可能暂时失败的IO操作(带返回值)
            /// </summary>
            private static T RetryIOOperation<T>(Func<T> operation)
            {
                int attempts = 0;
                while (true)
                {
                    try
                    {
                        return operation();
                    }
                    catch (IOException) when (attempts < FILE_OPERATION_RETRY_COUNT)
                    {
                        attempts++;
                        System.Threading.Thread.Sleep(FILE_OPERATION_RETRY_DELAY_MS);
                    }
                }
            }

            /// <summary>
            /// 重试可能暂时失败的IO操作(无返回值)
            /// </summary>
            private static void RetryIOOperation(Action operation)
            {
                int attempts = 0;
                while (true)
                {
                    try
                    {
                        operation();
                        return;
                    }
                    catch (IOException) when (attempts < FILE_OPERATION_RETRY_COUNT)
                    {
                        attempts++;
                        System.Threading.Thread.Sleep(FILE_OPERATION_RETRY_DELAY_MS);
                    }
                }
            }

            /// <summary>
            /// 从完整路径中提取文件名(不含扩展名)
            /// </summary>
            /// <param name="path">完整路径</param>
            /// <returns>文件名(不含扩展名)</returns>
            public static string GetFileNameWithoutExtension(string path)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    throw new ArgumentException("路径不能为空或空白", nameof(path));
                }

                return Path.GetFileNameWithoutExtension(path);
            }

            /// <summary>
            /// 从完整路径中提取文件名(含扩展名)
            /// </summary>
            /// <param name="path">完整路径</param>
            /// <returns>文件名(含扩展名)</returns>
            public static string GetFileName(string path)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    throw new ArgumentException("路径不能为空或空白", nameof(path));
                }

                return Path.GetFileName(path);
            }

            /// <summary>
            /// 从完整路径中提取目录路径
            /// </summary>
            /// <param name="path">完整路径</param>
            /// <returns>目录路径</returns>
            public static string GetDirectoryPath(string path)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    throw new ArgumentException("路径不能为空或空白", nameof(path));
                }

                return Path.GetDirectoryName(path);
            }

            /// <summary>
            /// 从完整路径中提取文件扩展名(包含点)
            /// </summary>
            /// <param name="path">完整路径</param>
            /// <returns>文件扩展名(如".txt")</returns>
            public static string GetFileExtension(string path)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    throw new ArgumentException("路径不能为空或空白", nameof(path));
                }

                return Path.GetExtension(path);
            }

            /// <summary>
            /// 从完整路径中提取最后一级目录名
            /// </summary>
            /// <param name="path">完整路径</param>
            /// <returns>最后一级目录名</returns>
            public static string GetLastDirectoryName(string path)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    throw new ArgumentException("路径不能为空或空白", nameof(path));
                }

                path = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return Path.GetFileName(path);
            }

            /// <summary>
            /// 组合多个路径部分
            /// </summary>
            /// <param name="paths">要组合的路径部分</param>
            /// <returns>组合后的路径</returns>
            public static string CombinePaths(params string[] paths)
            {
                if (paths == null || paths.Length == 0)
                {
                    throw new ArgumentException("必须提供至少一个路径部分", nameof(paths));
                }

                return Path.Combine(paths);
            }
        }
    }
}