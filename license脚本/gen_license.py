import sys
import json
import base64
import argparse
import os
from datetime import datetime, timedelta, timezone

try:
    from Crypto.PublicKey import RSA
    from Crypto.Signature import pkcs1_15
    from Crypto.Hash import SHA256
except ImportError:
    print("错误: 需要安装 'pycryptodome' 库。")
    print("请使用以下命令安装: pip install pycryptodome")
    sys.exit(1)

PRIVATE_KEY_FILE = "private_key.pem"
PUBLIC_KEY_FILE = "public_key.pem"

def generate_keys(force=False):
    """
    生成 RSA 公私钥对。
    """
    if not force and os.path.exists(PRIVATE_KEY_FILE) and os.path.exists(PUBLIC_KEY_FILE):
        print(f"密钥已存在于 {os.getcwd()}。跳过生成步骤。")
        return

    print("正在生成 RSA 密钥对 (2048 bits)...")
    key = RSA.generate(2048)
    private_key = key.export_key()
    public_key = key.publickey().export_key()

    with open(PRIVATE_KEY_FILE, "wb") as f:
        f.write(private_key)
    
    with open(PUBLIC_KEY_FILE, "wb") as f:
        f.write(public_key)
        
    print(f"密钥已生成: {PRIVATE_KEY_FILE}, {PUBLIC_KEY_FILE}")

def load_private_key():
    """
    加载私钥，如果不存在则自动生成。
    """
    if not os.path.exists(PRIVATE_KEY_FILE):
        print("未找到私钥。正在生成新密钥...")
        generate_keys()
        
    with open(PRIVATE_KEY_FILE, "rb") as f:
        return RSA.import_key(f.read())

def generate_license(machine_code, days_valid=30, start_date_str=None, expire_date_str=None, output_file="License.lic"):
    """
    生成 License 文件。
    """
    if start_date_str:
        try:
            # 解析为 naive 时间，然后替换为 UTC
            start_date_naive = datetime.strptime(start_date_str, "%Y-%m-%d")
            start_date = start_date_naive.replace(tzinfo=timezone.utc)
        except ValueError:
            print("错误: 开始日期格式必须为 YYYY-MM-DD")
            return
    else:
        start_date = datetime.now(timezone.utc)

    if expire_date_str:
        try:
            expire_date_naive = datetime.strptime(expire_date_str, "%Y-%m-%d")
            expire_date = expire_date_naive.replace(tzinfo=timezone.utc)
            # 重新计算有效天数用于显示（近似值）
            delta = expire_date - start_date
            days_valid = delta.days
        except ValueError:
            print("错误: 到期日期格式必须为 YYYY-MM-DD")
            return
    else:
        expire_date = start_date + timedelta(days=days_valid)
    
    start_timestamp = int(start_date.timestamp())
    expire_timestamp = int(expire_date.timestamp())
    
    # 负载数据
    data = {
        "machine_code": machine_code,
        "start_timestamp": start_timestamp,
        "expire_timestamp": expire_timestamp
    }
    
    # 规范化 JSON 字符串
    json_str = json.dumps(data, separators=(',', ':'))
    # print(f"License Payload: {json_str}")
    
    # 签名
    private_key = load_private_key()
    h = SHA256.new(json_str.encode('utf-8'))
    signature = pkcs1_15.new(private_key).sign(h)
    
    # 创建最终 License 结构
    license_content = {
        "data": base64.b64encode(json_str.encode('utf-8')).decode('utf-8'),
        "signature": base64.b64encode(signature).decode('utf-8')
    }
    
    final_json = json.dumps(license_content, separators=(',', ':'))
    
    # Base64 编码整个文件使其看起来像一个 blob
    final_blob = base64.b64encode(final_json.encode('utf-8')).decode('utf-8')
    
    with open(output_file, "w") as f:
        f.write(final_blob)
        
    print(f"\n[成功] License 文件已生成: {os.path.abspath(output_file)}")
    print(f"开始日期: {start_date.strftime('%Y-%m-%d %H:%M:%S')} (UTC)")
    print(f"有效期至: {expire_date.strftime('%Y-%m-%d %H:%M:%S')} (UTC)")
    print(f"有效天数: {days_valid}")

