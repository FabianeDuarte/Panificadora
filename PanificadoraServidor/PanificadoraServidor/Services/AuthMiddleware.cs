using Microsoft.EntityFrameworkCore;
using PanificadoraServidor.Data;
using PanificadoraServidor.Models;

namespace PanificadoraServidor.Services;

/// <summary>
/// Protege a API: /api/admin/* só para perfil Admin (site), /api/app/* só para perfil Vendedor (aplicativo),
/// /api/auth/* (exceto login) para qualquer usuário autenticado. Token no cabeçalho "Authorization: Bearer ...".
/// </summary>
public static class AuthMiddleware
{
    public static void UseAuthApi(this WebApplication app)
    {
        app.Use(async (ctx, next) =>
        {
            var path = ctx.Request.Path;
            bool admin = path.StartsWithSegments("/api/admin");
            bool vend = path.StartsWithSegments("/api/app");
            bool auth = path.StartsWithSegments("/api/auth") && !path.StartsWithSegments("/api/auth/login");
            if (!admin && !vend && !auth) { await next(); return; }

            var h = ctx.Request.Headers.Authorization.ToString();
            if (!h.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) { await Negar(ctx, 401, "Faça login para continuar."); return; }

            var db = ctx.RequestServices.GetRequiredService<AppDb>();
            var hash = TokenService.Hash(h[7..].Trim());
            var sessao = await db.Sessoes.FirstOrDefaultAsync(x => x.TokenHash == hash && x.Expira > DateTime.Now);
            var usuario = sessao == null ? null : await db.Usuarios.FirstOrDefaultAsync(u => u.Id == sessao.UsuarioId && u.Ativo);
            if (usuario == null) { await Negar(ctx, 401, "Sessão expirada. Entre novamente."); return; }

            if (admin && usuario.Perfil != "Admin") { await Negar(ctx, 403, "Acesso restrito a administradores."); return; }
            if (vend && (usuario.Perfil != "Vendedor" || usuario.VendedorId == null)) { await Negar(ctx, 403, "Acesso restrito a vendedores."); return; }

            ctx.Items["usuario"] = usuario;
            ctx.Items["sessao"] = sessao;
            await next();
        });
    }

    static async Task Negar(HttpContext ctx, int status, string msg)
    {
        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(new { erro = msg });
    }

    public static Usuario UsuarioLogado(this HttpContext ctx) => (Usuario)ctx.Items["usuario"]!;
}
