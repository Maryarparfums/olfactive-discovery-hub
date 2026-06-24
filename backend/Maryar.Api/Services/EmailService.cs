using System.Configuration;
using System.Net;
using System.Net.Mail;

namespace Maryar.Api.Services
{
    public class EmailService
    {
        private SmtpClient CriarSmtp()
        {
            var port = int.Parse(ConfigurationManager.AppSettings["Email.Port"] ?? "587");
            return new SmtpClient
            {
                Host           = ConfigurationManager.AppSettings["Email.Host"],
                Port           = port,
                EnableSsl      = port == 465,
                Credentials    = new NetworkCredential(
                                     ConfigurationManager.AppSettings["Email.Usuario"],
                                     ConfigurationManager.AppSettings["Email.Senha"]
                                 ),
                DeliveryMethod = SmtpDeliveryMethod.Network
            };
        }

        private MailMessage CriarMensagem(string destinatario, string assunto, string corpo)
        {
            var msg = new MailMessage
            {
                From       = new MailAddress(
                                 ConfigurationManager.AppSettings["Email.Remetente"],
                                 "Maryar"
                             ),
                Subject    = assunto,
                IsBodyHtml = true,
                Body       = corpo
            };
            msg.To.Add(destinatario);
            return msg;
        }

        public void EnviarLinkRedefinicao(string destinatario, string link)
        {
            var corpo = string.Format(@"
<!DOCTYPE html>
<html lang='pt-BR'>
<head>
  <meta charset='UTF-8' />
  <meta name='viewport' content='width=device-width, initial-scale=1.0' />
  <title>Redefinição de senha — Maryar</title>
</head>
<body style='margin:0;padding:0;background-color:#f5f2ee;font-family:Georgia,""Times New Roman"",serif;'>
  <table width='100%' cellpadding='0' cellspacing='0' border='0' style='background-color:#f5f2ee;padding:48px 16px;'>
    <tr>
      <td align='center' valign='top'>
        <table width='100%' cellpadding='0' cellspacing='0' border='0' style='max-width:520px;'>
          <tr>
            <td align='center' style='padding-bottom:32px;'>
              <p style='margin:0 0 2px 0;font-family:Arial,Helvetica,sans-serif;font-size:9px;letter-spacing:0.35em;text-transform:uppercase;color:#999999;'>Maryar</p>
              <p style='margin:0;font-family:Arial,Helvetica,sans-serif;font-size:9px;letter-spacing:0.2em;text-transform:uppercase;color:#c0b8b0;'>Perfumes</p>
            </td>
          </tr>
          <tr>
            <td style='background-color:#ffffff;border:1px solid #e5dfd8;'>
              <table width='100%' cellpadding='0' cellspacing='0' border='0'>
                <tr><td style='height:3px;background-color:#1a1a1a;'></td></tr>
              </table>
              <table width='100%' cellpadding='0' cellspacing='0' border='0'>
                <tr>
                  <td style='padding:48px 48px 40px;'>
                    <p style='margin:0 0 8px 0;font-family:Arial,Helvetica,sans-serif;font-size:9px;letter-spacing:0.25em;text-transform:uppercase;color:#b0a89e;'>Sua conta</p>
                    <h1 style='margin:0 0 32px 0;font-family:Georgia,""Times New Roman"",serif;font-size:26px;font-weight:normal;font-style:italic;color:#1a1a1a;line-height:1.3;'>
                      Redefinição de senha
                    </h1>
                    <table width='40' cellpadding='0' cellspacing='0' border='0' style='margin-bottom:28px;'>
                      <tr><td style='height:1px;background-color:#e5dfd8;'></td></tr>
                    </table>
                    <p style='margin:0 0 16px 0;font-family:Arial,Helvetica,sans-serif;font-size:13px;line-height:1.8;color:#555555;'>Olá,</p>
                    <p style='margin:0 0 32px 0;font-family:Arial,Helvetica,sans-serif;font-size:13px;line-height:1.8;color:#555555;'>
                      Recebemos uma solicitação para redefinir a senha da sua conta.
                      Clique no botão abaixo para escolher uma nova senha:
                    </p>
                    <table cellpadding='0' cellspacing='0' border='0' style='margin-bottom:36px;'>
                      <tr>
                        <td style='background-color:#1a1a1a;'>
                          <a href='{0}' style='display:inline-block;padding:16px 36px;font-family:Arial,Helvetica,sans-serif;font-size:9px;letter-spacing:0.3em;text-transform:uppercase;color:#ffffff;text-decoration:none;'>
                            Criar nova senha
                          </a>
                        </td>
                      </tr>
                    </table>
                    <table width='100%' cellpadding='0' cellspacing='0' border='0' style='margin-bottom:28px;border-left:2px solid #e5dfd8;'>
                      <tr>
                        <td style='padding:12px 16px;'>
                          <p style='margin:0;font-family:Arial,Helvetica,sans-serif;font-size:11px;line-height:1.6;color:#999999;'>
                            Este link expira em <strong style='color:#777777;'>1 hora</strong>.
                            Se você não solicitou a redefinição, ignore este e-mail — sua senha permanece a mesma.
                          </p>
                        </td>
                      </tr>
                    </table>
                    <p style='margin:0 0 4px 0;font-family:Arial,Helvetica,sans-serif;font-size:11px;color:#bbbbbb;'>Se o botão não funcionar, copie o link abaixo:</p>
                    <p style='margin:0;font-family:Arial,Helvetica,sans-serif;font-size:10px;color:#c0b8b0;word-break:break-all;line-height:1.5;'>{0}</p>
                  </td>
                </tr>
              </table>
            </td>
          </tr>
          <tr>
            <td align='center' style='padding:28px 16px 0;'>
              <p style='margin:0 0 6px 0;font-family:Arial,Helvetica,sans-serif;font-size:9px;letter-spacing:0.3em;text-transform:uppercase;color:#c0b8b0;'>Maryar Perfumes</p>
              <p style='margin:0;font-family:Arial,Helvetica,sans-serif;font-size:10px;color:#c0b8b0;'>
                <a href='https://www.maryar.com.br' style='color:#b0a89e;text-decoration:none;'>www.maryar.com.br</a>
              </p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>
", link);

            using (var smtp = CriarSmtp())
            using (var msg  = CriarMensagem(destinatario, "Redefinição de senha — Maryar", corpo))
                smtp.Send(msg);
        }

        public void EnviarConfirmacaoEmail(string destinatario, string link)
        {
            var corpo = string.Format(@"
<!DOCTYPE html>
<html lang='pt-BR'>
<head>
  <meta charset='UTF-8' />
  <meta name='viewport' content='width=device-width, initial-scale=1.0' />
  <title>Confirme seu e-mail — Maryar</title>
</head>
<body style='margin:0;padding:0;background-color:#f5f2ee;font-family:Georgia,""Times New Roman"",serif;'>
  <table width='100%' cellpadding='0' cellspacing='0' border='0' style='background-color:#f5f2ee;padding:48px 16px;'>
    <tr>
      <td align='center' valign='top'>
        <table width='100%' cellpadding='0' cellspacing='0' border='0' style='max-width:520px;'>
          <tr>
            <td align='center' style='padding-bottom:32px;'>
              <p style='margin:0 0 2px 0;font-family:Arial,Helvetica,sans-serif;font-size:9px;letter-spacing:0.35em;text-transform:uppercase;color:#999999;'>Maryar</p>
              <p style='margin:0;font-family:Arial,Helvetica,sans-serif;font-size:9px;letter-spacing:0.2em;text-transform:uppercase;color:#c0b8b0;'>Perfumes</p>
            </td>
          </tr>
          <tr>
            <td style='background-color:#ffffff;border:1px solid #e5dfd8;'>
              <table width='100%' cellpadding='0' cellspacing='0' border='0'>
                <tr><td style='height:3px;background-color:#1a1a1a;'></td></tr>
              </table>
              <table width='100%' cellpadding='0' cellspacing='0' border='0'>
                <tr>
                  <td style='padding:48px 48px 40px;'>
                    <p style='margin:0 0 8px 0;font-family:Arial,Helvetica,sans-serif;font-size:9px;letter-spacing:0.25em;text-transform:uppercase;color:#b0a89e;'>Bem-vindo(a)</p>
                    <h1 style='margin:0 0 32px 0;font-family:Georgia,""Times New Roman"",serif;font-size:26px;font-weight:normal;font-style:italic;color:#1a1a1a;line-height:1.3;'>
                      Confirme seu e-mail
                    </h1>
                    <table width='40' cellpadding='0' cellspacing='0' border='0' style='margin-bottom:28px;'>
                      <tr><td style='height:1px;background-color:#e5dfd8;'></td></tr>
                    </table>
                    <p style='margin:0 0 16px 0;font-family:Arial,Helvetica,sans-serif;font-size:13px;line-height:1.8;color:#555555;'>Olá,</p>
                    <p style='margin:0 0 32px 0;font-family:Arial,Helvetica,sans-serif;font-size:13px;line-height:1.8;color:#555555;'>
                      Obrigada por criar sua conta na Maryar.<br />
                      Clique no botão abaixo para confirmar seu e-mail e ativar sua conta:
                    </p>
                    <table cellpadding='0' cellspacing='0' border='0' style='margin-bottom:36px;'>
                      <tr>
                        <td style='background-color:#1a1a1a;'>
                          <a href='{0}' style='display:inline-block;padding:16px 36px;font-family:Arial,Helvetica,sans-serif;font-size:9px;letter-spacing:0.3em;text-transform:uppercase;color:#ffffff;text-decoration:none;'>
                            Confirmar e-mail
                          </a>
                        </td>
                      </tr>
                    </table>
                    <table width='100%' cellpadding='0' cellspacing='0' border='0' style='margin-bottom:28px;border-left:2px solid #e5dfd8;'>
                      <tr>
                        <td style='padding:12px 16px;'>
                          <p style='margin:0;font-family:Arial,Helvetica,sans-serif;font-size:11px;line-height:1.6;color:#999999;'>
                            Este link expira em <strong style='color:#777777;'>24 horas</strong>.
                            Se você não criou uma conta na Maryar, ou não solicitou alteração de e-mail, ignore esta mensagem.
                          </p>
                        </td>
                      </tr>
                    </table>
                    <p style='margin:0 0 4px 0;font-family:Arial,Helvetica,sans-serif;font-size:11px;color:#bbbbbb;'>Se o botão não funcionar, copie o link abaixo:</p>
                    <p style='margin:0;font-family:Arial,Helvetica,sans-serif;font-size:10px;color:#c0b8b0;word-break:break-all;line-height:1.5;'>{0}</p>
                  </td>
                </tr>
              </table>
            </td>
          </tr>
          <tr>
            <td align='center' style='padding:28px 16px 0;'>
              <p style='margin:0 0 6px 0;font-family:Arial,Helvetica,sans-serif;font-size:9px;letter-spacing:0.3em;text-transform:uppercase;color:#c0b8b0;'>Maryar Perfumes</p>
              <p style='margin:0;font-family:Arial,Helvetica,sans-serif;font-size:10px;color:#c0b8b0;'>
                <a href='https://www.maryar.com.br' style='color:#b0a89e;text-decoration:none;'>www.maryar.com.br</a>
              </p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>
", link);

            using (var smtp = CriarSmtp())
            using (var msg  = CriarMensagem(destinatario, "Confirme seu e-mail — Maryar", corpo))
                smtp.Send(msg);
        }
    }
}
