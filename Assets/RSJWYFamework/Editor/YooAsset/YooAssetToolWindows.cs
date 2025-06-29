using RSJWYFamework.Runtiem;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;

namespace RSJWYFamework.Editor
{
    public class YooAssetBuildWindow : OdinEditorWindow
    {
        [InlineEditor(InlineEditorModes.FullEditor)]
        [LabelText("配置文件")]
        public YooAssetPackages SettingData;
        
        [LabelText("构建目标")]
        public BuildTarget buildTarget = BuildTarget.StandaloneWindows64;

        [LabelText("构建输出的根目录")]
        [ReadOnly] 
        public string buildoutputRoot;
        [LabelText("内置文件的根目录")]
        [ReadOnly]
        public string streamingAssetsRoot;
        
        [LabelText("包版本")]
        public string PackageVersion ="test";

        [LabelText("清空缓存文件")]
        public bool ClearBuildCacheFiles;

        [LabelText("文件名样式")]
        public EFileNameStyle FileNameStyle=EFileNameStyle.BundleName_HashName;

        [LabelText("构建后文件拷贝模式")]
        public EBuildinFileCopyOption BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyAll;

        [LabelText("压缩选项")]
        public ECompressOption CompressOption = ECompressOption.LZ4;
        
        [Button("构建所有包",ButtonSizes.Gigantic)]
        void BuildAllPackage()
        {
            foreach (var package in SettingData.packages)
            {
                //Build(PackageName: package.PackageName,BuildPipeline: package.BuildPipeline);
            }
        }
        
