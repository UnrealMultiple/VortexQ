namespace Music.QQ.Enums;

public enum QrcodeLoginType
{
    /// <summary>
    /// 等待扫描
    /// </summary>
    SCAN,

    /// <summary>
    /// 已扫码未确认登录
    /// </summary>
    CONF,

    /// <summary>
    /// 二维码失效
    /// </summary>
    TIMEOUT,

    /// <summary>
    /// 扫描成功
    /// </summary>
    DONE,

    /// <summary>
    /// 二维码失效
    /// </summary>
    REFUSE,

    /// <summary>
    /// 未知状态。
    /// </summary>
    OTHER,

    /// <summary>
    /// 取消
    /// </summary>
    CANCEL
}
