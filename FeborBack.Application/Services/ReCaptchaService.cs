using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace FeborBack.Application.Services;

public interface IReCaptchaService
{
    Task<ReCaptchaResponse> VerifyTokenAsync(string token, string action);
}

public class ReCaptchaService : IReCaptchaService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ReCaptchaService> _logger;
    private readonly IWebHostEnvironment _environment;

    public ReCaptchaService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ReCaptchaService> logger,
        IWebHostEnvironment environment)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _environment = environment;
    }

    public async Task<ReCaptchaResponse> VerifyTokenAsync(string token, string action)
    {
        try
        {
            // OPCIÓN: En desarrollo, permitir tokens vacíos o retornar éxito automáticamente
            var enabledInDevelopment = _configuration.GetValue<bool>("ReCaptcha:EnabledInDevelopment", true);

            if (_environment.IsDevelopment() && !enabledInDevelopment)
            {
                _logger.LogWarning("reCAPTCHA deshabilitado en desarrollo - Permitiendo acceso");
                return new ReCaptchaResponse
                {
                    Success = true,
                    Score = 0.9,
                    Action = action,
                    Hostname = "localhost"
                };
            }

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Token de reCAPTCHA vacío");
                return new ReCaptchaResponse
                {
                    Success = false,
                    ErrorCodes = new List<string> { "missing-input-response" }
                };
            }

            // Log del token (primeros 20 caracteres para debug)
            _logger.LogInformation("Token recibido: {TokenPreview}...", token.Length > 20 ? token.Substring(0, 20) : token);

            var secretKey = _configuration["ReCaptcha:SecretKey"];
            var verifyUrl = _configuration["ReCaptcha:VerifyUrl"];

            if (string.IsNullOrEmpty(secretKey))
            {
                _logger.LogError("ReCaptcha SecretKey no configurada");
                return new ReCaptchaResponse
                {
                    Success = false,
                    ErrorCodes = new List<string> { "missing-secret-key" }
                };
            }

            var requestData = new Dictionary<string, string>
            {
                { "secret", secretKey },
                { "response", token }
            };

            var response = await _httpClient.PostAsync(
                verifyUrl ?? "https://www.google.com/recaptcha/api/siteverify",
                new FormUrlEncodedContent(requestData)
            );

            var jsonResponse = await response.Content.ReadAsStringAsync();

            // Log de la respuesta completa de Google
            _logger.LogInformation("Respuesta de Google reCAPTCHA: {Response}", jsonResponse);

            var result = JsonSerializer.Deserialize<ReCaptchaResponse>(
                jsonResponse,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (result == null)
            {
                _logger.LogError("Error al deserializar respuesta de reCAPTCHA");
                return new ReCaptchaResponse
                {
                    Success = false,
                    ErrorCodes = new List<string> { "deserialization-failed" }
                };
            }

            // Log detallado de la respuesta
            _logger.LogInformation(
                "reCAPTCHA Response: Success={Success}, Score={Score}, Action={Action}, Hostname={Hostname}",
                result.Success,
                result.Score,
                result.Action,
                result.Hostname
            );

            // Log de error codes si existen
            if (result.ErrorCodes != null && result.ErrorCodes.Any())
            {
                _logger.LogWarning("Error codes de reCAPTCHA: {ErrorCodes}", string.Join(", ", result.ErrorCodes));
            }

            // Validar score mínimo (solo advertir, no bloquear aquí)
            var minScore = _configuration.GetValue<double>("ReCaptcha:MinimumScore", 0.5);
            if (result.Success && result.Score < minScore)
            {
                _logger.LogWarning(
                    "Score de reCAPTCHA bajo: {Score} (mínimo recomendado: {MinScore}). Acción: {Action}",
                    result.Score,
                    minScore,
                    action
                );
            }

            // CAMBIO: Validar acción solo como advertencia, no como error bloqueante
            if (result.Success && !string.IsNullOrEmpty(action) && !string.IsNullOrEmpty(result.Action) && result.Action != action)
            {
                _logger.LogWarning(
                    "Acción de reCAPTCHA no coincide (no bloqueante). Esperada: {Expected}, Recibida: {Actual}",
                    action,
                    result.Action
                );
                // NO retornar error, solo advertir
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar token de reCAPTCHA");
            return new ReCaptchaResponse
            {
                Success = false,
                ErrorCodes = new List<string> { "verification-failed", ex.Message }
            };
        }
    }
}

public class ReCaptchaResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("score")]
    public double Score { get; set; }

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("challenge_ts")]
    public DateTime ChallengeTs { get; set; }

    [JsonPropertyName("hostname")]
    public string Hostname { get; set; } = string.Empty;

    [JsonPropertyName("error-codes")]
    public List<string>? ErrorCodes { get; set; }
}
