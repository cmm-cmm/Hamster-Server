using System.Text.Json.Serialization;

namespace HamsterServer.Models;

public enum ServerMode { Development, Production }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FrameworkPreset
{
    None = 0,
    Laravel = 1,
    Symfony = 2,
    CodeIgniter = 3,
    Yii = 4,
    CakePHP = 5,
    Phalcon = 6,
    Hyperf = 7,
    Slim = 8,
    WordPress = 9,
    Drupal = 10,
    Magento = 11
}

/// <summary>
/// The <c>error_reporting</c> level written to php.ini. <see cref="Auto"/> follows
/// <see cref="AppConfig.ServerMode"/> (Development → E_ALL, Production → E_ALL without deprecations).
/// Append new values; never reorder, the JSON converter accepts numeric values too.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PhpErrorReporting
{
    Auto = 0,
    All = 1,
    NoDeprecated = 2,
    NoNotices = 3
}

/// <summary>
/// What Hamster Server does when php.net publishes a newer patch of the PHP branch in use.
/// Append new values; never reorder, the JSON converter accepts numeric values too.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PhpUpdateMode
{
    /// <summary>Install the patch side-by-side, carry the extensions over, switch once PHP is idle.</summary>
    Auto = 0,
    /// <summary>Only report it (status bar, tray, PHP Version Manager).</summary>
    Notify = 1,
    /// <summary>Never contact php.net on its own.</summary>
    Off = 2
}

public class AppConfig
{
    [JsonPropertyName("serverMode")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ServerMode ServerMode { get; set; } = ServerMode.Development;

    [JsonPropertyName("apache")]
    public ApacheSettings Apache { get; set; } = new();

    [JsonPropertyName("php")]
    public PhpSettings Php { get; set; } = new();

    [JsonPropertyName("vhosts")]
    public List<VHostEntry> VHosts { get; set; } = [];

    [JsonPropertyName("minimizeToTray")]
    public bool MinimizeToTray { get; set; } = true;

    [JsonPropertyName("autoStart")]
    public bool AutoStart { get; set; } = false;

    [JsonPropertyName("startWithWindows")]
    public bool StartWithWindows { get; set; } = false;
}

public class ApacheSettings
{
    [JsonPropertyName("allowLanAccess")]
    public bool AllowLanAccess { get; set; } = false;

    [JsonPropertyName("port")]
    public int Port { get; set; } = 80;

    [JsonPropertyName("sslPort")]
    public int SslPort { get; set; } = 443;

    [JsonPropertyName("serverName")]
    public string ServerName { get; set; } = "localhost";

    [JsonPropertyName("enableSsl")]
    public bool EnableSsl { get; set; } = false;

    [JsonPropertyName("sslCertFile")]
    public string SslCertFile { get; set; } = "";

    [JsonPropertyName("sslKeyFile")]
    public string SslKeyFile { get; set; } = "";

    [JsonPropertyName("extraPorts")]
    public List<PortEntry> ExtraPorts { get; set; } = [];

    [JsonPropertyName("maxClients")]
    public int MaxClients { get; set; } = 256;

    [JsonPropertyName("keepAliveTimeout")]
    public int KeepAliveTimeout { get; set; } = 15;

    [JsonPropertyName("enableGzip")]
    public bool EnableGzip { get; set; } = true;

    [JsonPropertyName("enableCache")]
    public bool EnableCache { get; set; } = true;

    [JsonPropertyName("enableBrotli")]
    public bool EnableBrotli { get; set; } = false;

    [JsonPropertyName("customDirectives")]
    public string CustomDirectives { get; set; } = "";
}

public class PhpSettings
{
    [JsonPropertyName("currentVersion")]
    public string CurrentVersion { get; set; } = "";

    [JsonPropertyName("fcgiPort")]
    public int FcgiPort { get; set; } = 9000;

    [JsonPropertyName("workers")]
    // Each php-cgi worker costs ~16 MB private / ~35 MB working set. ProcessorCount * 2 put
    // 24 of them (≈410 MB) on a 12-core box, while a load ramp showed 8 workers already serve
    // more concurrency than a local dev machine ever asks for. Existing configs keep their value.
    public int Workers { get; set; } = Math.Clamp(Environment.ProcessorCount, 4, 8);

