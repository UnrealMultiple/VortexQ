using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using Vortex.Bot.Attributes;
using Vortex.Bot.Core.Service;
using Vortex.Bot.Database.Models;
using Vortex.Bot.Utility;

namespace Vortex.Bot.Command.Terraria;

[Command("注册", "reg", "register")]
[HelpText("注册游戏账号")]
[CommandType(CommandType.Group)]
[Permission("vortex.terraria.register")]
public static partial class RegisterCommand
{
    [Main]
    public static async Task Execute(GroupCommandArgs args, [Param("角色名称")] string characterName)
    {
        var serverManager = args.Context.Server?.Services.GetService<TerrariaServerService>();
        if (serverManager == null)
        {
            await args.ReplyWithAtAsync("服务器管理器未初始化");
            return;
        }

        if (!serverManager.TryGetUserServer(args.SenderUin, args.GroupUin, out var server) || server == null)
        {
            await args.ReplyWithAtAsync("未切换服务器或服务器无效!\n请先使用 '切换 <服务器名称>' 选择服务器");
            return;
        }

        if (characterName.Length > server.Config.RegisterNameMax)
        {
            await args.ReplyWithAtAsync($"注册的人物名称不能大于{server.Config.RegisterNameMax}个字符!");
            return;
        }

        if (server.Config.RegisterNameLimit && !PlayerNameRegex().IsMatch(characterName))
        {
            await args.ReplyWithAtAsync("注册的人物名称不能包含中文、字母、数字和/:[]以外的字符");
            return;
        }

        var existingUsers = TerrariaUser.GetUsersById(args.SenderUin, server.Config.Name);
        if (existingUsers.Count >= server.Config.RegisterMaxCount)
        {
            await args.ReplyWithAtAsync($"同一个服务器上注册账户不能超过{server.Config.RegisterMaxCount}个");
            return;
        }

        var password = Guid.NewGuid().ToString()[..8];

        try
        {
            TerrariaUser.Add(args.SenderUin, args.GroupUin, server.Config.Name, characterName, password);
            var result = await server.RegisterAccountAsync(characterName, password, server.Config.DefaultGroup);

            if (result?.Success == true)
            {
                if (args.Context.Configuration.Mail.Enabled)
                {
                    await SendRegistrationEmail(args, server.Config.Name, characterName, password);
                }

                var reply = $"注册成功!\n" +
                           $"注册服务器: {server.Config.Name}\n" +
                           $"注册名称: {characterName}\n" +
                           $"注册账号: {args.SenderUin}\n" +
                           $"注册密码已发送至您的QQ邮箱\n" +
                           $"进入服务器后可使用 /password [当前密码] [新密码] 修改你的密码";
                await args.ReplyWithAtAsync(reply);
            }
            else
            {
                TerrariaUser.Remove(server.Config.Name, characterName);
                await args.ReplyWithAtAsync($"服务器注册失败: {result?.Message ?? "无法连接服务器"}");
            }
        }
        catch (InvalidOperationException ex)
        {
            await args.ReplyWithAtAsync(ex.Message);
        }
    }

    private static async Task SendRegistrationEmail(CommandArgs args, string serverName, string characterName, string password)
    {
        try
        {
            var mailConfig = args.Context.Configuration.Mail;

            var emailBody = MailTemplateUtility.RenderTemplate("RegisterEmail", new RegisterEmailModel
            {
                ServerName = serverName,
                CharacterName = characterName,
                QQNumber = args.SenderUin.ToString(),
                Password = password
            });

            await Task.Run(() =>
            {
                using var mail = MailUtility.Builder(mailConfig.Host, mailConfig.Port, mailConfig.Password, mailConfig.EnableSsl)
                    .SetSender(mailConfig.From)
                    .AddTarget($"{args.SenderUin}@qq.com")
                    .SetTile($"[{serverName}] Terraria 服务器注册成功")
                    .SetBody(emailBody)
                    .EnableHtmlBody(true);

                mail.Send();
            });
        }
        catch (Exception ex)
        {
            args.Logger.LogWarning("发送注册邮件失败: {Error}", ex.ToString());
        }
    }

    [GeneratedRegex("^[a-zA-Z0-9\u4e00-\u9fa5\\[\\]:/ ]+$")]
    private static partial Regex PlayerNameRegex();
}
