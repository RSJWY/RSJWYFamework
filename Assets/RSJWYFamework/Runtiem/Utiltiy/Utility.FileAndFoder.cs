using Assets.RSJWYFamework.Runtiem.Logger;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

namespace Assets.RSJWYFamework.Runtiem.Utiltiy
{
    public static partial class Utility
    {

        /// <summary>
        /// 文件文件夹工具
        /// 部分代码来自YooAsset EditorTools
        /// </summary>
        public static class FileAndFolder
        {

            /// <summary>
            /// 检测文件目录是否存在并创建
            /// </summary>
            /// <param name="folderPathName">文件夹路径</param>
            public static void CheckDirectoryExistsAndCreate(string folderPathName)
            {
                if (!Directory.Exists(folderPathName))
                {
                    Directory.CreateDirectory(folderPathName);
                }
            }
            /// <summary>
            /// 检测文件是否存在并创建
            /// </summary>
            /// <param name="fileName">文件夹路径</param>
            public static void CheckFileExistsAndCreate(string fileName)
            {
                if (!File.Exists(fileName))
                {
                    File.Create(fileName);
                }
            }
            /// <summary>
            /// 创建文件，如果路径文件夹不存在，则创建
            /// </summary>
            /// <param name="folderPathName">文件路径</param>
            /// <param name="fileName">文件名</param>
            public static void CheckDirectoryAndFileCreate(string folderPathName, string fileName)
            {
                CheckDirectoryExistsAndCreate(folderPathName);
                CheckFileExistsAndCreate($"{folderPathName}/{fileName}");
            }
            /// <summary>
            /// 创建文件，如果路径文件夹不存在，则创建
            /// </summary>
            /// <param name="FolderORFilePath">文件或者文件夹路径</param>
            public static void CheckDirectoryAndFileCreate(string FolderORFilePath)
            {
                // 检查路径是否包含文件名
                if (!File.Exists(FolderORFilePath) || !Directory.Exists(FolderORFilePath))
                {
                    //如果不包含有效的文件夹或者文件
                    //提取目录，检测是否存在并创建
                    string directoryPath = Path.GetDirectoryName(FolderORFilePath);
                    CheckDirectoryExistsAndCreate(directoryPath);
                    //检测文件是否存在并创建
                    CheckFileExistsAndCreate(FolderORFilePath);
                }
            }
            /// <summary>
            /// 创建文件夹
            /// </summary>
            public static bool CreateDirectory(string directory)
            {
                if (Directory.Exists(directory) == false)
                {
                    Directory.CreateDirectory(directory);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            /// <summary>
            /// 创建文件所在的目录
            /// </summary>
            /// <param name="filePath">文件路径</param>
            public static void CreateFileDirectory(string filePath)
            {
                string destDirectory = Path.GetDirectoryName(filePath);
                CreateDirectory(destDirectory);
            }
            /// <summary>
            /// 清空文件夹
            /// </summary>
            /// <param name="directoryPath"></param>
            /// <exception cref="DirectoryNotFoundException"></exception>
            public static void ClearDirectory(string directoryPath)
            {
                if (Directory.Exists(directoryPath) == false)
                    return;

                // 删除文件
                string[] allFiles = Directory.GetFiles(directoryPath);
                for (int i = 0; i < allFiles.Length; i++)
                {
                    File.Delete(allFiles[i]);
                }

                // 删除文件夹
                string[] allFolders = Directory.GetDirectories(directoryPath);
                for (int i = 0; i < allFolders.Length; i++)
                {
                    Directory.Delete(allFolders[i], true);
                }

            }
            /// <summary>
            /// 删除文件夹及子目录
            /// </summary>
            public static bool DeleteDirectory(string directory)
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                    APPLogger.Log($"Directory '{directory}' deleted.");
                    return true;
                }
                else
                {
                    return false;
                }
            }

            /// <summary>
            /// 获取规范的路径
            /// </summary>
            public static string GetRegularPath(string path)
            {
                return path.Replace('\\', '/').Replace("\\", "/"); //替换为Linux路径格式
            }

            /// <summary>
            /// 获取文件字节大小
            /// </summary>
            public static long GetFileSize(string filePath)
            {
                FileInfo fileInfo = new FileInfo(filePath);
                return fileInfo.Length;
            }

            /// <summary>
            /// 读取文件的所有文本内容
            /// </summary>
            public static string ReadFileAllText(string filePath)
            {
                if (File.Exists(filePath) == false)
                    return string.Empty;
                return File.ReadAllText(filePath, Encoding.UTF8);
            }

