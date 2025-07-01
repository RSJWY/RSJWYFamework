using System;
using System.Collections.Generic;
using RSJWYFamework.Runtime;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;

namespace RSJWYFamework.Editor
{
    public class YooAssetToolWindows : OdinEditorWindow
    {
        [ReadOnly]
        [LabelText("配置文件")]
        public AppConfig appConfig;
        
        [LabelText("包列表数据")]
        [BoxGroup("包列表")]
        [ShowInInspector]
        public List<YooAssetPackageDataEditor> packageDataList=new ();

        [LabelText("构建输出的根目录")]
        [ReadOnly] 
        public string buildoutputRoot;
        [LabelText("内置文件的根目录")]
        [ReadOnly]
        public string streamingAssetsRoot;

        [LabelText("包版本")] public EditorPrefString PackageVersion = 
            new("RSJWYFamework.Editor.YooAsset.YooAssetToolWindows.PackageVersion","test");
        
        [LabelText("构建目标平台")]
        public BuildTarget buildTarget = BuildTarget.StandaloneWindows64;

        [LabelText("文件名样式")] public EditorPrefEnum<EFileNameStyle> FileNameStyle =
            new("RSJWYFamework.Editor.YooAsset.YooAssetToolWindows.FileNameStyle", EFileNameStyle.BundleName_HashName);

        [LabelText("构建后文件拷贝模式")]
        public EditorPrefEnum<EBuildinFileCopyOption> BuildinFileCopyOption = 
            new("RSJWYFamework.Editor.YooAsset.YooAssetToolWindows.BuildinFileCopyOption",EBuildinFileCopyOption.ClearAndCopyAll);

        [LabelText("内置文件拷贝参数")]
        public string BuildinFileCopyParams=string.Empty;

        [LabelText("压缩选项")] public EditorPrefEnum<ECompressOption> CompressOption =
            new("RSJWYFamework.Editor.YooAsset.YooAssetToolWindows.CompressOption",ECompressOption.LZ4);
        
        [LabelText("清空缓存文件")]
        public EditorPrefBool  ClearBuildCacheFiles= 
            new ("RSJWYFamework.Editor.YooAsset.YooAssetToolWindows.ClearBuildCacheFiles", true);

        [LabelText("使用依赖资源缓存数据库")]
        public EditorPrefBool  UseAssetDependencyDB=
            new ("RSJWYFamework.Editor.YooAsset.YooAssetToolWindows.UseAssetDependencyDB", true);
        
        [Button("构建所有包",ButtonSizes.Gigantic)]
        void BuildAllPackage()
        {
            foreach (var packageData in packageDataList)
            {
                var result = Build(packageData.PackageName, packageData.BuildPipeline);
                if (result.Success)
                {
                    AppLogger.Log($"包：{packageData.PackageName}构建成功");
                }
                else
                {
                    AppLogger.Error($"包：{packageData.PackageName}构建失败！\n{result.ErrorInfo}");
                }
            }
        }
        
