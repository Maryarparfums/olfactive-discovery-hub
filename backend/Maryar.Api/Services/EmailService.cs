namespace SeuProjeto.Services;

public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task EnviarEmailRedefinicaoAsync(string destinatario, string link)
    {
        var smtp = new System.Net.Mail.SmtpClient
        {
            Host = _config["Email:Host"]!,
            Port = int.Parse(_config["Email:Port"]!),
            EnableSsl = true,
            Credentials = new System.Net.NetworkCredential(
                _config["Email:Usuario"]!,
                _config["Email:Senha"]!
            )
        };

        var mensagem = new System.Net.Mail.MailMessage
        {
            From = new System.Net.Mail.MailAddress(
                _config["Email:Remetente"]!,
                "Maryar"
            ),
            Subject = "Redefinição de senha — Maryar",
            IsBodyHtml = true,
            Body = $"""
                <p>Olá,</p>
                <p>Recebemos uma solicitação para redefinir a senha da sua conta.</p>
                <p>
                    <a href="{link}" style="color:#000;font-weight:bold">
                        Clique aqui para criar uma nova senha
                    </a>
                </p>
                <p>Este link expira em <strong>1 hora</strong>.</p>
                <p>Se você não solicitou isso, ignore este e-mail.</p>
                <br/>
                <p>Maryar Perfumes</p>
            """
        };

        mensagem.To.Add(destinatario);
        await smtp.SendMailAsync(mensagem);
    }
}
