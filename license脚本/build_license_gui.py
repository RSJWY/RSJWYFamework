import PyInstaller.__main__
import os
import shutil

def build():
    # 定义路径
    base_dir = os.path.dirname(os.path.abspath(__file__))
    script_path = os.path.join(base_dir, 'license_gui.py')
    dist_dir = os.path.join(base_dir, 'dist')
    build_dir = os.path.join(base_dir, 'build')
    
    print(f"开始构建 {script_path}...")
    
    # 清理旧构建 (可选)
    if os.path.exists(dist_dir):
        try:
            shutil.rmtree(dist_dir)
            print("已清理旧 dist 目录")
        except Exception as e:
            print(f"清理 dist 目录失败: {e}")

    # PyInstaller 参数
    args = [
        script_path,
        '--onefile',            # 创建单个可执行文件
        '--noconsole',          # 不显示控制台窗口 (GUI应用)
        '--name=LicenseGenerator',  # 可执行文件的名称
        '--clean',              # 清理 PyInstaller 缓存
        f'--distpath={dist_dir}',
        f'--workpath={build_dir}',
        '--hidden-import=Crypto', # 显式添加 pycryptodome 支持
        # 注意: gen_license.py 会被自动分析并包含，因为它是被导入的
    ]
    
    # 运行 PyInstaller
    try:
        PyInstaller.__main__.run(args)
        print(f"\n[成功] 构建完成！")
        print(f"可执行文件位于: {os.path.join(dist_dir, 'LicenseGenerator.exe')}")
    except Exception as e:
        print(f"\n[失败] 构建过程中出错: {e}")

if __name__ == "__main__":
    build()
