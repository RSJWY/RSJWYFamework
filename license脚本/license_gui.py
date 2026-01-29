import tkinter as tk
from tkinter import ttk, messagebox, scrolledtext, filedialog
import sys
import os
import json
import base64
from datetime import datetime

# 尝试导入 gen_license 模块
# 假设 gen_license.py 在同一目录下
try:
    import gen_license
except ImportError:
    # 如果直接运行且导入失败，尝试添加当前目录到 sys.path
    sys.path.append(os.path.dirname(os.path.abspath(__file__)))
    try:
        import gen_license
    except ImportError:
        # 创建一个简单的占位符，避免 GUI 启动失败
        class GenLicenseMock:
            PRIVATE_KEY_FILE = "private_key.pem"
            PUBLIC_KEY_FILE = "public_key.pem"
            def generate_keys(self, force=False): raise ImportError("gen_license.py not found")
            def generate_license(self, *args): raise ImportError("gen_license.py not found")
            def pem_to_xml(self, content): return "Error: gen_license.py not found"
        gen_license = GenLicenseMock()

class LicenseApp:
    def __init__(self, root):
        self.root = root
        self.root.title("License 生成器与查看器")
        self.root.geometry("700x800")
        
        # 设置样式
        self.style = ttk.Style()
        self.style.theme_use('clam')
        
        # 创建选项卡
        self.notebook = ttk.Notebook(root)
        self.notebook.pack(fill='both', expand=True, padx=10, pady=10)
        
        # 选项卡 1: 生成 License
        self.tab_gen_license = ttk.Frame(self.notebook)
        self.notebook.add(self.tab_gen_license, text='生成 License')
        self.setup_gen_license_tab()
        
        # 选项卡 2: 密钥管理
        self.tab_keys = ttk.Frame(self.notebook)
        self.notebook.add(self.tab_keys, text='密钥管理')
        self.setup_keys_tab()
        
        # 选项卡 3: 查看 License
        self.tab_view_license = ttk.Frame(self.notebook)
        self.notebook.add(self.tab_view_license, text='查看/验证 License')
        self.setup_view_license_tab()
        
        # 底部日志区域
        self.log_frame = ttk.LabelFrame(root, text="操作日志")
        self.log_frame.pack(fill='both', expand=True, padx=10, pady=5)
        
        self.log_text = scrolledtext.ScrolledText(self.log_frame, height=8, state='disabled')
        self.log_text.pack(fill='both', expand=True, padx=5, pady=5)
        
        self.log("应用程序已启动。")
        if isinstance(gen_license, type):
             self.log("警告: 未找到 gen_license.py，部分功能不可用。")
        
        self.check_keys_status()

    def log(self, message):
        self.log_text.config(state='normal')
        self.log_text.insert(tk.END, f"[{datetime.now().strftime('%H:%M:%S')}] {message}\n")
        self.log_text.see(tk.END)
        self.log_text.config(state='disabled')

    def setup_gen_license_tab(self):
        frame = self.tab_gen_license
        
        # 机器码
        ttk.Label(frame, text="机器码 (Machine Code):").grid(row=0, column=0, sticky='w', padx=10, pady=5)
        self.machine_code_var = tk.StringVar()
        ttk.Entry(frame, textvariable=self.machine_code_var, width=50).grid(row=0, column=1, sticky='we', padx=10, pady=5)
        
        # 有效天数
        ttk.Label(frame, text="有效天数 (Days):").grid(row=1, column=0, sticky='w', padx=10, pady=5)
        self.days_var = tk.IntVar(value=30)
        ttk.Spinbox(frame, from_=1, to=3650, textvariable=self.days_var, width=10).grid(row=1, column=1, sticky='w', padx=10, pady=5)
        
        # 开始日期
        ttk.Label(frame, text="开始日期 (YYYY-MM-DD, 可选):").grid(row=2, column=0, sticky='w', padx=10, pady=5)
        self.start_date_var = tk.StringVar()
        ttk.Entry(frame, textvariable=self.start_date_var, width=20).grid(row=2, column=1, sticky='w', padx=10, pady=5)
        
        # 到期日期
        ttk.Label(frame, text="到期日期 (YYYY-MM-DD, 可选):").grid(row=3, column=0, sticky='w', padx=10, pady=5)
        self.expire_date_var = tk.StringVar()
        ttk.Entry(frame, textvariable=self.expire_date_var, width=20).grid(row=3, column=1, sticky='w', padx=10, pady=5)
        
        # 输出文件名
        ttk.Label(frame, text="输出文件名:").grid(row=4, column=0, sticky='w', padx=10, pady=5)
        self.filename_var = tk.StringVar(value="License.lic")
        ttk.Entry(frame, textvariable=self.filename_var, width=30).grid(row=4, column=1, sticky='w', padx=10, pady=5)
        
        # 按钮区
        btn_frame = ttk.Frame(frame)
        btn_frame.grid(row=5, column=0, columnspan=2, pady=20)
        
        ttk.Button(btn_frame, text="生成 License 文件", command=self.generate_license).pack(side='left', padx=10)
        ttk.Button(btn_frame, text="打开所在文件夹", command=self.open_folder).pack(side='left', padx=10)

        # 结果显示区
        ttk.Label(frame, text="生成的 License 内容 (可直接复制):").grid(row=6, column=0, sticky='w', padx=10, pady=5)
        self.result_text = scrolledtext.ScrolledText(frame, height=5)
        self.result_text.grid(row=7, column=0, columnspan=2, padx=10, pady=5, sticky='we')
        
        # 复制按钮
        ttk.Button(frame, text="复制 License 内容到剪贴板", command=self.copy_license_content).grid(row=8, column=0, columnspan=2, pady=10)

    def setup_keys_tab(self):
        frame = self.tab_keys
        
        # 密钥状态
        self.keys_status_label = ttk.Label(frame, text="正在检查密钥状态...", foreground="gray")
        self.keys_status_label.pack(pady=10)
        
        # 生成密钥按钮
        btn_frame = ttk.Frame(frame)
        btn_frame.pack(pady=5)
        
        ttk.Button(btn_frame, text="生成/重置密钥对", command=self.generate_keys).pack(side='left', padx=5)
        ttk.Button(btn_frame, text="刷新公钥显示", command=self.show_public_key_xml).pack(side='left', padx=5)
        
        # 公钥显示区域
        ttk.Label(frame, text="Unity 公钥 (XML 格式) - 复制此内容到 Unity Inspector:").pack(anchor='w', padx=10, pady=5)
        self.xml_text = scrolledtext.ScrolledText(frame, height=12)
        self.xml_text.pack(fill='both', expand=True, padx=10, pady=5)
        
        # PEM 格式显示
        ttk.Label(frame, text="标准公钥 (PEM 格式):").pack(anchor='w', padx=10, pady=5)
        self.pem_text = scrolledtext.ScrolledText(frame, height=8)
        self.pem_text.pack(fill='both', expand=True, padx=10, pady=5)

    def setup_view_license_tab(self):
        frame = self.tab_view_license
        
        # 顶部按钮区
        btn_frame = ttk.Frame(frame)
        btn_frame.pack(fill='x', padx=10, pady=10)
        
        ttk.Button(btn_frame, text="加载 License 文件...", command=self.load_license_file).pack(side='left')
        ttk.Button(btn_frame, text="验证输入内容", command=self.verify_input_content).pack(side='left', padx=10)
        ttk.Button(btn_frame, text="清空所有", command=self.clear_verify_tab).pack(side='left', padx=10)
        
        self.loaded_file_label = ttk.Label(btn_frame, text="未选择文件")
        self.loaded_file_label.pack(side='left', padx=10)
        
        # 输入区
        ttk.Label(frame, text="License 内容 (粘贴到此处):").pack(anchor='w', padx=10, pady=(5,0))
        self.verify_input_text = scrolledtext.ScrolledText(frame, height=6)
        self.verify_input_text.pack(fill='x', padx=10, pady=5)
        
        # 结果显示区
        ttk.Label(frame, text="验证/解析结果:").pack(anchor='w', padx=10, pady=(10,0))
        self.license_info_text = scrolledtext.ScrolledText(frame, height=15)
        self.license_info_text.pack(fill='both', expand=True, padx=10, pady=5)

    def clear_verify_tab(self):
        self.verify_input_text.delete(1.0, tk.END)
        self.license_info_text.delete(1.0, tk.END)
        self.loaded_file_label.config(text="未选择文件")

    def verify_input_content(self):
        content = self.verify_input_text.get(1.0, tk.END).strip()
        if not content:
            messagebox.showwarning("提示", "请输入或粘贴 License 内容。")
            return
        self.parse_license_content(content, source="用户输入")

    def check_keys_status(self):
        priv_exists = os.path.exists(gen_license.PRIVATE_KEY_FILE)
        pub_exists = os.path.exists(gen_license.PUBLIC_KEY_FILE)
        
        if priv_exists and pub_exists:
            self.keys_status_label.config(text="✅ 密钥对已存在 (private_key.pem, public_key.pem)", foreground="green")
            self.show_public_key_xml() # 自动加载
            return True
        else:
            self.keys_status_label.config(text="❌ 密钥对不完整或缺失，请先生成密钥。", foreground="red")
            return False

    def generate_keys(self):
        if os.path.exists(gen_license.PRIVATE_KEY_FILE):
            if not messagebox.askyesno("确认", "密钥文件已存在，重新生成将导致旧 License 失效！\n是否继续？"):
                return
        
        try:
            gen_license.generate_keys(force=True)
            self.log("密钥对生成成功。")
            self.check_keys_status()
            messagebox.showinfo("成功", "RSA 密钥对已生成。")
        except Exception as e:
            self.log(f"生成密钥失败: {e}")
            messagebox.showerror("错误", f"生成密钥失败: {e}")

    def show_public_key_xml(self):
        if not os.path.exists(gen_license.PUBLIC_KEY_FILE):
            return
            
        try:
            with open(gen_license.PUBLIC_KEY_FILE, "r") as f:
                content = f.read()
            
            # 显示 XML
            xml_key = gen_license.pem_to_xml(content)
            self.xml_text.delete(1.0, tk.END)
            self.xml_text.insert(tk.END, xml_key)
            
            # 显示 PEM
            self.pem_text.delete(1.0, tk.END)
            self.pem_text.insert(tk.END, content)
            
            self.log("已刷新公钥显示。")
        except Exception as e:
            self.log(f"读取公钥失败: {e}")

    def generate_license(self):
        if not self.check_keys_status():
            messagebox.showerror("错误", "请先在'密钥管理'页签生成密钥对。")
            return

        machine_code = self.machine_code_var.get().strip()
        if not machine_code:
            messagebox.showerror("错误", "机器码不能为空。")
            return
            
        days = self.days_var.get()
        start_date = self.start_date_var.get().strip() or None
        expire_date = self.expire_date_var.get().strip() or None
        filename = self.filename_var.get().strip() or "License.lic"
        
        try:
            # 重定向 stdout 以捕获 gen_license 的输出
            import io
            from contextlib import redirect_stdout
            
            f = io.StringIO()
            with redirect_stdout(f):
                gen_license.generate_license(machine_code, days, start_date, expire_date, filename)
            
            output = f.getvalue()
            self.log(output)
            
            if os.path.exists(filename):
                self.log(f"文件保存于: {os.path.abspath(filename)}")
                
                # 读取并显示内容
                try:
                    with open(filename, "r") as f:
                        content = f.read()
                    self.result_text.delete(1.0, tk.END)
                    self.result_text.insert(tk.END, content)
                except Exception as e:
                    self.log(f"读取生成文件失败: {e}")

                messagebox.showinfo("成功", f"License 已生成: {filename}")
            else:
                messagebox.showwarning("警告", "License 生成可能未完成，请检查日志。")
                
        except Exception as e:
            self.log(f"生成 License 失败: {e}")
            messagebox.showerror("错误", f"生成 License 失败: {e}")

    def open_folder(self):
        path = os.getcwd()
        os.startfile(path)

    def copy_license_content(self):
        content = self.result_text.get(1.0, tk.END).strip()
        if not content:
            messagebox.showwarning("警告", "没有内容可复制，请先生成 License。")
            return
            
        self.root.clipboard_clear()
        self.root.clipboard_append(content)
        self.root.update()
        messagebox.showinfo("成功", "License 内容已复制到剪贴板。")

    def load_license_file(self):
        filepath = filedialog.askopenfilename(filetypes=[("License Files", "*.lic"), ("All Files", "*.*")])
        if not filepath:
            return
            
        self.loaded_file_label.config(text=os.path.basename(filepath))
        try:
            with open(filepath, "r") as f:
                content = f.read()
            self.verify_input_text.delete(1.0, tk.END)
            self.verify_input_text.insert(tk.END, content)
            self.parse_license_content(content, source=filepath)
        except Exception as e:
            self.log(f"读取文件失败: {e}")
            messagebox.showerror("错误", f"读取文件失败: {e}")

    def parse_license_content(self, content_blob, source="Unknown"):
        try:
            # 1. Base64 Decode Outer Blob
            try:
                # 兼容可能包含的空白字符
                content_blob = content_blob.strip()
                json_str = base64.b64decode(content_blob).decode('utf-8')
                license_content = json.loads(json_str)
            except Exception as e:
                self.license_info_text.delete(1.0, tk.END)
                self.license_info_text.insert(tk.END, f"格式错误 (非有效 Base64/JSON): {e}")
                return

            self.license_info_text.delete(1.0, tk.END)
            self.license_info_text.insert(tk.END, f"=== 来源: {source} ===\n")
            self.license_info_text.insert(tk.END, "=== License 文件结构 ===\n")
            self.license_info_text.insert(tk.END, f"Signature (Base64): {license_content.get('signature')[:50]}...\n\n")
            
            # 2. Base64 Decode Data Payload
            data_b64 = license_content.get('data')
            if data_b64:
                payload_json = base64.b64decode(data_b64).decode('utf-8')
                payload = json.loads(payload_json)
                
                self.license_info_text.insert(tk.END, "=== License 有效负载 ===\n")
                self.license_info_text.insert(tk.END, f"机器码 (Machine Code): {payload.get('machine_code')}\n")
                
                start_ts = payload.get('start_timestamp')
                expire_ts = payload.get('expire_timestamp')
                
                if start_ts:
                    start_dt = datetime.fromtimestamp(start_ts)
                    self.license_info_text.insert(tk.END, f"开始时间: {start_dt} (Local)\n")
                
                if expire_ts:
                    expire_dt = datetime.fromtimestamp(expire_ts)
                    self.license_info_text.insert(tk.END, f"到期时间: {expire_dt} (Local)\n")
                    
                    if start_ts:
                        days = (expire_dt - start_dt).days
                        self.license_info_text.insert(tk.END, f"有效时长: 约 {days} 天\n")
                        
                        is_expired = datetime.now() > expire_dt
                        status = "已过期" if is_expired else "有效"
                        self.license_info_text.insert(tk.END, f"当前状态: {status}\n")
            
            self.log(f"已解析 License 内容 (来源: {source})")

        except Exception as e:
            self.log(f"解析 License 失败: {e}")
            self.license_info_text.delete(1.0, tk.END)
            self.license_info_text.insert(tk.END, f"解析失败: {e}")

if __name__ == "__main__":
    root = tk.Tk()
    app = LicenseApp(root)
    root.mainloop()
