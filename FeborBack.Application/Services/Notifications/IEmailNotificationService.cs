namespace FeborBack.Application.Services.Notifications;

public interface IEmailNotificationService
{
    /// <summary>
    /// Notifica a los administradores sobre una nueva solicitud CDAT
    /// </summary>
    Task NotifyAdminsNewCDATRequestAsync(int contactRequestId, string nombre, string apellidos, string email);

    /// <summary>
    /// Notifica a los administradores sobre una nueva solicitud del Simulador de Crédito
    /// </summary>
    Task NotifyAdminsNewCreditRequestAsync(int contactRequestId, string nombre, string apellidos, string email);

    /// <summary>
    /// Notifica al asesor comercial sobre una asignación de solicitud CDAT
    /// </summary>
    Task NotifyAdvisorCdatAssignmentAsync(
        int assignmentId,
        int cdatRequestId,
        string clientNombre,
        string clientApellidos,
        string clientEmail,
        string clientTelefono,
        string clientCiudad,
        string advisorEmail,
        string advisorName,
        string assignedByName,
        string? notes);

    /// <summary>
    /// Notifica al asesor comercial sobre una asignación de solicitud de Crédito
    /// </summary>
    Task NotifyAdvisorCreditAssignmentAsync(
        int assignmentId,
        int creditRequestId,
        string clientNombre,
        string clientApellidos,
        string clientEmail,
        string clientTelefono,
        string clientCiudad,
        string advisorEmail,
        string advisorName,
        string assignedByName,
        string? notes);

    /// <summary>
    /// Envía un correo de bienvenida al usuario con su contraseña temporal
    /// </summary>
    Task SendWelcomeEmailWithTemporaryPasswordAsync(
        string userEmail,
        string fullName,
        string temporaryPassword);

    /// <summary>
    /// Envía un correo al usuario cuando su contraseña ha sido reseteada
    /// </summary>
    Task SendPasswordResetEmailAsync(
        string userEmail,
        string fullName,
        string temporaryPassword);

    /// <summary>
    /// Notifica a los administradores sobre una nueva solicitud de ahorro externa
    /// </summary>
    Task NotifyAdminsNewSavingsRequestAsync(
        int requestId,
        string nombre,
        string apellidos,
        string email,
        string productoAhorro);

    /// <summary>
    /// Notifica al asesor comercial sobre una asignación de solicitud de ahorro
    /// </summary>
    Task NotifyAdvisorSavingsAssignmentAsync(
        int assignmentId,
        int savingsRequestId,
        string clientNombre,
        string clientApellidos,
        string clientEmail,
        string clientTelefono,
        string productoAhorro,
        string advisorEmail,
        string advisorName,
        string assignedByName,
        string? notes);

    /// <summary>
    /// Notifica a los administradores sobre una nueva solicitud de convenio externa
    /// </summary>
    Task NotifyAdminsNewConvenioRequestAsync(
        int requestId,
        string nombre,
        string apellidos,
        string email,
        string tipoConvenio);

    /// <summary>
    /// Notifica al asesor comercial sobre una asignación de solicitud de convenio
    /// </summary>
    Task NotifyAdvisorConvenioAssignmentAsync(
        int assignmentId,
        int convenioRequestId,
        string clientNombre,
        string clientApellidos,
        string clientEmail,
        string clientTelefono,
        string tipoConvenio,
        string advisorEmail,
        string advisorName,
        string assignedByName,
        string? notes);

    /// <summary>
    /// Notifica a los administradores sobre una nueva solicitud de crédito externa
    /// </summary>
    Task NotifyAdminsNewCreditExternalRequestAsync(
        int requestId,
        string nombre,
        string apellidos,
        string email,
        string productoCredito);

    /// <summary>
    /// Notifica al asesor comercial sobre una asignación de solicitud de crédito externa
    /// </summary>
    Task NotifyAdvisorCreditExternalAssignmentAsync(
        int assignmentId,
        int creditRequestId,
        string clientNombre,
        string clientApellidos,
        string clientEmail,
        string clientTelefono,
        string productoCredito,
        string advisorEmail,
        string advisorName,
        string assignedByName,
        string? notes);

    /// <summary>
    /// Envía el código de verificación de doble factor al correo del usuario
    /// </summary>
    Task SendTwoFactorCodeAsync(string userEmail, string fullName, string code);
}
