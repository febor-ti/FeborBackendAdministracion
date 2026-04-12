namespace FeborBack.Application.Services;

public interface IOtpService
{
    /// <summary>Genera un token de sesión único (GUID) para identificar el proceso 2FA.</summary>
    string GenerateSessionToken();

    /// <summary>Genera un código numérico de 6 dígitos.</summary>
    string GenerateCode();

    /// <summary>Almacena el código en caché asociado al sessionToken, con expiración de 10 minutos.</summary>
    void StoreCode(string sessionToken, int userId, string code);

    /// <summary>Recupera el (userId, code) almacenado. Retorna null si expiró o no existe.</summary>
    (int UserId, string Code)? GetCode(string sessionToken);

    /// <summary>Elimina el código del caché (tras verificación exitosa o intento inválido).</summary>
    void RemoveCode(string sessionToken);
}
