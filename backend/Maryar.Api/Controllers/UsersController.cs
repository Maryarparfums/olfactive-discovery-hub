[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IDbConnection _db; // MySqlConnection ou Dapper

    public UsersController(IDbConnection db)
    {
        _db = db;
    }

    // GET /api/users/me
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var user = await _db.QueryFirstOrDefaultAsync<UserProfileDto>(
            @"SELECT name, email, phone, cpf, cep, logradouro, numero,
                     complemento, bairro, cidade, estado
              FROM users WHERE id = @Id",
            new { Id = userId });

        if (user == null) return NotFound(new { error = "Usuário não encontrado." });

        return Ok(user);
    }

    // PUT /api/users/me
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UserProfileDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var rows = await _db.ExecuteAsync(
            @"UPDATE users SET
                name         = @Name,
                email        = @Email,
                phone        = @Phone,
                cpf          = @Cpf,
                cep          = @Cep,
                logradouro   = @Logradouro,
                numero       = @Numero,
                complemento  = @Complemento,
                bairro       = @Bairro,
                cidade       = @Cidade,
                estado       = @Estado
              WHERE id = @Id",
            new
            {
                dto.Name, dto.Email, dto.Phone, dto.Cpf,
                dto.Cep, dto.Logradouro, dto.Numero,
                dto.Complemento, dto.Bairro, dto.Cidade, dto.Estado,
                Id = userId
            });

        if (rows == 0) return NotFound(new { error = "Usuário não encontrado." });

        return Ok(new { message = "Perfil atualizado com sucesso." });
    }
}