            /// <summary>
            /// 拷贝文件
            /// </summary>
            public static void CopyFile(string sourcePath, string destPath, bool overwrite)
            {
                if (File.Exists(sourcePath) == false)
                    throw new FileNotFoundException(sourcePath);

                // 创建目录
                CreateFileDirectory(destPath);

                // 复制文件
                File.Copy(sourcePath, destPath, overwrite);
            }
            /// <summary>
            /// 文件重命名
            /// </summary>
            public static void FileRename(string filePath, string newName)
            {
                string dirPath = Path.GetDirectoryName(filePath);
                string destPath;
                if (Path.HasExtension(filePath))
                {
                    string extentsion = Path.GetExtension(filePath);
                    destPath = $"{dirPath}/{newName}{extentsion}";
                }
                else
                {
                    destPath = $"{dirPath}/{newName}";
                }
                FileInfo fileInfo = new FileInfo(filePath);
                fileInfo.MoveTo(destPath);
            }

            /// <summary>
            /// 移动文件
            /// </summary>
            public static void MoveFile(string filePath, string destPath)
            {
                if (File.Exists(destPath))
                    File.Delete(destPath);

                FileInfo fileInfo = new FileInfo(filePath);
                fileInfo.MoveTo(destPath);
            }
            /// <summary>
            /// 拷贝文件夹
            /// 注意：包括所有子目录的文件
            /// </summary>
            public static void CopyDirectory(string sourcePath, string destPath)
            {
                sourcePath = GetRegularPath(sourcePath);

                // If the destination directory doesn't exist, create it.
                if (Directory.Exists(destPath) == false)
                    Directory.CreateDirectory(destPath);

                string[] fileList = Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories);
                foreach (string file in fileList)
                {
                    string temp = GetRegularPath(file);
                    string savePath = temp.Replace(sourcePath, destPath);
                    CopyFile(file, savePath, true);
                }
            }
            /// <summary>
            /// 获取项目工程路径
            /// </summary>
            public static string GetProjectPath()
            {
                string projectPath = Path.GetDirectoryName(Application.dataPath);
                return GetRegularPath(projectPath);
            }
            /// <summary>
            /// 转换文件的绝对路径为Unity资源路径
            /// 例如 D:\\YourPorject\\Assets\\Works\\file.txt 替换为 Assets/Works/file.txt
            /// </summary>
            public static string AbsolutePathToAssetPath(string absolutePath)
            {
                string content = GetRegularPath(absolutePath);
                return Substring(content, "Assets/", true);
            }

            /// <summary>
            /// 转换Unity资源路径为文件的绝对路径
            /// 例如：Assets/Works/file.txt 替换为 D:\\YourPorject/Assets/Works/file.txt
            /// </summary>
            public static string AssetPathToAbsolutePath(string assetPath)
            {
                string projectPath = GetProjectPath();
                return $"{projectPath}/{assetPath}";
            }

            /// <summary>
            /// 递归查找目标文件夹路径
            /// </summary>
            /// <param name="root">搜索的根目录</param>
            /// <param name="folderName">目标文件夹名称</param>
            /// <returns>返回找到的文件夹路径，如果没有找到返回空字符串</returns>
            public static string FindFolder(string root, string folderName)
            {
                DirectoryInfo rootInfo = new DirectoryInfo(root);
                DirectoryInfo[] infoList = rootInfo.GetDirectories();
                for (int i = 0; i < infoList.Length; i++)
                {
                    string fullPath = infoList[i].FullName;
                    if (infoList[i].Name == folderName)
                        return fullPath;

                    string result = FindFolder(fullPath, folderName);
                    if (string.IsNullOrEmpty(result) == false)
                        return result;
                }
                return string.Empty;
            }

            /// <summary>
            /// 截取字符串
            /// 获取匹配到的后面内容
            /// </summary>
            /// <param name="content">内容</param>
            /// <param name="key">关键字</param>
            /// <param name="includeKey">分割的结果里是否包含关键字</param>
            /// <param name="searchBegin">是否使用初始匹配的位置，否则使用末尾匹配的位置</param>
            public static string Substring(string content, string key, bool includeKey, bool firstMatch = true)
            {
                if (string.IsNullOrEmpty(key))
                    return content;

                int startIndex = -1;
                if (firstMatch)
                    startIndex = content.IndexOf(key); //返回子字符串第一次出现位置		
                else
                    startIndex = content.LastIndexOf(key); //返回子字符串最后出现的位置

                // 如果没有找到匹配的关键字
                if (startIndex == -1)
                    return content;

                if (includeKey)
                    return content.Substring(startIndex);
                else
                    return content.Substring(startIndex + key.Length);
            }
            /// <summary>
            /// 移除路径里的后缀名
            /// </summary>
            public static string RemoveExtension(string str)
            {
                if (string.IsNullOrEmpty(str))
                    return str;

                int index = str.LastIndexOf('.');
                if (index == -1)
                    return str;
                else
                    return str.Remove(index); //"assets/config/test.unity3d" --> "assets/config/test"
            }

            public static bool OpenFolder(string folderPath)
            {
                if (Directory.Exists(folderPath))
                {
                    // 在Windows上打开文件夹
                    Application.OpenURL("file:///" + folderPath);
                    return true;
                }
                else
                {
                    Debug.LogWarning("Folder does not exist: " + folderPath);
                    return false;
                }
            }
        }
    }
}