        /// <summary>
        /// 构建
        /// </summary>
        /// <param name="PackageName"></param>
        /// <param name="BuildPipeline"></param>
        private PackageInvokeBuildResult Build(string PackageName,EBuildPipeline BuildPipeline)
        {
            string packageName =PackageName;
            string buildPipelineName = BuildPipeline.ToString();

            if (buildPipelineName == EBuildPipeline.EditorSimulateBuildPipeline.ToString())
            {
                string projectPath = EditorTools.GetProjectPath();
                string outputRoot = $"{projectPath}/Bundles/Tester_ESBP";

                var buildParameters = new EditorSimulateBuildParameters();
                buildParameters.BuildOutputRoot = outputRoot;
                buildParameters.BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
                buildParameters.BuildPipeline = EBuildPipeline.EditorSimulateBuildPipeline.ToString();
                buildParameters.BuildBundleType = (int)EBuildBundleType.VirtualBundle;
                buildParameters.BuildTarget = EditorUserBuildSettings.activeBuildTarget;
                buildParameters.PackageName = packageName;
                buildParameters.PackageVersion = "TestVersion";
                buildParameters.FileNameStyle = EFileNameStyle.HashName;
                buildParameters.BuildinFileCopyOption = EBuildinFileCopyOption.None;
                buildParameters.BuildinFileCopyParams = string.Empty;
                buildParameters.ClearBuildCacheFiles = true;
                buildParameters.UseAssetDependencyDB = true;

                var pipeline = new EditorSimulateBuildPipeline();
                BuildResult buildResult = pipeline.Run(buildParameters, false);
                if (buildResult.Success)
                {
                    var reulst = new PackageInvokeBuildResult();
                    reulst.PackageRootDirectory = buildResult.OutputPackageDirectory;
                    return reulst;
                }
                else
                {
                    Debug.LogError(buildResult.ErrorInfo);
                    throw new System.Exception($"{nameof(EditorSimulateBuildPipeline)} build failed !");
                }
            }
            else if (buildPipelineName == EBuildPipeline.ScriptableBuildPipeline.ToString())
            {
                string projectPath = EditorTools.GetProjectPath();
                string outputRoot = $"{projectPath}/Bundles/Tester_SBP";

                // 内置着色器资源包名称
                var builtinShaderBundleName = UtilityEditor.YooAsset.GetBuiltinShaderBundleName(packageName);
                var buildParameters = new ScriptableBuildParameters();

                buildParameters.BuildOutputRoot = outputRoot;
                buildParameters.BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
                buildParameters.BuildPipeline = EBuildPipeline.ScriptableBuildPipeline.ToString();
                buildParameters.BuildBundleType = (int)EBuildBundleType.AssetBundle;
                buildParameters.BuildTarget = EditorUserBuildSettings.activeBuildTarget;
                buildParameters.PackageName = packageName;
                buildParameters.PackageVersion = "TestVersion";
                buildParameters.EnableSharePackRule = true;
                buildParameters.VerifyBuildingResult = true;
                buildParameters.FileNameStyle = EFileNameStyle.HashName;
                buildParameters.BuildinFileCopyOption = EBuildinFileCopyOption.None;
                buildParameters.BuildinFileCopyParams = string.Empty;
                buildParameters.CompressOption = ECompressOption.LZ4;
                buildParameters.ClearBuildCacheFiles = true;
                buildParameters.UseAssetDependencyDB = true;
                buildParameters.BuiltinShadersBundleName = builtinShaderBundleName;
                buildParameters.EncryptionServices = BuildPipeline switch
                {
                    EBuildPipeline.BuiltinBuildPipeline => new Utiltiy.YooAsset.EncryptPF(),
                    EBuildPipeline.RawFileBuildPipeline => new Utiltiy.YooAsset.EncryptRF(),
                    _ => buildParameters.EncryptionServices
                };

                var pipeline = new ScriptableBuildPipeline();
                BuildResult buildResult = pipeline.Run(buildParameters, false);
                if (buildResult.Success)
                {
                    var reulst = new PackageInvokeBuildResult();
                    reulst.PackageRootDirectory = buildResult.OutputPackageDirectory;
                    return reulst;
                }
                else
                {
                    Debug.LogError(buildResult.ErrorInfo);
                    throw new System.Exception($"{nameof(ScriptableBuildPipeline)} build failed !");
                }
            }
            else if (buildPipelineName == EBuildPipeline.BuiltinBuildPipeline.ToString())
            {
                string projectPath = EditorTools.GetProjectPath();
                string outputRoot = $"{projectPath}/Bundles/Tester_BBP";

                var buildParameters = new BuiltinBuildParameters();
                buildParameters.BuildOutputRoot = outputRoot;
                buildParameters.BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
                buildParameters.BuildPipeline = EBuildPipeline.ScriptableBuildPipeline.ToString();
                buildParameters.BuildBundleType = (int)EBuildBundleType.AssetBundle;
                buildParameters.BuildTarget = EditorUserBuildSettings.activeBuildTarget;
                buildParameters.PackageName = packageName;
                buildParameters.PackageVersion = "TestVersion";
                buildParameters.EnableSharePackRule = true;
                buildParameters.VerifyBuildingResult = true;
                buildParameters.FileNameStyle = EFileNameStyle.HashName;
                buildParameters.BuildinFileCopyOption = EBuildinFileCopyOption.None;
                buildParameters.BuildinFileCopyParams = string.Empty;
                buildParameters.CompressOption = ECompressOption.LZ4;
                buildParameters.ClearBuildCacheFiles = true;
                buildParameters.UseAssetDependencyDB = true;
                buildParameters.EncryptionServices = BuildPipeline switch
                {
                    EBuildPipeline.BuiltinBuildPipeline => new Utiltiy.YooAsset.EncryptPF(),
                    EBuildPipeline.RawFileBuildPipeline => new Utiltiy.YooAsset.EncryptRF(),
                    _ => buildParameters.EncryptionServices
                };

                var pipeline = new BuiltinBuildPipeline();
                BuildResult buildResult = pipeline.Run(buildParameters, false);
                if (buildResult.Success)
                {
                    var reulst = new PackageInvokeBuildResult();
                    reulst.PackageRootDirectory = buildResult.OutputPackageDirectory;
                    return reulst;
                }
                else
                {
                    Debug.LogError(buildResult.ErrorInfo);
                    throw new System.Exception($"{nameof(BuiltinBuildPipeline)} build failed !");
                }
            }
            else if (buildPipelineName == EBuildPipeline.RawFileBuildPipeline.ToString())
            {
                string projectPath = EditorTools.GetProjectPath();
                string outputRoot = $"{projectPath}/Bundles/Tester_RFBP";

                var buildParameters = new RawFileBuildParameters();
                buildParameters.BuildOutputRoot = outputRoot;
                buildParameters.BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
                buildParameters.BuildPipeline = EBuildPipeline.RawFileBuildPipeline.ToString();
                buildParameters.BuildBundleType = (int)EBuildBundleType.RawBundle;
                buildParameters.BuildTarget = EditorUserBuildSettings.activeBuildTarget;
                buildParameters.PackageName = packageName;
                buildParameters.PackageVersion = "TestVersion";
                buildParameters.VerifyBuildingResult = true;
                buildParameters.FileNameStyle = EFileNameStyle.HashName;
                buildParameters.BuildinFileCopyOption = EBuildinFileCopyOption.None;
                buildParameters.BuildinFileCopyParams = string.Empty;
                buildParameters.ClearBuildCacheFiles = true;
                buildParameters.UseAssetDependencyDB = true;

                var pipeline = new RawFileBuildPipeline();
                BuildResult buildResult = pipeline.Run(buildParameters, false);
                if (buildResult.Success)
                {
                    var reulst = new PackageInvokeBuildResult();
                    reulst.PackageRootDirectory = buildResult.OutputPackageDirectory;
                    return reulst;
                }
                else
                {
                    Debug.LogError(buildResult.ErrorInfo);
                    throw new System.Exception($"{nameof(RawFileBuildPipeline)} build failed !");
                }
            }
            else
            {
                throw new System.NotImplementedException(buildPipelineName);
            }
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();
            if (SettingData == null)
            {
                SettingData = AssetDatabase.LoadAssetAtPath<YooAssetPackages>("Assets/Resources/YooAssetModuleSetting.asset");
            }
            buildoutputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot();
            streamingAssetsRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();

        }
    }
}