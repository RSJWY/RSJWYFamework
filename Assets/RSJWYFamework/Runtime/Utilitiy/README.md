# Utility 模块说明文档

`RSJWYFamework.Runtime.Utilitiy` 命名空间提供了一系列常用的工具类，涵盖了加密解密、文件操作、时间管理、进度模拟、数据校验以及 GameObject 操作等功能。这些工具类大部分作为 `Utility` 静态类的嵌套类或扩展方法存在，方便全局调用。

## 目录

1. [基础工具 (Utility)](#1-基础工具-utility)
2. [加密与校验](#2-加密与校验)
    - [AES 加密 (AESTool)](#21-aes-加密-aestool)
    - [CRC32 校验 (CRC32)](#22-crc32-校验-crc32)
    - [Modbus CRC16 (ModbusCRC16)](#23-modbus-crc16-modbuscrc16)
    - [RSA 验证 (RSA)](#24-rsa-验证-rsa)
3. [文件与目录操作 (FileAndFolder)](#3-文件与目录操作-fileandfolder)
4. [时间与计时](#4-时间与计时)
    - [游戏时间 (GameTime)](#41-游戏时间-gametime)
    - [时间戳 (Timestamp)](#42-时间戳-timestamp)
5. [进度模拟 (FakeProgress)](#5-进度模拟-fakeprogress)
6. [GameObject 工具 (GameObjectTool)](#6-gameobject-工具-gameobjecttool)

---

## 1. 基础工具 (Utility)

位于 `Utility.cs`，提供通用的数据转换和处理方法。

- **ConvertHexStringToByteArray(string hexString)**: 将 16 进制字符串转换为字节数组。
- **LoadJson<T>(string JsonTxT)**: 将 JSON 字符串反序列化为对象 (基于 Newtonsoft.Json)。
- **ConvertJaggedArrayToOneDimensional(byte[][] jaggedArray)**: 将二维字节数组展平为一维数组。
- **UIntToByteArray(uint value)** / **ByteArrayToUInt(byte[] byteArray)**: `uint` 与 `byte[4]` 之间的相互转换。
- **Shuffle<T>(List<T> list)**: 对列表进行随机洗牌。
- **GetProgress(long total, long current)**: 计算进度百分比 (0.0 - 1.0)。

## 2. 加密与校验

### 2.1 AES 加密 (AESTool)
位于 `Utility.AES.cs`，提供 AES 对称加密功能，支持字符串、字节数组及文件的加密解密。

- **AESEncrypt / AESDecrypt**: 字符串或字节数组的加密与解密。
- **AESFileEncrypt(string path, string EncryptKey)**: 对文件进行加密，会在文件头部添加 `AESEncrypt` 标识。
- **AESFileDecrypt(string path, string EncryptKey)**: 解密文件（会修改原文件）。
- **AESFileByteDecrypt(string path, string EncryptKey)**: 读取并解密文件内容，返回字节数组，不修改原文件。

> **注意**: 类内部硬编码了默认的 IV (`btIV`) 和 Salt (`salt`)，以及文件头标识 (`AESHead`)。

### 2.2 CRC32 校验 (CRC32)
位于 `Utility.CRC32.cs`，提供 CRC32 校验值的计算。

- **GetCrc32(byte[] bytes, ...)**: 计算字节数组的 CRC32 值。
- **GetCrc32(Stream stream)**: 计算流的 CRC32 值。
- **GetCrc32Bytes(int crc32, byte[] bytes, int offset)**: 将 CRC32 数值写入字节数组。

### 2.3 Modbus CRC16 (ModbusCRC16)
位于 `Utility.ModBusCRC16.cs`，专用于 Modbus 协议的 CRC16 校验。

- **AddCRC16ToHexString(string hexString)**: 计算 16 进制字符串的 CRC16，并返回添加了校验码的新字符串及字节数组。
- **CalculateCRC16(byte[] data)**: 计算字节数组的 Modbus CRC16 校验值。

### 2.4 RSA 验证 (RSA)
位于 `Utility.RSA.cs`，提供 RSA 签名验证功能。

- **verifyDevice(byte[] signdata, byte[] signature, byte[] certs_byte)**: 验证设备证书签名。

## 3. 文件与目录操作 (FileAndFolder)

位于 `Utility.FileAndFoder.cs`，封装了 `System.IO` 操作，增加了重试机制和安全性检查。

- **EnsureDirectoryExists / EnsureFileExists**: 确保目录或文件存在，不存在则创建。
- **DeleteDirectory(string path, bool recursive)**: 安全删除目录。
- **ClearDirectory(string path)**: 清空目录内容但不删除目录本身。
- **NormalizePath(string path)**: 规范化路径分隔符为正斜杠 `/`。
- **GetFileSize(string path)**: 获取文件大小。
- **ReadAllText(string path)**: 读取文件所有文本（UTF-8）。
- **CopyFile(string source, string dest, bool overwrite)**: 复制文件。

## 4. 时间与计时

### 4.1 游戏时间 (GameTime)
位于 `Utility.GameTime.cs`，提供当前帧的时间快照，避免在同一帧内多次调用 Unity Time API 导致的不一致（虽然 Unity Time API 在同一帧内通常是不变的，但此工具可用于特定逻辑的时间锁定）。

- **StartFrame()**: 在帧开始时调用，缓存当前帧的 `Time.time`, `deltaTime` 等。
- **GetStartFrame()**: 获取当前帧的时间快照对象 `ReGameTime`。

### 4.2 时间戳 (Timestamp)
位于 `Utility.Timestamp.cs`，提供高性能计时和多种时间格式。

- **UnixTimestampSeconds / UnixTimestampMilliseconds**: 获取当前 UTC 时间戳。
- **SystemTicks**: 获取 `DateTime.Now.Ticks`。
- **HighPrecisionTicks / HighPrecisionMilliseconds**: 基于 `Stopwatch` 的高精度计时。
- **FormattedLocalTime**: 获取格式化的本地时间字符串。
- **GenerateTimeBasedID**: 生成基于时间戳的唯一 ID。

## 5. 进度模拟 (FakeProgress)

位于 `FakeProgress.cs` 和 `FakeProgressMono.cs`，用于在真实进度不可知或过快时，提供平滑的 UI 进度条体验。

- **核心逻辑**:
    1.  **VisualValue**: 当前显示的进度。
    2.  **TargetValue**: 真实的进度目标。
    3.  **FakeTarget**: 虚假进度的上限（默认 0.9）。在真实任务未完成前，进度条会缓慢增长但不会超过此值。
    4.  **CatchUpSpeed**: 当真实进度更新或完成时，VisualValue 追赶 TargetValue 的速度。
- **FakeProgressMono**: MonoBehaviour 封装版本，提供 UnityEvent 回调，方便在 Inspector 中使用。

## 6. GameObject 工具 (GameObjectTool)

位于 `Utility.GameobjectTool.cs`，封装了 `Instantiate` 和 `Destroy`。

- **Clone<T>(...)**: 泛型克隆方法，支持设置位置、旋转、父节点等。
- **CloneGameObject(GameObject original, bool isUI)**: 克隆 GameObject，针对 UI 对象处理了 RectTransform 的锚点和偏移，确保 UI 克隆后布局正确。
- **Destory(GameObject gameObject)**: 销毁对象。
