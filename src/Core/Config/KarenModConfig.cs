using BaseLib.Config;

namespace ShoujoKagekiAijoKaren;

public class KarenModConfig : SimpleModConfig
{

    /// <summary>
    /// 上传对局数据
    /// </summary>
    public static bool EnableDataUpload { get; set; } = true;

    /// <summary>
    /// 显示长颈鹿启动画面
    /// </summary>
    public static bool ShowSplashScreen { get; set; } = true;
}