def pem_to_xml(pem_content):
    """
    将 PEM 格式的 RSA 公钥转换为 C# XML 格式。
    需要手动解析 ASN.1 结构提取 Modulus 和 Exponent。
    """
    try:
        key = RSA.import_key(pem_content)
        modulus = base64.b64encode(key.n.to_bytes((key.n.bit_length() + 7) // 8, byteorder='big')).decode('utf-8')
        exponent = base64.b64encode(key.e.to_bytes((key.e.bit_length() + 7) // 8, byteorder='big')).decode('utf-8')
        return f"<RSAKeyValue><Modulus>{modulus}</Modulus><Exponent>{exponent}</Exponent></RSAKeyValue>"
    except Exception as e:
        return f"Error converting to XML: {e}"

def show_public_key():
    """
    显示公钥内容，格式化为 XML 字符串 (Unity 兼容)。
    """
    if not os.path.exists(PUBLIC_KEY_FILE):
        print("未找到公钥。请先生成密钥。")
        return

    with open(PUBLIC_KEY_FILE, "r") as f:
        content = f.read()
        
    print("\n=== Unity 公钥 (XML 格式) ===")
    print("由于 Unity 对 PEM 格式支持不佳，请使用以下 XML 格式：\n")
    
    xml_key = pem_to_xml(content)
    print(xml_key)
    
    print("\n===================================================")
    print("请复制上方 XML 内容 (从 <RSAKeyValue> 到 </RSAKeyValue>)")
    print("并粘贴到 Unity Inspector 的 'Public Key PEM' 字段中。")
    print("(注：虽然字段名还叫 PEM，但代码会自动识别 XML)")
    print("===================================================\n")

def interactive_mode():
    """
    交互模式。
    """
    while True:
        print("\n" + "="*40)
        print(" UE License 生成器 - 交互模式")
        print("="*40)
        print("1. 生成密钥 (创建新的密钥对)")
        print("2. 生成 License (创建 .lic 文件)")
        print("3. 显示公钥 (用于 C++ 集成)")
        print("4. 退出")
        
        choice = input("\n请输入选项 (1-4): ").strip()
        
        if choice == '1':
            confirm = input("这将覆盖现有的密钥（如果存在）。是否继续？(y/n): ").lower()
            if confirm == 'y':
                generate_keys(force=True)
            else:
                generate_keys(force=False)
                
        elif choice == '2':
            machine_code = input("请输入机器码 (Machine Code): ").strip()
            if not machine_code:
                print("错误: 机器码不能为空。")
                continue
                
            start_date_input = input("请输入开始日期 (YYYY-MM-DD, 默认: 现在): ").strip()
            start_date_str = start_date_input if start_date_input else None

            expire_date_input = input("请输入到期日期 (YYYY-MM-DD, 可选): ").strip()
            expire_date_str = expire_date_input if expire_date_input else None
            
            days = 30
            if not expire_date_str:
                days_input = input("请输入有效天数 (默认: 30): ").strip()
                days = int(days_input) if days_input.isdigit() else 30
            
            filename = input("输出文件名 (默认: License.lic): ").strip()
            if not filename:
                filename = "License.lic"
                
            try:
                generate_license(machine_code, days, start_date_str, expire_date_str, filename)
            except Exception as e:
                print(f"生成 License 时出错: {e}")
                
        elif choice == '3':
            show_public_key()
            
        elif choice == '4':
            print("再见!")
            sys.exit(0)
            
        else:
            print("无效选项，请重试。")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="生成 UE License 文件 (RSA 签名)")
    parser.add_argument("--code", help="机器码", required=False)
    parser.add_argument("--days", help="有效天数", type=int, default=30)
    parser.add_argument("--start-date", help="开始日期 (YYYY-MM-DD)", default=None)
    parser.add_argument("--expire-date", help="到期日期 (YYYY-MM-DD)", default=None)
    parser.add_argument("--out", help="输出文件名", default="License.lic")
    parser.add_argument("--gen-keys", help="仅生成密钥", action="store_true")
    
    # 如果没有提供参数，进入交互模式
    if len(sys.argv) == 1:
        interactive_mode()
    else:
        args = parser.parse_args()
        
        if args.gen_keys:
            generate_keys()
            sys.exit(0)
        
        machine_code = args.code
        if not machine_code:
            print("错误: 非交互模式下必须提供机器码。")
            sys.exit(1)
            
        generate_license(machine_code, args.days, args.start_date, args.expire_date, args.out)
