namespace TitoRegistry.Domain.Enums;

/// <summary>
/// Tipo de ajuste financiero aplicado a la liquidación del chofer:
/// Ingreso/Incentivo (suma al pago neto) o Deducción (resta del pago neto).
/// </summary>
public enum BenefitType
{
    /// <summary>
    /// Beneficio, incentivo, bono o viático que suma a la liquidación.
    /// </summary>
    Incentive = 1,

    /// <summary>
    /// Deducción, descuento o cobro que resta de la liquidación.
    /// </summary>
    Deduction = 2
}
