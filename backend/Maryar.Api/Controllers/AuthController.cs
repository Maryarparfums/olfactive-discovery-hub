[HttpPost]
public IHttpActionResult SignIn([FromBody] SignInRequest req)
{
    try
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return Content(HttpStatusCode.BadRequest, new { error = "Dados inválidos." });
        var user = _users.GetByEmail(req.Email);
        if (user == null || !PasswordHasher.Verify(req.Password, user.PasswordHash))
            return Content(HttpStatusCode.Unauthorized, new { error = "Credenciais inválidas." });
        DateTime exp;
        var token = _jwt.Issue(user, out exp);
        return Ok(new AuthResponse
        {
            UserId = user.Id, Name = user.Name, Email = user.Email,
            Role = user.Role, Token = token, ExpiresAt = exp
        });
    }
    catch (Exception ex)
    {
        return Content(HttpStatusCode.InternalServerError, new
        {
            error = ex.Message,
            tipo = ex.GetType().Name,
            stack = ex.StackTrace
        });
    }
}