    /// <summary>
    /// Số request một php-cgi worker phục vụ trước khi tự thoát và được thay bằng worker mới.
    /// <c>0</c> = không bao giờ recycle, và đây là giá trị bắt buộc khi FastCGI connection reuse
    /// đang bật: Apache có thể tái sử dụng đúng socket mà worker vừa đóng khi đạt giới hạn, tạo
    /// HTTP 503 rải rác (xem doc/PERFORMANCE_EVALUATION.md). ApacheConfigWriter.FcgiReuse vì vậy
    /// chỉ phát tham số reuse khi giá trị này là 0.
    /// </summary>
    [JsonPropertyName("maxRequests")]
    public int MaxRequests { get; set; } = 0;

    /// <summary>
    /// Tự tính ngưỡng restart theo số worker thay vì dùng con số cố định. Bật mặc định vì một
    /// ngưỡng cố định không theo kịp khi người dùng đổi Workers: 1024 MB là gấp 6 lần mức nền của
    /// 4 worker nhưng chỉ gấp 2 lần của 12 worker, và ở mức gấp 2 thì pool bị recycle liên tục.
    /// Xem <c>ServerManager.EffectiveMemoryThresholdMb</c>.
    /// </summary>
    [JsonPropertyName("memoryRestartAuto")]
    public bool MemoryRestartAuto { get; set; } = true;

    /// <summary>
    /// Tổng working set (MB) của cả pool php-cgi mà khi vượt qua, Hamster Server sẽ khởi động lại
    /// pool — nhưng chỉ vào lúc pool đang rảnh, không có request nào đang chạy. <c>0</c> = tắt.
    /// Bị bỏ qua khi <see cref="MemoryRestartAuto"/> đang bật.
    /// <para>
    /// Đây là đối trọng của <see cref="MaxRequests"/> = 0: khi worker không bao giờ tự recycle thì
    /// không còn cơ chế nào giải phóng bộ nhớ tích lũy, nên việc dọn dẹp chuyển sang đo bộ nhớ thật
    /// thay vì đếm số request. Xem doc/PERFORMANCE_EVALUATION.md.
    /// </para>
    /// </summary>
    [JsonPropertyName("memoryRestartThreshold")]
    public int MemoryRestartThreshold { get; set; } = 1024;

    [JsonPropertyName("memoryLimit")]
    public int MemoryLimit { get; set; } = 512;

    [JsonPropertyName("maxExecutionTime")]
    public int MaxExecutionTime { get; set; } = 300;

    /// <summary><c>upload_max_filesize</c> in MB.</summary>
    [JsonPropertyName("uploadMaxFilesize")]
    public int UploadMaxFilesize { get; set; } = 128;

    /// <summary><c>post_max_size</c> in MB.</summary>
    [JsonPropertyName("postMaxSize")]
    public int PostMaxSize { get; set; } = 128;

    [JsonPropertyName("shortOpenTag")]
    public bool ShortOpenTag { get; set; } = false;

    [JsonPropertyName("errorReporting")]
    public PhpErrorReporting ErrorReporting { get; set; } = PhpErrorReporting.Auto;

    [JsonPropertyName("enableOpcache")]
    public bool EnableOpcache { get; set; } = true;

    [JsonPropertyName("opcacheMemory")]
    public int OpcacheMemory { get; set; } = 256;

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = "Asia/Ho_Chi_Minh";

    [JsonPropertyName("useBundledCaCertificate")]
    public bool UseBundledCaCertificate { get; set; } = true;

    /// <summary>
    /// Patch updates within the current major.minor branch only — moving to another branch changes
    /// the extension ABI and stays a manual choice. See <c>PhpUpdateService</c>.
    /// </summary>
    [JsonPropertyName("updateMode")]
    public PhpUpdateMode UpdateMode { get; set; } = PhpUpdateMode.Auto;

    /// <summary>
    /// Danh sách extension được enable trong php.ini.
    /// Mỗi tên tương ứng với php_{name}.dll trong thư mục ext/ của PHP.
    /// </summary>
    [JsonPropertyName("enabledExtensions")]
    public List<string> EnabledExtensions { get; set; } =
    [
        "bz2", "curl", "exif", "fileinfo", "ftp", "gd", "gettext", "intl",
        "mbstring", "mysqli", "odbc", "openssl", "pdo_mysql", "pdo_sqlite",
        "sockets", "sodium", "sqlite3", "xsl", "zip"
    ];
}
