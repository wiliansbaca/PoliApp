namespace PoliApp.Domain;

/// <summary>Error de regla de negocio (se devuelve al cliente como 409/400).</summary>
public class DomainException(string mensaje) : Exception(mensaje);
