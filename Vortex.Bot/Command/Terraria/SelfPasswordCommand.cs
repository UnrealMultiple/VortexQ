using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text;
using Vortex.Bot.Attributes;
using Vortex.Bot.Core.Service;
using Vortex.Bot.Database.Models;
using Vortex.Bot.Utility;

namespace Vortex.Bot.Command.Terraria;

[Command("我的密码", "mypwd", "password")]
[HelpText("查看自己的游戏密码")]
[CommandType(CommandType.Group)]
[Permission("vortex.terraria.selfpassword")]
public static class SelfPasswordCommand
{
    [Main]
    public static async Task Execute(GroupCommandArgs args)
    {
        var serverManager = args.Context.Server?.Services.GetService<TerrariaServerService>();
        if (serverManager == null)
        {
            await args.ReplyWithAtAsync("服务器管理器未初始化");
            return;
        }

        if (!serverManager.TryGetUserServer(args.SenderUin, args.GroupUin, out var server) || server == null)
        {
            await args.ReplyWithAtAsync("服务器无效或未切换至一个有效服务器!");
            return;
        }

        var users = TerrariaUser.GetUsersById(args.SenderUin, server.Config.Name);

        if (users.Count == 0)
        {
            await args.ReplyWithAtAsync($"{server.Config.Name}上未找到你的注册信息。");
            return;
        }

        var mailConfig = args.Context.Configuration.Mail;
        if (!mailConfig.Enabled)
        {
            await args.ReplyWithAtAsync("邮件服务未启用，无法发送密码。");
            return;
        }

        try
        {
            var passwordListHtml = new StringBuilder();
            foreach (var user in users)
            {
                passwordListHtml.AppendLine("<tr>");
                passwordListHtml.AppendLine("    <td style=\"padding: 15px 0; border-bottom: 1px solid #eee;\">");
                passwordListHtml.AppendLine("        <table role=\"presentation\" cellspacing=\"0\" cellpadding=\"0\" border=\"0\" width=\"100%\">");
                passwordListHtml.AppendLine("            <tr>");
                passwordListHtml.AppendLine($"                <td style=\"color: #666; font-size: 14px;\">人物: {user.Name}</td>");
                passwordListHtml.AppendLine($"                <td style=\"color: #333; font-weight: 600; font-size: 16px; text-align: right; font-family: 'Courier New', monospace;\">{user.Password}</td>");
                passwordListHtml.AppendLine("            </tr>");
                passwordListHtml.AppendLine("        </table>");
                passwordListHtml.AppendLine("    </td>");
                passwordListHtml.AppendLine("</tr>");
            }

            var emailBody = MailTemplateUtility.GetTemplate("PasswordQueryEmail")
                .Replace("{{ServerName}}", server.Config.Name)
                .Replace("{{PasswordList}}", passwordListHtml.ToString());

            await Task.Run(() =>
            {
                using var mail = MailUtility.Builder(mailConfig.Host, mailConfig.Port, mailConfig.Password, mailConfig.EnableSsl)
                    .SetSender(mailConfig.From)
                    .AddTarget($"{args.SenderUin}@qq.com")
                    .SetTile($"[{server.Config.Name}] Terraria 服务器密码查询")
                    .SetBody(emailBody)
                    .EnableHtmlBody(true);

                mail.Send();
            });

            await args.ReplyWithAtAsync("密码已发送至您的QQ邮箱，请查收！");
        }
        catch (Exception ex)
        {
            args.Logger.LogWarning("发送密码查询邮件失败: {Error}", ex.ToString());
            await args.ReplyWithAtAsync("发送邮件失败，请稍后重试或联系管理员。");
        }
    }
}
