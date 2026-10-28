using Microsoft.EntityFrameworkCore;
using PanificadoraServidor.Data;
using PanificadoraServidor.Models;
using PanificadoraServidor.Services;

namespace PanificadoraServidor.Endpoints;

public static class AuthEndpoints
{
    const int MaxFalhas = 5;
    const int MinutosBloqueio = 5;
    static readonly string[] Perfis = { "Admin", "Vendedor" };

    public static void MapAuth(this WebApplication app)
    {
        var g = app.MapGroup("/api/auth");

        g.MapPost("/login", async Task<IResult> (LoginDto dto, AppDb db) =>
        {
            var login = (dto.Login ?? "").Trim().ToLowerInvariant();
            var u = await db.Usuarios.FirstOrDefaultAsync(x => x.Login == login);

            if (u?.BloqueadoAte > DateTime.Now)
                return Results.Json(new { erro = $"Muitas tentativas. Usuário bloqueado até {u.BloqueadoAte:HH:mm}." }, statusCode: 423);

            bool senhaOk = Senha.Verificar(dto.Senha ?? "", u?.SenhaHash);   // sempre calcula, para não revelar se o login existe
            if (u == null || !u.Ativo || !senhaOk)
            {
                if (u != null && u.Ativo)
                {
                    u.Falhas++;
                    if (u.Falhas >= MaxFalhas) { u.BloqueadoAte = DateTime.Now.AddMinutes(MinutosBloqueio); u.Falhas = 0; }
                    await db.SaveChangesAsync();
                }
                return Results.Json(new { erro = "Usuário ou senha inválidos." }, statusCode: 401);
            }

            if (u.Perfil == "Vendedor")
            {
                var vend = u.VendedorId == null ? null : await db.Vendedores.FindAsync(u.VendedorId);
                if (vend == null || !vend.Ativo) return Results.Json(new { erro = "Vendedor vinculado inativo ou inexistente." }, statusCode: 403);
            }

            u.Falhas = 0; u.BloqueadoAte = null;
            db.Sessoes.RemoveRange(await db.Sessoes.Where(s => s.Expira < DateTime.Now).ToListAsync());   // limpeza

            var token = TokenService.Novo();
            var expira = DateTime.Now.AddHours(u.Perfil == "Admin" ? 12 : 24 * 30);
            db.Sessoes.Add(new Sessao { TokenHash = TokenService.Hash(token), UsuarioId = u.Id, Expira = expira });
            await db.SaveChangesAsync();
            return Results.Ok(new { token, nome = u.Nome, perfil = u.Perfil, vendedorId = u.VendedorId, expira });
        });

        g.MapGet("/me", (HttpContext ctx) =>
        {
            var u = ctx.UsuarioLogado();
            return Results.Ok(new { nome = u.Nome, login = u.Login, perfil = u.Perfil, vendedorId = u.VendedorId });
        });

        g.MapPost("/logout", async (HttpContext ctx, AppDb db) =>
        {
            var s = (Sessao)ctx.Items["sessao"]!;
            db.Sessoes.Remove(s);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        g.MapPost("/trocar-senha", async Task<IResult> (TrocarSenhaDto dto, HttpContext ctx, AppDb db) =>
        {
            var u = await db.Usuarios.FindAsync(ctx.UsuarioLogado().Id);
            if (u == null || !Senha.Verificar(dto.SenhaAtual ?? "", u.SenhaHash)) return Results.BadRequest(new { erro = "Senha atual incorreta." });
            if ((dto.NovaSenha ?? "").Length < 6) return Results.BadRequest(new { erro = "A nova senha deve ter pelo menos 6 caracteres." });
            u.SenhaHash = Senha.Hash(dto.NovaSenha!);
            var atual = (Sessao)ctx.Items["sessao"]!;
            db.Sessoes.RemoveRange(await db.Sessoes.Where(s => s.UsuarioId == u.Id && s.Id != atual.Id).ToListAsync());   // derruba os outros aparelhos
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // ----- gestão de usuários (somente Admin: o middleware já exige) -----
        var a = app.MapGroup("/api/admin/usuarios");

        a.MapGet("", async (AppDb db) => (await db.Usuarios.AsNoTracking().OrderBy(u => EF.Property<int>(u, "Id")).ToListAsync())
            .Select(u => new { u.Id, u.Nome, u.Login, u.Perfil, u.VendedorId, u.Ativo }));

        a.MapPost("", async Task<IResult> (UsuarioDto d, AppDb db) =>
        {
            var erro = await Validar(db, d, 0, exigeSenha: true);
            if (erro != null) return Results.BadRequest(new { erro });
            db.Usuarios.Add(new Usuario
            {
                Nome = d.Nome.Trim(), Login = d.Login.Trim().ToLowerInvariant(), SenhaHash = Senha.Hash(d.Senha!), Perfil = d.Perfil,
                VendedorId = d.Perfil == "Vendedor" ? d.VendedorId : null, Ativo = d.Ativo
            });
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        a.MapPut("/{id:int}", async Task<IResult> (int id, UsuarioDto d, HttpContext ctx, AppDb db) =>
        {
            var u = await db.Usuarios.FindAsync(id);
            if (u == null) return Results.NotFound();
            var erro = await Validar(db, d, id, exigeSenha: false);
            if (erro == null && id == ctx.UsuarioLogado().Id && (!d.Ativo || d.Perfil != "Admin")) erro = "Você não pode inativar nem rebaixar o seu próprio usuário.";
            if (erro != null) return Results.BadRequest(new { erro });

            u.Nome = d.Nome.Trim(); u.Login = d.Login.Trim().ToLowerInvariant(); u.Perfil = d.Perfil;
            u.VendedorId = d.Perfil == "Vendedor" ? d.VendedorId : null; u.Ativo = d.Ativo;
            bool derrubar = !d.Ativo;
            if (!string.IsNullOrEmpty(d.Senha)) { u.SenhaHash = Senha.Hash(d.Senha); u.Falhas = 0; u.BloqueadoAte = null; derrubar = true; }
            if (derrubar) db.Sessoes.RemoveRange(await db.Sessoes.Where(s => s.UsuarioId == u.Id).ToListAsync());
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        a.MapDelete("/{id:int}", async Task<IResult> (int id, HttpContext ctx, AppDb db) =>
        {
            if (id == ctx.UsuarioLogado().Id) return Results.BadRequest(new { erro = "Você não pode inativar o seu próprio usuário." });
            var u = await db.Usuarios.FindAsync(id);
            if (u == null) return Results.NotFound();
            u.Ativo = false;
            db.Sessoes.RemoveRange(await db.Sessoes.Where(s => s.UsuarioId == id).ToListAsync());
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    static async Task<string?> Validar(AppDb db, UsuarioDto d, int id, bool exigeSenha)
    {
        if (string.IsNullOrWhiteSpace(d.Nome)) return "Informe o nome.";
        var login = (d.Login ?? "").Trim().ToLowerInvariant();
        if (login.Length < 3) return "O login deve ter pelo menos 3 caracteres.";
        if (await db.Usuarios.AnyAsync(u => u.Login == login && u.Id != id)) return "Já existe um usuário com este login.";
        if (!Perfis.Contains(d.Perfil)) return "Perfil inválido.";
        if (exigeSenha && (d.Senha ?? "").Length < 6) return "A senha deve ter pelo menos 6 caracteres.";
        if (!string.IsNullOrEmpty(d.Senha) && d.Senha.Length < 6) return "A senha deve ter pelo menos 6 caracteres.";
        if (d.Perfil == "Vendedor")
        {
            if (d.VendedorId == null || !await db.Vendedores.AnyAsync(v => v.Id == d.VendedorId && v.Ativo)) return "Selecione um vendedor ativo para este usuário.";
            if (await db.Usuarios.AnyAsync(u => u.Perfil == "Vendedor" && u.Ativo && u.VendedorId == d.VendedorId && u.Id != id && d.Ativo)) return "Este vendedor já possui um usuário ativo.";
        }
        return null;
    }
}
