using Vortex.Bot.Attributes;
using Vortex.Bot.Database.Models;
using Vortex.Bot.Utility;

namespace Vortex.Bot.Command.Terraria;

[Command("绑定角色", "bindcharacter")]
[HelpText("绑定您的 Terraria 角色到当前账号，方便后续使用相关功能")]
[CommandType(CommandType.Group)]
[Permission("vortex.terraria.bindcharacter")]
[DefaultCommand]
public static class BindCharacterCommand
{
    [Main]
    public static async Task Execute(GroupCommandArgs args, string characterName)
    {
        var key = AESSecurityUtility.Encrypt($"{characterName}");
        var verifyAction = "bindcharacter";
        if (string.IsNullOrWhiteSpace(characterName))
        {
            await args.ReplyWithAtAsync("角色名称不能为空，请提供一个有效的角色名称。");
            return;
        }
        if (args.GetPendingVerification(verifyAction, key) != null)
        {
            await args.ReplyWithAtAsync("您已有待确认的绑定角色操作，请发送 /验证 或 /取消绑定角色");
            return;
        }
        var guid = Guid.NewGuid().ToString();
        args.CreateVerification(
            actionType: verifyAction,
            actionName: "绑定角色",
            timeoutSeconds: 60,
            verifyKey: key,
            data: new { CharacterName = characterName, VerifyCode = guid, UserUin = args.SenderUin, args.GroupUin }
        );
        await args.ReplyWithAtAsync($"请在60秒内使用角色 {characterName} 进入服务器" +
            $"\n发送 /验证 来确认绑定角色");
        _ = args.StartVerificationTimeoutAsync(verifyAction, async (v) =>
        {
            await args.ReplyWithAtAsync($"绑定角色确认已超时，操作已取消。");
        }, key);
    }
}

[Command("验证")]
[HelpText("确认绑定角色确保绑定信息正确")]
[CommandType(CommandType.Server)]
[Permission("vortex.terraria.verifycharacter")]
[DefaultCommand]
public static class VerifyCharacterCommand
{
    [Main]
    public static async Task Execute(ServerCommandArgs args)
    {
        if(!args.Player.IsLogin)
        {
            await args.ReplyAsync("您尚未登录，请先登录后再进行验证。");
            return;
        }

        if(args.Server == null)
        {
            await args.ReplyAsync("无法获取服务器信息！");
            return;
        }
        Console.WriteLine(args.User?.Id);
        var key = AESSecurityUtility.Encrypt($"{args.Player.Name}");
        var result = args.Verify("bindcharacter", key);
        Console.WriteLine(key);
        if (result.Success && result.Verification?.Data != null)
        {
            dynamic data = result.Verification.Data;
            TerrariaUser.Add(
                id: data.UserUin,
                groupId: data.GroupUin,
                server: args.Server.Config.Name,
                name: data.CharacterName,
                password: ""
            );
            await args.ReplyAsync($"✅ 角色 {data.CharacterName} 绑定成功！");
        }
        else
        {
            await args.ReplyWithAtAsync($"❌ {result.Message}");
        }
    }
}
