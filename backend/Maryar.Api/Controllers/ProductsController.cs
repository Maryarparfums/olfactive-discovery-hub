[HttpGet, Route("")]
public IHttpActionResult List()
{
    return Ok(new { id = 1, name = "teste" });
}