        /// <summary>
        /// 构建
        /// </summary>
        /// <param name="PackageName"></param>
        /// <param name="BuildPipeline"></param>
        private BuildResult Build(string PackageName,EBuildPipeline BuildPipeline)
        {
            AppLogger.Log($"构建资源包：{PackageName}，构建管线为：{BuildPipeline}");
            string packageName =PackageName;
            string buildPipelineName = BuildPipeline.ToString();
            BuildResult buildResult;
            //模拟构建
            if (buildPipelineName == EBuildPipeline.EditorSimulateBuildPipeline.ToString())
            {
                var buildParameters = new EditorSimulateBuildParameters();
                buildParameters.BuildOutputRoot = buildoutputRoot;
                buildParameters.BuildinFileRoot = streamingAssetsRoot;
                buildParameters.BuildPipeline = EBuildPipeline.EditorSimulateBuildPipeline.ToString();
                buildParameters.BuildBundleType = (int)EBuildBundleType.VirtualBundle;
                buildParameters.BuildTarget = buildTarget;
                buildParameters.PackageName = packageName;
                buildParameters.PackageVersion = PackageVersion;
                buildParameters.FileNameStyle =FileNameStyle;
                buildParameters.BuildinFileCopyOption =BuildinFileCopyOption;
                buildParameters.BuildinFileCopyParams = BuildinFileCopyParams;
                buildParameters.ClearBuildCacheFiles = ClearBuildCacheFiles;
                buildParameters.UseAssetDependencyDB = UseAssetDependencyDB;

                var pipeline = new EditorSimulateBuildPipeline();
                buildResult = pipeline.Run(buildParameters, false);
            }
            //可编程构建管线
            else if (buildPipelineName == EBuildPipeline.ScriptableBuildPipeline.ToString())
            {
                // 内置着色器资源包名称
                var builtinShaderBundleName = UtilityEditor.YooAsset.GetBuiltinShaderBundleName(packageName);
                var buildParameters = new ScriptableBuildParameters();

                buildParameters.BuildOutputRoot = buildoutputRoot;
                buildParameters.BuildinFileRoot = streamingAssetsRoot;
                buildParameters.BuildPipeline = EBuildPipeline.ScriptableBuildPipeline.ToString();
                buildParameters.BuildBundleType = (int)EBuildBundleType.AssetBundle;
                buildParameters.BuildTarget =buildTarget;
                buildParameters.PackageName = packageName;
                buildParameters.PackageVersion = PackageVersion;
                buildParameters.EnableSharePackRule = true;
                buildParameters.VerifyBuildingResult = true;
                buildParameters.FileNameStyle =FileNameStyle;
                buildParameters.BuildinFileCopyOption = BuildinFileCopyOption;
                buildParameters.BuildinFileCopyParams = BuildinFileCopyParams;
                buildParameters.CompressOption = CompressOption;
                buildParameters.ClearBuildCacheFiles = ClearBuildCacheFiles;
                buildParameters.UseAssetDependencyDB = UseAssetDependencyDB;
                buildParameters.BuiltinShadersBundleName = builtinShaderBundleName;
                buildParameters.EncryptionServices = new Utility.YooAsset.EncryptPF();
                buildParameters.ManifestServices = new Utility.YooAsset.AppHotPackgaeManifestServices();

                var pipeline = new ScriptableBuildPipeline();
                buildResult = pipeline.Run(buildParameters, false);
            }
            //内置构建管线
            else if (buildPipelineName == EBuildPipeline.BuiltinBuildPipeline.ToString())
            {

                var buildParameters = new BuiltinBuildParameters();
                buildParameters.BuildOutputRoot = buildoutputRoot;
                buildParameters.BuildinFileRoot = streamingAssetsRoot;
                buildParameters.BuildPipeline = EBuildPipeline.ScriptableBuildPipeline.ToString();
                buildParameters.BuildBundleType = (int)EBuildBundleType.AssetBundle;
                buildParameters.BuildTarget = buildTarget;
                buildParameters.PackageName = packageName;
                buildParameters.PackageVersion = PackageVersion;
                buildParameters.EnableSharePackRule = true;
                buildParameters.VerifyBuildingResult = true;
                buildParameters.FileNameStyle = FileNameStyle;
                buildParameters.BuildinFileCopyOption =BuildinFileCopyOption;
                buildParameters.BuildinFileCopyParams =BuildinFileCopyParams;
                buildParameters.CompressOption = CompressOption ;
                buildParameters.ClearBuildCacheFiles = ClearBuildCacheFiles;
                buildParameters.UseAssetDependencyDB = UseAssetDependencyDB;
                buildParameters.EncryptionServices = new Utility.YooAsset.EncryptPF();
                buildParameters.ManifestServices = new Utility.YooAsset.AppHotPackgaeManifestServices();
                
                var pipeline = new BuiltinBuildPipeline();
                buildResult = pipeline.Run(buildParameters, false);
            }
            //原生文件构建管线
            else if (buildPipelineName == EBuildPipeline.RawFileBuildPipeline.ToString())
            {

                var buildParameters = new RawFileBuildParameters();
                buildParameters.BuildOutputRoot = buildoutputRoot;
                buildParameters.BuildinFileRoot =streamingAssetsRoot;
                buildParameters.BuildPipeline = EBuildPipeline.RawFileBuildPipeline.ToString();
                buildParameters.BuildBundleType = (int)EBuildBundleType.RawBundle;
                buildParameters.BuildTarget = buildTarget;
                buildParameters.PackageName = packageName;
                buildParameters.PackageVersion = PackageVersion;
                buildParameters.VerifyBuildingResult = true;
                buildParameters.FileNameStyle = FileNameStyle;
                buildParameters.BuildinFileCopyOption = BuildinFileCopyOption;
                buildParameters.BuildinFileCopyParams =BuildinFileCopyParams;
                buildParameters.ClearBuildCacheFiles = ClearBuildCacheFiles;
                buildParameters.UseAssetDependencyDB = UseAssetDependencyDB;
                buildParameters.EncryptionServices = new Utility.YooAsset.EncryptRF();
                buildParameters.ManifestServices = new Utility.YooAsset.AppHotPackgaeManifestServices();
                
                var pipeline = new RawFileBuildPipeline();
                buildResult = pipeline.Run(buildParameters, false);
            }
            else
            {
                throw new System.NotImplementedException(buildPipelineName);
            }

            return buildResult;
        }
        [MenuItem("RSJWYFamework/打开YooAsset资源构建工具")]
        public static void OpenYooAssetBuildWindowsA()
        {
            var _windows = OdinEditorWindow.GetWindow<YooAssetToolWindows>();
            _windows.titleContent = new GUIContent("YooAsset资源构建工具");
            _windows.Show();
        }
        [Button("获取包名",ButtonSizes.Large)]
        [BoxGroup("包列表")]
        public void GetAppConfigToPackageData()
        {
            packageDataList.Clear();
            foreach (var packages in appConfig.YooAssetPackageData )
            {
                var _package=new YooAssetPackageDataEditor();
                _package.PackageName = packages.PackageName;
                _package.PackageTips = packages.PackageTips;
                //获取上次存储的管线设置
                var _YooAssetPackageDataEditorData =
                    EditorPrefs.GetString($"YooAssetToolWindows_{_package.PackageName}");
                if (string.IsNullOrEmpty(_YooAssetPackageDataEditorData))
                {
                    _package.BuildPipeline = EBuildPipeline.BuiltinBuildPipeline;
                }
                else
                {
                    
                    if (Enum.TryParse<EBuildPipeline>(_YooAssetPackageDataEditorData, out var buildPipeline))
                    {
                        _package.BuildPipeline = buildPipeline;
                    }
                    else
                    {
                        _package.BuildPipeline = EBuildPipeline.BuiltinBuildPipeline;
                    }
                }
                packageDataList.Add(_package);
            }
            AppLogger.Log("载入上次配置的构建管线");
        }
        protected override void OnEnable()
        {
            base.OnEnable();
            buildoutputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot();
            streamingAssetsRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
            var appSB = UtilityEditor.GetSettingConfigList<AppConfig>();
            if (appSB.Count!=1)
            {
                AppLogger.Warning($"App配置文件不存在或者不唯一！数量：{appSB.Count}");
            }
            else
            {
                appConfig=appSB[0];
                GetAppConfigToPackageData();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            //存储构建管线
            foreach (var packages in packageDataList)
            {
                EditorPrefs.SetString($"YooAssetToolWindows_{packages.PackageName}", packages.BuildPipeline.ToString());
            }
        }
        
    }
}